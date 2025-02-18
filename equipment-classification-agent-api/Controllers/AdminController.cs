using Asp.Versioning;
using equipment_classification_agent_api.Services;
using Azure.Search.Documents.Indexes.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace equipment_classification_agent_api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{v:apiVersion}/[controller]")]
public class AdminController : ControllerBase
{
    private readonly ILogger<AdminController> _logger;
    private readonly IAzureAISearchService _azureAISearchService;

    public AdminController(
        ILogger<AdminController> logger, 
        IAzureAISearchService azureAISearchService)
    {
        _logger = logger;
        _azureAISearchService = azureAISearchService;
    }

    [MapToApiVersion("1.0")]
    [HttpPost("indexing")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAISearchIndex()
    {
        try
        {
            if (ModelState.IsValid == false)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Creating AI search index");

            SearchIndex searchIndex = await _azureAISearchService.CreateAISearchIndexAsync();
            
            _logger.LogInformation("AI search index created");

            _logger.LogInformation("Indexing data");

            await _azureAISearchService.IndexDataAsync(searchIndex);

            _logger.LogInformation("Data indexed");

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, string.Empty);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}