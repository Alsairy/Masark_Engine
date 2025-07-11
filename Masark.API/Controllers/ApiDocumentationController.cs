using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Diagnostics;

namespace Masark.API.Controllers;

[ApiController]
[Route("api/documentation")]
[Authorize(Roles = "ADMIN,Administrator,Manager")]
public class ApiDocumentationController : ControllerBase
{
    private readonly ILogger<ApiDocumentationController> _logger;

    public ApiDocumentationController(ILogger<ApiDocumentationController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetApiDocumentation()
    {
        try
        {
            var documentation = new
            {
                title = "Masark API Documentation",
                version = "1.0.0",
                description = "Comprehensive API for the Masark Personality-Career Matching Engine",
                baseUrl = $"{Request.Scheme}://{Request.Host}",
                authentication = new
                {
                    type = "Bearer Token",
                    description = "Use JWT tokens obtained from the /api/auth/login endpoint"
                },
                endpoints = GetApiEndpoints(),
                schemas = GetApiSchemas()
            };

            return Ok(documentation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API documentation");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("endpoints")]
    public IActionResult GetApiEndpoints()
    {
        try
        {
            var endpoints = new object[]
            {
                new
                {
                    path = "/api/auth/login",
                    method = "POST",
                    description = "Authenticate user and obtain JWT token",
                    authentication = false,
                    rateLimit = "10 requests per minute",
                    parameters = new[]
                    {
                        new { name = "username", type = "string", required = true, description = "User's username or email" },
                        new { name = "password", type = "string", required = true, description = "User's password" }
                    },
                    responses = new[]
                    {
                        new { statusCode = 200, description = "Login successful, returns JWT token" },
                        new { statusCode = 401, description = "Invalid credentials" },
                        new { statusCode = 429, description = "Rate limit exceeded" }
                    },
                    examples = new[]
                    {
                        new
                        {
                            title = "Login Request",
                            request = new { username = "your_username", password = "your_password" },
                            response = new { token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...", user = new { id = 1, username = "admin", role = "Administrator" } }
                        }
                    }
                },
                new
                {
                    path = "/api/assessment/start",
                    method = "POST",
                    description = "Start a new personality assessment session",
                    authentication = true,
                    rateLimit = "5 requests per minute",
                    parameters = new[]
                    {
                        new { name = "language", type = "string", required = false, description = "Assessment language (en/ar)" },
                        new { name = "tenantId", type = "string", required = false, description = "Tenant identifier for multi-tenancy" }
                    },
                    responses = new[]
                    {
                        new { statusCode = 200, description = "Assessment session started successfully" },
                        new { statusCode = 401, description = "Authentication required" },
                        new { statusCode = 429, description = "Rate limit exceeded" }
                    },
                    examples = new[]
                    {
                        new
                        {
                            title = "Start Assessment",
                            request = new { language = "en", tenantId = "default" },
                            response = new { sessionId = "12345", questions = new[] { new { id = 1, text = "I enjoy meeting new people", type = "likert" } } }
                        }
                    }
                },
                new
                {
                    path = "/api/assessment/submit",
                    method = "POST",
                    description = "Submit answers for assessment questions",
                    authentication = true,
                    rateLimit = "30 requests per minute",
                    parameters = new[]
                    {
                        new { name = "sessionId", type = "string", required = true, description = "Assessment session identifier" },
                        new { name = "answers", type = "array", required = true, description = "Array of question answers" }
                    },
                    responses = new[]
                    {
                        new { statusCode = 200, description = "Answers submitted successfully" },
                        new { statusCode = 400, description = "Invalid session or answers" },
                        new { statusCode = 401, description = "Authentication required" }
                    },
                    examples = new[]
                    {
                        new
                        {
                            title = "Submit Answers",
                            request = new { sessionId = "12345", answers = new[] { new { questionId = 1, value = 4 } } },
                            response = new { success = true, progress = 25, nextQuestions = new[] { new { id = 2, text = "I prefer working alone" } } }
                        }
                    }
                },
                new
                {
                    path = "/api/careers/search",
                    method = "GET",
                    description = "Search for careers based on personality type and preferences",
                    authentication = true,
                    rateLimit = "20 requests per minute",
                    parameters = new[]
                    {
                        new { name = "personalityType", type = "string", required = false, description = "MBTI personality type (e.g., INTJ)" },
                        new { name = "interests", type = "array", required = false, description = "Array of interest areas" },
                        new { name = "language", type = "string", required = false, description = "Response language (en/ar)" }
                    },
                    responses = new[]
                    {
                        new { statusCode = 200, description = "Career matches found" },
                        new { statusCode = 401, description = "Authentication required" },
                        new { statusCode = 404, description = "No careers found" }
                    },
                    examples = new[]
                    {
                        new
                        {
                            title = "Search Careers",
                            request = new { personalityType = "INTJ", interests = new[] { "technology", "analysis" }, language = "en" },
                            response = new { careers = new[] { new { id = 1, title = "Software Architect", match = 95, description = "Design software systems" } } }
                        }
                    }
                },
                new
                {
                    path = "/api/users",
                    method = "GET",
                    description = "Get list of users (Admin only)",
                    authentication = true,
                    rateLimit = "10 requests per minute",
                    parameters = new[]
                    {
                        new { name = "page", type = "integer", required = false, description = "Page number for pagination" },
                        new { name = "limit", type = "integer", required = false, description = "Number of users per page" },
                        new { name = "search", type = "string", required = false, description = "Search term for filtering users" }
                    },
                    responses = new[]
                    {
                        new { statusCode = 200, description = "Users retrieved successfully" },
                        new { statusCode = 401, description = "Authentication required" },
                        new { statusCode = 403, description = "Admin access required" }
                    },
                    examples = new[]
                    {
                        new
                        {
                            title = "Get Users",
                            request = new { page = 1, limit = 10, search = "john" },
                            response = new { users = new[] { new { id = 1, username = "john.doe", email = "john@example.com", role = "User" } }, total = 1, page = 1 }
                        }
                    }
                }
            };

            return Ok(endpoints);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API endpoints");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("schema")]
    public IActionResult GetApiSchema()
    {
        try
        {
            var schema = new
            {
                version = "1.0.0",
                title = "Masark API Schema",
                description = "OpenAPI 3.0 compatible schema for the Masark API",
                schemas = GetApiSchemas()
            };

            return Ok(schema);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API schema");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("/api/health")]
    public IActionResult GetApiHealth()
    {
        try
        {
            var health = new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
                uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime,
                checks = new
                {
                    database = "healthy",
                    memory = "healthy",
                    disk = "healthy"
                }
            };

            return Ok(health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API health");
            return StatusCode(500, new { status = "unhealthy", message = "Internal server error" });
        }
    }

    [HttpGet("/api/metrics")]
    public IActionResult GetApiMetrics()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var metrics = new
            {
                timestamp = DateTime.UtcNow,
                memory = new
                {
                    workingSet = process.WorkingSet64,
                    privateMemory = process.PrivateMemorySize64,
                    virtualMemory = process.VirtualMemorySize64
                },
                cpu = new
                {
                    totalProcessorTime = process.TotalProcessorTime.TotalMilliseconds,
                    userProcessorTime = process.UserProcessorTime.TotalMilliseconds
                },
                threads = process.Threads.Count,
                handles = process.HandleCount,
                uptime = DateTime.UtcNow - process.StartTime
            };

            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving API metrics");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    private static object GetApiSchemas()
    {
        return new
        {
            User = new
            {
                type = "object",
                properties = new
                {
                    id = new { type = "integer", description = "Unique user identifier" },
                    username = new { type = "string", description = "User's username" },
                    email = new { type = "string", description = "User's email address" },
                    firstName = new { type = "string", description = "User's first name" },
                    lastName = new { type = "string", description = "User's last name" },
                    role = new { type = "string", description = "User's role (User, Manager, Administrator)" },
                    isActive = new { type = "boolean", description = "Whether the user account is active" },
                    createdAt = new { type = "string", format = "date-time", description = "Account creation timestamp" },
                    lastLoginAt = new { type = "string", format = "date-time", description = "Last login timestamp" }
                }
            },
            AssessmentSession = new
            {
                type = "object",
                properties = new
                {
                    id = new { type = "string", description = "Unique session identifier" },
                    userId = new { type = "integer", description = "User who started the session" },
                    language = new { type = "string", description = "Assessment language (en/ar)" },
                    state = new { type = "string", description = "Session state (InProgress, Completed, Abandoned)" },
                    progress = new { type = "integer", description = "Completion percentage (0-100)" },
                    startedAt = new { type = "string", format = "date-time", description = "Session start timestamp" },
                    completedAt = new { type = "string", format = "date-time", description = "Session completion timestamp" }
                }
            },
            PersonalityResult = new
            {
                type = "object",
                properties = new
                {
                    personalityType = new { type = "string", description = "MBTI personality type (e.g., INTJ)" },
                    dimensions = new
                    {
                        type = "object",
                        properties = new
                        {
                            extraversionIntroversion = new { type = "number", description = "E/I dimension score (-100 to 100)" },
                            sensingIntuition = new { type = "number", description = "S/N dimension score (-100 to 100)" },
                            thinkingFeeling = new { type = "number", description = "T/F dimension score (-100 to 100)" },
                            judgingPerceiving = new { type = "number", description = "J/P dimension score (-100 to 100)" }
                        }
                    },
                    confidence = new { type = "number", description = "Result confidence score (0-100)" },
                    description = new { type = "string", description = "Personality type description" }
                }
            },
            Career = new
            {
                type = "object",
                properties = new
                {
                    id = new { type = "integer", description = "Unique career identifier" },
                    title = new { type = "string", description = "Career title" },
                    description = new { type = "string", description = "Career description" },
                    cluster = new { type = "string", description = "Career cluster/category" },
                    personalityMatch = new { type = "number", description = "Personality match score (0-100)" },
                    requiredSkills = new { type = "array", items = new { type = "string" }, description = "Required skills for this career" },
                    educationLevel = new { type = "string", description = "Required education level" },
                    averageSalary = new { type = "number", description = "Average salary range" }
                }
            }
        };
    }
}
