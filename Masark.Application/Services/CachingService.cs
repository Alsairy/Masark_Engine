using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Masark.Application.Options;
using System.Text.Json;

namespace Masark.Application.Services
{
    public class CacheEntry<T>
    {
        public T Value { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastAccessed { get; set; }
        public TimeSpan? TimeToLive { get; set; }
        public int AccessCount { get; set; }

        public CacheEntry(T value, TimeSpan? ttl = null)
        {
            Value = value;
            CreatedAt = DateTime.UtcNow;
            LastAccessed = DateTime.UtcNow;
            TimeToLive = ttl;
            AccessCount = 1;
        }

        public bool IsExpired => TimeToLive.HasValue && DateTime.UtcNow - CreatedAt > TimeToLive.Value;

        public void UpdateAccess()
        {
            LastAccessed = DateTime.UtcNow;
            AccessCount++;
        }
    }

    public class InMemoryCache<T>
    {
        private readonly ConcurrentDictionary<string, CacheEntry<T>> _cache;
        private readonly int _maxSize;
        private readonly Timer _cleanupTimer;
        private readonly ILogger _logger;

        public InMemoryCache(int maxSize = 1000, ILogger? logger = null)
        {
            _cache = new ConcurrentDictionary<string, CacheEntry<T>>();
            _maxSize = maxSize;
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            
            _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
        }

        public T? Get(string key)
        {
            if (_cache.TryGetValue(key, out var entry))
            {
                if (entry.IsExpired)
                {
                    _cache.TryRemove(key, out _);
                    return default(T);
                }

                entry.UpdateAccess();
                return entry.Value;
            }

            return default(T);
        }

        public void Set(string key, T value, TimeSpan? ttl = null)
        {
            var entry = new CacheEntry<T>(value, ttl);
            
            if (_cache.Count >= _maxSize)
            {
                EvictLeastRecentlyUsed();
            }

            _cache.AddOrUpdate(key, entry, (k, oldEntry) => entry);
        }

        public bool Remove(string key)
        {
            return _cache.TryRemove(key, out _);
        }

        public void Clear()
        {
            _cache.Clear();
        }

        public int Count => _cache.Count;

        public Dictionary<string, object> GetStats()
        {
            var totalAccesses = _cache.Values.Sum(e => e.AccessCount);
            var expiredCount = _cache.Values.Count(e => e.IsExpired);

            return new Dictionary<string, object>
            {
                ["total_entries"] = _cache.Count,
                ["total_accesses"] = totalAccesses,
                ["expired_entries"] = expiredCount,
                ["max_size"] = _maxSize,
                ["hit_ratio"] = totalAccesses > 0 ? (double)(_cache.Count - expiredCount) / totalAccesses : 0.0
            };
        }

        private void EvictLeastRecentlyUsed()
        {
            if (_cache.IsEmpty) return;

            var lruEntry = _cache.OrderBy(kvp => kvp.Value.LastAccessed).First();
            _cache.TryRemove(lruEntry.Key, out _);
            _logger.LogDebug("Evicted LRU cache entry: {Key}", lruEntry.Key);
        }

        private void CleanupExpiredEntries(object? state)
        {
            var expiredKeys = _cache
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }

            if (expiredKeys.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} expired cache entries", expiredKeys.Count);
            }
        }

        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }
    }

    public interface ICachingService
    {
        Task<List<Dictionary<string, object>>?> GetQuestionsAsync(string language = "en");
        Task CacheQuestionsAsync(List<Dictionary<string, object>> questions, string language = "en");

        Task<List<Dictionary<string, object>>?> GetCareersAsync(string language = "en");
        Task CacheCareersAsync(List<Dictionary<string, object>> careers, string language = "en");

        Task<List<Dictionary<string, object>>?> GetPersonalityTypesAsync(string language = "en");
        Task CachePersonalityTypesAsync(List<Dictionary<string, object>> personalityTypes, string language = "en");

        Task<List<Dictionary<string, object>>?> GetCareerMatchesAsync(string personalityType, Dictionary<string, object>? filters = null);
        Task CacheCareerMatchesAsync(string personalityType, List<Dictionary<string, object>> matches, Dictionary<string, object>? filters = null);

        Task<Dictionary<string, object>?> GetSessionAsync(string sessionId);
        Task CacheSessionAsync(string sessionId, Dictionary<string, object> sessionData);

        Task<Dictionary<string, object>?> GetReportAsync(string sessionId, string reportType);
        Task CacheReportAsync(string sessionId, string reportType, Dictionary<string, object> reportData);

        Task InvalidateCacheAsync(string? cacheType = null, string? pattern = null);
        Task WarmCacheAsync();
        Dictionary<string, object> GetCacheStats();
        Task ClearAllCacheAsync();

        Task<T> CacheAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null);
    }

    public class CachingService : ICachingService
    {
        private readonly IDistributedCache? _distributedCache;
        private readonly IMemoryCache _memoryCache;
        private readonly InMemoryCache<List<Dictionary<string, object>>> _questionCache;
        private readonly InMemoryCache<List<Dictionary<string, object>>> _careerCache;
        private readonly InMemoryCache<List<Dictionary<string, object>>> _personalityCache;
        private readonly InMemoryCache<Dictionary<string, object>> _sessionCache;
        private readonly InMemoryCache<Dictionary<string, object>> _reportCache;
        private readonly InMemoryCache<object> _genericCache;
        private readonly ILogger<CachingService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly bool _enableDistributedCache;

        public CachingService(
            IDistributedCache? distributedCache,
            IMemoryCache memoryCache,
            IOptions<CachingOptions> cachingOptions,
            ILogger<CachingService> logger)
        {
            _distributedCache = distributedCache;
            _memoryCache = memoryCache;
            _enableDistributedCache = cachingOptions.Value.EnableDistributedCache;
            _logger = logger;
            
            _questionCache = new InMemoryCache<List<Dictionary<string, object>>>(500, logger);
            _careerCache = new InMemoryCache<List<Dictionary<string, object>>>(1000, logger);
            _personalityCache = new InMemoryCache<List<Dictionary<string, object>>>(100, logger);
            _sessionCache = new InMemoryCache<Dictionary<string, object>>(5000, logger);
            _reportCache = new InMemoryCache<Dictionary<string, object>>(2000, logger);
            _genericCache = new InMemoryCache<object>(10000, logger);
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task<List<Dictionary<string, object>>?> GetQuestionsAsync(string language = "en")
        {
            var cacheKey = $"masark:questions:{language}";
            
            if (_enableDistributedCache && _distributedCache != null)
            {
                try
                {
                    var distributedResult = await GetFromDistributedCacheAsync<List<Dictionary<string, object>>>(cacheKey);
                    if (distributedResult != null)
                    {
                        _logger.LogDebug("Questions Redis cache HIT for language {Language}", language);
                        _memoryCache.Set(cacheKey, distributedResult, TimeSpan.FromMinutes(5));
                        return distributedResult;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis cache failed for questions, falling back to memory cache");
                }
            }

            var memoryResult = _memoryCache.Get<List<Dictionary<string, object>>>(cacheKey);
            if (memoryResult != null)
            {
                _logger.LogDebug("Questions memory cache HIT for language {Language}", language);
                return memoryResult;
            }

            var fallbackResult = _questionCache.Get(cacheKey);
            _logger.LogDebug("Questions cache {Status} for language {Language}", 
                fallbackResult != null ? "FALLBACK HIT" : "MISS", language);
            return fallbackResult;
        }

        public async Task CacheQuestionsAsync(List<Dictionary<string, object>> questions, string language = "en")
        {
            var cacheKey = $"masark:questions:{language}";
            var ttl = TimeSpan.FromHours(2);
            
            if (_enableDistributedCache && _distributedCache != null)
            {
                try
                {
                    await SetInDistributedCacheAsync(cacheKey, questions, ttl);
                    _logger.LogDebug("Cached {Count} questions in Redis for language {Language}", questions.Count, language);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cache questions in Redis, using memory cache");
                }
            }

            _memoryCache.Set(cacheKey, questions, TimeSpan.FromMinutes(30));
            _questionCache.Set(cacheKey, questions, ttl);
            _logger.LogDebug("Cached {Count} questions for language {Language}", questions.Count, language);
        }

        public async Task<List<Dictionary<string, object>>?> GetCareersAsync(string language = "en")
        {
            var cacheKey = $"careers_{language}";
            var result = _careerCache.Get(cacheKey);
            _logger.LogDebug("Careers cache {Status} for language {Language}", 
                result != null ? "HIT" : "MISS", language);
            return await Task.FromResult(result);
        }

        public async Task CacheCareersAsync(List<Dictionary<string, object>> careers, string language = "en")
        {
            var cacheKey = $"careers_{language}";
            _careerCache.Set(cacheKey, careers, TimeSpan.FromHours(4));
            _logger.LogDebug("Cached {Count} careers for language {Language}", careers.Count, language);
            await Task.CompletedTask;
        }

        public async Task<List<Dictionary<string, object>>?> GetPersonalityTypesAsync(string language = "en")
        {
            var cacheKey = $"personality_types_{language}";
            var result = _personalityCache.Get(cacheKey);
            _logger.LogDebug("Personality types cache {Status} for language {Language}", 
                result != null ? "HIT" : "MISS", language);
            return await Task.FromResult(result);
        }

        public async Task CachePersonalityTypesAsync(List<Dictionary<string, object>> personalityTypes, string language = "en")
        {
            var cacheKey = $"personality_types_{language}";
            _personalityCache.Set(cacheKey, personalityTypes, TimeSpan.FromHours(2));
            _logger.LogDebug("Cached {Count} personality types for language {Language}", personalityTypes.Count, language);
            await Task.CompletedTask;
        }

        public async Task<List<Dictionary<string, object>>?> GetCareerMatchesAsync(string personalityType, Dictionary<string, object>? filters = null)
        {
            var cacheKey = GenerateCareerCacheKey(personalityType, filters);
            var result = _careerCache.Get(cacheKey);
            _logger.LogDebug("Career matches cache {Status} for personality type {PersonalityType}", 
                result != null ? "HIT" : "MISS", personalityType);
            return await Task.FromResult(result);
        }

        public async Task CacheCareerMatchesAsync(string personalityType, List<Dictionary<string, object>> matches, Dictionary<string, object>? filters = null)
        {
            var cacheKey = GenerateCareerCacheKey(personalityType, filters);
            _careerCache.Set(cacheKey, matches, TimeSpan.FromMinutes(30));
            _logger.LogDebug("Cached {Count} career matches for personality type {PersonalityType}", matches.Count, personalityType);
            await Task.CompletedTask;
        }

        public async Task<Dictionary<string, object>?> GetSessionAsync(string sessionId)
        {
            var cacheKey = $"session_{sessionId}";
            var result = _sessionCache.Get(cacheKey);
            _logger.LogDebug("Session cache {Status} for session {SessionId}", 
                result != null ? "HIT" : "MISS", sessionId);
            return await Task.FromResult(result);
        }

        public async Task CacheSessionAsync(string sessionId, Dictionary<string, object> sessionData)
        {
            var cacheKey = $"session_{sessionId}";
            _sessionCache.Set(cacheKey, sessionData, TimeSpan.FromHours(1));
            _logger.LogDebug("Cached session data for session {SessionId}", sessionId);
            await Task.CompletedTask;
        }

        public async Task<Dictionary<string, object>?> GetReportAsync(string sessionId, string reportType)
        {
            var cacheKey = $"report_{sessionId}_{reportType}";
            var result = _reportCache.Get(cacheKey);
            _logger.LogDebug("Report cache {Status} for session {SessionId} type {ReportType}", 
                result != null ? "HIT" : "MISS", sessionId, reportType);
            return await Task.FromResult(result);
        }

        public async Task CacheReportAsync(string sessionId, string reportType, Dictionary<string, object> reportData)
        {
            var cacheKey = $"report_{sessionId}_{reportType}";
            _reportCache.Set(cacheKey, reportData, TimeSpan.FromMinutes(15));
            _logger.LogDebug("Cached report for session {SessionId} type {ReportType}", sessionId, reportType);
            await Task.CompletedTask;
        }

        public async Task InvalidateCacheAsync(string? cacheType = null, string? pattern = null)
        {
            if (cacheType == "questions")
            {
                _questionCache.Clear();
                _logger.LogInformation("Invalidated questions cache");
            }
            else if (cacheType == "careers")
            {
                _careerCache.Clear();
                _logger.LogInformation("Invalidated careers cache");
            }
            else if (cacheType == "personality_types")
            {
                _personalityCache.Clear();
                _logger.LogInformation("Invalidated personality types cache");
            }
            else if (cacheType == "sessions")
            {
                _sessionCache.Clear();
                _logger.LogInformation("Invalidated sessions cache");
            }
            else if (cacheType == "reports")
            {
                _reportCache.Clear();
                _logger.LogInformation("Invalidated reports cache");
            }
            else
            {
                await ClearAllCacheAsync();
            }

            await Task.CompletedTask;
        }


        public Dictionary<string, object> GetCacheStats()
        {
            return new Dictionary<string, object>
            {
                ["questions_cache"] = _questionCache.GetStats(),
                ["careers_cache"] = _careerCache.GetStats(),
                ["personality_cache"] = _personalityCache.GetStats(),
                ["sessions_cache"] = _sessionCache.GetStats(),
                ["reports_cache"] = _reportCache.GetStats(),
                ["generic_cache"] = _genericCache.GetStats(),
                ["total_memory_usage"] = GC.GetTotalMemory(false),
                ["cache_implementation"] = "Redis-first with Memory and InMemory fallback",
                ["distributed_cache_type"] = "Redis",
                ["memory_cache_type"] = "ASP.NET Core IMemoryCache",
                ["fallback_cache_type"] = "Custom InMemoryCache with TTL and LRU"
            };
        }

        public async Task ClearAllCacheAsync()
        {
            _questionCache.Clear();
            _careerCache.Clear();
            _personalityCache.Clear();
            _sessionCache.Clear();
            _reportCache.Clear();
            _genericCache.Clear();
            
            _logger.LogInformation("Cleared all caches");
            await Task.CompletedTask;
        }

        public async Task<T> CacheAsync<T>(string key, Func<Task<T>> factory, TimeSpan? ttl = null)
        {
            var cacheKey = $"masark:generic:{key}";
            var defaultTtl = ttl ?? TimeSpan.FromMinutes(30);
            
            if (_enableDistributedCache && _distributedCache != null)
            {
                try
                {
                    var distributedResult = await GetFromDistributedCacheAsync<T>(cacheKey);
                    if (distributedResult != null)
                    {
                        _logger.LogDebug("Generic Redis cache HIT for key {Key}", key);
                        _memoryCache.Set(cacheKey, distributedResult, TimeSpan.FromMinutes(5));
                        return distributedResult;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Redis cache failed for key, falling back");
                }
            }

            var memoryResult = _memoryCache.Get<T>(cacheKey);
            if (memoryResult != null)
            {
                _logger.LogDebug("Generic memory cache HIT for key {Key}", key);
                return memoryResult;
            }

            var cachedValue = _genericCache.Get(key);
            if (cachedValue is T fallbackResult)
            {
                _logger.LogDebug("Generic fallback cache HIT for key {Key}", key);
                return fallbackResult;
            }

            _logger.LogDebug("Generic cache MISS for key {Key}, executing factory", key);
            var value = await factory();
            
            if (_enableDistributedCache && _distributedCache != null)
            {
                try
                {
                    await SetInDistributedCacheAsync(cacheKey, value, defaultTtl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cache in Redis for key");
                }
            }

            _memoryCache.Set(cacheKey, value, TimeSpan.FromMinutes(5));
            _genericCache.Set(key, value!, defaultTtl);
            return value;
        }

        private async Task<T?> GetFromDistributedCacheAsync<T>(string key)
        {
            if (_distributedCache == null) return default;
            
            var cachedBytes = await _distributedCache.GetAsync(key);
            if (cachedBytes == null) return default;

            var cachedJson = System.Text.Encoding.UTF8.GetString(cachedBytes);
            return JsonSerializer.Deserialize<T>(cachedJson, _jsonOptions);
        }

        private async Task SetInDistributedCacheAsync<T>(string key, T value, TimeSpan ttl)
        {
            if (_distributedCache == null) return;
            
            var json = JsonSerializer.Serialize(value, _jsonOptions);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ttl
            };
            
            await _distributedCache.SetAsync(key, bytes, options);
        }

        public async Task WarmCacheAsync()
        {
            try
            {
                _logger.LogInformation("Starting distributed cache warm-up");

                var warmupTasks = new List<Task>();

                var questionsEn = Enumerable.Range(1, 36).Select(i => new Dictionary<string, object>
                {
                    ["id"] = i,
                    ["text"] = $"Question {i}",
                    ["dimension"] = GetDimensionForQuestion(i)
                }).ToList();

                var questionsAr = Enumerable.Range(1, 36).Select(i => new Dictionary<string, object>
                {
                    ["id"] = i,
                    ["text"] = $"سؤال {i}",
                    ["dimension"] = GetDimensionForQuestion(i)
                }).ToList();

                warmupTasks.Add(CacheQuestionsAsync(questionsEn, "en"));
                warmupTasks.Add(CacheQuestionsAsync(questionsAr, "ar"));

                var careers = Enumerable.Range(1, 20).Select(i => new Dictionary<string, object>
                {
                    ["id"] = i,
                    ["name"] = $"Career {i}",
                    ["cluster"] = $"Cluster {(i % 5) + 1}"
                }).ToList();

                warmupTasks.Add(CacheCareersAsync(careers, "en"));
                warmupTasks.Add(CacheCareersAsync(careers, "ar"));

                var personalityTypes = new[] { "INTJ", "INTP", "ENTJ", "ENTP", "INFJ", "INFP", "ENFJ", "ENFP",
                                             "ISTJ", "ISFJ", "ESTJ", "ESFJ", "ISTP", "ISFP", "ESTP", "ESFP" }
                    .Select(type => new Dictionary<string, object>
                    {
                        ["code"] = type,
                        ["name"] = $"{type} Personality",
                        ["description"] = $"Description for {type}"
                    }).ToList();

                warmupTasks.Add(CachePersonalityTypesAsync(personalityTypes, "en"));
                warmupTasks.Add(CachePersonalityTypesAsync(personalityTypes, "ar"));

                await Task.WhenAll(warmupTasks);
                _logger.LogInformation("Distributed cache warm-up completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during distributed cache warm-up");
            }
        }

        private string GenerateCareerCacheKey(string personalityType, Dictionary<string, object>? filters)
        {
            var keyParts = new List<string> { $"careers_{personalityType}" };
            
            if (filters != null && filters.Count > 0)
            {
                var filterParts = filters.OrderBy(kvp => kvp.Key)
                    .Select(kvp => $"{kvp.Key}:{kvp.Value}");
                keyParts.AddRange(filterParts);
            }
            
            return string.Join("_", keyParts);
        }

        private string GetDimensionForQuestion(int questionId)
        {
            return (questionId % 4) switch
            {
                1 => "E_I",
                2 => "S_N", 
                3 => "T_F",
                0 => "J_P",
                _ => "E_I"
            };
        }

        public void Dispose()
        {
            _questionCache?.Dispose();
            _careerCache?.Dispose();
            _personalityCache?.Dispose();
            _sessionCache?.Dispose();
            _reportCache?.Dispose();
            _genericCache?.Dispose();
        }
    }

    public static class CachingExtensions
    {
        public static async Task<T> WithCacheAsync<T>(this ICachingService cachingService, 
            string key, Func<Task<T>> factory, TimeSpan? ttl = null)
        {
            return await cachingService.CacheAsync(key, factory, ttl);
        }
    }
}
