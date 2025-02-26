using Dapper;
using equipment_classification_agent_api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace equipment_classification_agent_api.Services;

public interface IAzureSQLService
{
    Task<List<GolfBall>> GetGolfBallsAsync();
    Task<List<string>> GetManufacturers();
    Task CreateChatSessionAsync(Guid sessionId, DateTime createdAt);
    Task CreateChatMessageAsync(Guid sessionId, string sender, string messageContent, DateTime? timestamp = null);
    Task<ChatSession> GetChatSessionWithMessagesAsync(Guid sessionId);
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

        _logger.LogInformation($"Getting golf balls from database. Query: {sql}");

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            golfBalls = (List<GolfBall>)await connection.QueryAsync<GolfBall>(sql);
        }

        return golfBalls;
    }

    public async Task<List<string>> GetManufacturers()
    {
        var sql = "SELECT DISTINCT manufacturer FROM [dbo].[tblGolfBalls]";
        var manufacturers = new List<string>();

        _logger.LogInformation($"Getting manufacturers from database. Query: {sql}");

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            manufacturers = (List<string>)await connection.QueryAsync<string>(sql);
        }

        return manufacturers;
    }

    public async Task CreateChatSessionAsync(Guid sessionId, DateTime createdAt)
    {
        var checkSql = "SELECT COUNT(1) FROM [dbo].[ChatSessions] WHERE SessionId = @SessionId";

        var insertSql = @"
        INSERT INTO [dbo].[ChatSessions] (SessionId, CreatedAt)
        VALUES (@SessionId, @CreatedAt)";

        using (var connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync();

            var exists = await connection.ExecuteScalarAsync<bool>(checkSql, new { SessionId = sessionId });

            if (!exists)
            {
                await connection.ExecuteAsync(insertSql, new { SessionId = sessionId, CreatedAt = createdAt });
            }
            else
            {
                _logger.LogInformation("A record with the same SessionId already exists.");
            }
        }

        _logger.LogInformation($"Created chat session with SessionId: {sessionId}");
    }

    public async Task CreateChatMessageAsync(Guid sessionId, string sender, string messageContent, DateTime? timestamp = null)
    {
        var sql = @"
        INSERT INTO ChatMessages (SessionId, Sender, MessageContent, Timestamp)
        VALUES (@SessionId, @Sender, @MessageContent, @Timestamp)";

        var parameters = new
        {
            SessionId = sessionId,
            Sender = sender,
            MessageContent = messageContent,
            Timestamp = timestamp ?? DateTime.UtcNow
        };

        using var connection = new SqlConnection(_connectionString);
        await connection.ExecuteAsync(sql, parameters);
        _logger.LogInformation($"Created chat message with SessionId: {sessionId}");
    }

    public async Task<ChatSession> GetChatSessionWithMessagesAsync(Guid sessionId)
    {
        var sql = @"
        SELECT 
            cs.SessionId, 
            cs.CreatedAt,
            cm.MessageId,
            cm.SessionId, 
            cm.Sender,
            cm.MessageContent,
            cm.Timestamp
        FROM [dbo].[ChatSessions] cs
        LEFT JOIN [dbo].[ChatMessages] cm ON cs.SessionId = cm.SessionId
        WHERE cs.SessionId = @SessionId";

        using var connection = new SqlConnection(_connectionString);
        var sessionDictionary = new Dictionary<Guid, ChatSession>();

        var result = await connection.QueryAsync<ChatSession, ChatMessage, ChatSession>(
            sql,
            (session, message) =>
            {
                if (!sessionDictionary.TryGetValue(session.SessionId, out var currentSession))
                {
                    currentSession = session;
                    sessionDictionary.Add(currentSession.SessionId, currentSession);
                }

                if (message != null)
                {
                    currentSession.ChatMessages.Add(message);
                }

                return currentSession;
            },
            new { SessionId = sessionId },
            splitOn: "MessageId"
        );

        return result.FirstOrDefault();
    }
}