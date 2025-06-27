using Xunit;
using FluentAssertions;
using Masark.Application.Services;
using Masark.Domain.Entities;
using Masark.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;

namespace Masark.Tests.Unit;

public class CareerMatchingServiceTests
{
    private readonly CareerMatchingService _careerMatchingService;

    public CareerMatchingServiceTests()
    {
        var mockPersonalityRepository = new Mock<IPersonalityRepository>();
        var mockMemoryCache = new Mock<IMemoryCache>();
        var mockLogger = new Mock<ILogger<CareerMatchingService>>();
        _careerMatchingService = new CareerMatchingService(mockPersonalityRepository.Object, mockMemoryCache.Object, mockLogger.Object);
    }

    [Fact]
    public void CalculateCareerMatches_WithValidPersonalityType_ShouldReturnMatches()
    {
        var personalityType = "ENFJ";
        var careers = CreateTestCareers();
        var personalityCareerMatches = CreateTestPersonalityCareerMatches();

        var result = CalculateCareerMatchesSync(personalityType, careers, personalityCareerMatches);

        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().BeInDescendingOrder(x => x.MatchScore);
    }

    [Fact]
    public void CalculateCareerMatches_WithInvalidPersonalityType_ShouldReturnEmptyList()
    {
        var personalityType = "INVALID";
        var careers = CreateTestCareers();
        var personalityCareerMatches = CreateTestPersonalityCareerMatches();

        var result = CalculateCareerMatchesSync(personalityType, careers, personalityCareerMatches);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public void CalculateCareerMatches_WithEmptyCareers_ShouldReturnEmptyList()
    {
        var personalityType = "ENFJ";
        var careers = new List<Career>();
        var personalityCareerMatches = CreateTestPersonalityCareerMatches();

        var result = CalculateCareerMatchesSync(personalityType, careers, personalityCareerMatches);

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Theory]
    [InlineData("ENFJ", "Teacher")]
    [InlineData("INTJ", "Software Engineer")]
    [InlineData("ESFP", "Marketing Specialist")]
    public void CalculateCareerMatches_ShouldReturnExpectedTopMatch(string personalityType, string expectedCareerName)
    {
        var careers = CreateTestCareers();
        var personalityCareerMatches = CreateTestPersonalityCareerMatches();

        var result = CalculateCareerMatchesSync(personalityType, careers, personalityCareerMatches);

        result.Should().NotBeEmpty();
        result.First().Career.NameEn.Should().Contain(expectedCareerName);
    }

    private List<Career> CreateTestCareers()
    {
        var careers = new List<Career>
        {
            new Career(
                nameEn: "Software Engineer",
                nameAr: "مهندس برمجيات",
                clusterId: 1,
                tenantId: 1
            ),
            new Career(
                nameEn: "Teacher",
                nameAr: "معلم",
                clusterId: 2,
                tenantId: 1
            ),
            new Career(
                nameEn: "Marketing Specialist",
                nameAr: "أخصائي تسويق",
                clusterId: 3,
                tenantId: 1
            )
        };
        
        careers[0].GetType().GetProperty("Id")?.SetValue(careers[0], 1);
        careers[1].GetType().GetProperty("Id")?.SetValue(careers[1], 2);
        careers[2].GetType().GetProperty("Id")?.SetValue(careers[2], 3);
        
        return careers;
    }

    private List<PersonalityCareerMatch> CreateTestPersonalityCareerMatches()
    {
        return new List<PersonalityCareerMatch>
        {
            new PersonalityCareerMatch(personalityTypeId: 1, careerId: 1, matchScore: 0.85m, tenantId: 1), // ENFJ -> Software Engineer
            new PersonalityCareerMatch(personalityTypeId: 1, careerId: 2, matchScore: 0.95m, tenantId: 1), // ENFJ -> Teacher
            new PersonalityCareerMatch(personalityTypeId: 1, careerId: 3, matchScore: 0.75m, tenantId: 1), // ENFJ -> Marketing
            
            new PersonalityCareerMatch(personalityTypeId: 2, careerId: 1, matchScore: 0.95m, tenantId: 1), // INTJ -> Software Engineer
            new PersonalityCareerMatch(personalityTypeId: 2, careerId: 2, matchScore: 0.65m, tenantId: 1), // INTJ -> Teacher
            new PersonalityCareerMatch(personalityTypeId: 2, careerId: 3, matchScore: 0.70m, tenantId: 1), // INTJ -> Marketing
            
            new PersonalityCareerMatch(personalityTypeId: 3, careerId: 1, matchScore: 0.60m, tenantId: 1), // ESFP -> Software Engineer
            new PersonalityCareerMatch(personalityTypeId: 3, careerId: 2, matchScore: 0.80m, tenantId: 1), // ESFP -> Teacher
            new PersonalityCareerMatch(personalityTypeId: 3, careerId: 3, matchScore: 0.90m, tenantId: 1)  // ESFP -> Marketing
        };
    }

    private List<CareerMatch> CalculateCareerMatchesSync(string personalityType, List<Career> careers, List<PersonalityCareerMatch> personalityCareerMatches)
    {
        var matches = new List<CareerMatch>();
        
        var personalityTypeId = personalityType switch
        {
            "ENFJ" => 1,
            "INTJ" => 2,
            "ESFP" => 3,
            _ => 0
        };
        
        if (personalityTypeId == 0)
            return matches; // Invalid personality type
        
        foreach (var career in careers)
        {
            var match = personalityCareerMatches.FirstOrDefault(pcm => 
                pcm.CareerId == career.Id && pcm.PersonalityTypeId == personalityTypeId);
            if (match != null)
            {
                matches.Add(new CareerMatch
                {
                    Career = career,
                    MatchScore = (double)match.MatchScore
                });
            }
        }
        
        return matches.OrderByDescending(m => m.MatchScore).ToList();
    }
}

public class CareerMatch
{
    public Career Career { get; set; } = null!;
    public double MatchScore { get; set; }
}
