using Asp.Versioning;
using equipment_classification_agent_api.Models;
using equipment_classification_agent_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace equipment_classification_agent_api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{v:apiVersion}/[controller]")]
public class EquipmentClassificationController : ControllerBase
{
    private readonly ILogger<EquipmentClassificationController> _logger;
    private readonly AzureStorageService _azureStorageService;
    private readonly IAzureOpenAIService _azureOpenAIService;
    private readonly IAzureAISearchService _azureAISearchService;

    public EquipmentClassificationController(
        ILogger<EquipmentClassificationController> logger,
        AzureStorageService azureStorageService,
        IAzureOpenAIService azureOpenAIService,
        IAzureAISearchService azureAISearchService)
    {
        _logger = logger;
        _azureStorageService = azureStorageService;
        _azureOpenAIService = azureOpenAIService;
        _azureAISearchService = azureAISearchService;
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

            Guid sessionId = request.SessionId == Guid.Empty ? Guid.NewGuid() : request.SessionId;

            foreach (var image in request.Images)
            {
               _logger.LogInformation($"Uploading image. {image.FileName}");
               await _azureStorageService.UploadImageAsync(image.OpenReadStream(), image.FileName, sessionId.ToString());
            }

            var queryTuple = await _azureOpenAIService.ExtractImageDetailsAsync(request);
            _logger.LogInformation($"NLP Query: {queryTuple.nlpQuery} Filter: {queryTuple.filter}");

            var response = new EquipmentClassificationResponse();
            
            response.AzureAISearchQueryResults = await _azureAISearchService.SearchGolfBallAsync(queryTuple.nlpQuery,filter: queryTuple.filter);
            response.SessionId = sessionId;
            response.NLPQuery = queryTuple.nlpQuery;
            response.AISearchFilter = queryTuple.filter;
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}