using Masark.Domain.Common;
using System;

namespace Masark.Domain.Entities
{
    public class PersonalityCareerMatch : Entity
    {
        public int PersonalityTypeId { get; private set; }
        public int CareerId { get; private set; }
        public decimal MatchScore { get; private set; }

        public virtual PersonalityType? PersonalityType { get; private set; }
        public virtual Career? Career { get; private set; }

        protected PersonalityCareerMatch() { }

        public PersonalityCareerMatch(int personalityTypeId, int careerId, decimal matchScore, int tenantId) : base(tenantId)
        {
            PersonalityTypeId = personalityTypeId;
            CareerId = careerId;
            SetMatchScore(matchScore);
        }

        public void UpdateMatchScore(decimal matchScore)
        {
            SetMatchScore(matchScore);
            UpdateTimestamp();
        }

        private void SetMatchScore(decimal matchScore)
        {
            if (matchScore < 0 || matchScore > 1)
                throw new ArgumentException("Match score must be between 0.0 and 1.0", nameof(matchScore));
            
            MatchScore = matchScore;
        }
    }
}
