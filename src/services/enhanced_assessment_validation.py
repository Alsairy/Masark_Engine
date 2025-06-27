"""
Enhanced Assessment Validation Service for Masark Engine
Provides comprehensive validation of assessment quality, response patterns,
and professional-grade psychometric analysis
"""

from typing import Dict, List, Tuple, Optional, NamedTuple
from dataclasses import dataclass
from datetime import datetime, timedelta
import statistics
import math
from models.masark_models import (
    AssessmentSession, AssessmentAnswer, Question, PersonalityType, db
)
from services.enhanced_personality_scoring import EnhancedPersonalityScoringService
import logging

logger = logging.getLogger(__name__)

@dataclass
class ResponsePattern:
    """Analysis of response patterns for validation"""
    total_responses: int
    response_distribution: Dict[str, int]  # A vs B distribution
    response_time_analysis: Dict[str, float]  # If timing data available
    sequential_patterns: Dict[str, int]  # Patterns like AAAA, ABAB, etc.
    dimension_balance: Dict[str, float]  # Balance across dimensions

@dataclass
class QualityFlags:
    """Quality flags for assessment validation"""
    rapid_completion: bool  # Completed too quickly
    uniform_responses: bool  # Too many same responses in a row
    extreme_bias: bool  # Strong bias toward one option
    inconsistent_responses: bool  # Inconsistent with similar questions
    incomplete_engagement: bool  # Signs of disengagement
    
@dataclass
class ValidationReport:
    """Comprehensive validation report"""
    session_id: int
    overall_validity: float  # 0-1 score
    quality_level: str  # Excellent, Good, Acceptable, Poor
    response_pattern: ResponsePattern
    quality_flags: QualityFlags
    recommendations: List[str]
    detailed_analysis: Dict[str, any]
    pass_threshold: bool  # Whether assessment meets minimum standards

class EnhancedAssessmentValidationService:
    """
    Service for comprehensive assessment validation using professional standards
    """
    
    # Validation thresholds
    VALIDITY_THRESHOLDS = {
        'excellent': 0.85,
        'good': 0.70,
        'acceptable': 0.55,
        'poor': 0.40
    }
    
    # Response pattern thresholds
    PATTERN_THRESHOLDS = {
        'max_consecutive_same': 8,  # Maximum consecutive same responses
        'min_response_time_per_question': 3,  # Minimum seconds per question
        'max_response_time_per_question': 300,  # Maximum seconds per question
        'extreme_bias_threshold': 0.8,  # >80% same response = extreme bias
        'minimum_dimension_balance': 0.2  # Each dimension should have at least 20% responses
    }
    
    def __init__(self):
        self.logger = logging.getLogger(f"{__name__}.{self.__class__.__name__}")
        self.scoring_service = EnhancedPersonalityScoringService()
    
    def validate_assessment(self, session_id: int) -> ValidationReport:
        """
        Perform comprehensive assessment validation
        
        Args:
            session_id: ID of the assessment session to validate
            
        Returns:
            ValidationReport with detailed analysis
        """
        try:
            # Get session and related data
            session = AssessmentSession.query.get(session_id)
            if not session:
                raise ValueError(f"Session {session_id} not found")
            
            answers = AssessmentAnswer.query.filter_by(session_id=session_id).all()
            questions = {q.id: q for q in Question.query.filter_by(is_active=True).all()}
            
            # Analyze response patterns
            response_pattern = self._analyze_response_patterns(answers, questions, session)
            
            # Identify quality flags
            quality_flags = self._identify_quality_flags(answers, questions, session, response_pattern)
            
            # Calculate overall validity score
            validity_score = self._calculate_validity_score(response_pattern, quality_flags)
            
            # Determine quality level
            quality_level = self._determine_quality_level(validity_score)
            
            # Generate recommendations
            recommendations = self._generate_recommendations(quality_flags, validity_score, response_pattern)
            
            # Create detailed analysis
            detailed_analysis = self._create_detailed_analysis(answers, questions, session, response_pattern)
            
            # Determine if assessment passes minimum standards
            pass_threshold = validity_score >= self.VALIDITY_THRESHOLDS['acceptable']
            
            report = ValidationReport(
                session_id=session_id,
                overall_validity=validity_score,
                quality_level=quality_level,
                response_pattern=response_pattern,
                quality_flags=quality_flags,
                recommendations=recommendations,
                detailed_analysis=detailed_analysis,
                pass_threshold=pass_threshold
            )
            
            self.logger.info(f"Assessment validation completed for session {session_id}: "
                           f"{quality_level} quality ({validity_score:.2f})")
            
            return report
            
        except Exception as e:
            self.logger.error(f"Error validating assessment {session_id}: {str(e)}")
            raise
    
    def _analyze_response_patterns(self, answers: List[AssessmentAnswer], 
                                 questions: Dict[int, Question],
                                 session: AssessmentSession) -> ResponsePattern:
        """Analyze response patterns for validation"""
        
        # Basic response distribution
        response_distribution = {'A': 0, 'B': 0}
        for answer in answers:
            response_distribution[answer.selected_option] += 1
        
        # Sequential pattern analysis
        response_sequence = [answer.selected_option for answer in sorted(answers, key=lambda x: x.question_id)]
        sequential_patterns = self._analyze_sequential_patterns(response_sequence)
        
        # Dimension balance analysis
        dimension_balance = self._analyze_dimension_balance(answers, questions)
        
        # Response time analysis (if available)
        response_time_analysis = self._analyze_response_times(answers, session)
        
        return ResponsePattern(
            total_responses=len(answers),
            response_distribution=response_distribution,
            response_time_analysis=response_time_analysis,
            sequential_patterns=sequential_patterns,
            dimension_balance=dimension_balance
        )
    
    def _analyze_sequential_patterns(self, response_sequence: List[str]) -> Dict[str, int]:
        """Analyze sequential response patterns"""
        patterns = {
            'consecutive_A': 0,
            'consecutive_B': 0,
            'alternating_AB': 0,
            'alternating_BA': 0,
            'max_consecutive_same': 0,
            'total_runs': 0
        }
        
        if not response_sequence:
            return patterns
        
        # Count consecutive responses
        current_consecutive = 1
        max_consecutive = 1
        runs = 1
        
        for i in range(1, len(response_sequence)):
            if response_sequence[i] == response_sequence[i-1]:
                current_consecutive += 1
                max_consecutive = max(max_consecutive, current_consecutive)
            else:
                current_consecutive = 1
                runs += 1
        
        patterns['max_consecutive_same'] = max_consecutive
        patterns['total_runs'] = runs
        
        # Count specific patterns
        for i in range(len(response_sequence) - 1):
            if response_sequence[i] == 'A' and response_sequence[i+1] == 'A':
                patterns['consecutive_A'] += 1
            elif response_sequence[i] == 'B' and response_sequence[i+1] == 'B':
                patterns['consecutive_B'] += 1
            elif response_sequence[i] == 'A' and response_sequence[i+1] == 'B':
                patterns['alternating_AB'] += 1
            elif response_sequence[i] == 'B' and response_sequence[i+1] == 'A':
                patterns['alternating_BA'] += 1
        
        return patterns
    
    def _analyze_dimension_balance(self, answers: List[AssessmentAnswer], 
                                 questions: Dict[int, Question]) -> Dict[str, float]:
        """Analyze balance of responses across personality dimensions"""
        dimension_counts = {'EI': 0, 'SN': 0, 'TF': 0, 'JP': 0}
        total_responses = len(answers)
        
        for answer in answers:
            question = questions.get(answer.question_id)
            if question:
                dimension = question.dimension.value.replace('-', '')
                if dimension in dimension_counts:
                    dimension_counts[dimension] += 1
        
        # Convert to proportions
        dimension_balance = {}
        for dim, count in dimension_counts.items():
            dimension_balance[dim] = count / total_responses if total_responses > 0 else 0
        
        return dimension_balance
    
    def _analyze_response_times(self, answers: List[AssessmentAnswer], 
                              session: AssessmentSession) -> Dict[str, float]:
        """Analyze response times if available"""
        # Placeholder for response time analysis
        # In a real implementation, you would have timing data
        
        analysis = {
            'average_time_per_question': 30.0,  # Default assumption
            'total_time': 1800.0,  # 30 minutes default
            'time_variance': 0.5,
            'rapid_responses': 0,
            'slow_responses': 0
        }
        
        # If session has timing data, calculate actual metrics
        if session.started_at and session.completed_at:
            total_time = (session.completed_at - session.started_at).total_seconds()
            avg_time = total_time / len(answers) if answers else 0
            
            analysis['total_time'] = total_time
            analysis['average_time_per_question'] = avg_time
            
            # Flag rapid completion
            if avg_time < self.PATTERN_THRESHOLDS['min_response_time_per_question']:
                analysis['rapid_responses'] = len(answers)
            elif avg_time > self.PATTERN_THRESHOLDS['max_response_time_per_question']:
                analysis['slow_responses'] = len(answers)
        
        return analysis
    
    def _identify_quality_flags(self, answers: List[AssessmentAnswer], 
                              questions: Dict[int, Question],
                              session: AssessmentSession,
                              response_pattern: ResponsePattern) -> QualityFlags:
        """Identify quality flags that may indicate invalid responses"""
        
        # Rapid completion flag
        rapid_completion = (response_pattern.response_time_analysis['average_time_per_question'] < 
                          self.PATTERN_THRESHOLDS['min_response_time_per_question'])
        
        # Uniform responses flag
        uniform_responses = (response_pattern.sequential_patterns['max_consecutive_same'] > 
                           self.PATTERN_THRESHOLDS['max_consecutive_same'])
        
        # Extreme bias flag
        total_responses = response_pattern.total_responses
        if total_responses > 0:
            a_ratio = response_pattern.response_distribution['A'] / total_responses
            extreme_bias = (a_ratio > self.PATTERN_THRESHOLDS['extreme_bias_threshold'] or 
                          a_ratio < (1 - self.PATTERN_THRESHOLDS['extreme_bias_threshold']))
        else:
            extreme_bias = False
        
        # Inconsistent responses flag (simplified)
        inconsistent_responses = self._check_response_consistency(answers, questions)
        
        # Incomplete engagement flag
        incomplete_engagement = self._check_engagement_level(response_pattern, session)
        
        return QualityFlags(
            rapid_completion=rapid_completion,
            uniform_responses=uniform_responses,
            extreme_bias=extreme_bias,
            inconsistent_responses=inconsistent_responses,
            incomplete_engagement=incomplete_engagement
        )
    
    def _check_response_consistency(self, answers: List[AssessmentAnswer], 
                                  questions: Dict[int, Question]) -> bool:
        """Check for inconsistent responses across similar questions"""
        # This is a simplified check - in practice, you'd identify semantically similar questions
        # and check if responses are consistent
        
        # For now, check if responses within each dimension are too varied
        dimension_responses = {'EI': [], 'SN': [], 'TF': [], 'JP': []}
        
        for answer in answers:
            question = questions.get(answer.question_id)
            if question:
                dimension = question.dimension.value.replace('-', '')
                if dimension in dimension_responses:
                    # Convert to numeric for analysis
                    numeric_response = 1 if answer.selected_option == 'A' else 0
                    dimension_responses[dimension].append(numeric_response)
        
        # Check variance within dimensions
        high_variance_count = 0
        for dim, responses in dimension_responses.items():
            if len(responses) > 2:
                variance = statistics.variance(responses)
                # High variance (close to 0.25 for binary) indicates inconsistency
                if variance > 0.2:
                    high_variance_count += 1
        
        # If more than half the dimensions show high variance, flag as inconsistent
        return high_variance_count > 2
    
    def _check_engagement_level(self, response_pattern: ResponsePattern, 
                              session: AssessmentSession) -> bool:
        """Check for signs of incomplete engagement"""
        
        # Multiple indicators of disengagement
        disengagement_indicators = 0
        
        # Too many consecutive same responses
        if response_pattern.sequential_patterns['max_consecutive_same'] > 6:
            disengagement_indicators += 1
        
        # Extreme response bias
        total = response_pattern.total_responses
        if total > 0:
            a_ratio = response_pattern.response_distribution['A'] / total
            if a_ratio > 0.9 or a_ratio < 0.1:
                disengagement_indicators += 1
        
        # Too rapid completion
        if response_pattern.response_time_analysis['average_time_per_question'] < 5:
            disengagement_indicators += 1
        
        # Too few runs (alternations between A and B)
        expected_runs = total / 2 if total > 0 else 0
        actual_runs = response_pattern.sequential_patterns['total_runs']
        if expected_runs > 0 and actual_runs < expected_runs * 0.3:
            disengagement_indicators += 1
        
        # If 2 or more indicators, flag as incomplete engagement
        return disengagement_indicators >= 2
    
    def _calculate_validity_score(self, response_pattern: ResponsePattern, 
                                quality_flags: QualityFlags) -> float:
        """Calculate overall validity score based on patterns and flags"""
        
        # Start with perfect score
        validity_score = 1.0
        
        # Deduct points for quality flags
        flag_penalties = {
            'rapid_completion': 0.2,
            'uniform_responses': 0.3,
            'extreme_bias': 0.25,
            'inconsistent_responses': 0.15,
            'incomplete_engagement': 0.35
        }
        
        for flag_name, penalty in flag_penalties.items():
            if getattr(quality_flags, flag_name):
                validity_score -= penalty
        
        # Additional deductions based on response patterns
        
        # Penalty for extreme consecutive responses
        max_consecutive = response_pattern.sequential_patterns['max_consecutive_same']
        if max_consecutive > 10:
            validity_score -= 0.2
        elif max_consecutive > 8:
            validity_score -= 0.1
        
        # Penalty for extreme response distribution
        total = response_pattern.total_responses
        if total > 0:
            a_ratio = response_pattern.response_distribution['A'] / total
            if a_ratio > 0.85 or a_ratio < 0.15:
                validity_score -= 0.15
        
        # Penalty for poor dimension balance
        min_dimension_balance = min(response_pattern.dimension_balance.values()) if response_pattern.dimension_balance else 0
        if min_dimension_balance < self.PATTERN_THRESHOLDS['minimum_dimension_balance']:
            validity_score -= 0.1
        
        # Ensure score is between 0 and 1
        return max(0.0, min(1.0, validity_score))
    
    def _determine_quality_level(self, validity_score: float) -> str:
        """Determine quality level based on validity score"""
        if validity_score >= self.VALIDITY_THRESHOLDS['excellent']:
            return 'Excellent'
        elif validity_score >= self.VALIDITY_THRESHOLDS['good']:
            return 'Good'
        elif validity_score >= self.VALIDITY_THRESHOLDS['acceptable']:
            return 'Acceptable'
        else:
            return 'Poor'
    
    def _generate_recommendations(self, quality_flags: QualityFlags, 
                                validity_score: float,
                                response_pattern: ResponsePattern) -> List[str]:
        """Generate recommendations based on validation results"""
        recommendations = []
        
        if quality_flags.rapid_completion:
            recommendations.append("Consider retaking the assessment with more time for reflection on each question")
        
        if quality_flags.uniform_responses:
            recommendations.append("Response pattern suggests possible disengagement - consider retesting in a different setting")
        
        if quality_flags.extreme_bias:
            recommendations.append("Strong response bias detected - results may not accurately reflect personality preferences")
        
        if quality_flags.inconsistent_responses:
            recommendations.append("Some responses appear inconsistent - consider discussing results with a qualified professional")
        
        if quality_flags.incomplete_engagement:
            recommendations.append("Assessment may not reflect true preferences due to incomplete engagement")
        
        if validity_score < self.VALIDITY_THRESHOLDS['acceptable']:
            recommendations.append("Assessment quality is below acceptable standards - retesting is strongly recommended")
        
        if validity_score >= self.VALIDITY_THRESHOLDS['excellent']:
            recommendations.append("Excellent assessment quality - results are highly reliable")
        
        # Pattern-specific recommendations
        max_consecutive = response_pattern.sequential_patterns['max_consecutive_same']
        if max_consecutive > 12:
            recommendations.append("Extremely uniform response pattern detected - consider whether all questions were read carefully")
        
        return recommendations
    
    def _create_detailed_analysis(self, answers: List[AssessmentAnswer], 
                                questions: Dict[int, Question],
                                session: AssessmentSession,
                                response_pattern: ResponsePattern) -> Dict[str, any]:
        """Create detailed analysis for the validation report"""
        
        analysis = {
            'response_statistics': {
                'total_questions': len(answers),
                'response_distribution': response_pattern.response_distribution,
                'dimension_balance': response_pattern.dimension_balance,
                'sequential_patterns': response_pattern.sequential_patterns
            },
            'timing_analysis': response_pattern.response_time_analysis,
            'session_metadata': {
                'started_at': session.started_at.isoformat() if session.started_at else None,
                'completed_at': session.completed_at.isoformat() if session.completed_at else None,
                'language_preference': session.language_preference,
                'deployment_mode': session.deployment_mode
            }
        }
        
        # Add statistical measures
        if answers:
            response_sequence = [1 if answer.selected_option == 'A' else 0 
                               for answer in sorted(answers, key=lambda x: x.question_id)]
            
            analysis['statistical_measures'] = {
                'mean_response': statistics.mean(response_sequence),
                'response_variance': statistics.variance(response_sequence) if len(response_sequence) > 1 else 0,
                'response_range': max(response_sequence) - min(response_sequence),
                'response_entropy': self._calculate_entropy(response_sequence)
            }
        
        return analysis
    
    def _calculate_entropy(self, response_sequence: List[int]) -> float:
        """Calculate entropy of response sequence"""
        if not response_sequence:
            return 0.0
        
        # Count occurrences
        counts = {0: 0, 1: 0}
        for response in response_sequence:
            counts[response] += 1
        
        # Calculate entropy
        total = len(response_sequence)
        entropy = 0.0
        
        for count in counts.values():
            if count > 0:
                probability = count / total
                entropy -= probability * math.log2(probability)
        
        return entropy
    
    def batch_validate_assessments(self, session_ids: List[int]) -> Dict[int, ValidationReport]:
        """Validate multiple assessments in batch"""
        results = {}
        
        for session_id in session_ids:
            try:
                results[session_id] = self.validate_assessment(session_id)
            except Exception as e:
                self.logger.error(f"Error validating session {session_id}: {str(e)}")
                # Create a minimal error report
                results[session_id] = ValidationReport(
                    session_id=session_id,
                    overall_validity=0.0,
                    quality_level='Error',
                    response_pattern=ResponsePattern(0, {}, {}, {}, {}),
                    quality_flags=QualityFlags(False, False, False, False, False),
                    recommendations=[f"Error during validation: {str(e)}"],
                    detailed_analysis={},
                    pass_threshold=False
                )
        
        return results
    
    def get_validation_summary(self, session_ids: List[int]) -> Dict[str, any]:
        """Get summary statistics for multiple assessment validations"""
        validation_results = self.batch_validate_assessments(session_ids)
        
        valid_results = [r for r in validation_results.values() if r.quality_level != 'Error']
        
        if not valid_results:
            return {'error': 'No valid assessments to analyze'}
        
        validity_scores = [r.overall_validity for r in valid_results]
        quality_levels = [r.quality_level for r in valid_results]
        
        summary = {
            'total_assessments': len(session_ids),
            'valid_assessments': len(valid_results),
            'validity_statistics': {
                'mean_validity': statistics.mean(validity_scores),
                'median_validity': statistics.median(validity_scores),
                'min_validity': min(validity_scores),
                'max_validity': max(validity_scores),
                'std_validity': statistics.stdev(validity_scores) if len(validity_scores) > 1 else 0
            },
            'quality_distribution': {
                level: quality_levels.count(level) for level in ['Excellent', 'Good', 'Acceptable', 'Poor']
            },
            'pass_rate': sum(1 for r in valid_results if r.pass_threshold) / len(valid_results),
            'common_flags': self._analyze_common_flags(valid_results)
        }
        
        return summary
    
    def _analyze_common_flags(self, validation_results: List[ValidationReport]) -> Dict[str, float]:
        """Analyze common quality flags across multiple assessments"""
        flag_counts = {
            'rapid_completion': 0,
            'uniform_responses': 0,
            'extreme_bias': 0,
            'inconsistent_responses': 0,
            'incomplete_engagement': 0
        }
        
        total = len(validation_results)
        
        for result in validation_results:
            for flag_name in flag_counts.keys():
                if getattr(result.quality_flags, flag_name):
                    flag_counts[flag_name] += 1
        
        # Convert to percentages
        return {flag: count / total for flag, count in flag_counts.items()}

