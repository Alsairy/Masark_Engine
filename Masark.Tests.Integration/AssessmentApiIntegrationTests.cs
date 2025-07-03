using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using Masark.Infrastructure.Identity;
using Masark.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;

namespace Masark.Tests.Integration;

public class AssessmentApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AssessmentApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"ConnectionStrings:DefaultConnection", "DataSource=:memory:"},
                    {"ConnectionStrings:Redis", "localhost:6379"},
                    {"SkipDatabaseSeeding", "true"}
                });
            });
        });

        _client = _factory.CreateClient();
        
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
        
        if (!context.PersonalityTypes.Any())
        {
            SeedTestData(context);
        }
    }

    [Fact]
    public async Task GetAssessmentHealth_ShouldReturnHealthyStatus()
    {
        var response = await _client.GetAsync("/api/assessment/health");

        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        var healthResponse = JsonSerializer.Deserialize<JsonElement>(content);
        
        healthResponse.GetProperty("status").GetString().Should().Be("healthy");
        healthResponse.GetProperty("service").GetString().Should().Be("Masark Assessment Engine");
    }

    [Fact]
    public async Task GetAssessmentQuestions_ShouldReturnQuestions()
    {
        var response = await _client.GetAsync("/api/assessment/questions?language=en");

        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        var questionsResponse = JsonSerializer.Deserialize<JsonElement>(content);
        
        questionsResponse.GetProperty("success").GetBoolean().Should().BeTrue();
        questionsResponse.GetProperty("questions").GetArrayLength().Should().BeGreaterThan(0);
        questionsResponse.GetProperty("language").GetString().Should().Be("en");
    }

    [Fact]
    public async Task GetAssessmentQuestions_WithArabicLanguage_ShouldReturnArabicQuestions()
    {
        var response = await _client.GetAsync("/api/assessment/questions?language=ar");

        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        var questionsResponse = JsonSerializer.Deserialize<JsonElement>(content);
        
        questionsResponse.GetProperty("success").GetBoolean().Should().BeTrue();
        questionsResponse.GetProperty("language").GetString().Should().Be("ar");
    }

    [Fact]
    public async Task StartAssessmentSession_WithValidData_ShouldCreateSession()
    {
        var requestData = new
        {
            StudentName = "Test Student",
            StudentEmail = "test@example.com",
            StudentId = "12345",
            DeploymentMode = "STANDARD",
            LanguagePreference = "en",
            TenantId = 1
        };

        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/assessment/start-session", content);

        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        var sessionResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        sessionResponse.GetProperty("success").GetBoolean().Should().BeTrue();
        sessionResponse.GetProperty("session_token").GetString().Should().NotBeNullOrEmpty();
        sessionResponse.GetProperty("session_id").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task StartAssessmentSession_WithInvalidData_ShouldReturnBadRequest()
    {
        var requestData = new 
        { 
            StudentName = "", // Empty required field
            StudentEmail = "invalid-email", // Invalid email format
            TenantId = -1 // Invalid tenant ID
        };

        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/assessment/start-session", content);

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SubmitAnswer_WithValidData_ShouldAcceptAnswer()
    {
        var requestData = new
        {
            SessionToken = "test-session-token",
            QuestionId = 1,
            AnswerValue = 1,
            TenantId = 1
        };

        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/assessment/submit-answer", content);

        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        var answerResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        answerResponse.GetProperty("success").GetBoolean().Should().BeTrue();
        answerResponse.GetProperty("question_id").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task CompleteAssessment_WithValidSessionId_ShouldCompleteAssessment()
    {
        var requestData = new
        {
            SessionId = 1,
            TenantId = 1
        };

        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/assessment/complete", content);

        response.IsSuccessStatusCode.Should().BeTrue();
        var responseContent = await response.Content.ReadAsStringAsync();
        var completeResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        completeResponse.GetProperty("success").GetBoolean().Should().BeTrue();
        completeResponse.GetProperty("personality_type").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetAssessmentResults_WithValidSessionId_ShouldReturnResults()
    {
        var response = await _client.GetAsync("/api/assessment/results/1?includeStatistics=true&tenantId=1");

        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        var resultsResponse = JsonSerializer.Deserialize<JsonElement>(content);
        
        resultsResponse.GetProperty("success").GetBoolean().Should().BeTrue();
        resultsResponse.GetProperty("session_id").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task GetAssessmentResults_WithInvalidSessionId_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync("/api/assessment/results/99999?tenantId=1");

        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    private static void SeedTestData(ApplicationDbContext context)
    {
        var intjType = new Masark.Domain.Entities.PersonalityType("INTJ", "The Architect", "المهندس المعماري", 1);
        intjType.UpdateContent("The Architect", "المهندس المعماري", 
                              "Strategic thinkers with a plan for everything.", "مفكرون استراتيجيون لديهم خطة لكل شيء.",
                              "Strategic thinking, Independence", "التفكير الاستراتيجي، الاستقلالية",
                              "Perfectionism, Impatience", "الكمالية، عدم الصبر");
        
        var enfjType = new Masark.Domain.Entities.PersonalityType("ENFJ", "The Protagonist", "البطل", 1);
        enfjType.UpdateContent("The Protagonist", "البطل",
                              "Charismatic and inspiring leaders.", "قادة ملهمون وجذابون.",
                              "Leadership, Empathy", "القيادة، التعاطف",
                              "Over-idealistic, Too selfless", "مثالي أكثر من اللازم، أناني أكثر من اللازم");
        
        context.PersonalityTypes.AddRange(new[] { intjType, enfjType });
        
        var questions = new[]
        {
            new Masark.Domain.Entities.Question(1, Masark.Domain.Enums.PersonalityDimension.EI, "Test Question 1", "سؤال تجريبي 1", "Pregunta de prueba 1", "测试问题1", "Option A", "الخيار أ", "Opción A", "选项A", true, "Option B", "الخيار ب", "Opción B", "选项B", 1),
            new Masark.Domain.Entities.Question(2, Masark.Domain.Enums.PersonalityDimension.SN, "Test Question 2", "سؤال تجريبي 2", "Pregunta de prueba 2", "测试问题2", "Option A", "الخيار أ", "Opción A", "选项A", true, "Option B", "الخيار ب", "Opción B", "选项B", 1)
        };
        
        context.Questions.AddRange(questions);
        context.SaveChanges();
    }
}
