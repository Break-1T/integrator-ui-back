using integrator_ui_back.Constants;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Primitives;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace integrator_ui_back.Classes;

public class ReverseProxyConfigProvider : IProxyConfigProvider
{
    private const string RouteEtlIdTemplate = "{0}-etl-route";
    private const string ClusterIdTemplate = "{0}-cluster";
    private const string CatchAll = "{**catch-all}";

    private readonly IKubernetes _kubernetes;
    private CustomMemoryConfig _config;

    public ReverseProxyConfigProvider(IKubernetes kubernetes)
    {
        this._kubernetes = kubernetes;

        this.Init();
    }

    public IProxyConfig GetConfig() => this._config;

    /// <summary>
    /// By calling this method from the source we can dynamically adjust the proxy configuration.
    /// Since our provider is registered in DI mechanism it can be injected via constructors anywhere.
    /// </summary>
    public void Update(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
    {
        var oldConfig = this._config;
        this._config = new CustomMemoryConfig(routes, clusters);
        oldConfig.SignalChange();
    }

    public void Init()
    {
        var yarpIntegratorHost = Environment.GetEnvironmentVariable(ProxyConstants.YARP_INTEGRATOR_HOST);

        var oldConfig = this._config;

        var deploymentList = this._kubernetes.AppsV1.ListNamespacedDeployment(DeploymentConstants.IntegrationNamespace);

        var routes = new List<RouteConfig>();
        var clusters = new List<ClusterConfig>();

        var order = 0;

        foreach (var deployment in deploymentList)
        {
            var routeTemplate = $"{deployment.Name()}/{CatchAll}";

            var routeConfig = new RouteConfig
            {
                RouteId = string.Format(RouteEtlIdTemplate, deployment.Name()),
                ClusterId = string.Format(ClusterIdTemplate, deployment.Name()),
                Match = new RouteMatch
                {
                    Path = $"{ProxyConstants.ProxyPath}/{routeTemplate}",
                    
                },
                Order = order++,
            }
            .WithTransformPathRemovePrefix(prefix: $"{ProxyConstants.ProxyPath}/{deployment.Name()}")
            .WithTransformRequestHeader("X-Route", $"{ProxyConstants.ProxyPath}/{deployment.Name()}")
            .WithTransformXForwarded()
            .WithTransformCopyRequestHeaders()
            .WithTransformCopyResponseHeaders();

            routes.Add(routeConfig);

            var clusterConfig = new ClusterConfig
            {
                ClusterId = string.Format(ClusterIdTemplate, deployment.Name()),
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    {
                        $"{deployment.Name()}", new DestinationConfig 
                        { 
                            Address = string.IsNullOrEmpty(yarpIntegratorHost)
                                ? $"http://{deployment.Name()}.{deployment.Namespace()}.svc.cluster.local."
                                : $"{yarpIntegratorHost}/{deployment.Name()}.{deployment.Namespace()}",
                        }
                    }
                },
                SessionAffinity = new SessionAffinityConfig()
                {
                    Enabled = true,
                    Policy = "Cookie",
                    FailurePolicy = "Redistribute",
                    AffinityKeyName = "ProxySessionAffinity",
                    Cookie = new()
                    {
                        SameSite = SameSiteMode.None,
                        SecurePolicy = CookieSecurePolicy.Always,
                        HttpOnly = true,
                        IsEssential = true
                    }
                }
            };

            clusters.Add(clusterConfig);
        }

        this._config = new CustomMemoryConfig(routes, clusters);

        oldConfig?.SignalChange();
    }

    private class CustomMemoryConfig : IProxyConfig
    {
        private readonly CancellationTokenSource _cts = new();

        public CustomMemoryConfig(IReadOnlyList<RouteConfig> routes, IReadOnlyList<ClusterConfig> clusters)
        {
            this.Routes = routes;
            this.Clusters = clusters;
            this.ChangeToken = new CancellationChangeToken(this._cts.Token);
        }

        public IReadOnlyList<RouteConfig> Routes { get; }

        public IReadOnlyList<ClusterConfig> Clusters { get; }

        public IChangeToken ChangeToken { get; }

        internal void SignalChange()
        {
            this._cts.Cancel();
        }
    }
}