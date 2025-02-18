namespace equipment_classification_agent_api.Models
{
    public class AzureStorageOptions
    {
        public const string AzureStorage = "AzureStorageOptions";

        public string StorageConnectionString { get; set; }
        public string ImageContainer { get; set; }
    }
}