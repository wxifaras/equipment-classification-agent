using equipment_classification_agent_api.Models;
using Microsoft.Extensions.Options;

namespace equipment_classification_agent_api.Services;

public class AzureStorageService
{
    private readonly string _storageConnectionString;
    private readonly ILogger<AzureStorageOptions> _logger;

    public AzureStorageService(
        IOptions<AzureStorageOptions> options,
        ILogger<AzureStorageOptions> logger)
    {
        _storageConnectionString = options.Value.StorageConnectionString;
        _logger = logger;
    }
}
