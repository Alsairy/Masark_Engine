using Masark.Domain.Common;
using Masark.Domain.Enums;
using System;
using System.Collections.Generic;

namespace Masark.Domain.Entities
{
    public class Question : Entity, IAggregateRoot
    {
        public int OrderNumber { get; private set; }
        public PersonalityDimension Dimension { get; private set; }
        
        public string TextEn { get; private set; }
        public string TextAr { get; private set; }
        public string TextEs { get; private set; }
        public string TextZh { get; private set; }
        
        public string OptionATextEn { get; private set; }
        public string OptionATextAr { get; private set; }
        public string OptionATextEs { get; private set; }
        public string OptionATextZh { get; private set; }
        public bool OptionAMapsToFirst { get; private set; }
        
        public string OptionBTextEn { get; private set; }
        public string OptionBTextAr { get; private set; }
        public string OptionBTextEs { get; private set; }
        public string OptionBTextZh { get; private set; }
        
        public bool IsActive { get; private set; }
        
        public virtual ICollection<AssessmentAnswer> Answers { get; private set; }

        protected Question() 
        {
            Answers = new List<AssessmentAnswer>();
        }

        public Question(int orderNumber, PersonalityDimension dimension, 
                       string textEn, string textAr, string textEs, string textZh,
                       string optionATextEn, string optionATextAr, string optionATextEs, string optionATextZh, bool optionAMapsToFirst,
                       string optionBTextEn, string optionBTextAr, string optionBTextEs, string optionBTextZh, int tenantId) : base(tenantId)
        {
            OrderNumber = orderNumber;
            Dimension = dimension;
            TextEn = textEn ?? throw new ArgumentNullException(nameof(textEn));
            TextAr = textAr ?? throw new ArgumentNullException(nameof(textAr));
            TextEs = textEs ?? throw new ArgumentNullException(nameof(textEs));
            TextZh = textZh ?? throw new ArgumentNullException(nameof(textZh));
            OptionATextEn = optionATextEn ?? throw new ArgumentNullException(nameof(optionATextEn));
            OptionATextAr = optionATextAr ?? throw new ArgumentNullException(nameof(optionATextAr));
            OptionATextEs = optionATextEs ?? throw new ArgumentNullException(nameof(optionATextEs));
            OptionATextZh = optionATextZh ?? throw new ArgumentNullException(nameof(optionATextZh));
            OptionAMapsToFirst = optionAMapsToFirst;
            OptionBTextEn = optionBTextEn ?? throw new ArgumentNullException(nameof(optionBTextEn));
            OptionBTextAr = optionBTextAr ?? throw new ArgumentNullException(nameof(optionBTextAr));
            OptionBTextEs = optionBTextEs ?? throw new ArgumentNullException(nameof(optionBTextEs));
            OptionBTextZh = optionBTextZh ?? throw new ArgumentNullException(nameof(optionBTextZh));
            IsActive = true;
            Answers = new List<AssessmentAnswer>();
        }

        public void Update(string textEn, string textAr, string textEs, string textZh,
                          string optionATextEn, string optionATextAr, string optionATextEs, string optionATextZh, bool optionAMapsToFirst,
                          string optionBTextEn, string optionBTextAr, string optionBTextEs, string optionBTextZh)
        {
            TextEn = textEn ?? throw new ArgumentNullException(nameof(textEn));
            TextAr = textAr ?? throw new ArgumentNullException(nameof(textAr));
            TextEs = textEs ?? throw new ArgumentNullException(nameof(textEs));
            TextZh = textZh ?? throw new ArgumentNullException(nameof(textZh));
            OptionATextEn = optionATextEn ?? throw new ArgumentNullException(nameof(optionATextEn));
            OptionATextAr = optionATextAr ?? throw new ArgumentNullException(nameof(optionATextAr));
            OptionATextEs = optionATextEs ?? throw new ArgumentNullException(nameof(optionATextEs));
            OptionATextZh = optionATextZh ?? throw new ArgumentNullException(nameof(optionATextZh));
            OptionAMapsToFirst = optionAMapsToFirst;
            OptionBTextEn = optionBTextEn ?? throw new ArgumentNullException(nameof(optionBTextEn));
            OptionBTextAr = optionBTextAr ?? throw new ArgumentNullException(nameof(optionBTextAr));
            OptionBTextEs = optionBTextEs ?? throw new ArgumentNullException(nameof(optionBTextEs));
            OptionBTextZh = optionBTextZh ?? throw new ArgumentNullException(nameof(optionBTextZh));
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

        public string GetText(string language = "en")
        {
            return language switch
            {
                "ar" => TextAr,
                "es" => TextEs,
                "zh" => TextZh,
                _ => TextEn
            };
        }

        public string GetOptionAText(string language = "en")
        {
            return language switch
            {
                "ar" => OptionATextAr,
                "es" => OptionATextEs,
                "zh" => OptionATextZh,
                _ => OptionATextEn
            };
        }

        public string GetOptionBText(string language = "en")
        {
            return language switch
            {
                "ar" => OptionBTextAr,
                "es" => OptionBTextEs,
                "zh" => OptionBTextZh,
                _ => OptionBTextEn
            };
        }
    }
}
