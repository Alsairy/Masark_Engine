using Masark.Domain.Common;
using Masark.Domain.Enums;
using System;

namespace Masark.Domain.Entities
{
    public class TieBreakerQuestion : Entity, IAggregateRoot
    {
        public string TextEn { get; private set; } = string.Empty;
        public string TextAr { get; private set; } = string.Empty;
        public string OptionAEn { get; private set; } = string.Empty;
        public string OptionAAr { get; private set; } = string.Empty;
        public string OptionBEn { get; private set; } = string.Empty;
        public string OptionBAr { get; private set; } = string.Empty;
        public PersonalityDimension Dimension { get; private set; }
        public bool OptionAMapsToFirst { get; private set; }
        public int OrderIndex { get; private set; }
        public bool IsActive { get; private set; }

        protected TieBreakerQuestion() { }

        public TieBreakerQuestion(string textEn, string textAr, string optionAEn, string optionAAr, 
                                string optionBEn, string optionBAr, PersonalityDimension dimension, 
                                bool optionAMapsToFirst, int orderIndex, int tenantId) : base(tenantId)
        {
            TextEn = textEn ?? throw new ArgumentNullException(nameof(textEn));
            TextAr = textAr ?? throw new ArgumentNullException(nameof(textAr));
            OptionAEn = optionAEn ?? throw new ArgumentNullException(nameof(optionAEn));
            OptionAAr = optionAAr ?? throw new ArgumentNullException(nameof(optionAAr));
            OptionBEn = optionBEn ?? throw new ArgumentNullException(nameof(optionBEn));
            OptionBAr = optionBAr ?? throw new ArgumentNullException(nameof(optionBAr));
            Dimension = dimension;
            OptionAMapsToFirst = optionAMapsToFirst;
            OrderIndex = orderIndex;
            IsActive = true;
        }

        public string GetText(string language = "en")
        {
            return language == "ar" ? TextAr : TextEn;
        }

        public string GetOptionA(string language = "en")
        {
            return language == "ar" ? OptionAAr : OptionAEn;
        }

        public string GetOptionB(string language = "en")
        {
            return language == "ar" ? OptionBAr : OptionBEn;
        }

        public void Update(string textEn, string textAr, string optionAEn, string optionAAr, 
                          string optionBEn, string optionBAr, bool optionAMapsToFirst)
        {
            TextEn = textEn ?? throw new ArgumentNullException(nameof(textEn));
            TextAr = textAr ?? throw new ArgumentNullException(nameof(textAr));
            OptionAEn = optionAEn ?? throw new ArgumentNullException(nameof(optionAEn));
            OptionAAr = optionAAr ?? throw new ArgumentNullException(nameof(optionAAr));
            OptionBEn = optionBEn ?? throw new ArgumentNullException(nameof(optionBEn));
            OptionBAr = optionBAr ?? throw new ArgumentNullException(nameof(optionBAr));
            OptionAMapsToFirst = optionAMapsToFirst;
            UpdateTimestamp();
        }

        public void SetActive(bool isActive)
        {
            IsActive = isActive;
            UpdateTimestamp();
        }
    }
}
