using StackExchange.Redis;
using System.Diagnostics;

namespace apiContact.Services;

/// <summary>
/// Service for verifying Redis connection and health
/// </summary>
public interface IRedisConnectionService
{
    Task<bool> VerifyConnectionAsync();
    Task<ConnectionHealthStatus> GetHealthStatusAsync();
}

public class RedisConnectionService : IRedisConnectionService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisConnectionService> _logger;

    public RedisConnectionService(
        IConnectionMultiplexer redis,
        ILogger<RedisConnectionService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Verify Redis connection is working
    /// </summary>
    public async Task<bool> VerifyConnectionAsync()
    {
        try
        {
            _logger.LogInformation("🔍 Verifying Redis connection...");

            var stopwatch = Stopwatch.StartNew();

            if (!_redis.IsConnected)
            {
                _logger.LogWarning("⚠️ Redis connection multiplexer reports disconnected state");
                return false;
            }

            var server = _redis.GetServer(_redis.GetEndPoints().FirstOrDefault() 
                ?? throw new InvalidOperationException("No Redis endpoints available"));

            var pingResponse = await server.PingAsync();
            
            stopwatch.Stop();

            _logger.LogInformation(
                "✅ Redis connection verified successfully (Response time: {ElapsedMs}ms)",
                stopwatch.ElapsedMilliseconds);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "⚠️ Redis connection verification failed - continuing with in-memory cache");
            return false;
        }
    }

    /// <summary>
    /// Get detailed Redis connection health status
    /// </summary>
    public async Task<ConnectionHealthStatus> GetHealthStatusAsync()
    {
        var status = new ConnectionHealthStatus();

        try
        {
            status.IsConnected = _redis.IsConnected;
            status.ConnectedEndpoints = _redis.GetEndPoints().Select(ep => ep.ToString()).ToList() ?? new();

            if (_redis.IsConnected)
            {
                var server = _redis.GetServer(_redis.GetEndPoints().First());
                status.ServerInfo = server.Info().FirstOrDefault()?.ToString() ?? "Unknown";
                status.IsHealthy = await VerifyConnectionAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Redis health status");
            status.IsHealthy = false;
            status.ErrorMessage = ex.Message;
        }

        return status;
    }
}

public class ConnectionHealthStatus
{
    public bool IsConnected { get; set; }
    public bool IsHealthy { get; set; }
    public List<string> ConnectedEndpoints { get; set; } = new();
    public string? ServerInfo { get; set; }
    public string? ErrorMessage { get; set; }
}
