using integrator_ui_back.Classes;
using integrator_ui_back.Interfaces;
using integrator_ui_back.Services;
using k8s;
using System.Text.Json;

namespace integrator_ui_back.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the integration ui.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="hostingEnvironment">The hosting environment.</param>
    /// <returns>IServiceCollection.</returns>
    public static IServiceCollection AddIntegrationUI(this IServiceCollection services,
        IConfiguration configuration, IWebHostEnvironment hostingEnvironment)
    {
        var config = KubernetesClientConfiguration.BuildDefaultConfig();
        services.AddSingleton<IKubernetes>(new Kubernetes(config));

        services.AddScoped<IDeploymentService, DeploymentService>();

        //services.AddSingleton<IProxyConfigProvider, ReverseProxyConfigProvider>();
        //services.AddReverseProxy().AddTransforms(context => context.AddRequestAuthUserHeaderTransform());

        services.AddSingleton<DeploymentManager>();
        services.AddHostedService<DeploymentScannerHostedService>();

        services.AddHttpClient();

        return services;
    }

}
