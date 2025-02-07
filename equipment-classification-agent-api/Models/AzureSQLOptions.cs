using System.ComponentModel.DataAnnotations;

namespace equipment_classification_agent_api.Models;

public class AzureSQLOptions
{
    public const string AzureSQL = "AzureSQLOptions";

    [Required]
    public string ConnectionString { get; set; }
}
