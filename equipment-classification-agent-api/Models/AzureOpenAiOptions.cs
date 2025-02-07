using System.ComponentModel.DataAnnotations;

namespace equipment_classification_agent_api.Models;

public class AzureOpenAIOptions
{
    public const string AzureOpenAI = "AzureOpenAIOptions";

    [Required]
    public string DeploymentName { get; set; }
    [Required]
    public string EndPoint { get; set; }
    [Required]
    public string ApiKey { get; set; }
    [Required]
    public string AzureOpenAIEmbeddingModel { get; set; }
    [Required]
    public string AzureOpenAIEmbeddingDeployment { get; set; }
    [Required]
    public string AzureOpenAIEmbeddingDimensions { get; set; }
}