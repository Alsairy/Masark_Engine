using Masark.Domain.Common;
using System;

namespace Masark.Domain.Entities
{
    public class CareerPathway : Entity
    {
        public int CareerId { get; private set; }
        public int PathwayId { get; private set; }

        public virtual Career? Career { get; private set; }
        public virtual Pathway? Pathway { get; private set; }

        protected CareerPathway() { }

        public CareerPathway(int careerId, int pathwayId, int tenantId) : base(tenantId)
        {
            CareerId = careerId;
            PathwayId = pathwayId;
        }

        public void UpdateRelationship(int careerId, int pathwayId)
        {
            CareerId = careerId;
            PathwayId = pathwayId;
            UpdateTimestamp();
        }
    }
}
