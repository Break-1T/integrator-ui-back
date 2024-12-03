using integrator_ui_back.Classes;
using integrator_ui_back.Constants;
using integrator_ui_back.Interfaces;
using integrator_ui_back.Services;
using k8s;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using Yarp.ReverseProxy.Configuration;

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

        services.AddSingleton<IProxyConfigProvider, ReverseProxyConfigProvider>();
        services.AddReverseProxy();

        services.AddSingleton<DeploymentManager>();
        services.AddHostedService<DeploymentScannerHostedService>();

        services.AddHttpClient();

        services.AddAuthorization(config =>
        {
            // add policy
            //config.AddPolicy(UserRole.AdministerPolicy,
            //    p => p.RequireAssertion(context => CheckAdministerPolicy(context.User)));

            //config.AddPolicy(UserRole.AdministerPolicyUser,
            //    p => p.RequireAssertion(context => CheckAdministerPolicyUser(context.User)));

            //private static bool CheckAdministerPolicy(ClaimsPrincipal claimsPrincipal)
            //{
            //    var clientId = Environment.GetEnvironmentVariable(AuthConstants.EnvironmentNameEnvVariable);

            //    return claimsPrincipal.HasClaim(c => c.Type == AuthConstants.SvcAccessClaim && c.Value == clientId) &&
            //           claimsPrincipal.HasClaim(c =>
            //               c.Type == AuthConstants.RoleClaim && c.Value is UserRole.Admin or UserRole.Developer);
            //}

            //private static bool CheckAdministerPolicyUser(ClaimsPrincipal claimsPrincipal)
            //{
            //    var clientId = Environment.GetEnvironmentVariable(AuthConstants.EnvironmentNameEnvVariable);

            //    return claimsPrincipal.HasClaim(c => c.Type == AuthConstants.SvcAccessClaim && c.Value == clientId) &&
            //           claimsPrincipal.HasClaim(c =>
            //               c.Type == AuthConstants.RoleClaim &&
            //               c.Value is UserRole.Admin or UserRole.User or UserRole.Developer);
            //}
        });

        services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Authority = Environment.GetEnvironmentVariable(AuthConstants.IdentityServerUrlEnvVariable);
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                };

                // only for development
                options.RequireHttpsMetadata = false;
            });
        

        services.AddCors();
        services.AddHealthChecks();
        services.AddRouting(options => options.LowercaseUrls = true);
        services.AddMvcCore()
            .AddDataAnnotations()
            .AddApiExplorer()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(context.ModelState);
            });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Name = "Authorization",
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Please insert JWT token into field"
            });

            options.AddSecurityRequirement(
                new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] { }
                    }
                }
            );
        });
        services.AddControllers();

        return services;
    }

}
