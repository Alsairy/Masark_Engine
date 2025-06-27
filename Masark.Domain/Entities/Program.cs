using Masark.Domain.Common;
using System;
using System.Collections.Generic;

namespace Masark.Domain.Entities
{
    public class Program : Entity, IAggregateRoot
    {
        public string NameEn { get; private set; }
        public string NameAr { get; private set; }
        public string DescriptionEn { get; private set; }
        public string DescriptionAr { get; private set; }

        public virtual ICollection<CareerProgram> CareerPrograms { get; private set; }

        protected Program() 
        {
            CareerPrograms = new List<CareerProgram>();
        }

        public Program(string nameEn, string nameAr, int tenantId) : base(tenantId)
        {
            NameEn = nameEn ?? throw new ArgumentNullException(nameof(nameEn));
            NameAr = nameAr ?? throw new ArgumentNullException(nameof(nameAr));
            CareerPrograms = new List<CareerProgram>();
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
