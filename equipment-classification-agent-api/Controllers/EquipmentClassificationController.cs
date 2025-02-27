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

            var sessionId = request.SessionId == Guid.Empty ? Guid.NewGuid() : request.SessionId;

            foreach (var image in request.Images)
            {
                _logger.LogInformation($"Uploading image. {image.FileName}");
                await _azureStorageService.UploadImageAsync(image.OpenReadStream(), image.FileName, sessionId.ToString());
            }

            var response = await ExtractAndClassifyImagesAsync(request, sessionId);

            if (response.AzureAISearchQueryResults.Count == 0)
            {
                // 2nd pass
                response = await ExtractAndClassifyImagesAsync(request, sessionId);
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    private async Task<EquipmentClassificationResponse> ExtractAndClassifyImagesAsync(EquipmentClassificationRequest request, Guid sessionId)
    {
        // generate SAS URLs that will be sent to the LLM for data extraction
        var fileName = string.Empty;
        var imageUrlList = new List<string>();

        foreach (var image in request.Images)
        {
            fileName = $"{request.SessionId}/{image.FileName}";
            var imageUrl = await _azureStorageService.GenerateSasUriAsync(fileName);
            imageUrlList.Add(imageUrl);
        }

        // to ensure that the markings are extracted properly, we are going to have the LLM evaluate this three times,
        // then we will take the list of json results from each and have another LLM evaluate the three results against
        // the images to construct a final json extraction result
        var golfBallDetailsList = new List<GolfBallLLMDetail>();
        for (int i = 0; i < 3; i++)
        {
            var golfBallDetails = await _azureOpenAIService.ExtractImageDetailsAsync(imageUrlList, null, sessionId);
            golfBallDetailsList.Add(golfBallDetails);
        }

        // get the final golf ball details by using the LLM to evaluate the three results against the images
        var finalGolfBallDetails = await _azureOpenAIService.ExtractImageDetailsAsync(imageUrlList, golfBallDetailsList, sessionId);

        var queryTuple = await _azureOpenAIService.GenerateNLQueryAsync(finalGolfBallDetails, sessionId);

        _logger.LogInformation($"NLP Query: {queryTuple.nlpQuery} Filter: {queryTuple.filter}");

        var response = new EquipmentClassificationResponse();

        response.AzureAISearchQueryResults = await _azureAISearchService.SearchGolfBallAsync(queryTuple.nlpQuery, filter: queryTuple.filter);

        response.SessionId = sessionId;
        response.NLPQuery = queryTuple.nlpQuery;
        response.AISearchFilter = queryTuple.filter;
        return response;
    }
}