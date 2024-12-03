using IdentityModel.Client;
using integrator_ui_back.Constants;
using integrator_ui_back.Models.RequestModels;
using Microsoft.AspNetCore.Mvc;

namespace integrator_ui_back.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    /// <summary>
    /// Gets the access token.
    /// </summary>
    /// <param name="clientCredentials">The client credentials.</param>
    [HttpPost]
    [Route("get-access-token")] // Access from UI
    public async Task<IActionResult> GetAccessTokenAsync([FromBody] GetAccessTokenRequest request)
    {
        TokenResponse result = null;
        using (var client = new HttpClient())
        {
            result = await client.RequestPasswordTokenAsync(new PasswordTokenRequest
            {
                Address = $"{Environment.GetEnvironmentVariable(AuthConstants.IdentityServerUrlEnvVariable)}/connect/token",
                ClientId = Environment.GetEnvironmentVariable(AuthConstants.IdentityServerClientIdEnvVariable),
                ClientSecret = Environment.GetEnvironmentVariable(AuthConstants.IdentityServerClientSecretEnvVariable),
                UserName = request.UserName,
                Password = request.Password,
                Scope = "offline_access",
                GrantType = "password"
            });
        }

        return result.IsError
            ? this.BadRequest(result.Raw)
            : this.Ok(result.Raw);
    }

    /// <summary>
    /// Gets the access by refresh token.
    /// </summary>
    /// <param name="refreshTokenInfo">The refresh token information.</param>
    [HttpPost, Route("refresh-token")]
    public async Task<IActionResult> GetAccessByRefreshTokenAsync([FromQuery(Name ="refreshToken")] string refreshToken)
    {
        TokenResponse result = null;
        using (var client = new HttpClient())
        {
            result = await client.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = $"{Environment.GetEnvironmentVariable(AuthConstants.IdentityServerUrlEnvVariable)}/connect/token",
                ClientId = Environment.GetEnvironmentVariable(AuthConstants.IdentityServerClientIdEnvVariable),
                ClientSecret = Environment.GetEnvironmentVariable(AuthConstants.IdentityServerClientSecretEnvVariable),
                GrantType = "refresh_token",
                RefreshToken = refreshToken
            });
        }

        return result.IsError
            ? this.BadRequest(result.Raw)
            : this.Ok(result.Raw);
    }
}
