using System.ComponentModel.DataAnnotations;

namespace equipment_classification_agent_api.Models;

public class AzureOpenAIOptions
{
    public const string AzureOpenAI = "AzureOpenAIOptions";

    [Required]
    public string AzureOpenAIDeploymentName { get; set; }

    [Required]
    public string AzureOpenAIEndPoint { get; set; }

    [Required]
    public string AzureOpenAIKey { get; set; }

    [Required]
    public string AzureOpenAIEmbeddingModel { get; set; }

    [Required]
    public string AzureOpenAIEmbeddingDeployment { get; set; }

    [Required]
    public string AzureOpenAIEmbeddingDimensions { get; set; }

    [Required]
    public bool EnableChatHistory { get; set; }
}