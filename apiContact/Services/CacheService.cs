using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace apiContact.Services
{
    /// <summary>
    /// Two-tier cache: Redis (distributed) → IMemoryCache (in-process fallback).
    /// Redis is used when IConnectionMultiplexer is registered in DI.
    /// Falls back to IMemoryCache silently — no config or restart required.
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache                _local;
        private readonly IConnectionMultiplexer?     _redis;
        private readonly ILogger<CacheService>       _log;
        private readonly TimeSpan                    _defaultTtl = TimeSpan.FromMinutes(5);

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public CacheService(
            IMemoryCache            local,
            ILogger<CacheService>   log,
            IConnectionMultiplexer? redis = null)
        {
            _local = local;
            _log   = log;
            _redis = redis;
        }

        // ── Read ──────────────────────────────────────────────────────────────────
        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            // 1. Try local memory first (fastest)
            if (_local.TryGetValue(key, out T? local)) return local;

            // 2. Try Redis
            if (_redis is not null)
            {
                try
                {
                    var db  = _redis.GetDatabase();
                    var raw = await db.StringGetAsync(key);
                    if (raw.HasValue)
                    {
                        var value = JsonSerializer.Deserialize<T>(raw!, _jsonOpts);
                        if (value is not null)
                        {
                            _local.Set(key, value, TimeSpan.FromMinutes(1)); // populate L1
                            return value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Redis GET failed for key={Key}; using memory cache", key);
                }
            }

            return null;
        }

        // ── Write ─────────────────────────────────────────────────────────────────
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
        {
            var ttl = expiry ?? _defaultTtl;

            _local.Set(key, value, ttl);

            if (_redis is not null)
            {
                try
                {
                    var db  = _redis.GetDatabase();
                    var raw = JsonSerializer.Serialize(value, _jsonOpts);
                    await db.StringSetAsync(key, raw, ttl);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Redis SET failed for key={Key}", key);
                }
            }
        }

        // ── Evict ─────────────────────────────────────────────────────────────────
        public async Task RemoveAsync(string key)
        {
            _local.Remove(key);

            if (_redis is not null)
            {
                try
                {
                    await _redis.GetDatabase().KeyDeleteAsync(key);
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Redis DEL failed for key={Key}", key);
                }
            }
        }

        /// <summary>
        /// Remove all keys that start with <paramref name="prefix"/> from both tiers.
        /// For Redis this uses SCAN — safe on production clusters (no KEYS command).
        /// </summary>
        public async Task RemoveByPrefixAsync(string prefix)
        {
            // L1: IMemoryCache has no enumeration API — track with a key set
            // stored under a convention key so we can enumerate and remove
            // We keep a HashSet<string> in memory under the sentinel key.
            const string sentinelSuffix = "__keys__";
            var sentinelKey = $"{prefix}{sentinelSuffix}";

            if (_local.TryGetValue(sentinelKey, out HashSet<string>? tracked) && tracked is not null)
            {
                foreach (var k in tracked) _local.Remove(k);
                _local.Remove(sentinelKey);
            }

            if (_redis is not null)
            {
                try
                {
                    var endpoints = _redis.GetEndPoints();
                    foreach (var ep in endpoints)
                    {
                        var server = _redis.GetServer(ep);
                        await foreach (var k in server.KeysAsync(pattern: $"{prefix}*"))
                            await _redis.GetDatabase().KeyDeleteAsync(k);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Redis prefix-delete failed for prefix={Prefix}", prefix);
                }
            }
        }

        /// <summary>
        /// Internal helper — registers a key in the L1 sentinel set so prefix
        /// eviction can find it later without requiring IMemoryCache enumeration.
        /// </summary>
        internal void TrackKey(string prefix, string key)
        {
            const string sentinelSuffix = "__keys__";
            var sentinelKey = $"{prefix}{sentinelSuffix}";
            var set = _local.GetOrCreate(sentinelKey, _ => new HashSet<string>())!;
            set.Add(key);
        }
    }
}
