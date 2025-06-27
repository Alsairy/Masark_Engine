using Xunit;
using FluentAssertions;
using Masark.Application.Services;
using Masark.Domain.Entities;
using Masark.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;

namespace Masark.Tests.Unit;

public class PersonalityScoringServiceTests
{
    private readonly PersonalityScoringService _scoringService;

    public PersonalityScoringServiceTests()
    {
        var mockLogger = new Mock<ILogger<PersonalityScoringService>>();
        _scoringService = new PersonalityScoringService(mockLogger.Object);
    }

    [Fact]
    public void CalculatePersonalityType_WithBalancedAnswers_ShouldReturnCorrectType()
    {
        var answers = new List<AssessmentAnswer>
        {
            new AssessmentAnswer(sessionId: 1, questionId: 1, selectedOption: "A", tenantId: 1),
            new AssessmentAnswer(sessionId: 1, questionId: 2, selectedOption: "B", tenantId: 1),
            new AssessmentAnswer(sessionId: 1, questionId: 3, selectedOption: "A", tenantId: 1),
            new AssessmentAnswer(sessionId: 1, questionId: 4, selectedOption: "B", tenantId: 1),
            new AssessmentAnswer(sessionId: 1, questionId: 5, selectedOption: "A", tenantId: 1),
            new AssessmentAnswer(sessionId: 1, questionId: 6, selectedOption: "B", tenantId: 1),
            new AssessmentAnswer(sessionId: 1, questionId: 7, selectedOption: "A", tenantId: 1),
            new AssessmentAnswer(sessionId: 1, questionId: 8, selectedOption: "B", tenantId: 1)
        };

        var questions = CreateTestQuestions();
        var result = CalculatePersonalityTypeSync(answers, questions);

        result.Should().NotBeNull();
        result.PersonalityType.Should().NotBeNullOrEmpty();
        result.DimensionScores.Should().HaveCount(4);
        result.DimensionScores.Should().ContainKeys("EI", "SN", "TF", "JP");
    }

    [Fact]
    public void CalculatePersonalityType_WithExtrovertedAnswers_ShouldShowExtroversion()
    {
        var answers = new List<AssessmentAnswer>
        {
            new AssessmentAnswer(sessionId: 1, questionId: 1, selectedOption: "A", tenantId: 1),
            new AssessmentAnswer(sessionId: 1, questionId: 2, selectedOption: "A", tenantId: 1),
            new AssessmentAnswer(sessionId: 1, questionId: 3, selectedOption: "B", tenantId: 1),
            new AssessmentAnswer(sessionId: 1, questionId: 4, selectedOption: "B", tenantId: 1),
            new AssessmentAnswer(sessionId: 1, questionId: 5, selectedOption: "A", tenantId: 1),
            new AssessmentAnswer(sessionId: 1, questionId: 6, selectedOption: "B", tenantId: 1),
            new AssessmentAnswer(sessionId: 1, questionId: 7, selectedOption: "A", tenantId: 1),
            new AssessmentAnswer(sessionId: 1, questionId: 8, selectedOption: "B", tenantId: 1)
        };

        var questions = CreateTestQuestions();
        var result = CalculatePersonalityTypeSync(answers, questions);

        result.DimensionScores["EI"].Should().BeGreaterThan(0.5);
        result.PersonalityType.Should().StartWith("E");
    }

    [Fact]
    public void CalculatePersonalityType_WithIntrovertedAnswers_ShouldShowIntroversion()
    {
        var answers = new List<AssessmentAnswer>
        {
            new AssessmentAnswer(sessionId: 1, questionId: 1, selectedOption: "B", tenantId: 1),
            new AssessmentAnswer(sessionId: 1, questionId: 2, selectedOption: "B", tenantId: 1),
            new AssessmentAnswer(sessionId: 1, questionId: 3, selectedOption: "A", tenantId: 1),
            new AssessmentAnswer(sessionId: 1, questionId: 4, selectedOption: "A", tenantId: 1),
            new AssessmentAnswer(sessionId: 1, questionId: 5, selectedOption: "B", tenantId: 1),
            new AssessmentAnswer(sessionId: 1, questionId: 6, selectedOption: "A", tenantId: 1),
            new AssessmentAnswer(sessionId: 1, questionId: 7, selectedOption: "B", tenantId: 1),
            new AssessmentAnswer(sessionId: 1, questionId: 8, selectedOption: "A", tenantId: 1)
        };

        var questions = CreateTestQuestions();
        var result = CalculatePersonalityTypeSync(answers, questions);

        result.DimensionScores["EI"].Should().BeLessThan(0.5);
        result.PersonalityType.Should().StartWith("I");
    }

    [Fact]
    public void CalculatePersonalityType_WithEmptyAnswers_ShouldThrowException()
    {
        var answers = new List<AssessmentAnswer>();
        var questions = CreateTestQuestions();

        Action act = () => CalculatePersonalityTypeSync(answers, questions);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CalculatePersonalityType_WithNullAnswers_ShouldThrowException()
    {
        List<AssessmentAnswer> answers = null;
        var questions = CreateTestQuestions();

        Action act = () => CalculatePersonalityTypeSync(answers, questions);

        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData("ENFJ")]
    [InlineData("INTJ")]
    [InlineData("ESFP")]
    [InlineData("ISTP")]
    public void ValidatePersonalityType_WithValidTypes_ShouldReturnTrue(string personalityType)
    {
        var isValid = ValidatePersonalityTypeSync(personalityType);
        isValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("ABCD")]
    [InlineData("ENF")]
    [InlineData("ENFGJ")]
    [InlineData("")]
    [InlineData(null)]
    public void ValidatePersonalityType_WithInvalidTypes_ShouldReturnFalse(string personalityType)
    {
        var isValid = ValidatePersonalityTypeSync(personalityType);
        isValid.Should().BeFalse();
    }

    private List<Question> CreateTestQuestions()
    {
        return new List<Question>
        {
            new Question(
                orderNumber: 1,
                dimension: PersonalityDimension.EI,
                textEn: "Do you prefer group activities?",
                textAr: "هل تفضل الأنشطة الجماعية؟",
                optionATextEn: "Yes, I love being around people",
                optionATextAr: "نعم، أحب أن أكون حول الناس",
                optionAMapsToFirst: true,
                optionBTextEn: "No, I prefer solitary activities",
                optionBTextAr: "لا، أفضل الأنشطة الفردية",
                tenantId: 1
            ),
            new Question(
                orderNumber: 2,
                dimension: PersonalityDimension.EI,
                textEn: "How do you recharge your energy?",
                textAr: "كيف تستعيد طاقتك؟",
                optionATextEn: "By socializing with others",
                optionATextAr: "من خلال التواصل مع الآخرين",
                optionAMapsToFirst: true,
                optionBTextEn: "By spending time alone",
                optionBTextAr: "من خلال قضاء الوقت وحدي",
                tenantId: 1
            ),
            new Question(
                orderNumber: 3,
                dimension: PersonalityDimension.SN,
                textEn: "Do you focus more on details or the big picture?",
                textAr: "هل تركز أكثر على التفاصيل أم الصورة الكبيرة؟",
                optionATextEn: "Details and facts",
                optionATextAr: "التفاصيل والحقائق",
                optionAMapsToFirst: true,
                optionBTextEn: "Big picture and possibilities",
                optionBTextAr: "الصورة الكبيرة والإمكانيات",
                tenantId: 1
            ),
            new Question(
                orderNumber: 4,
                dimension: PersonalityDimension.SN,
                textEn: "How do you prefer to learn?",
                textAr: "كيف تفضل أن تتعلم؟",
                optionATextEn: "Through hands-on experience",
                optionATextAr: "من خلال التجربة العملية",
                optionAMapsToFirst: true,
                optionBTextEn: "Through theories and concepts",
                optionBTextAr: "من خلال النظريات والمفاهيم",
                tenantId: 1
            ),
            new Question(
                orderNumber: 5,
                dimension: PersonalityDimension.TF,
                textEn: "When making decisions, do you rely more on logic or feelings?",
                textAr: "عند اتخاذ القرارات، هل تعتمد أكثر على المنطق أم المشاعر؟",
                optionATextEn: "Logic and objective analysis",
                optionATextAr: "المنطق والتحليل الموضوعي",
                optionAMapsToFirst: true,
                optionBTextEn: "Feelings and personal values",
                optionBTextAr: "المشاعر والقيم الشخصية",
                tenantId: 1
            ),
            new Question(
                orderNumber: 6,
                dimension: PersonalityDimension.TF,
                textEn: "What motivates you more in work?",
                textAr: "ما الذي يحفزك أكثر في العمل؟",
                optionATextEn: "Efficiency and competence",
                optionATextAr: "الكفاءة والجدارة",
                optionAMapsToFirst: true,
                optionBTextEn: "Harmony and helping others",
                optionBTextAr: "الانسجام ومساعدة الآخرين",
                tenantId: 1
            ),
            new Question(
                orderNumber: 7,
                dimension: PersonalityDimension.JP,
                textEn: "Do you prefer structure or flexibility?",
                textAr: "هل تفضل البنية أم المرونة؟",
                optionATextEn: "Structure and planning",
                optionATextAr: "البنية والتخطيط",
                optionAMapsToFirst: true,
                optionBTextEn: "Flexibility and spontaneity",
                optionBTextAr: "المرونة والعفوية",
                tenantId: 1
            ),
            new Question(
                orderNumber: 8,
                dimension: PersonalityDimension.JP,
                textEn: "How do you approach deadlines?",
                textAr: "كيف تتعامل مع المواعيد النهائية؟",
                optionATextEn: "Plan ahead and finish early",
                optionATextAr: "أخطط مسبقاً وأنهي مبكراً",
                optionAMapsToFirst: true,
                optionBTextEn: "Work best under pressure",
                optionBTextAr: "أعمل بشكل أفضل تحت الضغط",
                tenantId: 1
            )
        };
    }

    private PersonalityResult CalculatePersonalityTypeSync(List<AssessmentAnswer> answers, List<Question> questions)
    {
        if (answers == null)
            throw new ArgumentNullException(nameof(answers));
        if (answers.Count == 0)
            throw new ArgumentException("Answers cannot be empty", nameof(answers));

        var scores = new Dictionary<PersonalityDimension, int>
        {
            { PersonalityDimension.EI, 0 },
            { PersonalityDimension.SN, 0 },
            { PersonalityDimension.TF, 0 },
            { PersonalityDimension.JP, 0 }
        };

        var dimensionCounts = new Dictionary<PersonalityDimension, int>
        {
            { PersonalityDimension.EI, 0 },
            { PersonalityDimension.SN, 0 },
            { PersonalityDimension.TF, 0 },
            { PersonalityDimension.JP, 0 }
        };

        foreach (var answer in answers)
        {
            var question = questions.FirstOrDefault(q => q.OrderNumber == answer.QuestionId);
            if (question != null)
            {
                dimensionCounts[question.Dimension]++;
                
                var isFirstOption = answer.SelectedOption == "A";
                var mapsToFirst = question.OptionAMapsToFirst;
                
                if ((isFirstOption && mapsToFirst) || (!isFirstOption && !mapsToFirst))
                {
                    scores[question.Dimension]++;
                }
            }
        }

        var personalityType = "";
        personalityType += dimensionCounts[PersonalityDimension.EI] > 0 && scores[PersonalityDimension.EI] >= (dimensionCounts[PersonalityDimension.EI] / 2.0) ? "E" : "I";
        personalityType += dimensionCounts[PersonalityDimension.SN] > 0 && scores[PersonalityDimension.SN] >= (dimensionCounts[PersonalityDimension.SN] / 2.0) ? "S" : "N";
        personalityType += dimensionCounts[PersonalityDimension.TF] > 0 && scores[PersonalityDimension.TF] >= (dimensionCounts[PersonalityDimension.TF] / 2.0) ? "T" : "F";
        personalityType += dimensionCounts[PersonalityDimension.JP] > 0 && scores[PersonalityDimension.JP] >= (dimensionCounts[PersonalityDimension.JP] / 2.0) ? "J" : "P";

        var dimensionScores = new Dictionary<string, double>
        {
            ["EI"] = dimensionCounts[PersonalityDimension.EI] > 0 ? scores[PersonalityDimension.EI] / (double)dimensionCounts[PersonalityDimension.EI] : 0.5,
            ["SN"] = dimensionCounts[PersonalityDimension.SN] > 0 ? scores[PersonalityDimension.SN] / (double)dimensionCounts[PersonalityDimension.SN] : 0.5,
            ["TF"] = dimensionCounts[PersonalityDimension.TF] > 0 ? scores[PersonalityDimension.TF] / (double)dimensionCounts[PersonalityDimension.TF] : 0.5,
            ["JP"] = dimensionCounts[PersonalityDimension.JP] > 0 ? scores[PersonalityDimension.JP] / (double)dimensionCounts[PersonalityDimension.JP] : 0.5
        };

        return new PersonalityResult
        {
            PersonalityType = personalityType,
            DimensionScores = dimensionScores
        };
    }

    private bool ValidatePersonalityTypeSync(string personalityType)
    {
        if (string.IsNullOrEmpty(personalityType) || personalityType.Length != 4)
            return false;

        var validTypes = new[]
        {
            "ENFJ", "ENFP", "ENTJ", "ENTP", "ESFJ", "ESFP", "ESTJ", "ESTP",
            "INFJ", "INFP", "INTJ", "INTP", "ISFJ", "ISFP", "ISTJ", "ISTP"
        };

        return validTypes.Contains(personalityType.ToUpper());
    }
}

public class PersonalityResult
{
    public string PersonalityType { get; set; } = string.Empty;
    public Dictionary<string, double> DimensionScores { get; set; } = new();
}
