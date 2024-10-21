using System.Text.Json.Serialization;

namespace integrator_ui_back.Models;

/// <summary>
/// WorkerSettings.
/// </summary>
public class WorkerSettings
{
    /// <summary>
    /// Gets or sets the configuration.
    /// </summary>
    [JsonPropertyName("config")]
    public List<WorkerSetting> Config { get; set; }

    /// <summary>
    /// Gets or sets the secret.
    /// </summary>
    [JsonPropertyName("secret")]
    public List<WorkerSetting> Secret { get; set; }

}
