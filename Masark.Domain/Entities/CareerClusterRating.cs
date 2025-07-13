using Masark.Domain.Common;
using System;

namespace Masark.Domain.Entities
{
    public class CareerClusterRating : Entity, IAggregateRoot
    {
        public int Value { get; private set; }
        public string DescriptionEn { get; private set; } = string.Empty;
        public string DescriptionAr { get; private set; } = string.Empty;

        protected CareerClusterRating() { }

        public CareerClusterRating(int value, string descriptionEn, string descriptionAr, int tenantId) : base(tenantId)
        {
            if (value < 1 || value > 5)
                throw new ArgumentException("Rating value must be between 1 and 5");

            Value = value;
            DescriptionEn = descriptionEn ?? throw new ArgumentNullException(nameof(descriptionEn));
            DescriptionAr = descriptionAr ?? throw new ArgumentNullException(nameof(descriptionAr));
        }

        public string GetDescription(string language = "en")
        {
            return language == "ar" ? DescriptionAr : DescriptionEn;
        }

        public void Update(string descriptionEn, string descriptionAr)
        {
            DescriptionEn = descriptionEn ?? throw new ArgumentNullException(nameof(descriptionEn));
            DescriptionAr = descriptionAr ?? throw new ArgumentNullException(nameof(descriptionAr));
            UpdateTimestamp();
        }
    }
}
