
using integrator_ui_back.Classes;
using integrator_ui_back.Constants;
using k8s;
using k8s.Models;
using System.Text;
using System.Text.Json;

namespace integrator_ui_back.Services;

public class DeploymentScannerHostedService : BackgroundService
{
    private readonly ILogger<DeploymentScannerHostedService> _logger;
    private readonly IKubernetes _kubernetesClient;
    private readonly DeploymentManager _deploymentManager;

    public DeploymentScannerHostedService(
        ILogger<DeploymentScannerHostedService> logger, 
        IKubernetes kubernetes, 
        DeploymentManager deploymentManager)
    {
        this._logger = logger;
        this._kubernetesClient = kubernetes;
        this._deploymentManager = deploymentManager;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this._logger.LogInformation("DeploymentScanner Background Service is starting");

        var dwConnected = false;
        var pwConnected = false;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!dwConnected)
                {
                    var deploymentMessages = this._kubernetesClient.AppsV1.ListNamespacedDeploymentWithHttpMessagesAsync(
                        namespaceParameter: DeploymentConstants.IntegrationNamespace,
                        watch: true, cancellationToken: stoppingToken);

                    deploymentMessages.Watch<V1Deployment, V1DeploymentList>(this.OnDeploymentEvent,
                        (Exception exception) => this._logger.LogCritical(exception, "Error occured during k8s deployment watch"),
                        delegate { dwConnected = false; }
                    );
                    dwConnected = true;
                }

                if (pwConnected)
                    continue;

                var podMessages = this._kubernetesClient.CoreV1.ListNamespacedPodWithHttpMessagesAsync(
                    namespaceParameter: DeploymentConstants.IntegrationNamespace,
                    watch: true, cancellationToken: stoppingToken);

                podMessages.Watch<V1Pod, V1PodList>(this.OnPodEvent,
                    exception => this._logger.LogCritical(exception, "Error occured during k8s pod watch"),
                    delegate { pwConnected = false; }
                );

                pwConnected = true;
            }
            finally
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private void OnDeploymentEvent(WatchEventType deploymentEventType, V1Deployment deployment)
    {
        //if (deployment.GetLabel("part") == null || deployment.GetLabel("part").StartsWith("ui"))
        //{
        //    return;
        //}

        var deploymentNamespace = deployment.Namespace();
        var deploymentName = deployment.Name();
        var workerUrl = $"http://{deploymentName}.{deploymentNamespace}.svc.cluster.local.";

        if (deploymentEventType is WatchEventType.Added or WatchEventType.Modified)
        {
            DeploymentInformation deploymentInformation = null;
            lock (this._deploymentManager)
            {
                deploymentInformation = this._deploymentManager.DeploymentInformationList.Find(e =>
                    e.DeploymentName == deploymentName);
            }

            if (deploymentInformation == null)
            {
                deploymentInformation = new DeploymentInformation
                {
                    Id = deployment.Metadata.Uid,
                    ContainerInformationList = [],
                    WorkerUrl = workerUrl,
                    DeploymentName = deploymentName,
                    Namespace = deploymentNamespace,
                    PodInformationList = [],
                };

                var labels = deployment.Spec.Selector.MatchLabels
                    .Select(label => label.Key + "=" + label.Value)
                    .ToList();

                var podList = this._kubernetesClient.CoreV1.ListNamespacedPod(
                    namespaceParameter: deploymentNamespace,
                    labelSelector: string.Join(",", labels));

                lock (this._deploymentManager)
                {
                    this._deploymentManager.DeploymentInformationList.Add(deploymentInformation);
                }

                //this._signalRNotificator.SendEntity(deploymentInformation, EntityState.Added);

                foreach (var pod in podList.Items)
                {
                    this.OnPodEvent(WatchEventType.Added, pod);
                }
            }

            foreach (var container in deployment.Spec.Template.Spec.Containers)
            {
                var containerInformation = deploymentInformation.ContainerInformationList.FirstOrDefault(e => e.Name == container.Name);

                if (containerInformation == null)
                {
                    containerInformation = new ContainerInformation { Name = container.Name };
                    deploymentInformation.ContainerInformationList.Add(containerInformation);
                }

                containerInformation.MemoryLimit = container.Resources?.Limits?["memory"]?.CanonicalizeString();
                containerInformation.CpuLimit = container.Resources?.Limits?["cpu"]?.CanonicalizeString();
                containerInformation.MemoryRequest = container.Resources?.Requests?["memory"]?.CanonicalizeString();
                containerInformation.CpuRequest = container.Resources?.Requests?["cpu"]?.CanonicalizeString();
                containerInformation.ImagePath = container.Image;
            }

            //this._signalRNotificator.SendEntity(deploymentInformation, EntityState.Modified);

            this._logger.LogDebug(
                "Event Type: {EventType},\r\nNamespace:{Namespace}.\r\nName: {MetadataName}\r\nStatus: {StatusReadyReplicas}",
                deploymentEventType,
                deployment.Metadata.Namespace(),
                deployment.Metadata.Name,
                deployment.Status.ReadyReplicas
            );
        }
        else
        {
            lock (this._deploymentManager)
            {
                var deploymentInfo = this._deploymentManager.DeploymentInformationList.FirstOrDefault(e => e.DeploymentName == deploymentName);

                if (deploymentInfo == null)
                {
                    return;
                }

                this._deploymentManager.DeploymentInformationList.Remove(deploymentInfo);

                //this._signalRNotificator.SendEntity(deploymentInfo, EntityState.Deleted);
            }
        }
    }

    private void OnPodEvent(WatchEventType podEventType, V1Pod pod)
    {
        lock (this._deploymentManager)
        {
            var workerName = pod.GetLabel("worker-name");

            var deploymentInformation = this._deploymentManager.DeploymentInformationList.FirstOrDefault(e => e.DeploymentName == workerName);

            if (deploymentInformation == null)
            {
                return;
            }

            var pi = deploymentInformation.PodInformationList.FirstOrDefault(e => e.Name == pod.Name());

            switch (podEventType)
            {
                case WatchEventType.Added:
                case WatchEventType.Modified:
                    if (pi != null)
                    {
                        deploymentInformation.PodInformationList.Remove(pi);
                    }

                    if (!pod.DeletionTimestamp().HasValue)
                    {
                        var currentErrors = pod.Status?.ContainerStatuses
                            ?.Select(e => e.State)
                            ?.Select(e => e.Waiting)
                            ?.Where(e => e != null)
                            ?.Select(e => $"{e.Reason} : {e.Message}");
                        var lastErrors = pod.Status?.ContainerStatuses
                            ?.Select(e => e.LastState)
                            ?.Select(e => e.Terminated)
                            ?.Where(e => e != null)
                            ?.Select(e => $"{e.Reason} : {e.Message}");

                        deploymentInformation.PodInformationList.Add(new PodInformation()
                        {
                            Name = pod.Name(),
                            IsRunning = pod.Status?.ContainerStatuses?.All(e => e.Ready) == true,
                            ContainerStatus = JsonSerializer.Serialize(pod.Status?.ContainerStatuses?.Select(e => e.State)),
                            LastStartTime = pod.Status?.StartTime,
                            RestartCount = pod.Status?.ContainerStatuses?.Sum(e => e.RestartCount) ?? 0,
                        });
                    }

                    //this._signalRNotificator.SendEntity(deploymentInformation, EntityState.Modified);

                    break;

                case WatchEventType.Deleted:
                    if (pi != null)
                    {
                        deploymentInformation.PodInformationList.Remove(pi);
                    }

                    //this._signalRNotificator.SendEntity(deploymentInformation, EntityState.Modified);
                    break;
                case WatchEventType.Error:
                case WatchEventType.Bookmark:
                    break;

                default:
                    break;
            }
        }
    }
}
