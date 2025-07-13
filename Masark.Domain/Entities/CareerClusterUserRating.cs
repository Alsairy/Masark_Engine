using Masark.Domain.Common;
using System;

namespace Masark.Domain.Entities
{
    public class CareerClusterUserRating : Entity, IAggregateRoot
    {
        public int AssessmentSessionId { get; private set; }
        public virtual AssessmentSession? AssessmentSession { get; private set; }

        public int CareerClusterId { get; private set; }
        public virtual CareerCluster? CareerCluster { get; private set; }

        public int CareerClusterRatingId { get; private set; }
        public virtual CareerClusterRating? CareerClusterRating { get; private set; }

        public DateTime RatedAt { get; private set; }

        protected CareerClusterUserRating() { }

        public CareerClusterUserRating(int assessmentSessionId, int careerClusterId, int careerClusterRatingId, int tenantId) : base(tenantId)
        {
            AssessmentSessionId = assessmentSessionId;
            CareerClusterId = careerClusterId;
            CareerClusterRatingId = careerClusterRatingId;
            RatedAt = DateTime.UtcNow;
        }

        public void UpdateRating(int careerClusterRatingId)
        {
            CareerClusterRatingId = careerClusterRatingId;
            RatedAt = DateTime.UtcNow;
            UpdateTimestamp();
        }

        public int GetRatingValue()
        {
            return CareerClusterRating?.Value ?? 0;
        }
    }
}
