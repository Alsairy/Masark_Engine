using Masark.Domain.Common;
using System;

namespace Masark.Domain.Entities
{
    public class CareerProgram : Entity
    {
        public int CareerId { get; private set; }
        public int ProgramId { get; private set; }

        public virtual Career? Career { get; private set; }
        public virtual Program? Program { get; private set; }

        protected CareerProgram() { }

        public CareerProgram(int careerId, int programId, int tenantId) : base(tenantId)
        {
            CareerId = careerId;
            ProgramId = programId;
        }

        public void UpdateRelationship(int careerId, int programId)
        {
            CareerId = careerId;
            ProgramId = programId;
            UpdateTimestamp();
        }
    }
}
