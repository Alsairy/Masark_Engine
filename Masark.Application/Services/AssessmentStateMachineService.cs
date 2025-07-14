using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Masark.Domain.Entities;
using Masark.Domain.Enums;
using Masark.Application.Interfaces;

namespace Masark.Application.Services
{
    public class StateTransitionResult
    {
        public bool Success { get; set; }
        public AssessmentState NewState { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> StateData { get; set; } = new();
        public List<string> AllowedActions { get; set; } = new();
        public bool RequiresTieBreaker { get; set; }
        public List<TieBreakerQuestion> TieBreakerQuestions { get; set; } = new();
    }

    public class AssessmentStateInfo
    {
        public AssessmentState CurrentState { get; set; }
        public AssessmentState? PreviousState { get; set; }
        public DateTime StateEnteredAt { get; set; }
        public Dictionary<string, object> StateData { get; set; } = new();
        public List<string> AllowedTransitions { get; set; } = new();
        public bool CanProgress { get; set; }
        public string? BlockingReason { get; set; }
        public int ProgressPercentage { get; set; }
    }

    public interface IAssessmentStateMachineService
    {
        Task<StateTransitionResult> TransitionToStateAsync(int sessionId, AssessmentState targetState, Dictionary<string, object>? transitionData = null);
        Task<AssessmentStateInfo> GetCurrentStateInfoAsync(int sessionId);
        Task<bool> CanTransitionToStateAsync(int sessionId, AssessmentState targetState);
        Task<List<AssessmentState>> GetAllowedTransitionsAsync(int sessionId);
        Task<StateTransitionResult> ProcessAnswerSubmissionAsync(int sessionId, int questionId, string selectedOption);
        Task<StateTransitionResult> ProcessClusterRatingSubmissionAsync(int sessionId, Dictionary<int, int> clusterRatings);
        Task<StateTransitionResult> ProcessTieBreakerResolutionAsync(int sessionId, Dictionary<int, string> tieBreakerAnswers);
        Task<StateTransitionResult> ProcessAssessmentRatingAsync(int sessionId, int rating, string? feedback);
        Task<bool> ValidateStateTransitionAsync(int sessionId, AssessmentState fromState, AssessmentState toState);
        Task<Dictionary<string, object>> GetStateRequirementsAsync(AssessmentState state);
    }

    public class AssessmentStateMachineService : IAssessmentStateMachineService
    {
        private readonly IPersonalityRepository _personalityRepository;
        private readonly ILogger<AssessmentStateMachineService> _logger;

        private readonly Dictionary<AssessmentState, List<AssessmentState>> _allowedTransitions = new()
        {
            [AssessmentState.AnswerQuestions] = new() { AssessmentState.RateCareerClusters },
            [AssessmentState.RateCareerClusters] = new() { AssessmentState.CalculateAssessment },
            [AssessmentState.CalculateAssessment] = new() { AssessmentState.TieResolvement, AssessmentState.RateAssessment },
            [AssessmentState.TieResolvement] = new() { AssessmentState.RateAssessment },
            [AssessmentState.RateAssessment] = new() { AssessmentState.Report },
            [AssessmentState.Report] = new() { }
        };

        private readonly Dictionary<AssessmentState, int> _stateProgressPercentages = new()
        {
            [AssessmentState.AnswerQuestions] = 20,
            [AssessmentState.RateCareerClusters] = 40,
            [AssessmentState.CalculateAssessment] = 60,
            [AssessmentState.TieResolvement] = 75,
            [AssessmentState.RateAssessment] = 90,
            [AssessmentState.Report] = 100
        };

        public AssessmentStateMachineService(
            IPersonalityRepository personalityRepository,
            ILogger<AssessmentStateMachineService> logger)
        {
            _personalityRepository = personalityRepository;
            _logger = logger;
        }

        public async Task<StateTransitionResult> TransitionToStateAsync(int sessionId, AssessmentState targetState, Dictionary<string, object>? transitionData = null)
        {
            try
            {
                var session = await _personalityRepository.GetSessionByIdAsync(sessionId);
                if (session == null)
                {
                    return new StateTransitionResult
                    {
                        Success = false,
                        ErrorMessage = "Assessment session not found"
                    };
                }

                var currentState = session.CurrentState;
                
                if (!await ValidateStateTransitionAsync(sessionId, currentState, targetState))
                {
                    return new StateTransitionResult
                    {
                        Success = false,
                        ErrorMessage = $"Invalid state transition from {currentState} to {targetState}"
                    };
                }

                var stateRequirements = await GetStateRequirementsAsync(targetState);
                var validationResult = await ValidateStateRequirementsAsync(sessionId, targetState, stateRequirements, transitionData);
                
                if (!validationResult.Success)
                {
                    return validationResult;
                }

                session.TransitionTo(targetState);
                await _personalityRepository.UpdateAssessmentSessionAsync(session);

                var stateInfo = await GetCurrentStateInfoAsync(sessionId);
                var allowedActions = await GetAllowedActionsForStateAsync(targetState);

                _logger.LogInformation("Session {SessionId} transitioned from {FromState} to {ToState}", 
                    sessionId, currentState, targetState);

                return new StateTransitionResult
                {
                    Success = true,
                    NewState = targetState,
                    StateData = stateInfo.StateData,
                    AllowedActions = allowedActions,
                    RequiresTieBreaker = targetState == AssessmentState.TieResolvement,
                    TieBreakerQuestions = targetState == AssessmentState.TieResolvement 
                        ? await GetTieBreakerQuestionsAsync(sessionId) 
                        : new List<TieBreakerQuestion>()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transitioning session {SessionId} to state {TargetState}", sessionId, targetState);
                return new StateTransitionResult
                {
                    Success = false,
                    ErrorMessage = "Failed to transition state"
                };
            }
        }

        public async Task<AssessmentStateInfo> GetCurrentStateInfoAsync(int sessionId)
        {
            try
            {
                var session = await _personalityRepository.GetSessionByIdAsync(sessionId);
                if (session == null)
                {
                    throw new ArgumentException($"Assessment session {sessionId} not found");
                }

                var allowedTransitions = await GetAllowedTransitionsAsync(sessionId);
                var canProgress = await CanProgressFromCurrentStateAsync(sessionId);
                var blockingReason = canProgress ? null : await GetProgressBlockingReasonAsync(sessionId);

                return new AssessmentStateInfo
                {
                    CurrentState = session.CurrentState,
                    PreviousState = null, // Not tracked in current entity
                    StateEnteredAt = session.UpdatedAt,
                    StateData = await GetStateDataAsync(sessionId, session.CurrentState),
                    AllowedTransitions = allowedTransitions.Select(s => s.ToString()).ToList(),
                    CanProgress = canProgress,
                    BlockingReason = blockingReason,
                    ProgressPercentage = _stateProgressPercentages.GetValueOrDefault(session.CurrentState, 0)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting state info for session {SessionId}", sessionId);
                throw;
            }
        }

        public async Task<bool> CanTransitionToStateAsync(int sessionId, AssessmentState targetState)
        {
            try
            {
                var session = await _personalityRepository.GetSessionByIdAsync(sessionId);
                if (session == null) return false;

                return session.CanTransitionTo(targetState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking transition possibility for session {SessionId} to state {TargetState}", sessionId, targetState);
                return false;
            }
        }

        public async Task<List<AssessmentState>> GetAllowedTransitionsAsync(int sessionId)
        {
            try
            {
                var session = await _personalityRepository.GetSessionByIdAsync(sessionId);
                if (session == null) return new List<AssessmentState>();

                var allowedStates = _allowedTransitions.GetValueOrDefault(session.CurrentState, new List<AssessmentState>());
                var validTransitions = new List<AssessmentState>();

                foreach (var state in allowedStates)
                {
                    if (await CanTransitionToStateAsync(sessionId, state))
                    {
                        validTransitions.Add(state);
                    }
                }

                return validTransitions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting allowed transitions for session {SessionId}", sessionId);
                return new List<AssessmentState>();
            }
        }

        public async Task<StateTransitionResult> ProcessAnswerSubmissionAsync(int sessionId, int questionId, string selectedOption)
        {
            try
            {
                var session = await _personalityRepository.GetSessionByIdAsync(sessionId);
                if (session == null || session.CurrentState != AssessmentState.AnswerQuestions)
                {
                    return new StateTransitionResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid state for answer submission"
                    };
                }

                var answers = await _personalityRepository.GetAnswersBySessionIdAsync(sessionId);
                var allQuestions = await _personalityRepository.GetQuestionsOrderedAsync();
                var totalQuestions = allQuestions.Count;
                
                var isComplete = answers.Count >= totalQuestions;
                
                if (isComplete)
                {
                    return await TransitionToStateAsync(sessionId, AssessmentState.RateCareerClusters);
                }

                return new StateTransitionResult
                {
                    Success = true,
                    NewState = AssessmentState.AnswerQuestions,
                    StateData = new Dictionary<string, object>
                    {
                        ["answered_questions"] = answers.Count,
                        ["total_questions"] = totalQuestions,
                        ["progress_percentage"] = totalQuestions > 0 ? (int)((double)answers.Count / totalQuestions * 100) : 0
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing answer submission for session {SessionId}", sessionId);
                return new StateTransitionResult
                {
                    Success = false,
                    ErrorMessage = "Failed to process answer submission"
                };
            }
        }

        public async Task<StateTransitionResult> ProcessClusterRatingSubmissionAsync(int sessionId, Dictionary<int, int> clusterRatings)
        {
            try
            {
                var session = await _personalityRepository.GetSessionByIdAsync(sessionId);
                if (session == null || session.CurrentState != AssessmentState.RateCareerClusters)
                {
                    return new StateTransitionResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid state for cluster rating submission"
                    };
                }

                var requiredClusterCount = 16;
                
                if (clusterRatings.Count < requiredClusterCount)
                {
                    return new StateTransitionResult
                    {
                        Success = false,
                        ErrorMessage = $"All {requiredClusterCount} career clusters must be rated"
                    };
                }

                foreach (var (clusterId, rating) in clusterRatings)
                {
                    if (rating < 1 || rating > 5)
                    {
                        return new StateTransitionResult
                        {
                            Success = false,
                            ErrorMessage = "Cluster ratings must be between 1 and 5"
                        };
                    }
                }

                return await TransitionToStateAsync(sessionId, AssessmentState.CalculateAssessment, 
                    new Dictionary<string, object> { ["cluster_ratings"] = clusterRatings });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing cluster rating submission for session {SessionId}", sessionId);
                return new StateTransitionResult
                {
                    Success = false,
                    ErrorMessage = "Failed to process cluster rating submission"
                };
            }
        }

        public async Task<StateTransitionResult> ProcessTieBreakerResolutionAsync(int sessionId, Dictionary<int, string> tieBreakerAnswers)
        {
            try
            {
                var session = await _personalityRepository.GetSessionByIdAsync(sessionId);
                if (session == null || session.CurrentState != AssessmentState.TieResolvement)
                {
                    return new StateTransitionResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid state for tie-breaker resolution"
                    };
                }

                var tieBreakerQuestions = await GetTieBreakerQuestionsAsync(sessionId);
                
                if (tieBreakerAnswers.Count < tieBreakerQuestions.Count)
                {
                    return new StateTransitionResult
                    {
                        Success = false,
                        ErrorMessage = "All tie-breaker questions must be answered"
                    };
                }

                return await TransitionToStateAsync(sessionId, AssessmentState.RateAssessment,
                    new Dictionary<string, object> { ["tie_breaker_answers"] = tieBreakerAnswers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing tie-breaker resolution for session {SessionId}", sessionId);
                return new StateTransitionResult
                {
                    Success = false,
                    ErrorMessage = "Failed to process tie-breaker resolution"
                };
            }
        }

        public async Task<StateTransitionResult> ProcessAssessmentRatingAsync(int sessionId, int rating, string? feedback)
        {
            try
            {
                var session = await _personalityRepository.GetSessionByIdAsync(sessionId);
                if (session == null || session.CurrentState != AssessmentState.RateAssessment)
                {
                    return new StateTransitionResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid state for assessment rating"
                    };
                }

                if (rating < 1 || rating > 5)
                {
                    return new StateTransitionResult
                    {
                        Success = false,
                        ErrorMessage = "Assessment rating must be between 1 and 5"
                    };
                }

                return await TransitionToStateAsync(sessionId, AssessmentState.Report,
                    new Dictionary<string, object> 
                    { 
                        ["assessment_rating"] = rating,
                        ["assessment_feedback"] = feedback ?? string.Empty
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing assessment rating for session {SessionId}", sessionId);
                return new StateTransitionResult
                {
                    Success = false,
                    ErrorMessage = "Failed to process assessment rating"
                };
            }
        }

        public async Task<bool> ValidateStateTransitionAsync(int sessionId, AssessmentState fromState, AssessmentState toState)
        {
            try
            {
                if (!_allowedTransitions.ContainsKey(fromState))
                {
                    return false;
                }

                var allowedTransitions = _allowedTransitions[fromState];
                if (!allowedTransitions.Contains(toState))
                {
                    return false;
                }

                var stateRequirements = await GetStateRequirementsAsync(toState);
                var validationResult = await ValidateStateRequirementsAsync(sessionId, toState, stateRequirements, null);
                
                return validationResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating state transition for session {SessionId}", sessionId);
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetStateRequirementsAsync(AssessmentState state)
        {
            return state switch
            {
                AssessmentState.AnswerQuestions => new Dictionary<string, object>
                {
                    ["requires_session"] = true,
                    ["min_questions_answered"] = 0
                },
                AssessmentState.RateCareerClusters => new Dictionary<string, object>
                {
                    ["requires_all_questions_answered"] = true,
                    ["requires_personality_scores"] = true
                },
                AssessmentState.CalculateAssessment => new Dictionary<string, object>
                {
                    ["requires_cluster_ratings"] = true,
                    ["min_cluster_ratings"] = 16
                },
                AssessmentState.TieResolvement => new Dictionary<string, object>
                {
                    ["requires_tie_detected"] = true,
                    ["requires_tie_breaker_questions"] = true
                },
                AssessmentState.RateAssessment => new Dictionary<string, object>
                {
                    ["requires_assessment_completed"] = true,
                    ["requires_personality_type_determined"] = true
                },
                AssessmentState.Report => new Dictionary<string, object>
                {
                    ["requires_assessment_rating"] = true,
                    ["requires_final_results"] = true
                },
                _ => new Dictionary<string, object>()
            };
        }

        private async Task<StateTransitionResult> ValidateStateRequirementsAsync(
            int sessionId, 
            AssessmentState targetState, 
            Dictionary<string, object> requirements,
            Dictionary<string, object>? transitionData)
        {
            try
            {
                var session = await _personalityRepository.GetSessionByIdAsync(sessionId);
                if (session == null)
                {
                    return new StateTransitionResult
                    {
                        Success = false,
                        ErrorMessage = "Session not found"
                    };
                }

                foreach (var (requirement, value) in requirements)
                {
                    switch (requirement)
                    {
                        case "requires_all_questions_answered":
                            if ((bool)value)
                            {
                                var answers = await _personalityRepository.GetAnswersBySessionIdAsync(sessionId);
                                var allQuestions = await _personalityRepository.GetQuestionsOrderedAsync();
                                if (answers.Count < allQuestions.Count)
                                {
                                    return new StateTransitionResult
                                    {
                                        Success = false,
                                        ErrorMessage = "All questions must be answered before proceeding"
                                    };
                                }
                            }
                            break;

                        case "requires_cluster_ratings":
                            if ((bool)value)
                            {
                                var hasRatings = transitionData?.ContainsKey("cluster_ratings") == true ||
                                               await HasClusterRatingsAsync(sessionId);
                                if (!hasRatings)
                                {
                                    return new StateTransitionResult
                                    {
                                        Success = false,
                                        ErrorMessage = "Career cluster ratings are required"
                                    };
                                }
                            }
                            break;

                        case "requires_tie_detected":
                            if ((bool)value)
                            {
                                var hasTie = await HasPersonalityTieAsync(sessionId);
                                if (!hasTie)
                                {
                                    return new StateTransitionResult
                                    {
                                        Success = false,
                                        ErrorMessage = "No personality tie detected, skipping tie resolution"
                                    };
                                }
                            }
                            break;

                        case "requires_assessment_completed":
                            if ((bool)value)
                            {
                                var isCompleted = await IsAssessmentCompletedAsync(sessionId);
                                if (!isCompleted)
                                {
                                    return new StateTransitionResult
                                    {
                                        Success = false,
                                        ErrorMessage = "Assessment must be completed first"
                                    };
                                }
                            }
                            break;
                    }
                }

                return new StateTransitionResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating state requirements for session {SessionId}", sessionId);
                return new StateTransitionResult
                {
                    Success = false,
                    ErrorMessage = "Failed to validate state requirements"
                };
            }
        }

        private async Task<List<string>> GetAllowedActionsForStateAsync(AssessmentState state)
        {
            return state switch
            {
                AssessmentState.AnswerQuestions => new List<string> { "submit_answer", "get_next_question", "get_progress" },
                AssessmentState.RateCareerClusters => new List<string> { "submit_cluster_rating", "get_clusters", "get_progress" },
                AssessmentState.CalculateAssessment => new List<string> { "get_calculation_status", "get_preliminary_results" },
                AssessmentState.TieResolvement => new List<string> { "submit_tie_breaker_answer", "get_tie_breaker_questions" },
                AssessmentState.RateAssessment => new List<string> { "submit_assessment_rating", "get_assessment_results" },
                AssessmentState.Report => new List<string> { "get_final_report", "download_report", "share_report" },
                _ => new List<string>()
            };
        }

        private async Task<Dictionary<string, object>> GetStateDataAsync(int sessionId, AssessmentState state)
        {
            var data = new Dictionary<string, object>();

            switch (state)
            {
                case AssessmentState.AnswerQuestions:
                    var answers = await _personalityRepository.GetAnswersBySessionIdAsync(sessionId);
                    var allQuestions = await _personalityRepository.GetQuestionsOrderedAsync();
                    data["answered_questions"] = answers.Count;
                    data["total_questions"] = allQuestions.Count;
                    data["progress_percentage"] = allQuestions.Count > 0 ? (int)((double)answers.Count / allQuestions.Count * 100) : 0;
                    break;

                case AssessmentState.RateCareerClusters:
                    data["total_clusters"] = 16; // Standard MBTI career clusters
                    data["clusters_rated"] = await GetRatedClustersCountAsync(sessionId);
                    break;

                case AssessmentState.CalculateAssessment:
                    data["calculation_status"] = "in_progress";
                    data["estimated_completion"] = DateTime.UtcNow.AddSeconds(30);
                    break;

                case AssessmentState.TieResolvement:
                    var tieBreakerQuestions = await GetTieBreakerQuestionsAsync(sessionId);
                    data["tie_breaker_questions_count"] = tieBreakerQuestions.Count;
                    data["tie_dimensions"] = await GetTiedDimensionsAsync(sessionId);
                    break;

                case AssessmentState.RateAssessment:
                    data["personality_type"] = await GetDeterminedPersonalityTypeAsync(sessionId) ?? string.Empty;
                    data["assessment_accuracy"] = await GetAssessmentAccuracyAsync(sessionId);
                    break;

                case AssessmentState.Report:
                    data["report_generated"] = true;
                    data["report_sections"] = await GetReportSectionsAsync(sessionId);
                    break;
            }

            return data;
        }

        private async Task<bool> CanProgressFromCurrentStateAsync(int sessionId)
        {
            var session = await _personalityRepository.GetSessionByIdAsync(sessionId);
            if (session == null) return false;

            return session.CurrentState switch
            {
                AssessmentState.AnswerQuestions => await AreAllQuestionsAnsweredAsync(sessionId),
                AssessmentState.RateCareerClusters => await AreAllClustersRatedAsync(sessionId),
                AssessmentState.CalculateAssessment => await IsCalculationCompleteAsync(sessionId),
                AssessmentState.TieResolvement => await AreTieBreakersResolvedAsync(sessionId),
                AssessmentState.RateAssessment => await IsAssessmentRatedAsync(sessionId),
                AssessmentState.Report => true,
                _ => false
            };
        }

        private async Task<string?> GetProgressBlockingReasonAsync(int sessionId)
        {
            var session = await _personalityRepository.GetSessionByIdAsync(sessionId);
            if (session == null) return "Session not found";

            return session.CurrentState switch
            {
                AssessmentState.AnswerQuestions => await AreAllQuestionsAnsweredAsync(sessionId) ? null : "Not all questions have been answered",
                AssessmentState.RateCareerClusters => await AreAllClustersRatedAsync(sessionId) ? null : "Not all career clusters have been rated",
                AssessmentState.CalculateAssessment => await IsCalculationCompleteAsync(sessionId) ? null : "Assessment calculation in progress",
                AssessmentState.TieResolvement => await AreTieBreakersResolvedAsync(sessionId) ? null : "Tie-breaker questions need to be answered",
                AssessmentState.RateAssessment => await IsAssessmentRatedAsync(sessionId) ? null : "Assessment needs to be rated",
                AssessmentState.Report => null,
                _ => "Unknown state"
            };
        }

        private async Task<List<TieBreakerQuestion>> GetTieBreakerQuestionsAsync(int sessionId)
        {
            try
            {
                var tiedDimensions = await GetTiedDimensionsAsync(sessionId);
                var tieBreakerQuestions = new List<TieBreakerQuestion>();

                return tieBreakerQuestions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tie-breaker questions for session {SessionId}", sessionId);
                return new List<TieBreakerQuestion>();
            }
        }

        private async Task<List<PersonalityDimension>> GetTiedDimensionsAsync(int sessionId)
        {
            return new List<PersonalityDimension>();
        }

        private async Task<bool> HasClusterRatingsAsync(int sessionId)
        {
            return false;
        }

        private async Task<bool> HasPersonalityTieAsync(int sessionId)
        {
            return false;
        }

        private async Task<bool> IsAssessmentCompletedAsync(int sessionId)
        {
            return true;
        }

        private async Task<int> GetRatedClustersCountAsync(int sessionId)
        {
            return 0;
        }

        private async Task<string?> GetDeterminedPersonalityTypeAsync(int sessionId)
        {
            return "INTJ";
        }

        private async Task<double> GetAssessmentAccuracyAsync(int sessionId)
        {
            return 0.85;
        }

        private async Task<List<string>> GetReportSectionsAsync(int sessionId)
        {
            return new List<string> { "personality_overview", "career_matches", "development_recommendations" };
        }

        private async Task<bool> AreAllQuestionsAnsweredAsync(int sessionId)
        {
            var answers = await _personalityRepository.GetAnswersBySessionIdAsync(sessionId);
            var allQuestions = await _personalityRepository.GetQuestionsOrderedAsync();
            return answers.Count >= allQuestions.Count;
        }

        private async Task<bool> AreAllClustersRatedAsync(int sessionId)
        {
            return false;
        }

        private async Task<bool> IsCalculationCompleteAsync(int sessionId)
        {
            return true;
        }

        private async Task<bool> AreTieBreakersResolvedAsync(int sessionId)
        {
            return true;
        }

        private async Task<bool> IsAssessmentRatedAsync(int sessionId)
        {
            return false;
        }
    }
}
