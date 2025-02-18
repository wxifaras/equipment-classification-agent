using System.ComponentModel.DataAnnotations;

namespace equipment_classification_agent_api.Models
{
    public class AzureStorageOptions
    {
        public const string AzureStorage = "AzureStorageOptions";

        [Required]
        public string StorageConnectionString { get; set; }

        [Required]
        public string ImageContainer { get; set; }
    }
}