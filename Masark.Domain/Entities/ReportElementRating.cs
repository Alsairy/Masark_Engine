using Masark.Domain.Common;
using System;

namespace Masark.Domain.Entities
{
    public class ReportElementRating : Entity, IAggregateRoot
    {
        public int ReportElementId { get; private set; }
        public virtual ReportElement ReportElement { get; private set; } = null!;

        public int AssessmentSessionId { get; private set; }
        public virtual AssessmentSession AssessmentSession { get; private set; } = null!;

        public int Rating { get; private set; } // 1-5 scale
        public string Comment { get; private set; } = string.Empty;
        public DateTime RatedAt { get; private set; }

        protected ReportElementRating() { }

        public ReportElementRating(int reportElementId, int assessmentSessionId, int rating, string comment, int tenantId) : base(tenantId)
        {
            ReportElementId = reportElementId;
            AssessmentSessionId = assessmentSessionId;
            SetRating(rating);
            Comment = comment ?? "";
            RatedAt = DateTime.UtcNow;
        }

        public void UpdateRating(int rating, string comment)
        {
            SetRating(rating);
            Comment = comment ?? "";
            RatedAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        private void SetRating(int rating)
        {
            if (rating < 1 || rating > 5)
                throw new ArgumentException("Rating must be between 1 and 5");
            
            Rating = rating;
        }
    }
}
