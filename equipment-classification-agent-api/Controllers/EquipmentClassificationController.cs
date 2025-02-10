using Asp.Versioning;
using equipment_classification_agent_api.Models;
using Microsoft.AspNetCore.Mvc;

namespace equipment_classification_agent_api.Controllers;

[ApiVersion("1.0")]
[ApiController]
[Route("api/v{v:apiVersion}/[controller]")]
public class EquipmentClassificationController : ControllerBase
{
    private readonly ILogger<EquipmentClassificationController> _logger;

    public EquipmentClassificationController(
        ILogger<EquipmentClassificationController> logger)
    {
        _logger = logger;
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
            if (!request.Images.Any())
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

            var response = new EquipmentClassificationResponse();
            foreach (var image in request.Images)
            {
                _logger.LogInformation($"Uploading image. {image.FileName}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image.");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }

        return Ok();
    }
}