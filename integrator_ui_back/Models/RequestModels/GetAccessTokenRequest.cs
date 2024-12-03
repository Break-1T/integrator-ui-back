using System.Security;
using System.Text.Json.Serialization;

namespace integrator_ui_back.Models.RequestModels;

/// <summary>
/// GetAccessTokenRequest
/// </summary>
public class GetAccessTokenRequest
{
    /// <summary>
    /// Gets or sets the name of the user.
    /// </summary>
    [JsonPropertyName("userName")]
    public string UserName { get; set; }

    /// <summary>
    /// Gets or sets the password.
    /// </summary>
    [JsonPropertyName("password")]
    public string Password { get; set; }
}
