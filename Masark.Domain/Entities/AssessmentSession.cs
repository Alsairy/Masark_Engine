using Masark.Domain.Common;
using Masark.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Masark.Domain.Entities
{
    public class AssessmentSession : Entity, IAggregateRoot
    {
        public string SessionToken { get; private set; }
        
        public string StudentName { get; private set; }
        public string StudentEmail { get; private set; }
        public string StudentId { get; private set; }
        
        public int? PersonalityTypeId { get; private set; }
        public virtual PersonalityType PersonalityType { get; private set; }
        
        public decimal? EStrength { get; private set; }
        public decimal? SStrength { get; private set; }
        public decimal? TStrength { get; private set; }
        public decimal? JStrength { get; private set; }
        
        public PreferenceStrength? EiClarity { get; private set; }
        public PreferenceStrength? SnClarity { get; private set; }
        public PreferenceStrength? TfClarity { get; private set; }
        public PreferenceStrength? JpClarity { get; private set; }
        
        public DeploymentMode DeploymentMode { get; private set; }
        public string LanguagePreference { get; private set; }
        public string IpAddress { get; private set; }
        public string UserAgent { get; private set; }
        
        public DateTime StartedAt { get; private set; }
        public DateTime? CompletedAt { get; private set; }
        public bool IsCompleted { get; private set; }
        
        public AssessmentState CurrentState { get; private set; }
        public bool RequiresTieBreaker { get; private set; }
        public int? AssessmentRating { get; private set; }

        public virtual ICollection<AssessmentAnswer> Answers { get; private set; }

        protected AssessmentSession() 
        {
            Answers = new List<AssessmentAnswer>();
        }

        public AssessmentSession(string sessionToken, DeploymentMode deploymentMode, 
                               string languagePreference, string ipAddress, string userAgent, int tenantId) : base(tenantId)
        {
            SessionToken = sessionToken ?? throw new ArgumentNullException(nameof(sessionToken));
            DeploymentMode = deploymentMode;
            LanguagePreference = languagePreference ?? "en";
            IpAddress = ipAddress;
            UserAgent = userAgent;
            StartedAt = DateTime.UtcNow;
            IsCompleted = false;
            CurrentState = AssessmentState.AnswerQuestions;
            RequiresTieBreaker = false;
            Answers = new List<AssessmentAnswer>();
        }

        public void SetStudentInfo(string studentName, string studentEmail, string studentId)
        {
            StudentName = studentName;
            StudentEmail = studentEmail;
            StudentId = studentId;
            UpdateTimestamp();
        }

        public void CompleteAssessment(int personalityTypeId, 
                                     decimal eStrength, decimal sStrength, decimal tStrength, decimal jStrength,
                                     PreferenceStrength eiClarity, PreferenceStrength snClarity, 
                                     PreferenceStrength tfClarity, PreferenceStrength jpClarity)
        {
            PersonalityTypeId = personalityTypeId;
            EStrength = ValidateStrength(eStrength);
            SStrength = ValidateStrength(sStrength);
            TStrength = ValidateStrength(tStrength);
            JStrength = ValidateStrength(jStrength);
            EiClarity = eiClarity;
            SnClarity = snClarity;
            TfClarity = tfClarity;
            JpClarity = jpClarity;
            CompletedAt = DateTime.UtcNow;
            IsCompleted = true;
            UpdateTimestamp();
        }

        public void CompleteAssessment(string personalityType, Dictionary<string, double> dimensionScores)
        {
            if (string.IsNullOrEmpty(personalityType))
                throw new ArgumentException("Personality type cannot be null or empty", nameof(personalityType));

            EStrength = (decimal)(dimensionScores.GetValueOrDefault("E", 0.5));
            SStrength = (decimal)(dimensionScores.GetValueOrDefault("S", 0.5));
            TStrength = (decimal)(dimensionScores.GetValueOrDefault("T", 0.5));
            JStrength = (decimal)(dimensionScores.GetValueOrDefault("J", 0.5));
            
            EiClarity = GetPreferenceStrength(dimensionScores.GetValueOrDefault("E", 0.5));
            SnClarity = GetPreferenceStrength(dimensionScores.GetValueOrDefault("S", 0.5));
            TfClarity = GetPreferenceStrength(dimensionScores.GetValueOrDefault("T", 0.5));
            JpClarity = GetPreferenceStrength(dimensionScores.GetValueOrDefault("J", 0.5));
            
            CompletedAt = DateTime.UtcNow;
            IsCompleted = true;
            UpdateTimestamp();
        }

        public void AddAnswer(AssessmentAnswer answer)
        {
            if (answer == null)
                throw new ArgumentNullException(nameof(answer));
            
            var existingAnswer = Answers.FirstOrDefault(a => a.QuestionId == answer.QuestionId);
            if (existingAnswer != null)
            {
                Answers.Remove(existingAnswer);
            }
            
            Answers.Add(answer);
            UpdateTimestamp();
        }

        public Dictionary<string, double> GetDimensionScores()
        {
            return new Dictionary<string, double>
            {
                ["E"] = (double)(EStrength ?? 0.5m),
                ["S"] = (double)(SStrength ?? 0.5m),
                ["T"] = (double)(TStrength ?? 0.5m),
                ["J"] = (double)(JStrength ?? 0.5m)
            };
        }

        public string GetPersonalityTypeCode()
        {
            if (PersonalityType != null)
                return PersonalityType.Code;
            
            var e = (EStrength ?? 0.5m) >= 0.5m ? "E" : "I";
            var s = (SStrength ?? 0.5m) >= 0.5m ? "S" : "N";
            var t = (TStrength ?? 0.5m) >= 0.5m ? "T" : "F";
            var j = (JStrength ?? 0.5m) >= 0.5m ? "J" : "P";
            
            return $"{e}{s}{t}{j}";
        }

        private PreferenceStrength GetPreferenceStrength(double score)
        {
            var strength = Math.Abs(score - 0.5) * 2;
            return strength switch
            {
                >= 0.75 => PreferenceStrength.VERY_CLEAR,
                >= 0.5 => PreferenceStrength.CLEAR,
                >= 0.25 => PreferenceStrength.MODERATE,
                _ => PreferenceStrength.SLIGHT
            };
        }

        private decimal ValidateStrength(decimal strength)
        {
            if (strength < 0 || strength > 1)
                throw new ArgumentException("Strength must be between 0.0 and 1.0");
            return strength;
        }

        public bool CanTransitionTo(AssessmentState newState)
        {
            return newState switch
            {
                AssessmentState.AnswerQuestions => CurrentState == AssessmentState.AnswerQuestions,
                AssessmentState.RateCareerClusters => CurrentState == AssessmentState.AnswerQuestions && HasAllAnswers(),
                AssessmentState.CalculateAssessment => CurrentState == AssessmentState.RateCareerClusters,
                AssessmentState.TieResolvement => CurrentState == AssessmentState.CalculateAssessment && RequiresTieBreaker,
                AssessmentState.RateAssessment => (CurrentState == AssessmentState.CalculateAssessment && !RequiresTieBreaker) || 
                                                 (CurrentState == AssessmentState.TieResolvement),
                AssessmentState.Report => CurrentState == AssessmentState.RateAssessment && AssessmentRating.HasValue,
                _ => false
            };
        }

        public void TransitionTo(AssessmentState newState)
        {
            if (!CanTransitionTo(newState))
            {
                throw new InvalidOperationException($"Cannot transition from {CurrentState} to {newState}");
            }

            CurrentState = newState;
            UpdateTimestamp();
        }

        public void MarkForTieBreaker()
        {
            if (CurrentState != AssessmentState.CalculateAssessment)
            {
                throw new InvalidOperationException("Can only mark for tie breaker during calculation state");
            }

            RequiresTieBreaker = true;
            UpdateTimestamp();
        }

        public void SetAssessmentRating(int rating)
        {
            if (CurrentState != AssessmentState.RateAssessment)
            {
                throw new InvalidOperationException("Can only set rating during rate assessment state");
            }

            if (rating < 1 || rating > 5)
            {
                throw new ArgumentException("Rating must be between 1 and 5");
            }

            AssessmentRating = rating;
            UpdateTimestamp();
        }

        public bool HasAllAnswers()
        {
            return Answers.Count >= 36;
        }

        public bool IsInState(AssessmentState state)
        {
            return CurrentState == state;
        }

        public string GetCurrentStateDescription()
        {
            return CurrentState switch
            {
                AssessmentState.AnswerQuestions => "Answering assessment questions",
                AssessmentState.RateCareerClusters => "Rating career clusters",
                AssessmentState.CalculateAssessment => "Calculating personality type",
                AssessmentState.TieResolvement => "Resolving tied dimensions",
                AssessmentState.RateAssessment => "Rating assessment experience",
                AssessmentState.Report => "Viewing results report",
                _ => "Unknown state"
            };
        }
    }
}
