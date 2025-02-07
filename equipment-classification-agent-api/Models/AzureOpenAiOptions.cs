using System.ComponentModel.DataAnnotations;

namespace equipment_classification_agent_api.Models;

public class AzureOpenAiOptions
{
    public const string AzureOpenAI = "AzureOpenAiOptions";

    [Required]
    public string DeploymentName { get; set; }
    [Required]
    public string EndPoint { get; set; }
    [Required]
    public string ApiKey { get; set; }
}