using Masark.Domain.Common;
using System;

namespace Masark.Domain.Entities
{
    public class ReportUserAnswer : Entity, IAggregateRoot
    {
        public int ReportElementQuestionId { get; private set; }
        public virtual ReportElementQuestion ReportElementQuestion { get; private set; } = null!;

        public int AssessmentSessionId { get; private set; }
        public virtual AssessmentSession AssessmentSession { get; private set; } = null!;

        public string AnswerText { get; private set; } = string.Empty;
        public int? AnswerRating { get; private set; } // For rating questions (1-5)
        public bool? AnswerBoolean { get; private set; } // For yes/no questions
        public string AnswerChoice { get; private set; } = string.Empty; // For multiple choice questions
        public DateTime AnsweredAt { get; private set; }

        protected ReportUserAnswer() { }

        public ReportUserAnswer(int reportElementQuestionId, int assessmentSessionId, int tenantId) : base(tenantId)
        {
            ReportElementQuestionId = reportElementQuestionId;
            AssessmentSessionId = assessmentSessionId;
            AnsweredAt = DateTime.UtcNow;
        }

        public void SetTextAnswer(string answerText)
        {
            AnswerText = answerText;
            AnsweredAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        public void SetRatingAnswer(int rating)
        {
            if (rating < 1 || rating > 5)
                throw new ArgumentException("Rating must be between 1 and 5");
            
            AnswerRating = rating;
            AnsweredAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        public void SetBooleanAnswer(bool answer)
        {
            AnswerBoolean = answer;
            AnsweredAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        public void SetChoiceAnswer(string choice)
        {
            AnswerChoice = choice ?? throw new ArgumentNullException(nameof(choice));
            AnsweredAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        public object GetAnswerValue()
        {
            if (AnswerText != null) return AnswerText;
            if (AnswerRating.HasValue) return AnswerRating.Value;
            if (AnswerBoolean.HasValue) return AnswerBoolean.Value;
            if (AnswerChoice != null) return AnswerChoice;
            return null;
        }
    }
}
