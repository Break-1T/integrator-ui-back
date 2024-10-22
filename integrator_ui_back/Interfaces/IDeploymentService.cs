using integrator_ui_back.Classes;
using integrator_ui_back.Models;
using Teamwork.Integrator.Core.Classes;

namespace integrator_ui_back.Interfaces;

public interface IDeploymentService
{
    /// <summary>
    /// Creates the integration asynchronous.
    /// </summary>
    /// <param name="integrationName">Name of the integration.</param>
    /// <param name="imageUrl">Url of the image.</param>
    /// <param name="memoryRequest">The memory request.</param>
    /// <param name="memoryLimit">The memory limit.</param>
    /// <param name="port">The port.</param>
    /// <param name="workerSettings">The worker settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>ServiceResult.</returns>
    Task<ServiceResult> CreateIntegrationAsync(string integrationName, string imageUrl, 
        string memoryRequest, string memoryLimit, int port, WorkerSettings workerSettings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the deployment names asynchronous.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>ServiceResult.</returns>
    Task<ServiceResult<string[]>> GetDeploymentNamesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the deployment information.
    /// </summary>
    /// <param name="deploymentName">Name of the deployment.</param>
    /// <returns>ServiceResult.</returns>
    ServiceResult<DeploymentInformation> GetDeploymentInformation(string deploymentName);

    /// <summary>
    /// Gets all deployment information.
    /// </summary>
    /// <returns>ServiceResult</returns>
    ServiceResult<List<DeploymentInformation>> GetAllDeploymentInformation();

}
