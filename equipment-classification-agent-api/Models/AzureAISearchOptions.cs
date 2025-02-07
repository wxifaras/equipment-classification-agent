using System.ComponentModel.DataAnnotations;

namespace equipment_classification_agent_api.Models;

public class AzureAISearchOptions
{
    public const string AzureAISearch = "AzureAISearchOptions";

    [Required]
    public string IndexName { get; set; }

    [Required]
    public string SearchServiceEndpoint { get; set; }

    [Required]
    public string SearchAdminKey { get; set; }
}