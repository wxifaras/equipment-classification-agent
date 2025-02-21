using Microsoft.Extensions.Caching.Memory;

namespace equipment_classification_agent_api.Services;

public interface ICacheService
{
    Task<List<string>> GetManufacturers();
}

public class CacheService : ICacheService
{
    private IMemoryCache _memoryCache;
    private readonly ILogger<CacheService> _logger;
    private readonly IAzureSQLService _azureSQLService;
    
    public CacheService(
        IMemoryCache memoryCache, 
        ILogger<CacheService> logger, 
        IAzureSQLService azureSQLService)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _azureSQLService = azureSQLService;
    }

    public async Task<List<string>> GetManufacturers()
    {
        _logger.LogInformation("Getting manufacturers from cache");
        return await _memoryCache.GetOrCreateAsync("Manufacturers", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(120);
            return await _azureSQLService.GetManufacturers();
        });
    }
}
