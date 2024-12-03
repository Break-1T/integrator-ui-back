using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace integrator_ui_back.Models.RequestModels;

/// <summary>
/// CreateIntegrationRequest.
/// </summary>
public class CreateIntegrationRequest
{
    /// <summary>
    /// Gets or sets the name of the integration.
    /// </summary>
    [Required]
    [JsonPropertyName("integrationName")]
    public string IntegrationName { get; set; }

    /// <summary>
    /// Gets or sets the image URL.
    /// </summary>
    [Required]
    [JsonPropertyName("imageUrl")]
    public string ImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the memory request.
    /// </summary>
    [Required]
    [JsonPropertyName("memoryRequest")]
    public string MemoryRequest { get; set; }

    /// <summary>
    /// Gets or sets the memory limit.
    /// </summary>
    [Required]
    [JsonPropertyName("memoryLimit")]
    public string MemoryLimit { get; set; }

    /// <summary>
    /// Gets or sets the port.
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; } = 8080;
    
    /// <summary>
    /// Gets or sets the worker settings.
    /// </summary>
    [JsonPropertyName("workerSettings")]
    public WorkerSettings WorkerSettings { get; set; }
}
