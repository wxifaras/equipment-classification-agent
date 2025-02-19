using equipment_classification_agent_api.Models;
using Microsoft.Extensions.Options;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace equipment_classification_agent_api.Services;

public class AzureStorageService
{
    private readonly string _storageConnectionString;
    private readonly ILogger<AzureStorageService> _logger;
    private readonly string _containerName;
    private readonly BlobServiceClient _blobServiceClient;

    public AzureStorageService(
        IOptions<AzureStorageOptions> options,
        ILogger<AzureStorageService> logger)
    {
        _storageConnectionString = options.Value.StorageConnectionString;
        _blobServiceClient = new BlobServiceClient(_storageConnectionString);
        _containerName= options.Value.ImagesContainer;
        _logger = logger;
    }

    public async Task UploadImageAsync(Stream imageStream, string fileName,String sesssionId)
    {
        string folderPath = $"{sesssionId}/{fileName}";
        var blobContainer = _blobServiceClient.GetBlobContainerClient(_containerName);
        var blobClient = blobContainer.GetBlobClient(folderPath);

        _logger.LogInformation($"Uploading image. {folderPath}");
        await blobClient.UploadAsync(imageStream, overwrite: true);
    }

    public async Task<string> GenerateSasUriAsync(string fileName)
    {
        Uri? sasUri = null;
        BlobClient blobClient = new BlobClient(_storageConnectionString, _containerName, fileName);

        if (blobClient.Exists())
        {
            BlobSasBuilder sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = _containerName,
                BlobName = fileName,
                Resource = "b",
            };

            sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddDays(1);
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            sasUri = blobClient.GenerateSasUri(sasBuilder);
        }

        return sasUri.ToString();
    }
}
