using Microsoft.EntityFrameworkCore;
using Masark.Application.Interfaces;
using Masark.Domain.Entities;
using Masark.Infrastructure.Identity;

namespace Masark.Infrastructure.Repositories
{
    public class PersonalityRepository : IPersonalityRepository
    {
        private readonly ApplicationDbContext _context;

        public PersonalityRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AssessmentSession?> GetAssessmentSessionAsync(string sessionToken)
        {
            return await _context.AssessmentSessions
                .Include(s => s.Answers)
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken);
        }

        public async Task<AssessmentSession> CreateAssessmentSessionAsync(AssessmentSession session)
        {
            _context.AssessmentSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<AssessmentSession> UpdateAssessmentSessionAsync(AssessmentSession session)
        {
            _context.AssessmentSessions.Update(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<List<Question>> GetQuestionsAsync(string? languagePreference = null)
        {
            return await _context.Questions
                .OrderBy(q => q.OrderNumber)
                .ToListAsync();
        }

        public async Task<Question?> GetQuestionAsync(int questionId)
        {
            return await _context.Questions
                .FirstOrDefaultAsync(q => q.Id == questionId);
        }

        public async Task<AssessmentAnswer> SaveAnswerAsync(AssessmentAnswer answer)
        {
            _context.AssessmentAnswers.Add(answer);
            await _context.SaveChangesAsync();
            return answer;
        }

        public async Task<List<AssessmentAnswer>> SaveAnswersBulkAsync(List<AssessmentAnswer> answers)
        {
            if (answers == null || !answers.Any())
                return new List<AssessmentAnswer>();

            _context.AssessmentAnswers.AddRange(answers);
            await _context.SaveChangesAsync();
            return answers;
        }

        public async Task<List<Question>> CreateQuestionsBulkAsync(List<Question> questions)
        {
            if (questions == null || !questions.Any())
                return new List<Question>();

            _context.Questions.AddRange(questions);
            await _context.SaveChangesAsync();
            return questions;
        }

        public async Task<List<PersonalityCareerMatch>> CreateCareerMatchesBulkAsync(List<PersonalityCareerMatch> matches)
        {
            if (matches == null || !matches.Any())
                return new List<PersonalityCareerMatch>();

            _context.PersonalityCareerMatches.AddRange(matches);
            await _context.SaveChangesAsync();
            return matches;
        }

        public async Task<List<Career>> CreateCareersBulkAsync(List<Career> careers)
        {
            if (careers == null || !careers.Any())
                return new List<Career>();

            _context.Careers.AddRange(careers);
            await _context.SaveChangesAsync();
            return careers;
        }

        public async Task UpdateAssessmentSessionsBulkAsync(List<AssessmentSession> sessions)
        {
            if (sessions == null || !sessions.Any())
                return;

            _context.AssessmentSessions.UpdateRange(sessions);
            await _context.SaveChangesAsync();
        }

        public async Task<List<PersonalityType>> GetPersonalityTypesAsync()
        {
            return await _context.PersonalityTypes.ToListAsync();
        }

        public async Task<PersonalityType?> GetPersonalityTypeAsync(string typeCode)
        {
            return await _context.PersonalityTypes
                .FirstOrDefaultAsync(pt => pt.Code == typeCode);
        }

        public async Task<List<Career>> GetCareersAsync()
        {
            return await _context.Careers
                .Include(c => c.Cluster)
                .ToListAsync();
        }

        public async Task<List<PersonalityCareerMatch>> GetCareerMatchesAsync(string personalityTypeCode)
        {
            return await _context.PersonalityCareerMatches
                .Include(pcm => pcm.Career)
                .ThenInclude(c => c.Cluster)
                .Include(pcm => pcm.PersonalityType)
                .Where(pcm => pcm.PersonalityType.Code == personalityTypeCode)
                .OrderByDescending(pcm => pcm.MatchScore)
                .ToListAsync();
        }

        public async Task<List<Program>> GetProgramsAsync()
        {
            return await _context.Programs.ToListAsync();
        }

        public async Task<List<Pathway>> GetPathwaysAsync()
        {
            return await _context.Pathways.ToListAsync();
        }

        public async Task<List<CareerProgram>> GetCareerProgramsAsync(int careerId)
        {
            return await _context.CareerPrograms
                .Include(cp => cp.Program)
                .Where(cp => cp.CareerId == careerId)
                .ToListAsync();
        }

        public async Task<List<CareerPathway>> GetCareerPathwaysAsync(int careerId)
        {
            return await _context.CareerPathways
                .Include(cp => cp.Pathway)
                .Where(cp => cp.CareerId == careerId)
                .ToListAsync();
        }

        public async Task<AssessmentSession> GetSessionByIdAsync(int sessionId)
        {
            var session = await _context.AssessmentSessions
                .Include(s => s.Answers)
                .FirstOrDefaultAsync(s => s.Id == sessionId);
            return session ?? throw new InvalidOperationException($"Assessment session with ID {sessionId} not found");
        }

        public async Task<List<AssessmentAnswer>> GetAnswersBySessionIdAsync(int sessionId)
        {
            return await _context.AssessmentAnswers
                .Where(a => a.SessionId == sessionId)
                .OrderBy(a => a.QuestionId)
                .ToListAsync();
        }

        public async Task<Dictionary<int, Question>> GetActiveQuestionsAsync()
        {
            var questions = await _context.Questions
                .Where(q => q.IsActive)
                .OrderBy(q => q.OrderNumber)
                .ToListAsync();
            return questions.ToDictionary(q => q.Id, q => q);
        }

        public async Task<PersonalityType> GetPersonalityTypeByCodeAsync(string typeCode)
        {
            var personalityType = await _context.PersonalityTypes
                .FirstOrDefaultAsync(pt => pt.Code == typeCode);
            return personalityType ?? throw new InvalidOperationException($"Personality type with code {typeCode} not found");
        }

        public async Task UpdateSessionWithResultsAsync(AssessmentSession session)
        {
            _context.AssessmentSessions.Update(session);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Question>> GetQuestionsOrderedAsync()
        {
            return await _context.Questions
                .Where(q => q.IsActive)
                .OrderBy(q => q.OrderNumber)
                .ToListAsync();
        }
    }
}
