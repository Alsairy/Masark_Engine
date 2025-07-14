using System;
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
    public class CareerRecommendationResult
    {
        public List<CareerRecommendation> Recommendations { get; set; } = new();
        public Dictionary<string, double> PersonalityDimensionScores { get; set; } = new();
        public Dictionary<int, double> ClusterAffinityScores { get; set; } = new();
        public string RecommendationStrategy { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class CareerRecommendation
    {
        public int CareerId { get; set; }
        public string CareerName { get; set; } = string.Empty;
        public double OverallScore { get; set; }
        public double PersonalityMatchScore { get; set; }
        public double ClusterAffinityScore { get; set; }
        public double OnetCompatibilityScore { get; set; }
        public double SalaryScore { get; set; }
        public double OutlookScore { get; set; }
        public string RecommendationReason { get; set; } = string.Empty;
        public List<string> MatchingFactors { get; set; } = new();
        public CareerMatch CareerDetails { get; set; } = new();
    }

    public interface ICareerRecommendationService
    {
        Task<CareerRecommendationResult> GetPersonalizedRecommendationsAsync(
            string personalityType, 
            Dictionary<int, int> clusterRatings,
            string language = "en",
            int limit = 10);
        
        Task<List<CareerRecommendation>> GetSimilarCareersAsync(int careerId, string language = "en", int limit = 5);
        
        Task<Dictionary<string, object>> GetCareerCompatibilityAnalysisAsync(
            int careerId, 
            string personalityType, 
            Dictionary<int, int> clusterRatings,
            string language = "en");
        
        Task<List<Dictionary<string, object>>> GetCareerPathwayRecommendationsAsync(
            int careerId, 
            DeploymentMode deploymentMode = DeploymentMode.STANDARD,
            string language = "en");
    }

    public class CareerRecommendationService : ICareerRecommendationService
    {
        private readonly IPersonalityRepository _personalityRepository;
        private readonly ICareerMatchingService _careerMatchingService;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<CareerRecommendationService> _logger;

        private readonly Dictionary<string, Dictionary<PersonalityDimension, double>> _personalityWeights = new()
        {
            ["INTJ"] = new() { [PersonalityDimension.EI] = -0.8, [PersonalityDimension.SN] = 0.9, [PersonalityDimension.TF] = 0.7, [PersonalityDimension.JP] = 0.8 },
            ["INTP"] = new() { [PersonalityDimension.EI] = -0.9, [PersonalityDimension.SN] = 0.8, [PersonalityDimension.TF] = 0.9, [PersonalityDimension.JP] = -0.7 },
            ["ENTJ"] = new() { [PersonalityDimension.EI] = 0.8, [PersonalityDimension.SN] = 0.7, [PersonalityDimension.TF] = 0.8, [PersonalityDimension.JP] = 0.9 },
            ["ENTP"] = new() { [PersonalityDimension.EI] = 0.9, [PersonalityDimension.SN] = 0.9, [PersonalityDimension.TF] = 0.6, [PersonalityDimension.JP] = -0.8 },
            ["INFJ"] = new() { [PersonalityDimension.EI] = -0.7, [PersonalityDimension.SN] = 0.8, [PersonalityDimension.TF] = -0.9, [PersonalityDimension.JP] = 0.7 },
            ["INFP"] = new() { [PersonalityDimension.EI] = -0.8, [PersonalityDimension.SN] = 0.7, [PersonalityDimension.TF] = -0.8, [PersonalityDimension.JP] = -0.9 },
            ["ENFJ"] = new() { [PersonalityDimension.EI] = 0.9, [PersonalityDimension.SN] = 0.6, [PersonalityDimension.TF] = -0.9, [PersonalityDimension.JP] = 0.8 },
            ["ENFP"] = new() { [PersonalityDimension.EI] = 0.8, [PersonalityDimension.SN] = 0.9, [PersonalityDimension.TF] = -0.7, [PersonalityDimension.JP] = -0.8 },
            ["ISTJ"] = new() { [PersonalityDimension.EI] = -0.8, [PersonalityDimension.SN] = -0.9, [PersonalityDimension.TF] = 0.7, [PersonalityDimension.JP] = 0.9 },
            ["ISFJ"] = new() { [PersonalityDimension.EI] = -0.7, [PersonalityDimension.SN] = -0.8, [PersonalityDimension.TF] = -0.8, [PersonalityDimension.JP] = 0.8 },
            ["ESTJ"] = new() { [PersonalityDimension.EI] = 0.9, [PersonalityDimension.SN] = -0.7, [PersonalityDimension.TF] = 0.8, [PersonalityDimension.JP] = 0.9 },
            ["ESFJ"] = new() { [PersonalityDimension.EI] = 0.8, [PersonalityDimension.SN] = -0.6, [PersonalityDimension.TF] = -0.9, [PersonalityDimension.JP] = 0.7 },
            ["ISTP"] = new() { [PersonalityDimension.EI] = -0.9, [PersonalityDimension.SN] = -0.8, [PersonalityDimension.TF] = 0.8, [PersonalityDimension.JP] = -0.9 },
            ["ISFP"] = new() { [PersonalityDimension.EI] = -0.8, [PersonalityDimension.SN] = -0.7, [PersonalityDimension.TF] = -0.8, [PersonalityDimension.JP] = -0.8 },
            ["ESTP"] = new() { [PersonalityDimension.EI] = 0.9, [PersonalityDimension.SN] = -0.9, [PersonalityDimension.TF] = 0.7, [PersonalityDimension.JP] = -0.9 },
            ["ESFP"] = new() { [PersonalityDimension.EI] = 0.8, [PersonalityDimension.SN] = -0.8, [PersonalityDimension.TF] = -0.7, [PersonalityDimension.JP] = -0.8 }
        };

        public CareerRecommendationService(
            IPersonalityRepository personalityRepository,
            ICareerMatchingService careerMatchingService,
            IMemoryCache memoryCache,
            ILogger<CareerRecommendationService> logger)
        {
            _personalityRepository = personalityRepository;
            _careerMatchingService = careerMatchingService;
            _memoryCache = memoryCache;
            _logger = logger;
        }

        public async Task<CareerRecommendationResult> GetPersonalizedRecommendationsAsync(
            string personalityType, 
            Dictionary<int, int> clusterRatings,
            string language = "en",
            int limit = 10)
        {
            try
            {
                var cacheKey = $"recommendations_{personalityType}_{string.Join(",", clusterRatings.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}_{language}_{limit}";
                
                if (_memoryCache.TryGetValue(cacheKey, out CareerRecommendationResult? cachedResult) && cachedResult != null)
                {
                    return cachedResult;
                }

                var careers = await _personalityRepository.GetCareersAsync();
                var activeCareers = careers.Where(c => c.IsActive).ToList();
                
                var personalityWeights = _personalityWeights.ContainsKey(personalityType) 
                    ? _personalityWeights[personalityType] 
                    : new Dictionary<PersonalityDimension, double>();

                var recommendations = new List<CareerRecommendation>();

                foreach (var career in activeCareers)
                {
                    var recommendation = await CalculateCareerRecommendationAsync(
                        career, personalityType, personalityWeights, clusterRatings, language);
                    
                    if (recommendation != null)
                    {
                        recommendations.Add(recommendation);
                    }
                }

                var topRecommendations = recommendations
                    .OrderByDescending(r => r.OverallScore)
                    .Take(limit)
                    .ToList();

                var result = new CareerRecommendationResult
                {
                    Recommendations = topRecommendations,
                    PersonalityDimensionScores = personalityWeights.ToDictionary(
                        kvp => kvp.Key.ToString(), 
                        kvp => kvp.Value),
                    ClusterAffinityScores = CalculateClusterAffinityScores(clusterRatings),
                    RecommendationStrategy = "PersonalityClusterHybrid",
                    GeneratedAt = DateTime.UtcNow
                };

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15),
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                };
                _memoryCache.Set(cacheKey, result, cacheOptions);

                _logger.LogInformation("Generated {Count} personalized career recommendations for {PersonalityType}", 
                    topRecommendations.Count, personalityType);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating personalized recommendations for {PersonalityType}", personalityType);
                throw;
            }
        }

        private async Task<CareerRecommendation?> CalculateCareerRecommendationAsync(
            Career career, 
            string personalityType, 
            Dictionary<PersonalityDimension, double> personalityWeights,
            Dictionary<int, int> clusterRatings,
            string language)
        {
            try
            {
                var personalityMatchScore = CalculatePersonalityMatchScore(personalityWeights);
                var clusterAffinityScore = CalculateClusterAffinityScore(career.ClusterId, clusterRatings);
                var onetCompatibilityScore = CalculateOnetCompatibilityScore(career, personalityType);
                var salaryScore = CalculateSalaryScore(career.AnnualSalary);
                var outlookScore = CalculateOutlookScore(career.OutlookGrowthPercentage);

                var overallScore = (personalityMatchScore * 0.35) + 
                                 (clusterAffinityScore * 0.30) + 
                                 (onetCompatibilityScore * 0.20) + 
                                 (salaryScore * 0.10) + 
                                 (outlookScore * 0.05);

                var matchingFactors = new List<string>();
                var recommendationReason = GenerateRecommendationReason(
                    personalityMatchScore, clusterAffinityScore, onetCompatibilityScore, 
                    salaryScore, outlookScore, matchingFactors, language);

                var careerDetails = await _careerMatchingService.GetCareerDetailsAsync(career.Id, language);
                var careerMatch = new CareerMatch
                {
                    CareerId = career.Id,
                    CareerNameEn = career.NameEn,
                    CareerNameAr = career.NameAr,
                    MatchScore = overallScore,
                    SsocCode = career.SsocCode,
                    DescriptionEn = career.DescriptionEn,
                    DescriptionAr = career.DescriptionAr,
                    OnetId = career.OnetId,
                    OnetJobZone = career.OnetJobZone,
                    AnnualSalary = career.AnnualSalary,
                    OutlookGrowthPercentage = career.OutlookGrowthPercentage,
                    WorkContext = career.WorkContext,
                    SkillsRequired = career.SkillsRequired,
                    EducationLevel = career.EducationLevel
                };

                return new CareerRecommendation
                {
                    CareerId = career.Id,
                    CareerName = language == "en" ? career.NameEn : career.NameAr,
                    OverallScore = overallScore,
                    PersonalityMatchScore = personalityMatchScore,
                    ClusterAffinityScore = clusterAffinityScore,
                    OnetCompatibilityScore = onetCompatibilityScore,
                    SalaryScore = salaryScore,
                    OutlookScore = outlookScore,
                    RecommendationReason = recommendationReason,
                    MatchingFactors = matchingFactors,
                    CareerDetails = careerMatch
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating recommendation for career {CareerId}", career.Id);
                return null;
            }
        }

        private double CalculatePersonalityMatchScore(Dictionary<PersonalityDimension, double> personalityWeights)
        {
            if (!personalityWeights.Any()) return 0.5;
            
            var totalWeight = personalityWeights.Values.Sum(Math.Abs);
            var normalizedScore = personalityWeights.Values.Sum() / personalityWeights.Count;
            
            return Math.Max(0.0, Math.Min(1.0, (normalizedScore + 1.0) / 2.0));
        }

        private double CalculateClusterAffinityScore(int clusterId, Dictionary<int, int> clusterRatings)
        {
            if (!clusterRatings.ContainsKey(clusterId)) return 0.5;
            
            var rating = clusterRatings[clusterId];
            return Math.Max(0.0, Math.Min(1.0, (rating - 1.0) / 4.0));
        }

        private double CalculateOnetCompatibilityScore(Career career, string personalityType)
        {
            var baseScore = 0.5;
            
            if (career.OnetJobZone > 0)
            {
                var jobZoneScore = career.OnetJobZone / 5.0;
                baseScore += (jobZoneScore - 0.5) * 0.3;
            }
            
            if (!string.IsNullOrEmpty(career.EducationLevel))
            {
                var educationScore = career.EducationLevel.ToLower() switch
                {
                    var level when level.Contains("bachelor") => 0.8,
                    var level when level.Contains("master") => 0.9,
                    var level when level.Contains("doctoral") => 1.0,
                    var level when level.Contains("associate") => 0.6,
                    var level when level.Contains("certificate") => 0.4,
                    _ => 0.5
                };
                baseScore += (educationScore - 0.5) * 0.2;
            }
            
            return Math.Max(0.0, Math.Min(1.0, baseScore));
        }

        private double CalculateSalaryScore(decimal? annualSalary)
        {
            if (!annualSalary.HasValue) return 0.5;
            
            var salary = (double)annualSalary.Value;
            var normalizedScore = Math.Log10(Math.Max(salary, 1000)) / Math.Log10(200000);
            
            return Math.Max(0.0, Math.Min(1.0, normalizedScore));
        }

        private double CalculateOutlookScore(decimal? outlookGrowthPercentage)
        {
            if (!outlookGrowthPercentage.HasValue) return 0.5;
            
            var growth = (double)outlookGrowthPercentage.Value;
            var normalizedScore = (growth + 10.0) / 20.0;
            
            return Math.Max(0.0, Math.Min(1.0, normalizedScore));
        }

        private Dictionary<int, double> CalculateClusterAffinityScores(Dictionary<int, int> clusterRatings)
        {
            return clusterRatings.ToDictionary(
                kvp => kvp.Key,
                kvp => Math.Max(0.0, Math.Min(1.0, (kvp.Value - 1.0) / 4.0))
            );
        }

        private string GenerateRecommendationReason(
            double personalityScore, double clusterScore, double onetScore,
            double salaryScore, double outlookScore, List<string> factors, string language)
        {
            var reasons = new List<string>();
            
            if (personalityScore > 0.7)
            {
                reasons.Add(language == "en" ? "Strong personality match" : "تطابق قوي مع الشخصية");
                factors.Add("personality_match");
            }
            
            if (clusterScore > 0.7)
            {
                reasons.Add(language == "en" ? "High cluster affinity" : "انجذاب عالي للمجموعة المهنية");
                factors.Add("cluster_affinity");
            }
            
            if (onetScore > 0.7)
            {
                reasons.Add(language == "en" ? "Excellent O*NET compatibility" : "توافق ممتاز مع O*NET");
                factors.Add("onet_compatibility");
            }
            
            if (salaryScore > 0.7)
            {
                reasons.Add(language == "en" ? "Competitive salary potential" : "إمكانية راتب تنافسي");
                factors.Add("salary_potential");
            }
            
            if (outlookScore > 0.7)
            {
                reasons.Add(language == "en" ? "Positive career outlook" : "توقعات مهنية إيجابية");
                factors.Add("career_outlook");
            }
            
            if (!reasons.Any())
            {
                return language == "en" ? "Moderate overall compatibility" : "توافق عام متوسط";
            }
            
            return string.Join(", ", reasons);
        }

        public async Task<List<CareerRecommendation>> GetSimilarCareersAsync(int careerId, string language = "en", int limit = 5)
        {
            try
            {
                var careers = await _personalityRepository.GetCareersAsync();
                var targetCareer = careers.FirstOrDefault(c => c.Id == careerId);
                
                if (targetCareer == null) return new List<CareerRecommendation>();
                
                var similarCareers = careers
                    .Where(c => c.Id != careerId && c.IsActive && c.ClusterId == targetCareer.ClusterId)
                    .Take(limit)
                    .ToList();
                
                var recommendations = new List<CareerRecommendation>();
                
                foreach (var career in similarCareers)
                {
                    var similarity = CalculateCareerSimilarity(targetCareer, career);
                    
                    recommendations.Add(new CareerRecommendation
                    {
                        CareerId = career.Id,
                        CareerName = language == "en" ? career.NameEn : career.NameAr,
                        OverallScore = similarity,
                        RecommendationReason = language == "en" ? "Similar career in same cluster" : "مهنة مشابهة في نفس المجموعة"
                    });
                }
                
                return recommendations.OrderByDescending(r => r.OverallScore).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting similar careers for {CareerId}", careerId);
                return new List<CareerRecommendation>();
            }
        }

        private double CalculateCareerSimilarity(Career career1, Career career2)
        {
            var similarity = 0.0;
            var factors = 0;
            
            if (career1.ClusterId == career2.ClusterId)
            {
                similarity += 0.4;
                factors++;
            }
            
            if (career1.OnetJobZone == career2.OnetJobZone && career1.OnetJobZone > 0)
            {
                similarity += 0.3;
                factors++;
            }
            
            if (career1.AnnualSalary.HasValue && career2.AnnualSalary.HasValue)
            {
                var salaryDiff = Math.Abs((double)(career1.AnnualSalary.Value - career2.AnnualSalary.Value));
                var maxSalary = Math.Max((double)career1.AnnualSalary.Value, (double)career2.AnnualSalary.Value);
                var salarySimilarity = 1.0 - (salaryDiff / maxSalary);
                similarity += salarySimilarity * 0.2;
                factors++;
            }
            
            if (!string.IsNullOrEmpty(career1.EducationLevel) && !string.IsNullOrEmpty(career2.EducationLevel))
            {
                if (career1.EducationLevel.Equals(career2.EducationLevel, StringComparison.OrdinalIgnoreCase))
                {
                    similarity += 0.1;
                }
                factors++;
            }
            
            return factors > 0 ? similarity / factors : 0.0;
        }

        public async Task<Dictionary<string, object>> GetCareerCompatibilityAnalysisAsync(
            int careerId, 
            string personalityType, 
            Dictionary<int, int> clusterRatings,
            string language = "en")
        {
            try
            {
                var careers = await _personalityRepository.GetCareersAsync();
                var career = careers.FirstOrDefault(c => c.Id == careerId);
                
                if (career == null)
                {
                    return new Dictionary<string, object> { ["error"] = "Career not found" };
                }
                
                var personalityWeights = _personalityWeights.ContainsKey(personalityType) 
                    ? _personalityWeights[personalityType] 
                    : new Dictionary<PersonalityDimension, double>();
                
                var recommendation = await CalculateCareerRecommendationAsync(
                    career, personalityType, personalityWeights, clusterRatings, language);
                
                if (recommendation == null)
                {
                    return new Dictionary<string, object> { ["error"] = "Unable to analyze compatibility" };
                }
                
                return new Dictionary<string, object>
                {
                    ["career_id"] = careerId,
                    ["career_name"] = language == "en" ? career.NameEn : career.NameAr,
                    ["overall_compatibility"] = recommendation.OverallScore,
                    ["personality_match"] = recommendation.PersonalityMatchScore,
                    ["cluster_affinity"] = recommendation.ClusterAffinityScore,
                    ["onet_compatibility"] = recommendation.OnetCompatibilityScore,
                    ["salary_score"] = recommendation.SalaryScore,
                    ["outlook_score"] = recommendation.OutlookScore,
                    ["recommendation_reason"] = recommendation.RecommendationReason,
                    ["matching_factors"] = recommendation.MatchingFactors,
                    ["compatibility_level"] = recommendation.OverallScore switch
                    {
                        >= 0.8 => language == "en" ? "Excellent" : "ممتاز",
                        >= 0.6 => language == "en" ? "Good" : "جيد",
                        >= 0.4 => language == "en" ? "Fair" : "مقبول",
                        _ => language == "en" ? "Poor" : "ضعيف"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing career compatibility for {CareerId}", careerId);
                return new Dictionary<string, object> { ["error"] = "Analysis failed" };
            }
        }

        public async Task<List<Dictionary<string, object>>> GetCareerPathwayRecommendationsAsync(
            int careerId, 
            DeploymentMode deploymentMode = DeploymentMode.STANDARD,
            string language = "en")
        {
            try
            {
                var pathways = await _personalityRepository.GetCareerPathwaysAsync(careerId);
                var recommendations = new List<Dictionary<string, object>>();
                
                foreach (var careerPathway in pathways)
                {
                    var pathway = careerPathway.Pathway;
                    
                    if (deploymentMode == DeploymentMode.STANDARD && pathway?.Source != PathwaySource.MOE)
                    {
                        continue;
                    }
                    
                    var recommendation = new Dictionary<string, object>
                    {
                        ["pathway_id"] = pathway?.Id ?? 0,
                        ["name"] = language == "en" ? (pathway?.NameEn ?? string.Empty) : (pathway?.NameAr ?? string.Empty),
                        ["description"] = language == "en" ? (pathway?.DescriptionEn ?? string.Empty) : (pathway?.DescriptionAr ?? string.Empty),
                        ["source"] = pathway?.Source.ToString() ?? "Unknown",
                        ["recommendation_score"] = CalculatePathwayRecommendationScore(pathway!),
                        ["estimated_duration"] = EstimatePathwayDuration(pathway!),
                        ["difficulty_level"] = AssessPathwayDifficulty(pathway!),
                        ["prerequisites"] = ExtractPathwayPrerequisites(pathway!, language)
                    };
                    
                    recommendations.Add(recommendation);
                }
                
                return recommendations.OrderByDescending(r => (double)r["recommendation_score"]).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pathway recommendations for career {CareerId}", careerId);
                return new List<Dictionary<string, object>>();
            }
        }

        private double CalculatePathwayRecommendationScore(Pathway pathway)
        {
            var score = 0.5;
            
            if (pathway.Source == PathwaySource.MOE)
            {
                score += 0.3;
            }
            else if (pathway.Source == PathwaySource.MAWHIBA)
            {
                score += 0.2;
            }
            
            if (!string.IsNullOrEmpty(pathway.DescriptionEn) && pathway.DescriptionEn.Length > 50)
            {
                score += 0.1;
            }
            
            return Math.Max(0.0, Math.Min(1.0, score));
        }

        private string EstimatePathwayDuration(Pathway pathway)
        {
            if (pathway.Source == PathwaySource.MOE)
            {
                return "2-4 years";
            }
            else if (pathway.Source == PathwaySource.MAWHIBA)
            {
                return "1-2 years";
            }
            
            return "Variable";
        }

        private string AssessPathwayDifficulty(Pathway pathway)
        {
            if (pathway.Source == PathwaySource.MOE)
            {
                return "Moderate to High";
            }
            else if (pathway.Source == PathwaySource.MAWHIBA)
            {
                return "High";
            }
            
            return "Variable";
        }

        private List<string> ExtractPathwayPrerequisites(Pathway pathway, string language)
        {
            var prerequisites = new List<string>();
            
            if (pathway.Source == PathwaySource.MOE)
            {
                prerequisites.Add(language == "en" ? "High school diploma" : "شهادة الثانوية العامة");
                prerequisites.Add(language == "en" ? "Minimum GPA requirements" : "متطلبات الحد الأدنى للمعدل");
            }
            else if (pathway.Source == PathwaySource.MAWHIBA)
            {
                prerequisites.Add(language == "en" ? "Exceptional academic performance" : "أداء أكاديمي استثنائي");
                prerequisites.Add(language == "en" ? "Specialized aptitude tests" : "اختبارات القدرات المتخصصة");
            }
            
            return prerequisites;
        }
    }
}
