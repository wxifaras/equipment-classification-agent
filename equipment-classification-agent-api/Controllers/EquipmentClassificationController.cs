using Asp.Versioning;
using equipment_classification_agent_api.Models;
using equipment_classification_agent_api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace equipment_classification_agent_api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{v:apiVersion}/[controller]")]
public class EquipmentClassificationController : ControllerBase
{
    private readonly ILogger<EquipmentClassificationController> _logger;
    private readonly AzureStorageService _azureStorageService;
    private readonly IAzureOpenAIService _azureOpenAIService;
    private readonly IMemoryCache _memoryCache;
    private readonly IAzureSQLService _azureSQLService;

    public EquipmentClassificationController(
        ILogger<EquipmentClassificationController> logger,
        AzureStorageService azureStorageService,
        IAzureOpenAIService azureOpenAIService,
        IMemoryCache memoryCache,
        IAzureSQLService azureSQLService)
    {
        _logger = logger;
        _azureStorageService = azureStorageService;
        _azureOpenAIService = azureOpenAIService;
        _memoryCache = memoryCache;
        _azureSQLService = azureSQLService;
    }

    [MapToApiVersion("1.0")]
    [HttpPost("images")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Upload([FromForm] EquipmentClassificationRequest request)
    {
        try
        {
            if (request.Images.Count == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var sessionId = string.Empty;

            if (string.IsNullOrEmpty(request.SessionId))
            {
                sessionId = Guid.NewGuid().ToString();
            }
            else
            {
                sessionId = request.SessionId;
            }

            foreach (var image in request.Images)
            {
               _logger.LogInformation($"Uploading image. {image.FileName}");
               await _azureStorageService.UploadImageAsync(image.OpenReadStream(), image.FileName, sessionId);
            }

            await CacheManufacturers();

            var response= await _azureOpenAIService.ExtractImageDetailsAsync(request);

            return Ok(new
            {
                sessionId = response.SessionId,
                azureAISearchQuery = response.AzureAISearchQuery
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    private async Task CacheManufacturers()
    {
        if (!_memoryCache.TryGetValue("Manufacturers", out List<string> manufacturers))
        {
            manufacturers = await _azureSQLService.GetManufacturers();
            _memoryCache.Set("Manufacturers", manufacturers, TimeSpan.FromMinutes(120));
        }
    }
}