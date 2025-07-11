using Masark.Domain.Common;
using System;

namespace Masark.Domain.Entities
{
    public class AssessmentAnswer : Entity
    {
        public int SessionId { get; private set; }
        public int QuestionId { get; private set; }
        public string SelectedOption { get; private set; } = string.Empty;
        public DateTime AnsweredAt { get; private set; }

        public virtual AssessmentSession? Session { get; private set; }
        public virtual Question? Question { get; private set; }

        protected AssessmentAnswer() { }

        public AssessmentAnswer(int sessionId, int questionId, string selectedOption, int tenantId) : base(tenantId)
        {
            SessionId = sessionId;
            QuestionId = questionId;
            SetSelectedOption(selectedOption);
            AnsweredAt = DateTime.UtcNow;
        }

        public void UpdateAnswer(string selectedOption)
        {
            SetSelectedOption(selectedOption);
            AnsweredAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        private void SetSelectedOption(string selectedOption)
        {
            if (selectedOption != "A" && selectedOption != "B")
                throw new ArgumentException("Selected option must be 'A' or 'B'", nameof(selectedOption));
            
            SelectedOption = selectedOption;
        }
    }
}
