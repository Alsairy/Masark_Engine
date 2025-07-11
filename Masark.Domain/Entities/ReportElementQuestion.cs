using Masark.Domain.Common;
using System;

namespace Masark.Domain.Entities
{
    public class ReportElementQuestion : Entity, IAggregateRoot
    {
        public int ReportElementId { get; private set; }
        public virtual ReportElement? ReportElement { get; private set; }

        public string QuestionTextEn { get; private set; } = string.Empty;
        public string QuestionTextAr { get; private set; } = string.Empty;
        public string QuestionType { get; private set; } = string.Empty; // "text", "rating", "multiple_choice", "yes_no"
        public string OptionsJson { get; private set; } = string.Empty; // JSON array of options for multiple choice
        public bool IsRequired { get; private set; }
        public int OrderIndex { get; private set; }

        public virtual ICollection<ReportUserAnswer> UserAnswers { get; private set; }

        protected ReportElementQuestion() 
        {
            UserAnswers = new List<ReportUserAnswer>();
        }

        public ReportElementQuestion(int reportElementId, string questionTextEn, string questionTextAr, 
                                   string questionType, string optionsJson, bool isRequired, int orderIndex, int tenantId) : base(tenantId)
        {
            ReportElementId = reportElementId;
            QuestionTextEn = questionTextEn ?? throw new ArgumentNullException(nameof(questionTextEn));
            QuestionTextAr = questionTextAr ?? throw new ArgumentNullException(nameof(questionTextAr));
            QuestionType = questionType ?? throw new ArgumentNullException(nameof(questionType));
            OptionsJson = optionsJson ?? "";
            IsRequired = isRequired;
            OrderIndex = orderIndex;
            UserAnswers = new List<ReportUserAnswer>();
        }

        public string GetQuestionText(string language = "en")
        {
            return language == "ar" ? QuestionTextAr : QuestionTextEn;
        }

        public void UpdateQuestion(string questionTextEn, string questionTextAr, string optionsJson)
        {
            QuestionTextEn = questionTextEn ?? throw new ArgumentNullException(nameof(questionTextEn));
            QuestionTextAr = questionTextAr ?? throw new ArgumentNullException(nameof(questionTextAr));
            OptionsJson = optionsJson ?? "";
            UpdateTimestamp();
        }
    }
}
