using Masark.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Masark.Application.Interfaces
{
    public interface IPersonalityRepository
    {
        Task<AssessmentSession> GetSessionByIdAsync(int sessionId);
        Task<List<AssessmentAnswer>> GetAnswersBySessionIdAsync(int sessionId);
        Task<Dictionary<int, Question>> GetActiveQuestionsAsync();
        Task<PersonalityType> GetPersonalityTypeByCodeAsync(string code);
        Task UpdateSessionWithResultsAsync(AssessmentSession session);
        Task<List<Question>> GetQuestionsOrderedAsync();
        
        Task<List<PersonalityCareerMatch>> GetCareerMatchesAsync(string personalityTypeCode);
        Task<List<Career>> GetCareersAsync();
        Task<List<CareerProgram>> GetCareerProgramsAsync(int careerId);
        Task<List<CareerPathway>> GetCareerPathwaysAsync(int careerId);
        
        Task<AssessmentSession?> GetAssessmentSessionAsync(string sessionToken);
        Task<AssessmentSession> CreateAssessmentSessionAsync(AssessmentSession session);
        Task<AssessmentSession> UpdateAssessmentSessionAsync(AssessmentSession session);
        Task<List<Question>> GetQuestionsAsync(string? languagePreference = null);
        Task<Question?> GetQuestionAsync(int questionId);
        Task<AssessmentAnswer> SaveAnswerAsync(AssessmentAnswer answer);
        Task<List<PersonalityType>> GetPersonalityTypesAsync();
        Task<PersonalityType?> GetPersonalityTypeAsync(string typeCode);
        Task<List<Program>> GetProgramsAsync();
        Task<List<Pathway>> GetPathwaysAsync();
    }
}
