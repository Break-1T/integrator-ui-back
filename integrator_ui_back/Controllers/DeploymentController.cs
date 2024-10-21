using integrator_ui_back.Interfaces;
using integrator_ui_back.Models.RequestModels;
using Microsoft.AspNetCore.Mvc;

namespace integrator_ui_back.Controllers;

[ApiController]
[Route("[controller]")]
public class DeploymentController(IDeploymentService deploymentService) : ControllerBase
{
    private readonly IDeploymentService _deploymentService = deploymentService;

    /// <summary>
    /// Creates the integration.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>IActionResult.</returns>
    [HttpPost("create-integration")]
    public async Task<IActionResult> CreateIntegration(
        [FromBody] CreateIntegrationRequest request,
        CancellationToken cancellationToken = default)
    {
        var createIntegrationResult = await this._deploymentService.CreateIntegrationAsync(request.IntegrationName, request.ImageUrl, 
            request.ImageVersion, request.MemoryRequest, request.MemoryLimit, request.Port, request.WorkerSettings, cancellationToken);

        if (!createIntegrationResult.IsSuccess)
        {
            return this.BadRequest(createIntegrationResult.ErrorMessage);
        }

        return this.Ok();
    }

    [HttpGet("get-deployment-names")]
    public async Task<IActionResult> GetDeploymentNames()
    {
        var getDeploymentNamesResult = await this._deploymentService.GetDeploymentNamesAsync();

        if (!getDeploymentNamesResult.IsSuccess)
        {
            return this.BadRequest(getDeploymentNamesResult.ErrorMessage);
        }

        return this.Ok(getDeploymentNamesResult.Result);
    }

    [HttpGet("get-all-deployment-information")]
    public IActionResult GetAllDeploymentInformation()
    {
        var getDeploymentInformationResult = this._deploymentService.GetAllDeploymentInformation();

        if (!getDeploymentInformationResult.IsSuccess)
        {
            return this.BadRequest(getDeploymentInformationResult.ErrorMessage);
        }

        return this.Ok(getDeploymentInformationResult.Result);
    }

    [HttpGet("get-deployment-information/{deploymentName}")]
    public IActionResult GetDeploymentInformation([FromRoute(Name = "deploymentName")] string deploymentName)
    {
        var getDeploymentInformationResult = this._deploymentService.GetDeploymentInformation(deploymentName);

        if (!getDeploymentInformationResult.IsSuccess)
        {
            return this.BadRequest(getDeploymentInformationResult.ErrorMessage);
        }

        return this.Ok(getDeploymentInformationResult.Result);
    }
}
