using Masark.Domain.Common;
using System;
using System.Collections.Generic;

namespace Masark.Domain.Entities
{
    public class Career : Entity, IAggregateRoot
    {
        public string NameEn { get; private set; }
        public string NameAr { get; private set; }
        public string DescriptionEn { get; private set; }
        public string DescriptionAr { get; private set; }
        public string SsocCode { get; private set; }
        public bool IsActive { get; private set; }

        public int ClusterId { get; private set; }
        public virtual CareerCluster Cluster { get; private set; }

        public virtual ICollection<CareerProgram> CareerPrograms { get; private set; }
        public virtual ICollection<CareerPathway> CareerPathways { get; private set; }
        public virtual ICollection<PersonalityCareerMatch> PersonalityMatches { get; private set; }

        protected Career() 
        {
            CareerPrograms = new List<CareerProgram>();
            CareerPathways = new List<CareerPathway>();
            PersonalityMatches = new List<PersonalityCareerMatch>();
        }

        public Career(string nameEn, string nameAr, int clusterId, int tenantId) : base(tenantId)
        {
            NameEn = nameEn ?? throw new ArgumentNullException(nameof(nameEn));
            NameAr = nameAr ?? throw new ArgumentNullException(nameof(nameAr));
            ClusterId = clusterId;
            IsActive = true;
            CareerPrograms = new List<CareerProgram>();
            CareerPathways = new List<CareerPathway>();
            PersonalityMatches = new List<PersonalityCareerMatch>();
        }

        public void Update(string nameEn, string nameAr, string descriptionEn, string descriptionAr, 
                          string ssocCode)
        {
            NameEn = nameEn ?? throw new ArgumentNullException(nameof(nameEn));
            NameAr = nameAr ?? throw new ArgumentNullException(nameof(nameAr));
            DescriptionEn = descriptionEn;
            DescriptionAr = descriptionAr;
            SsocCode = ssocCode;
            UpdateTimestamp();
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdateTimestamp();
        }

        public void Activate()
        {
            IsActive = true;
            UpdateTimestamp();
        }

        public string GetName(string language = "en")
        {
            return language == "ar" ? NameAr : NameEn;
        }

        public string GetDescription(string language = "en")
        {
            return language == "ar" ? DescriptionAr : DescriptionEn;
        }
    }
}
