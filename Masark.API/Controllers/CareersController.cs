using Microsoft.AspNetCore.Mvc;
using MediatR;
using Masark.Application.Queries.Assessment;
using Masark.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Masark.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CareersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CareersController> _logger;

        public CareersController(IMediator mediator, ILogger<CareersController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpPost("match")]
        public async Task<IActionResult> GetCareerMatches([FromBody] CareerMatchRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Invalid request data",
                        details = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                var deploymentMode = Enum.TryParse<DeploymentMode>(request.DeploymentMode?.ToUpper(), out var mode) ? mode : DeploymentMode.STANDARD;
                var language = request.Language?.ToLower() ?? "en";
                var limit = Math.Min(request.Limit ?? 10, 50);

                if (!new[] { "en", "ar" }.Contains(language))
                {
                    language = "en";
                }

                var query = new GetCareerMatchesQuery
                {
                    PersonalityType = request.PersonalityType?.ToUpper() ?? "INTJ",
                    Language = language,
                    Limit = limit,
                    TenantId = request.TenantId ?? 1
                };

                var result = await _mediator.Send(query);

                if (result.Success)
                {
                    var matchesData = result.Matches.Select(match => new
                    {
                        career_id = match.CareerId,
                        name = match.Career?.GetName(language) ?? "Unknown Career",
                        description = match.Career?.GetDescription(language) ?? "No description available",
                        match_score = Math.Round(match.MatchScore * 100, 1),
                        cluster = new
                        {
                            name = match.Career?.Cluster?.GetName(language) ?? "Unknown Cluster"
                        },
                        ssoc_code = match.Career?.SsocCode ?? "",
                        programs = new object[0],
                        pathways = new object[0]
                    });

                    return Ok(new
                    {
                        success = true,
                        personality_type = request.PersonalityType?.ToUpper() ?? "INTJ",
                        deployment_mode = deploymentMode.ToString(),
                        language = language,
                        total_matches = result.TotalCount,
                        matches = matchesData,
                        cached = false,
                        generated_at = DateTime.UtcNow.ToString("O")
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = result.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting career matches");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get career matches",
                    message = ex.Message
                });
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchCareers([FromQuery] string q, [FromQuery] string language = "en", [FromQuery] int limit = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(q))
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Search query (q) is required"
                    });
                }

                language = language?.ToLower() ?? "en";
                if (!new[] { "en", "ar" }.Contains(language))
                {
                    language = "en";
                }

                limit = Math.Min(limit, 100);

                return Ok(new
                {
                    success = true,
                    query = q.Trim(),
                    language = language,
                    total_results = 0,
                    careers = new object[0]
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching careers");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to search careers",
                    message = ex.Message
                });
            }
        }

        [HttpGet("{careerId}")]
        public async Task<IActionResult> GetCareerDetails(int careerId, [FromQuery] string language = "en")
        {
            try
            {
                language = language?.ToLower() ?? "en";
                if (!new[] { "en", "ar" }.Contains(language))
                {
                    language = "en";
                }

                return NotFound(new
                {
                    success = false,
                    error = "Career not found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting career details for {CareerId}", careerId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get career details",
                    message = ex.Message
                });
            }
        }

        [HttpGet("clusters/{clusterId}/careers")]
        public async Task<IActionResult> GetCareersByCluster(int clusterId, [FromQuery] string language = "en")
        {
            try
            {
                language = language?.ToLower() ?? "en";
                if (!new[] { "en", "ar" }.Contains(language))
                {
                    language = "en";
                }

                return NotFound(new
                {
                    success = false,
                    error = "Career cluster not found"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting careers for cluster {ClusterId}", clusterId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get careers for cluster",
                    message = ex.Message
                });
            }
        }

        [HttpGet("clusters")]
        public async Task<IActionResult> GetAllClusters([FromQuery] string language = "en")
        {
            try
            {
                language = language?.ToLower() ?? "en";
                if (!new[] { "en", "ar" }.Contains(language))
                {
                    language = "en";
                }

                return Ok(new
                {
                    success = true,
                    clusters = new object[0],
                    total_clusters = 0,
                    language = language
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting career clusters");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get career clusters",
                    message = ex.Message
                });
            }
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetCareerStats()
        {
            try
            {
                return Ok(new
                {
                    success = true,
                    statistics = new
                    {
                        total_careers = 0,
                        total_clusters = 0,
                        cluster_breakdown = new object[0],
                        cache_stats = new { }
                    },
                    generated_at = DateTime.UtcNow.ToString("O")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting career stats");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get career statistics",
                    message = ex.Message
                });
            }
        }

        [HttpGet("cluster-ratings")]
        public async Task<IActionResult> GetCareerClusterRatings([FromQuery] string language = "en")
        {
            try
            {
                language = language?.ToLower() ?? "en";
                if (!new[] { "en", "ar" }.Contains(language))
                {
                    language = "en";
                }

                var ratings = new[]
                {
                    new { id = 1, value = 1, description = language == "ar" ? "غير مهتم على الإطلاق" : "Not interested at all" },
                    new { id = 2, value = 2, description = language == "ar" ? "غير مهتم قليلاً" : "Slightly not interested" },
                    new { id = 3, value = 3, description = language == "ar" ? "محايد" : "Neutral" },
                    new { id = 4, value = 4, description = language == "ar" ? "مهتم قليلاً" : "Slightly interested" },
                    new { id = 5, value = 5, description = language == "ar" ? "مهتم جداً" : "Very interested" }
                };

                return Ok(new
                {
                    success = true,
                    career_cluster_ratings = ratings,
                    language = language,
                    total_ratings = ratings.Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting career cluster ratings");
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get career cluster ratings",
                    message = ex.Message
                });
            }
        }

        [HttpPost("assessments/{assessmentId}/career-cluster-ratings")]
        public async Task<IActionResult> RateCareerCluster(int assessmentId, [FromBody] CareerClusterRatingRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "Invalid request data",
                        details = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                if (request.CareerClusterId <= 0)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "career_cluster is required"
                    });
                }

                if (request.CareerClusterRatingId < 1 || request.CareerClusterRatingId > 5)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = "career_cluster_rating must be between 1 and 5"
                    });
                }

                return Ok(new
                {
                    success = true,
                    career_cluster_user_rating = new
                    {
                        id = new Random().Next(1000, 9999),
                        assessment = new { id = assessmentId },
                        career_cluster = new { id = request.CareerClusterId },
                        career_cluster_rating = new { id = request.CareerClusterRatingId, value = request.CareerClusterRatingId },
                        created_at = DateTime.UtcNow.ToString("O")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rating career cluster for assessment {AssessmentId}", assessmentId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to rate career cluster",
                    message = ex.Message
                });
            }
        }

        [HttpGet("assessments/{assessmentId}/career-cluster-ratings")]
        public async Task<IActionResult> GetAssessmentCareerClusterRatings(int assessmentId)
        {
            try
            {
                return Ok(new
                {
                    success = true,
                    assessment_id = assessmentId,
                    career_cluster_user_ratings = new object[0],
                    total_ratings = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting career cluster ratings for assessment {AssessmentId}", assessmentId);
                return StatusCode(500, new
                {
                    success = false,
                    error = "Failed to get career cluster ratings",
                    message = ex.Message
                });
            }
        }
    }

    public class CareerMatchRequest
    {
        public string? SessionToken { get; set; }
        public string? PersonalityType { get; set; }
        public string? DeploymentMode { get; set; } = "STANDARD";
        public string? Language { get; set; } = "en";
        public int? Limit { get; set; } = 10;
        public int? TenantId { get; set; }
    }

    public class CareerClusterRatingRequest
    {
        [Required]
        public int CareerClusterId { get; set; }

        [Required]
        [Range(1, 5)]
        public int CareerClusterRatingId { get; set; }
    }
}
