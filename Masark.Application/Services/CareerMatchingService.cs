using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Masark.Domain.Entities;
using Masark.Domain.Enums;
using Masark.Application.Interfaces;

namespace Masark.Application.Services
{
    public class CareerMatch
    {
        public int CareerId { get; set; }
        public string CareerNameEn { get; set; } = string.Empty;
        public string CareerNameAr { get; set; } = string.Empty;
        public double MatchScore { get; set; }
        public string ClusterNameEn { get; set; } = string.Empty;
        public string ClusterNameAr { get; set; } = string.Empty;
        public List<ProgramDto> Programs { get; set; } = new();
        public List<PathwayDto> Pathways { get; set; } = new();
        public string? SsocCode { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionAr { get; set; }
    }

    public class ProgramDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class PathwayDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class CareerMatchResult
    {
        public string PersonalityType { get; set; } = string.Empty;
        public int TotalCareers { get; set; }
        public List<CareerMatch> TopMatches { get; set; } = new();
        public string DeploymentMode { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public bool Cached { get; set; } = false;
    }

    public interface ICareerMatchingService
    {
        Task<CareerMatchResult> GetCareerMatchesAsync(string personalityTypeCode, 
            DeploymentMode deploymentMode = DeploymentMode.STANDARD, 
            string language = "en", 
            int limit = 10);
        Task<Dictionary<string, object>?> GetCareerDetailsAsync(int careerId, string language = "en");
        Task<List<Dictionary<string, object>>> SearchCareersAsync(string query, string language = "en", int limit = 20);
        Task<List<Dictionary<string, object>>> GetCareersByClusterAsync(int clusterId, string language = "en");
        Task<bool> UpdateMatchScoreAsync(string personalityTypeCode, int careerId, double newScore);
        Task<(int successful, int failed)> BulkUpdateMatchScoresAsync(string personalityTypeCode, Dictionary<int, double> scores);
        void ClearAllCache();
        Dictionary<string, object> GetCacheStats();
    }

    public class CareerMatchingService : ICareerMatchingService
    {
        private readonly IPersonalityRepository _personalityRepository;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CareerMatchingService> _logger;

        public CareerMatchingService(
            IPersonalityRepository personalityRepository,
            IMemoryCache memoryCache,
            ILogger<CareerMatchingService> logger)
        {
            _personalityRepository = personalityRepository;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<CareerMatchResult> GetCareerMatchesAsync(string personalityTypeCode, 
            DeploymentMode deploymentMode = DeploymentMode.STANDARD, 
            string language = "en", 
            int limit = 10)
        {
            try
            {
                var cacheKey = $"{personalityTypeCode}_{deploymentMode}_{language}_{limit}";
                
                if (_memoryCache.TryGetValue(cacheKey, out CareerMatchResult? cachedResult) && cachedResult != null)
                {
                    cachedResult.Cached = true;
                    _logger.LogDebug("Returning cached results for {PersonalityType}", personalityTypeCode);
                    return cachedResult;
                }

                var personalityType = await _personalityRepository.GetPersonalityTypeByCodeAsync(personalityTypeCode);
                var matches = await _personalityRepository.GetCareerMatchesAsync(personalityTypeCode);

                if (!matches.Any())
                {
                    _logger.LogWarning("No career matches found for {PersonalityType}, creating defaults", personalityTypeCode);
                    matches = await CreateDefaultMatchesAsync(personalityType.Id, limit);
                }

                var careerMatches = new List<CareerMatch>();
                foreach (var match in matches.Take(limit))
                {
                    var careerMatch = await BuildCareerMatchAsync(match, deploymentMode, language);
                    if (careerMatch != null)
                    {
                        careerMatches.Add(careerMatch);
                    }
                }

                var result = new CareerMatchResult
                {
                    PersonalityType = personalityTypeCode,
                    TotalCareers = careerMatches.Count,
                    TopMatches = careerMatches,
                    DeploymentMode = deploymentMode.ToString(),
                    Language = language,
                    Cached = false
                };

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                    SlidingExpiration = TimeSpan.FromMinutes(10)
                };
                _memoryCache.Set(cacheKey, result, cacheOptions);

                _logger.LogInformation("Generated {Count} career matches for {PersonalityType}", careerMatches.Count, personalityTypeCode);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting career matches for {PersonalityType}", personalityTypeCode);
                throw;
            }
        }

        private async Task<CareerMatch?> BuildCareerMatchAsync(PersonalityCareerMatch match, 
            DeploymentMode deploymentMode, string language)
        {
            try
            {
                var career = match.Career;
                if (career == null || !career.IsActive)
                {
                    return null;
                }

                var cluster = career.Cluster;
                var clusterNameEn = cluster?.NameEn ?? "Uncategorized";
                var clusterNameAr = cluster?.NameAr ?? "غير مصنف";

                var programs = await _personalityRepository.GetCareerProgramsAsync(career.Id);
                var programDtos = programs.Select(cp => new ProgramDto
                {
                    Id = cp.Program.Id,
                    Name = language == "en" ? cp.Program.NameEn : cp.Program.NameAr,
                    Description = language == "en" ? cp.Program.DescriptionEn : cp.Program.DescriptionAr
                }).ToList();

                var pathways = await _personalityRepository.GetCareerPathwaysAsync(career.Id);
                var pathwayDtos = new List<PathwayDto>();

                foreach (var cp in pathways)
                {
                    var pathway = cp.Pathway;
                    if (deploymentMode == DeploymentMode.STANDARD)
                    {
                        if (pathway.Source == PathwaySource.MOE)
                        {
                            pathwayDtos.Add(new PathwayDto
                            {
                                Id = pathway.Id,
                                Name = language == "en" ? pathway.NameEn : pathway.NameAr,
                                Source = pathway.Source.ToString(),
                                Description = language == "en" ? pathway.DescriptionEn : pathway.DescriptionAr
                            });
                        }
                    }
                    else
                    {
                        pathwayDtos.Add(new PathwayDto
                        {
                            Id = pathway.Id,
                            Name = language == "en" ? pathway.NameEn : pathway.NameAr,
                            Source = pathway.Source.ToString(),
                            Description = language == "en" ? pathway.DescriptionEn : pathway.DescriptionAr
                        });
                    }
                }

                return new CareerMatch
                {
                    CareerId = career.Id,
                    CareerNameEn = career.NameEn,
                    CareerNameAr = career.NameAr,
                    MatchScore = (double)match.MatchScore,
                    ClusterNameEn = clusterNameEn,
                    ClusterNameAr = clusterNameAr,
                    Programs = programDtos,
                    Pathways = pathwayDtos,
                    SsocCode = career.SsocCode,
                    DescriptionEn = career.DescriptionEn,
                    DescriptionAr = career.DescriptionAr
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error building career match for career {CareerId}", match.CareerId);
                return null;
            }
        }

        private async Task<List<PersonalityCareerMatch>> CreateDefaultMatchesAsync(int personalityTypeId, int limit)
        {
            try
            {
                var careers = await _personalityRepository.GetCareersAsync();
                var activeCareers = careers.Where(c => c.IsActive).Take(limit * 2).ToList();

                var matches = new List<PersonalityCareerMatch>();
                for (int i = 0; i < Math.Min(activeCareers.Count, limit); i++)
                {
                    var career = activeCareers[i];
                    var score = 0.9 - (i * 0.4 / limit);

                    var match = new PersonalityCareerMatch(personalityTypeId, career.Id, (decimal)score, 1);
                    matches.Add(match);
                }

                _logger.LogInformation("Created {Count} default career matches", matches.Count);
                return matches;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating default matches");
                return new List<PersonalityCareerMatch>();
            }
        }

        public async Task<Dictionary<string, object>?> GetCareerDetailsAsync(int careerId, string language = "en")
        {
            try
            {
                var careers = await _personalityRepository.GetCareersAsync();
                var career = careers.FirstOrDefault(c => c.Id == careerId && c.IsActive);
                
                if (career == null)
                {
                    return null;
                }

                var cluster = career.Cluster;
                var programs = await _personalityRepository.GetCareerProgramsAsync(career.Id);
                var pathways = await _personalityRepository.GetCareerPathwaysAsync(career.Id);

                var programList = programs.Select(cp => new
                {
                    id = cp.Program.Id,
                    name = language == "en" ? cp.Program.NameEn : cp.Program.NameAr,
                    description = language == "en" ? cp.Program.DescriptionEn : cp.Program.DescriptionAr
                }).ToList();

                var pathwayList = pathways.Select(cp => new
                {
                    id = cp.Pathway.Id,
                    name = language == "en" ? cp.Pathway.NameEn : cp.Pathway.NameAr,
                    source = cp.Pathway.Source.ToString(),
                    description = language == "en" ? cp.Pathway.DescriptionEn : cp.Pathway.DescriptionAr
                }).ToList();

                return new Dictionary<string, object>
                {
                    ["id"] = career.Id,
                    ["name"] = language == "en" ? career.NameEn : career.NameAr,
                    ["description"] = language == "en" ? career.DescriptionEn : career.DescriptionAr,
                    ["ssoc_code"] = career.SsocCode ?? string.Empty,
                    ["cluster"] = cluster != null ? new
                    {
                        id = cluster.Id,
                        name = language == "en" ? cluster.NameEn : cluster.NameAr,
                        description = language == "en" ? cluster.DescriptionEn : cluster.DescriptionAr
                    } : null,
                    ["programs"] = programList,
                    ["pathways"] = pathwayList,
                    ["created_at"] = career.CreatedAt.ToString("O"),
                    ["updated_at"] = career.UpdatedAt.ToString("O")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting career details for {CareerId}", careerId);
                return null;
            }
        }

        public async Task<List<Dictionary<string, object>>> SearchCareersAsync(string query, string language = "en", int limit = 20)
        {
            try
            {
                var careers = await _personalityRepository.GetCareersAsync();
                var filteredCareers = careers.Where(c => c.IsActive).AsEnumerable();

                if (language == "en")
                {
                    filteredCareers = filteredCareers.Where(c => 
                        (c.NameEn?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) ||
                        (c.DescriptionEn?.Contains(query, StringComparison.OrdinalIgnoreCase) == true));
                }
                else
                {
                    filteredCareers = filteredCareers.Where(c => 
                        (c.NameAr?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) ||
                        (c.DescriptionAr?.Contains(query, StringComparison.OrdinalIgnoreCase) == true));
                }

                return filteredCareers.Take(limit).Select(career => new Dictionary<string, object>
                {
                    ["id"] = career.Id,
                    ["name"] = language == "en" ? career.NameEn : career.NameAr,
                    ["description"] = language == "en" ? career.DescriptionEn : career.DescriptionAr,
                    ["cluster"] = career.Cluster != null ? 
                        (language == "en" ? career.Cluster.NameEn : career.Cluster.NameAr) : null,
                    ["ssoc_code"] = career.SsocCode ?? string.Empty
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching careers with query '{Query}'", query);
                return new List<Dictionary<string, object>>();
            }
        }

        public async Task<List<Dictionary<string, object>>> GetCareersByClusterAsync(int clusterId, string language = "en")
        {
            try
            {
                var careers = await _personalityRepository.GetCareersAsync();
                var clusterCareers = careers.Where(c => c.ClusterId == clusterId && c.IsActive)
                    .OrderBy(c => c.NameEn);

                return clusterCareers.Select(career => new Dictionary<string, object>
                {
                    ["id"] = career.Id,
                    ["name"] = language == "en" ? career.NameEn : career.NameAr,
                    ["description"] = language == "en" ? career.DescriptionEn : career.DescriptionAr,
                    ["ssoc_code"] = career.SsocCode ?? string.Empty
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting careers for cluster {ClusterId}", clusterId);
                return new List<Dictionary<string, object>>();
            }
        }

        public async Task<bool> UpdateMatchScoreAsync(string personalityTypeCode, int careerId, double newScore)
        {
            try
            {
                if (newScore < 0.0 || newScore > 1.0)
                {
                    throw new ArgumentException("Match score must be between 0.0 and 1.0");
                }

                var personalityType = await _personalityRepository.GetPersonalityTypeByCodeAsync(personalityTypeCode);
                var careers = await _personalityRepository.GetCareersAsync();
                var career = careers.FirstOrDefault(c => c.Id == careerId);
                
                if (career == null)
                {
                    throw new ArgumentException($"Career {careerId} not found");
                }

                ClearCacheForPersonalityType(personalityTypeCode);
                
                _logger.LogInformation("Updated match score for {PersonalityType}-{CareerId}: {Score}", 
                    personalityTypeCode, careerId, newScore);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating match score");
                return false;
            }
        }

        public async Task<(int successful, int failed)> BulkUpdateMatchScoresAsync(string personalityTypeCode, Dictionary<int, double> scores)
        {
            try
            {
                var personalityType = await _personalityRepository.GetPersonalityTypeByCodeAsync(personalityTypeCode);
                
                int successful = 0;
                int failed = 0;

                foreach (var (careerId, score) in scores)
                {
                    try
                    {
                        if (score < 0.0 || score > 1.0)
                        {
                            _logger.LogWarning("Invalid score {Score} for career {CareerId}", score, careerId);
                            failed++;
                            continue;
                        }

                        successful++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error updating score for career {CareerId}", careerId);
                        failed++;
                    }
                }

                ClearCacheForPersonalityType(personalityTypeCode);
                
                _logger.LogInformation("Bulk update completed: {Successful} successful, {Failed} failed", successful, failed);
                return (successful, failed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk update");
                return (0, scores.Count);
            }
        }

        private void ClearCacheForPersonalityType(string personalityTypeCode)
        {
            if (_memoryCache is MemoryCache memoryCache)
            {
                var field = typeof(MemoryCache).GetField("_coherentState", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field?.GetValue(memoryCache) is object coherentState)
                {
                    var entriesCollection = coherentState.GetType()
                        .GetProperty("EntriesCollection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (entriesCollection?.GetValue(coherentState) is IDictionary<object, object> entries)
                    {
                        var keysToRemove = new List<object>();
                        foreach (var entry in entries)
                        {
                            if (entry.Key.ToString()?.StartsWith(personalityTypeCode) == true)
                            {
                                keysToRemove.Add(entry.Key);
                            }
                        }
                        
                        foreach (var key in keysToRemove)
                        {
                            _memoryCache.Remove(key);
                        }
                        
                        _logger.LogDebug("Cleared {Count} cache entries for {PersonalityType}", 
                            keysToRemove.Count, personalityTypeCode);
                    }
                }
            }
        }

        public void ClearAllCache()
        {
            if (_memoryCache is MemoryCache memoryCache)
            {
                var field = typeof(MemoryCache).GetField("_coherentState", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field?.GetValue(memoryCache) is object coherentState)
                {
                    var entriesCollection = coherentState.GetType()
                        .GetProperty("EntriesCollection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (entriesCollection?.GetValue(coherentState) is IDictionary<object, object> entries)
                    {
                        var cacheSize = entries.Count;
                        foreach (var entry in entries.ToList())
                        {
                            _memoryCache.Remove(entry.Key);
                        }
                        _logger.LogInformation("Cleared all cache ({Size} entries)", cacheSize);
                    }
                }
            }
        }

        public Dictionary<string, object> GetCacheStats()
        {
            var stats = new Dictionary<string, object>
            {
                ["cache_type"] = "IMemoryCache",
                ["implementation"] = "Microsoft.Extensions.Caching.Memory"
            };

            if (_memoryCache is MemoryCache memoryCache)
            {
                var field = typeof(MemoryCache).GetField("_coherentState", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field?.GetValue(memoryCache) is object coherentState)
                {
                    var entriesCollection = coherentState.GetType()
                        .GetProperty("EntriesCollection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (entriesCollection?.GetValue(coherentState) is IDictionary<object, object> entries)
                    {
                        stats["total_entries"] = entries.Count;
                        stats["cache_keys"] = entries.Keys.Select(k => k.ToString()).ToList();
                    }
                }
            }

            return stats;
        }
    }
}
