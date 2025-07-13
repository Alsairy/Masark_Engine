using Masark.Domain.Common;
using Masark.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Masark.Domain.Entities
{
    public class Pathway : Entity, IAggregateRoot
    {
        public string NameEn { get; private set; } = string.Empty;
        public string NameAr { get; private set; } = string.Empty;
        public PathwaySource Source { get; private set; }
        public string DescriptionEn { get; private set; } = string.Empty;
        public string DescriptionAr { get; private set; } = string.Empty;

        public virtual ICollection<CareerPathway> CareerPathways { get; private set; }

        protected Pathway() 
        {
            CareerPathways = new List<CareerPathway>();
        }

        public Pathway(string nameEn, string nameAr, PathwaySource source, int tenantId) : base(tenantId)
        {
            NameEn = nameEn ?? throw new ArgumentNullException(nameof(nameEn));
            NameAr = nameAr ?? throw new ArgumentNullException(nameof(nameAr));
            Source = source;
            CareerPathways = new List<CareerPathway>();
        }

        public void Update(string nameEn, string nameAr, PathwaySource source, 
                          string descriptionEn, string descriptionAr)
        {
            NameEn = nameEn ?? throw new ArgumentNullException(nameof(nameEn));
            NameAr = nameAr ?? throw new ArgumentNullException(nameof(nameAr));
            Source = source;
            DescriptionEn = descriptionEn;
            DescriptionAr = descriptionAr;
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
