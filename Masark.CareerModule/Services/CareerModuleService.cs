using Microsoft.Extensions.Logging;
using Masark.Application.Interfaces;
using Masark.Domain.Entities;
using Masark.Application.Services;

namespace Masark.CareerModule.Services
{
    public interface ICareerModuleService
    {
        Task<IEnumerable<Career>> GetCareersAsync(string language);
        Task<IEnumerable<CareerMatch>> GetCareerMatchesAsync(string personalityType, string language);
        Task<IEnumerable<CareerCluster>> GetCareerClustersAsync(string language);
        Task<CareerRecommendation> GetPersonalizedRecommendationsAsync(string userId, string personalityType);
        Task<bool> UpdateCareerDataAsync(Career career);
        Task<CareerAnalytics> GetCareerAnalyticsAsync(string tenantId);
    }

    public class CareerModuleService : ICareerModuleService
    {
        private readonly ICareerMatchingService _careerMatchingService;
        private readonly IPersonalityRepository _personalityRepository;
        private readonly ICachingService _cachingService;
        private readonly ILogger<CareerModuleService> _logger;

        public CareerModuleService(
            ICareerMatchingService careerMatchingService,
            IPersonalityRepository personalityRepository,
            ICachingService cachingService,
            ILogger<CareerModuleService> logger)
        {
            _careerMatchingService = careerMatchingService;
            _personalityRepository = personalityRepository;
            _cachingService = cachingService;
            _logger = logger;
        }

        public async Task<IEnumerable<Career>> GetCareersAsync(string language)
        {
            _logger.LogInformation("Retrieving careers for language");

            var cacheKey = $"careers_{language}";
            var cachedCareers = await _cachingService.GetAsync<IEnumerable<Career>>(cacheKey);
            
            if (cachedCareers != null)
            {
                _logger.LogDebug("Careers retrieved from cache for language");
                return cachedCareers;
            }

            var careers = await _personalityRepository.GetCareersAsync(language);
            await _cachingService.SetAsync(cacheKey, careers, TimeSpan.FromHours(6));

            _logger.LogInformation("Retrieved careers for language");
            return careers;
        }

        public async Task<IEnumerable<CareerMatch>> GetCareerMatchesAsync(string personalityType, string language)
        {
            _logger.LogInformation("Getting career matches for personality type in language");

            var cacheKey = $"career_matches_{personalityType}_{language}";
            var cachedMatches = await _cachingService.GetAsync<IEnumerable<CareerMatch>>(cacheKey);
            
            if (cachedMatches != null)
            {
                _logger.LogDebug("Career matches retrieved from cache for personality type");
                return cachedMatches;
            }

            var matches = await _careerMatchingService.GetCareerMatchesAsync(personalityType, language);
            await _cachingService.SetAsync(cacheKey, matches, TimeSpan.FromHours(2));

            _logger.LogInformation("Found career matches for personality type");
            return matches;
        }

        public async Task<IEnumerable<CareerCluster>> GetCareerClustersAsync(string language)
        {
            _logger.LogInformation("Retrieving career clusters for language");

            var cacheKey = $"career_clusters_{language}";
            var cachedClusters = await _cachingService.GetAsync<IEnumerable<CareerCluster>>(cacheKey);
            
            if (cachedClusters != null)
            {
                return cachedClusters;
            }

            var clusters = await _personalityRepository.GetCareerClustersAsync(language);
            await _cachingService.SetAsync(cacheKey, clusters, TimeSpan.FromHours(12));

            return clusters;
        }

        public async Task<CareerRecommendation> GetPersonalizedRecommendationsAsync(string userId, string personalityType)
        {
            _logger.LogInformation("Getting personalized career recommendations for user with personality type");

            var userSessions = await _personalityRepository.GetUserSessionsAsync(userId);
            var latestSession = userSessions.OrderByDescending(s => s.CreatedAt).FirstOrDefault();

            if (latestSession == null)
            {
                throw new InvalidOperationException($"No assessment sessions found for user {userId}");
            }

            var answers = await _personalityRepository.GetSessionAnswersAsync(latestSession.Id);
            var careerMatches = await _careerMatchingService.GetCareerMatchesAsync(personalityType, latestSession.LanguagePreference);

            var recommendation = new CareerRecommendation
            {
                UserId = userId,
                PersonalityType = personalityType,
                TopMatches = careerMatches.Take(5).ToList(),
                RecommendationScore = CalculateRecommendationScore(answers, careerMatches),
                GeneratedAt = DateTime.UtcNow,
                LanguagePreference = latestSession.LanguagePreference
            };

            _logger.LogInformation("Generated personalized recommendations for user with top matches");

            return recommendation;
        }

        public async Task<bool> UpdateCareerDataAsync(Career career)
        {
            _logger.LogInformation("Updating career data for career");

            try
            {
                await _personalityRepository.UpdateCareerAsync(career);
                
                await _cachingService.RemoveByPatternAsync("careers_*");
                await _cachingService.RemoveByPatternAsync("career_matches_*");
                
                _logger.LogInformation("Career data updated successfully for career");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update career data for career");
                return false;
            }
        }

        public async Task<CareerAnalytics> GetCareerAnalyticsAsync(string tenantId)
        {
            _logger.LogInformation("Retrieving career analytics for tenant");

            var sessions = await _personalityRepository.GetCompletedSessionsAsync(tenantId);
            var careerMatches = new List<CareerMatch>();

            foreach (var session in sessions)
            {
                if (!string.IsNullOrEmpty(session.PersonalityType))
                {
                    var matches = await _careerMatchingService.GetCareerMatchesAsync(session.PersonalityType, session.LanguagePreference);
                    careerMatches.AddRange(matches);
                }
            }

            var analytics = new CareerAnalytics
            {
                TenantId = tenantId,
                TotalRecommendations = careerMatches.Count,
                TopCareersByPopularity = careerMatches
                    .GroupBy(m => m.Career.Title)
                    .OrderByDescending(g => g.Count())
                    .Take(10)
                    .ToDictionary(g => g.Key, g => g.Count()),
                PersonalityTypeDistribution = sessions
                    .Where(s => !string.IsNullOrEmpty(s.PersonalityType))
                    .GroupBy(s => s.PersonalityType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                GeneratedAt = DateTime.UtcNow
            };

            return analytics;
        }

        private double CalculateRecommendationScore(IEnumerable<AssessmentAnswer> answers, IEnumerable<CareerMatch> matches)
        {
            if (!answers.Any() || !matches.Any())
                return 0.0;

            var averageMatchScore = matches.Average(m => m.MatchPercentage);
            var answerConsistency = CalculateAnswerConsistency(answers);
            
            return (averageMatchScore * 0.7) + (answerConsistency * 0.3);
        }

        private double CalculateAnswerConsistency(IEnumerable<AssessmentAnswer> answers)
        {
            var strengthCounts = answers
                .GroupBy(a => a.PreferenceStrength)
                .ToDictionary(g => g.Key, g => g.Count());

            var totalAnswers = answers.Count();
            if (totalAnswers == 0) return 0.0;

            var consistencyScore = strengthCounts.Values.Max() / (double)totalAnswers;
            return Math.Min(consistencyScore * 100, 100.0);
        }
    }

    public class CareerMatch
    {
        public Career Career { get; set; } = new();
        public double MatchPercentage { get; set; }
        public string MatchReason { get; set; } = string.Empty;
        public int ConfidenceLevel { get; set; }
    }

    public class CareerRecommendation
    {
        public string UserId { get; set; } = string.Empty;
        public string PersonalityType { get; set; } = string.Empty;
        public List<CareerMatch> TopMatches { get; set; } = new();
        public double RecommendationScore { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string LanguagePreference { get; set; } = string.Empty;
    }

    public class CareerAnalytics
    {
        public string TenantId { get; set; } = string.Empty;
        public int TotalRecommendations { get; set; }
        public Dictionary<string, int> TopCareersByPopularity { get; set; } = new();
        public Dictionary<string, int> PersonalityTypeDistribution { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
    }
}
