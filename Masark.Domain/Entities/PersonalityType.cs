using Masark.Domain.Common;
using System;
using System.Collections.Generic;

namespace Masark.Domain.Entities
{
    public class PersonalityType : Entity, IAggregateRoot
    {
        public string Code { get; private set; }
        
        public string NameEn { get; private set; }
        public string NameAr { get; private set; }
        
        public string DescriptionEn { get; private set; }
        public string DescriptionAr { get; private set; }
        
        public string StrengthsEn { get; private set; }
        public string StrengthsAr { get; private set; }
        
        public string ChallengesEn { get; private set; }
        public string ChallengesAr { get; private set; }

        public virtual ICollection<AssessmentSession> Sessions { get; private set; }
        public virtual ICollection<PersonalityCareerMatch> CareerMatches { get; private set; }

        protected PersonalityType() 
        {
            Sessions = new List<AssessmentSession>();
            CareerMatches = new List<PersonalityCareerMatch>();
        }

        public PersonalityType(string code, string nameEn, string nameAr, int tenantId) : base(tenantId)
        {
            Code = code?.ToUpper() ?? throw new ArgumentNullException(nameof(code));
            NameEn = nameEn ?? throw new ArgumentNullException(nameof(nameEn));
            NameAr = nameAr ?? throw new ArgumentNullException(nameof(nameAr));
            Sessions = new List<AssessmentSession>();
            CareerMatches = new List<PersonalityCareerMatch>();
            
            ValidateCode();
        }

        public void UpdateContent(string nameEn, string nameAr, 
                                 string descriptionEn, string descriptionAr,
                                 string strengthsEn, string strengthsAr,
                                 string challengesEn, string challengesAr)
        {
            NameEn = nameEn ?? throw new ArgumentNullException(nameof(nameEn));
            NameAr = nameAr ?? throw new ArgumentNullException(nameof(nameAr));
            DescriptionEn = descriptionEn;
            DescriptionAr = descriptionAr;
            StrengthsEn = strengthsEn;
            StrengthsAr = strengthsAr;
            ChallengesEn = challengesEn;
            ChallengesAr = challengesAr;
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

        public string GetStrengths(string language = "en")
        {
            return language == "ar" ? StrengthsAr : StrengthsEn;
        }

        public string GetChallenges(string language = "en")
        {
            return language == "ar" ? ChallengesAr : ChallengesEn;
        }

        private void ValidateCode()
        {
            if (string.IsNullOrWhiteSpace(Code) || Code.Length != 4)
                throw new ArgumentException("Personality type code must be exactly 4 characters", nameof(Code));

            var validChars = new[] { 'E', 'I', 'S', 'N', 'T', 'F', 'J', 'P' };
            foreach (char c in Code)
            {
                if (Array.IndexOf(validChars, c) == -1)
                    throw new ArgumentException($"Invalid character '{c}' in personality type code", nameof(Code));
            }
        }
    }
}
