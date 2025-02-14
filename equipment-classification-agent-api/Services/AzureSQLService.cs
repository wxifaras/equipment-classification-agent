using Dapper;
using equipment_classification_agent_api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace equipment_classification_agent_api.Services;

public interface IAzureSQLService
{
    Task<List<GolfBall>> GetGolfBallsAsync();
}

public class AzureSQLService : IAzureSQLService
{
    private readonly string _connectionString;
    private readonly ILogger<AzureSQLService> _logger;

    public AzureSQLService(
        IOptions<AzureSQLOptions> options,
        ILogger<AzureSQLService> logger)
    {
        _connectionString = options.Value.ConnectionString;
        _logger = logger;
    }

    public async Task<List<GolfBall>> GetGolfBallsAsync()
    {
        var sql = "SELECT manufacturer,usga_lot_num,pole_marking,colour,constCode,ballSpecs,dimples,spin,pole_2,seam_marking,imageUrl FROM [dbo].[tblGolfBalls]";
        var golfBalls = new List<GolfBall>();

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            golfBalls = (List<GolfBall>)await connection.QueryAsync<GolfBall>(sql);
        }

        return golfBalls;
    }
}