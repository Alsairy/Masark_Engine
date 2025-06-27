using Masark.Domain.Common;
using System;
using System.Collections.Generic;

namespace Masark.Domain.Entities
{
    public class CareerCluster : Entity, IAggregateRoot
    {
        public string NameEn { get; private set; }
        public string NameAr { get; private set; }
        public string DescriptionEn { get; private set; }
        public string DescriptionAr { get; private set; }

        public virtual ICollection<Career> Careers { get; private set; }

        protected CareerCluster() 
        {
            Careers = new List<Career>();
        }

        public CareerCluster(string nameEn, string nameAr, int tenantId) : base(tenantId)
        {
            NameEn = nameEn ?? throw new ArgumentNullException(nameof(nameEn));
            NameAr = nameAr ?? throw new ArgumentNullException(nameof(nameAr));
            Careers = new List<Career>();
        }

        public void Update(string nameEn, string nameAr, string descriptionEn, string descriptionAr)
        {
            NameEn = nameEn ?? throw new ArgumentNullException(nameof(nameEn));
            NameAr = nameAr ?? throw new ArgumentNullException(nameof(nameAr));
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
