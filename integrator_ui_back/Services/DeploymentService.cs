using integrator_ui_back.Classes;
using integrator_ui_back.Constants;
using integrator_ui_back.Interfaces;
using integrator_ui_back.Models;
using k8s;
using k8s.Models;
using System.Collections.Generic;
using System.Text;
using Teamwork.Integrator.Core.Classes;

namespace integrator_ui_back.Services;

public class DeploymentService(ILogger<DeploymentService> logger, IKubernetes kubernetes, DeploymentManager deploymentManager) : IDeploymentService
{
    private readonly DeploymentManager _deploymentManager = deploymentManager;
    private readonly IKubernetes _kubernetesClient = kubernetes;
    private readonly ILogger<DeploymentService> _logger = logger;

    /// <inheritdoc/>
    public async Task<ServiceResult> CreateIntegrationAsync(string integrationName, string imageUrl,
        string memoryRequest, string memoryLimit, int port, WorkerSettings workerSettings, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(integrationName))
            return ServiceResult.FromError($"{nameof(integrationName)} has null or empty value.");

        if (string.IsNullOrEmpty(imageUrl))
            return ServiceResult.FromError($"{nameof(imageUrl)} has null or empty value.");

        if (string.IsNullOrEmpty(memoryRequest))
            return ServiceResult.FromError($"{nameof(memoryRequest)} has null or empty value.");

        if (string.IsNullOrEmpty(memoryLimit))
            return ServiceResult.FromError($"{nameof(memoryLimit)} has null or empty value.");

        this._logger.LogInformation("Creating Integration Service");

        try
        {
            var uiDeployment = await this.GetDeploymentDetailsAsync(DeploymentConstants.UiBackDeploymentName, cancellationToken);

            if (uiDeployment == null)
            {
                return ServiceResult.FromError("UI deployment couldn't be retrieved. Try again later");
            }

            if (this._deploymentManager.DeploymentInformationList.Any(e => e.DeploymentName == integrationName))
            {
                return ServiceResult.FromError($"Worker with Name: {integrationName} already exists.");
            }

            if (workerSettings.Secret?.Count > 0)
            {
                var secret = this.CreateCustomSecret(integrationName, DeploymentConstants.IntegrationNamespace, workerSettings.Secret);

                var secrets = await this._kubernetesClient.CoreV1.ListNamespacedSecretAsync(DeploymentConstants.IntegrationNamespace);
                if (secrets.Items.Any(e => e.Name() == secret.Name()))
                {
                    await this._kubernetesClient.CoreV1.DeleteNamespacedSecretAsync(secret.Name(), secret.Namespace());
                }

                await this._kubernetesClient.CoreV1.CreateNamespacedSecretAsync(secret, DeploymentConstants.IntegrationNamespace);
            }

            if (workerSettings.Config?.Count > 0)
            {
                var configMap = this.CreateCustomConfigMap(integrationName, DeploymentConstants.IntegrationNamespace, workerSettings.Config);

                var configMaps = await this._kubernetesClient.CoreV1.ListNamespacedConfigMapAsync(DeploymentConstants.IntegrationNamespace);

                if (configMaps.Items.Any(e => e.Name() == configMap.Name()))
                {
                    await this._kubernetesClient.CoreV1.DeleteNamespacedConfigMapAsync(configMap.Name(), configMap.Namespace());
                }

                await this._kubernetesClient.CoreV1.CreateNamespacedConfigMapAsync(configMap, DeploymentConstants.IntegrationNamespace);
            }

            var deploymentInfo = this.CreateDeployment(integrationName, DeploymentConstants.IntegrationNamespace, uiDeployment,
                imageUrl, memoryRequest, memoryLimit, port);

            var service = CreateService(integrationName, DeploymentConstants.IntegrationNamespace, port);

            var services = await this._kubernetesClient.CoreV1.ListNamespacedServiceAsync(DeploymentConstants.IntegrationNamespace);
            if (services.Items.Any(e => e.Name() == service.Name()))
            {
                await this._kubernetesClient.CoreV1.DeleteNamespacedServiceAsync(service.Name(), service.Namespace());
            }

            await this._kubernetesClient.CoreV1.CreateNamespacedServiceAsync(service, DeploymentConstants.IntegrationNamespace);

            var deployments = await this._kubernetesClient.AppsV1.ListNamespacedDeploymentAsync(DeploymentConstants.IntegrationNamespace);
            if (deployments.Items.Any(e => e.Name() == deploymentInfo.Name()))
            {
                await this._kubernetesClient.AppsV1.DeleteNamespacedDeploymentAsync(deploymentInfo.Name(),
                    deploymentInfo.Namespace());
            }

            await this._kubernetesClient.AppsV1.CreateNamespacedDeploymentAsync(deploymentInfo, DeploymentConstants.IntegrationNamespace);

            //if (this._configProvider is ReverseProxyConfigProvider reverseProxyConfigProvider)
            //{
            //    reverseProxyConfigProvider.Init();
            //}

            //await this.UpdateSwaggerConfig(
            //    workerName: workerName,
            //    currentRequest: this.Request,
            //    integrationType: integrationType,
            //    cancellationToken: cancellationToken);
            return ServiceResult.FromSuccess();
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "Unexpected error: could not create integration service");
            return ServiceResult.FromError("Unexpected error: could not create integration service");
        }
    }

    /// <inheritdoc/>
    public ServiceResult<List<DeploymentInformation>> GetAllDeploymentInformation()
    {
        return ServiceResult<List<DeploymentInformation>>.FromSuccess(this._deploymentManager.DeploymentInformationList);
    }

    /// <inheritdoc/>
    public ServiceResult<DeploymentInformation> GetDeploymentInformation(string deploymentName)
    {
        var deployment = this._deploymentManager.DeploymentInformationList.FirstOrDefault(d => d.DeploymentName == deploymentName);
        if (deployment == null)
        {
            return ServiceResult<DeploymentInformation>.FromError($"Could not find deployment information for {deploymentName}");
        }

        return ServiceResult<DeploymentInformation>.FromSuccess(deployment);
    }

    /// <inheritdoc/>
    public async Task<ServiceResult<string[]>> GetDeploymentNamesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var deployments = await this._kubernetesClient.AppsV1.ListNamespacedDeploymentAsync(
            DeploymentConstants.IntegrationNamespace, cancellationToken: cancellationToken);

            var deploymentList = deployments.Items.Select(e => e.Name());

            return ServiceResult<string[]>.FromSuccess([.. deploymentList]);
        }
        catch (Exception e)
        {
            this._logger.LogError(e, "Unexpected error: could not get deployment names");
            return ServiceResult<string[]>.FromError("Unexpected error: could not get deployment names");
        }
    }

    /// <inheritdoc/>
    public async Task<ServiceResult> DeletePodsForDeploymentAsync(string deploymentName, CancellationToken cancellationToken = default)
    {
        try
        {
            var deployment = await this._kubernetesClient.AppsV1.ReadNamespacedDeploymentAsync(deploymentName, DeploymentConstants.IntegrationNamespace, null, cancellationToken);
            var labels = string.Join(",", deployment.Labels().Select(x=> $"{x.Key}={x.Value}"));
            var pods = await this._kubernetesClient.CoreV1.DeleteCollectionNamespacedPodAsync(DeploymentConstants.IntegrationNamespace, labelSelector: labels);

            return ServiceResult.FromSuccess();
        }
        catch (Exception e)
        {
            this._logger.LogError(e, $"Unexpected error: could not get restart deployment {deploymentName}");
            return ServiceResult.FromError($"Unexpected error: could not get restart deployment {deploymentName}");
        }
    }

    /// <summary>
    /// Creates the custom configuration map.
    /// </summary>
    /// <param name="integrationName">Name of the integration.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>A <see cref="V1ConfigMap" /> instance representing the custom configuration map.</returns>
    private V1ConfigMap CreateCustomConfigMap(string integrationName, string ns, IEnumerable<WorkerSetting> settings)
    {
        return new()
        {
            ApiVersion = "v1",
            Kind = "ConfigMap",
            Metadata = new V1ObjectMeta
            {
                Name = integrationName,
                NamespaceProperty = ns,
                Labels = new Dictionary<string, string>() { { "integrationName", integrationName } }
            },
            Data = settings.ToDictionary(setting => setting.Name, setting => setting.Value)
        };
    }

    /// <summary>
    /// Creates the custom secret.
    /// </summary>
    /// <param name="integrationName">Name of the integration.</param>
    /// <param name="ns">The ns.</param>
    /// <param name="settings">The settings.</param>
    /// <returns>A V1Secret object representing the custom secret.</returns>
    private V1Secret CreateCustomSecret(string integrationName, string ns, IEnumerable<WorkerSetting> settings)
    {
        return new()
        {
            ApiVersion = "v1",
            Kind = "Secret",
            Metadata = new V1ObjectMeta
            {
                Name = integrationName,
                NamespaceProperty = ns,
                Labels = new Dictionary<string, string>() { { "integrationName", integrationName } }
            },
            Data = settings.ToDictionary(setting => setting.Name, setting => Encoding.Default.GetBytes(setting.Value))
        };
    }

    /// <summary>
    /// Creates the deployment.
    /// </summary>
    /// <param name="integrationName">Name of the integration.</param>
    /// <param name="ns">The namespace.</param>
    /// <param name="uiDeploymentInfo">The UI deployment information.</param>
    /// <param name="imageUrl">The image URL.</param>
    /// <param name="memoryRequest">The memory request.</param>
    /// <param name="memoryLimit">The memory limit.</param>
    /// <param name="port">The port.</param>
    /// <returns></returns>
    private V1Deployment CreateDeployment(
        string integrationName,
        string ns,
        V1Deployment uiDeploymentInfo,
        string imageUrl,
        string memoryRequest,
        string memoryLimit,
        int port)
    {
        var deployment = new V1Deployment
        {
            ApiVersion = "apps/v1",
            Kind = "Deployment",
            Metadata = new V1ObjectMeta
            {
                Name = integrationName,
                NamespaceProperty = ns,
                Labels = new Dictionary<string, string> { { "integrationName", integrationName } },
            },
            Spec = new V1DeploymentSpec
            {
                Replicas = 1,
                Strategy = new V1DeploymentStrategy
                {
                    Type = "Recreate"
                },
                Selector = new V1LabelSelector
                {
                    MatchLabels = new Dictionary<string, string> { { "integrationName", integrationName } },
                },
                Template = new V1PodTemplateSpec
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = integrationName,
                        Labels = new Dictionary<string, string> { { "integrationName", integrationName } },
                    },
                    Spec = new V1PodSpec
                    {
                        DnsConfig = new V1PodDNSConfig
                        {
                            Options = new List<V1PodDNSConfigOption>
                            {
                                new V1PodDNSConfigOption
                                {
                                    Name = "ndots",
                                    Value = "1",
                                }
                            }
                        },
                        AutomountServiceAccountToken = true,
                        ServiceAccount = uiDeploymentInfo.Spec.Template.Spec.ServiceAccount,
                        ImagePullSecrets = uiDeploymentInfo.Spec.Template.Spec.ImagePullSecrets,
                        InitContainers = uiDeploymentInfo.Spec.Template.Spec.InitContainers,
                        Containers = uiDeploymentInfo.Spec.Template.Spec.Containers,
                        NodeSelector = uiDeploymentInfo.Spec.Template.Spec.NodeSelector,
                        Volumes = uiDeploymentInfo.Spec.Template.Spec.Volumes,
                    }
                }
            },
        };

        var container = deployment.Spec.Template.Spec.Containers.First();
        container.Name = "integration";

        //clear all ENV Variables defined for UI
        container.Env =
        [
            new V1EnvVar
            {
                Name = DeploymentConstants.WorkerNameEnvVariable,
                Value = integrationName,
            },
            new V1EnvVar
            {
                Name = "Instance",
                ValueFrom = new V1EnvVarSource
                {
                    SecretKeyRef = new V1SecretKeySelector
                    {
                        Name = "pgsql-secret",
                        Key = "instance",
                    }
                },
            },
            new V1EnvVar
            {
                Name = "Username",
                ValueFrom = new V1EnvVarSource
                {
                    SecretKeyRef = new V1SecretKeySelector
                    {
                        Name = "pgsql-secret",
                        Key = "username",
                    }
                },
            },
            new V1EnvVar
            {
                Name = "Password",
                ValueFrom = new V1EnvVarSource
                {
                    SecretKeyRef = new V1SecretKeySelector
                    {
                        Name = "pgsql-secret",
                        Key = "password",
                    }
                },
            }
        ];

        //clear envFrom variable. Add default envFrom.
        container.EnvFrom =
        [
            new V1EnvFromSource
            {
                SecretRef = new V1SecretEnvSource
                {
                    Name = integrationName,
                    Optional = true,
                }
            },
            new V1EnvFromSource
            {
                ConfigMapRef = new V1ConfigMapEnvSource
                {
                    Name = integrationName,
                    Optional = true
                },
            },
        ];

        // No mounts should be passed from UI Container
        container.VolumeMounts?.Clear();
        deployment.Spec.Template.Spec.Volumes?.Clear();

        container.Ports =
        [
            new V1ContainerPort
            {
                Name = "http",
                ContainerPort = port
            }
        ];

        //add default Health Check
        container.LivenessProbe = new V1Probe
        {
            InitialDelaySeconds = 60,
            PeriodSeconds = 180,
            FailureThreshold = 5,
            HttpGet = new V1HTTPGetAction
            {
                Path = "/health",
                Port = new IntstrIntOrString("http"),
            }
        };

        container.ReadinessProbe = new V1Probe
        {
            InitialDelaySeconds = 30,
            PeriodSeconds = 30,
            FailureThreshold = 5,
            SuccessThreshold = 1,
            TimeoutSeconds = 30,
            TcpSocket = new V1TCPSocketAction
            {
                Port = new IntstrIntOrString("http")
            }
        };

        container.Image = imageUrl;

        container.Resources.Limits = new Dictionary<string, ResourceQuantity>
        {
            { "memory", new ResourceQuantity(memoryLimit) },
            { "cpu", new ResourceQuantity("100m") }
        };
        container.Resources.Requests = new Dictionary<string, ResourceQuantity>
        {
            { "memory", new ResourceQuantity(memoryRequest) },
            { "cpu", new ResourceQuantity("100m") }
        };

        return deployment;
    }

    /// <summary>
    /// Creates a service for a Kubernetes cluster.
    /// </summary>
    /// <param name="integrationName">Name of the integration.</param>
    /// <param name="ns">The ns.</param>
    /// <param name="port">The port.</param>
    /// <returns>The created V1Service object.</returns>
    private V1Service CreateService(string integrationName, string ns, int port)
    {
        return new()
        {
            ApiVersion = "v1",
            Kind = "Service",
            Metadata = new V1ObjectMeta
            {
                Name = integrationName,
                NamespaceProperty = ns,
                Labels = new Dictionary<string, string>() { { "integrationName", integrationName } }
            },
            Spec = new V1ServiceSpec
            {
                Type = "ClusterIP",
                Selector = new Dictionary<string, string>() { { "integrationName", integrationName } },
                Ports =
                [
                    new()
                    {
                        Name = "http",
                        Port = 80,
                        TargetPort = port,
                        Protocol = "TCP"
                    }
                ],
            },
        };
    }
    
    /// <summary>
    /// Gets the deployment details asynchronous.
    /// </summary>
    /// <param name="deploymentName">Name of the deployment.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>V1Deployment.</returns>
    private async Task<V1Deployment> GetDeploymentDetailsAsync(string deploymentName, CancellationToken cancellationToken = default)
    {
        return await this._kubernetesClient.AppsV1.ReadNamespacedDeploymentAsync(
                namespaceParameter: DeploymentConstants.IntegrationNamespace,
                name: deploymentName,
                cancellationToken: cancellationToken);
    }
}
