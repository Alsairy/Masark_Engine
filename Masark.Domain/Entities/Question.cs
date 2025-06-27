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
        
        public string OptionATextEn { get; private set; }
        public string OptionATextAr { get; private set; }
        public bool OptionAMapsToFirst { get; private set; }
        
        public string OptionBTextEn { get; private set; }
        public string OptionBTextAr { get; private set; }
        
        public bool IsActive { get; private set; }
        
        public virtual ICollection<AssessmentAnswer> Answers { get; private set; }

        protected Question() 
        {
            Answers = new List<AssessmentAnswer>();
        }

        public Question(int orderNumber, PersonalityDimension dimension, 
                       string textEn, string textAr,
                       string optionATextEn, string optionATextAr, bool optionAMapsToFirst,
                       string optionBTextEn, string optionBTextAr, int tenantId) : base(tenantId)
        {
            OrderNumber = orderNumber;
            Dimension = dimension;
            TextEn = textEn ?? throw new ArgumentNullException(nameof(textEn));
            TextAr = textAr ?? throw new ArgumentNullException(nameof(textAr));
            OptionATextEn = optionATextEn ?? throw new ArgumentNullException(nameof(optionATextEn));
            OptionATextAr = optionATextAr ?? throw new ArgumentNullException(nameof(optionATextAr));
            OptionAMapsToFirst = optionAMapsToFirst;
            OptionBTextEn = optionBTextEn ?? throw new ArgumentNullException(nameof(optionBTextEn));
            OptionBTextAr = optionBTextAr ?? throw new ArgumentNullException(nameof(optionBTextAr));
            IsActive = true;
            Answers = new List<AssessmentAnswer>();
        }

        public void Update(string textEn, string textAr,
                          string optionATextEn, string optionATextAr, bool optionAMapsToFirst,
                          string optionBTextEn, string optionBTextAr)
        {
            TextEn = textEn ?? throw new ArgumentNullException(nameof(textEn));
            TextAr = textAr ?? throw new ArgumentNullException(nameof(textAr));
            OptionATextEn = optionATextEn ?? throw new ArgumentNullException(nameof(optionATextEn));
            OptionATextAr = optionATextAr ?? throw new ArgumentNullException(nameof(optionATextAr));
            OptionAMapsToFirst = optionAMapsToFirst;
            OptionBTextEn = optionBTextEn ?? throw new ArgumentNullException(nameof(optionBTextEn));
            OptionBTextAr = optionBTextAr ?? throw new ArgumentNullException(nameof(optionBTextAr));
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
            return language == "ar" ? TextAr : TextEn;
        }

        public string GetOptionAText(string language = "en")
        {
            return language == "ar" ? OptionATextAr : OptionATextEn;
        }

        public string GetOptionBText(string language = "en")
        {
            return language == "ar" ? OptionBTextAr : OptionBTextEn;
        }
    }
}
