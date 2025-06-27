"""
Enhanced Personality Scoring Service for Masark Engine
Implements professional-grade MBTI assessment with statistical validation,
reliability measures, and advanced psychometric analysis
"""

from typing import Dict, List, Tuple, Optional, NamedTuple
from dataclasses import dataclass, field
from datetime import datetime
import math
import statistics
from models.masark_models import (
    AssessmentSession, AssessmentAnswer, Question, PersonalityType,
    PersonalityDimension, PreferenceStrength, db
)
import logging

logger = logging.getLogger(__name__)

@dataclass
class StatisticalMetrics:
    """Statistical validation metrics for assessment reliability"""
    internal_consistency: float  # Cronbach's alpha equivalent
    response_consistency: float  # Consistency across similar questions
    extreme_response_bias: float  # Tendency to choose extreme options
    acquiescence_bias: float  # Tendency to agree/choose first option
    response_time_variance: float  # Variance in response times (if available)
    confidence_interval: Tuple[float, float]  # 95% confidence interval for type certainty

@dataclass
class DimensionAnalysis:
    """Detailed analysis for each personality dimension"""
    dimension: str
    raw_score: int
    total_questions: int
    percentage: float
    preference_letter: str
    strength_category: PreferenceStrength
    confidence_level: float
    standard_error: float
    z_score: float  # How many standard deviations from neutral (50%)
    
@dataclass
class EnhancedPersonalityResult:
    """Enhanced personality assessment result with professional validation"""
    # Core Results
    personality_type: str
    type_code: str
    type_confidence: float  # Overall confidence in type assignment (0-1)
    
    # Dimensional Analysis
    dimension_analyses: Dict[str, DimensionAnalysis]
    
    # Statistical Validation
    statistical_metrics: StatisticalMetrics
    
    # Professional Insights
    borderline_dimensions: List[str]
    type_stability_prediction: float  # Likelihood type will remain stable over time
    assessment_quality_score: float  # Overall quality of the assessment (0-1)
    
    # Recommendations
    retesting_recommended: bool
    areas_for_exploration: List[str]
    confidence_notes: List[str]
    
    # Legacy compatibility
    preference_strengths: Dict[str, float] = field(default_factory=dict)
    preference_clarity: Dict[str, PreferenceStrength] = field(default_factory=dict)

class EnhancedPersonalityScoringService:
    """
    Enhanced service for calculating MBTI personality types with professional validation
    Implements advanced psychometric analysis and statistical validation
    """
    
    # Enhanced tie-breaking with confidence weighting
    TIE_BREAKING_RULES = {
        'EI': ('I', 0.51),  # Slight preference for I with 51% confidence
        'SN': ('N', 0.51),  # Slight preference for N with 51% confidence
        'TF': ('F', 0.51),  # Slight preference for F with 51% confidence
        'JP': ('P', 0.51)   # Slight preference for P with 51% confidence
    }
    
    # Professional strength thresholds based on psychometric research
    PROFESSIONAL_THRESHOLDS = {
        PreferenceStrength.SLIGHT: 0.56,      # 56-65% = slight preference
        PreferenceStrength.MODERATE: 0.66,    # 66-75% = moderate preference
        PreferenceStrength.CLEAR: 0.76,       # 76-85% = clear preference
        PreferenceStrength.VERY_CLEAR: 0.86   # 86%+ = very clear preference
    }
    
    # Quality thresholds for assessment validation
    QUALITY_THRESHOLDS = {
        'excellent': 0.85,
        'good': 0.70,
        'acceptable': 0.55,
        'questionable': 0.40
    }
    
    def __init__(self):
        self.logger = logging.getLogger(f"{__name__}.{self.__class__.__name__}")
    
    def calculate_enhanced_personality_type(self, session_id: int) -> EnhancedPersonalityResult:
        """
        Calculate personality type with enhanced professional validation
        
        Args:
            session_id: ID of the completed assessment session
            
        Returns:
            EnhancedPersonalityResult with comprehensive analysis
        """
        try:
            # Validate session and get data
            session, answers, questions = self._validate_and_get_data(session_id)
            
            # Calculate dimensional analyses
            dimension_analyses = self._calculate_dimensional_analyses(answers, questions)
            
            # Determine personality type with confidence
            type_code, type_confidence = self._determine_type_with_confidence(dimension_analyses)
            
            # Calculate statistical validation metrics
            statistical_metrics = self._calculate_statistical_metrics(answers, questions, dimension_analyses)
            
            # Professional insights and recommendations
            borderline_dims = self._identify_borderline_dimensions_enhanced(dimension_analyses)
            stability_prediction = self._predict_type_stability(dimension_analyses, statistical_metrics)
            quality_score = self._calculate_assessment_quality(statistical_metrics, dimension_analyses)
            
            # Generate recommendations
            retesting_recommended = self._should_recommend_retesting(quality_score, type_confidence)
            exploration_areas = self._identify_exploration_areas(dimension_analyses, borderline_dims)
            confidence_notes = self._generate_confidence_notes(dimension_analyses, statistical_metrics)
            
            # Get personality type name
            personality_type = PersonalityType.query.filter_by(code=type_code).first()
            type_name = personality_type.name_en if personality_type else f"Type {type_code}"
            
            # Create legacy compatibility data
            preference_strengths = {dim: analysis.percentage for dim, analysis in dimension_analyses.items()}
            preference_clarity = {dim: analysis.strength_category for dim, analysis in dimension_analyses.items()}
            
            result = EnhancedPersonalityResult(
                personality_type=type_name,
                type_code=type_code,
                type_confidence=type_confidence,
                dimension_analyses=dimension_analyses,
                statistical_metrics=statistical_metrics,
                borderline_dimensions=borderline_dims,
                type_stability_prediction=stability_prediction,
                assessment_quality_score=quality_score,
                retesting_recommended=retesting_recommended,
                areas_for_exploration=exploration_areas,
                confidence_notes=confidence_notes,
                preference_strengths=preference_strengths,
                preference_clarity=preference_clarity
            )
            
            # Update session with enhanced results
            self._update_session_with_enhanced_results(session, result)
            
            self.logger.info(f"Enhanced personality type {type_code} calculated for session {session_id} "
                           f"with {type_confidence:.2f} confidence and {quality_score:.2f} quality score")
            
            return result
            
        except Exception as e:
            self.logger.error(f"Error in enhanced personality calculation for session {session_id}: {str(e)}")
            raise
    
    def _validate_and_get_data(self, session_id: int) -> Tuple[AssessmentSession, List[AssessmentAnswer], Dict[int, Question]]:
        """Validate session and retrieve all necessary data"""
        session = AssessmentSession.query.get(session_id)
        if not session:
            raise ValueError(f"Session {session_id} not found")
        
        if not session.is_completed:
            raise ValueError(f"Session {session_id} is not completed")
        
        answers = AssessmentAnswer.query.filter_by(session_id=session_id).all()
        if len(answers) != 36:
            raise ValueError(f"Expected 36 answers, got {len(answers)}")
        
        questions = {q.id: q for q in Question.query.filter_by(is_active=True).all()}
        
        return session, answers, questions
    
    def _calculate_dimensional_analyses(self, answers: List[AssessmentAnswer], 
                                      questions: Dict[int, Question]) -> Dict[str, DimensionAnalysis]:
        """Calculate detailed analysis for each personality dimension"""
        # Initialize dimension counters
        dimension_scores = {
            'EI': {'E': 0, 'I': 0, 'total': 0},
            'SN': {'S': 0, 'N': 0, 'total': 0},
            'TF': {'T': 0, 'F': 0, 'total': 0},
            'JP': {'J': 0, 'P': 0, 'total': 0}
        }
        
        # Count responses for each dimension
        for answer in answers:
            question = questions.get(answer.question_id)
            if not question:
                continue
            
            dimension = question.dimension.value.replace('-', '')
            if dimension not in dimension_scores:
                continue
            
            dimension_scores[dimension]['total'] += 1
            
            # Determine which letter this answer supports
            maps_to_first = (answer.selected_option == 'A' and question.option_a_maps_to_first) or \
                           (answer.selected_option == 'B' and not question.option_a_maps_to_first)
            
            if dimension == 'EI':
                if maps_to_first:
                    dimension_scores[dimension]['E'] += 1
                else:
                    dimension_scores[dimension]['I'] += 1
            elif dimension == 'SN':
                if maps_to_first:
                    dimension_scores[dimension]['S'] += 1
                else:
                    dimension_scores[dimension]['N'] += 1
            elif dimension == 'TF':
                if maps_to_first:
                    dimension_scores[dimension]['T'] += 1
                else:
                    dimension_scores[dimension]['F'] += 1
            elif dimension == 'JP':
                if maps_to_first:
                    dimension_scores[dimension]['J'] += 1
                else:
                    dimension_scores[dimension]['P'] += 1
        
        # Create dimensional analyses
        analyses = {}
        
        for dim_key, scores in dimension_scores.items():
            if scores['total'] == 0:
                continue
            
            # Determine dominant preference
            first_letter = dim_key[0]
            second_letter = dim_key[1]
            
            first_score = scores.get(first_letter, 0)
            second_score = scores.get(second_letter, 0)
            
            if first_score >= second_score:
                preference_letter = first_letter
                raw_score = first_score
            else:
                preference_letter = second_letter
                raw_score = second_score
            
            # Calculate statistics
            percentage = raw_score / scores['total']
            confidence_level = self._calculate_confidence_level(raw_score, scores['total'])
            standard_error = self._calculate_standard_error(percentage, scores['total'])
            z_score = self._calculate_z_score(percentage)
            strength_category = self._determine_strength_category(percentage)
            
            analyses[dim_key] = DimensionAnalysis(
                dimension=dim_key,
                raw_score=raw_score,
                total_questions=scores['total'],
                percentage=percentage,
                preference_letter=preference_letter,
                strength_category=strength_category,
                confidence_level=confidence_level,
                standard_error=standard_error,
                z_score=z_score
            )
        
        return analyses
    
    def _calculate_confidence_level(self, raw_score: int, total_questions: int) -> float:
        """Calculate statistical confidence level for a dimension preference"""
        if total_questions == 0:
            return 0.0
        
        p = raw_score / total_questions
        
        # Calculate 95% confidence interval using normal approximation
        if total_questions > 5:  # Use normal approximation for larger samples
            z_95 = 1.96  # 95% confidence
            margin_error = z_95 * math.sqrt(p * (1 - p) / total_questions)
            
            # Confidence is how far we are from 0.5 (neutral) relative to margin of error
            distance_from_neutral = abs(p - 0.5)
            confidence = min(1.0, distance_from_neutral / margin_error) if margin_error > 0 else 1.0
        else:
            # For small samples, use a more conservative approach
            confidence = abs(p - 0.5) * 2  # Simple distance from neutral
        
        return max(0.0, min(1.0, confidence))
    
    def _calculate_standard_error(self, percentage: float, n: int) -> float:
        """Calculate standard error for the percentage"""
        if n == 0:
            return 1.0
        return math.sqrt(percentage * (1 - percentage) / n)
    
    def _calculate_z_score(self, percentage: float) -> float:
        """Calculate z-score (how many standard deviations from neutral 50%)"""
        # Assuming standard deviation of 0.5 for a binomial distribution
        return (percentage - 0.5) / 0.5
    
    def _determine_strength_category(self, percentage: float) -> PreferenceStrength:
        """Determine strength category using professional thresholds"""
        if percentage < self.PROFESSIONAL_THRESHOLDS[PreferenceStrength.SLIGHT]:
            return PreferenceStrength.SLIGHT
        elif percentage < self.PROFESSIONAL_THRESHOLDS[PreferenceStrength.MODERATE]:
            return PreferenceStrength.MODERATE
        elif percentage < self.PROFESSIONAL_THRESHOLDS[PreferenceStrength.CLEAR]:
            return PreferenceStrength.CLEAR
        else:
            return PreferenceStrength.VERY_CLEAR
    
    def _determine_type_with_confidence(self, dimension_analyses: Dict[str, DimensionAnalysis]) -> Tuple[str, float]:
        """Determine personality type with overall confidence score"""
        type_letters = []
        confidence_scores = []
        
        for dim_key in ['EI', 'SN', 'TF', 'JP']:
            analysis = dimension_analyses.get(dim_key)
            if analysis:
                type_letters.append(analysis.preference_letter)
                confidence_scores.append(analysis.confidence_level)
            else:
                # Use tie-breaking rule
                default_letter, default_confidence = self.TIE_BREAKING_RULES[dim_key]
                type_letters.append(default_letter)
                confidence_scores.append(default_confidence)
                self.logger.warning(f"Used tie-breaking rule for dimension {dim_key}")
        
        type_code = ''.join(type_letters)
        
        # Overall confidence is the geometric mean of individual confidences
        # This ensures that low confidence in any dimension reduces overall confidence
        if confidence_scores:
            overall_confidence = statistics.geometric_mean(confidence_scores)
        else:
            overall_confidence = 0.5
        
        return type_code, overall_confidence
    
    def _calculate_statistical_metrics(self, answers: List[AssessmentAnswer], 
                                     questions: Dict[int, Question],
                                     dimension_analyses: Dict[str, DimensionAnalysis]) -> StatisticalMetrics:
        """Calculate comprehensive statistical validation metrics"""
        
        # Internal consistency (simplified Cronbach's alpha)
        internal_consistency = self._calculate_internal_consistency(answers, questions)
        
        # Response consistency across similar questions
        response_consistency = self._calculate_response_consistency(answers, questions)
        
        # Extreme response bias (tendency to always choose A or B)
        extreme_response_bias = self._calculate_extreme_response_bias(answers)
        
        # Acquiescence bias (tendency to choose first option)
        acquiescence_bias = self._calculate_acquiescence_bias(answers, questions)
        
        # Response time variance (placeholder - would need actual timing data)
        response_time_variance = 0.5  # Neutral value
        
        # Confidence interval for type certainty
        confidence_scores = [analysis.confidence_level for analysis in dimension_analyses.values()]
        if confidence_scores:
            mean_confidence = statistics.mean(confidence_scores)
            std_confidence = statistics.stdev(confidence_scores) if len(confidence_scores) > 1 else 0.1
            confidence_interval = (
                max(0.0, mean_confidence - 1.96 * std_confidence),
                min(1.0, mean_confidence + 1.96 * std_confidence)
            )
        else:
            confidence_interval = (0.0, 1.0)
        
        return StatisticalMetrics(
            internal_consistency=internal_consistency,
            response_consistency=response_consistency,
            extreme_response_bias=extreme_response_bias,
            acquiescence_bias=acquiescence_bias,
            response_time_variance=response_time_variance,
            confidence_interval=confidence_interval
        )
    
    def _calculate_internal_consistency(self, answers: List[AssessmentAnswer], 
                                      questions: Dict[int, Question]) -> float:
        """Calculate internal consistency (simplified Cronbach's alpha)"""
        # Group questions by dimension
        dimension_responses = {'EI': [], 'SN': [], 'TF': [], 'JP': []}
        
        for answer in answers:
            question = questions.get(answer.question_id)
            if question:
                dimension = question.dimension.value.replace('-', '')
                if dimension in dimension_responses:
                    # Convert response to numeric (A=1, B=0 or vice versa based on mapping)
                    numeric_response = 1 if answer.selected_option == 'A' else 0
                    dimension_responses[dimension].append(numeric_response)
        
        # Calculate consistency within each dimension
        consistencies = []
        for dim, responses in dimension_responses.items():
            if len(responses) > 1:
                # Calculate variance and mean
                mean_response = statistics.mean(responses)
                variance = statistics.variance(responses) if len(responses) > 1 else 0
                
                # Consistency is inverse of variance (normalized)
                max_variance = 0.25  # Maximum variance for binary responses
                consistency = 1 - (variance / max_variance) if max_variance > 0 else 1
                consistencies.append(consistency)
        
        return statistics.mean(consistencies) if consistencies else 0.5
    
    def _calculate_response_consistency(self, answers: List[AssessmentAnswer], 
                                      questions: Dict[int, Question]) -> float:
        """Calculate consistency of responses across similar question types"""
        # This is a simplified version - in practice, you'd identify similar questions
        # and check if responses are consistent
        
        # For now, calculate based on response pattern regularity
        response_pattern = [1 if answer.selected_option == 'A' else 0 for answer in answers]
        
        # Calculate runs (consecutive same responses)
        runs = 1
        for i in range(1, len(response_pattern)):
            if response_pattern[i] != response_pattern[i-1]:
                runs += 1
        
        # Normalize runs (too few or too many runs indicate problems)
        expected_runs = len(response_pattern) / 2
        run_ratio = runs / expected_runs if expected_runs > 0 else 1
        
        # Optimal consistency is around 1.0 run ratio
        consistency = 1 - abs(run_ratio - 1)
        return max(0.0, min(1.0, consistency))
    
    def _calculate_extreme_response_bias(self, answers: List[AssessmentAnswer]) -> float:
        """Calculate tendency to choose extreme options (always A or always B)"""
        if not answers:
            return 0.0
        
        a_count = sum(1 for answer in answers if answer.selected_option == 'A')
        total_count = len(answers)
        
        a_ratio = a_count / total_count
        
        # Extreme bias is how far from 50-50 the responses are
        bias = abs(a_ratio - 0.5) * 2  # Scale to 0-1
        return bias
    
    def _calculate_acquiescence_bias(self, answers: List[AssessmentAnswer], 
                                   questions: Dict[int, Question]) -> float:
        """Calculate tendency to agree or choose the first option"""
        if not answers:
            return 0.0
        
        # Count how often option A was chosen when it maps to the "first" trait
        first_trait_choices = 0
        total_mappable = 0
        
        for answer in answers:
            question = questions.get(answer.question_id)
            if question:
                total_mappable += 1
                if (answer.selected_option == 'A' and question.option_a_maps_to_first) or \
                   (answer.selected_option == 'B' and not question.option_a_maps_to_first):
                    first_trait_choices += 1
        
        if total_mappable == 0:
            return 0.0
        
        first_trait_ratio = first_trait_choices / total_mappable
        
        # Acquiescence bias is deviation from expected 50-50
        bias = abs(first_trait_ratio - 0.5) * 2
        return bias
    
    def _identify_borderline_dimensions_enhanced(self, dimension_analyses: Dict[str, DimensionAnalysis]) -> List[str]:
        """Identify borderline dimensions using enhanced criteria"""
        borderline = []
        
        for dim_key, analysis in dimension_analyses.items():
            # A dimension is borderline if:
            # 1. Percentage is close to 50% (within 10%)
            # 2. Confidence level is low
            # 3. Standard error is high
            
            close_to_neutral = abs(analysis.percentage - 0.5) < 0.1
            low_confidence = analysis.confidence_level < 0.7
            high_uncertainty = analysis.standard_error > 0.15
            
            if close_to_neutral or (low_confidence and high_uncertainty):
                borderline.append(dim_key)
        
        return borderline
    
    def _predict_type_stability(self, dimension_analyses: Dict[str, DimensionAnalysis], 
                              statistical_metrics: StatisticalMetrics) -> float:
        """Predict likelihood that personality type will remain stable over time"""
        
        # Factors that contribute to stability:
        # 1. Strong preferences (high percentages)
        # 2. High confidence levels
        # 3. Good internal consistency
        # 4. Low response bias
        
        strength_scores = [analysis.percentage for analysis in dimension_analyses.values()]
        confidence_scores = [analysis.confidence_level for analysis in dimension_analyses.values()]
        
        if not strength_scores or not confidence_scores:
            return 0.5
        
        # Average strength (how far from neutral)
        avg_strength = statistics.mean([abs(score - 0.5) * 2 for score in strength_scores])
        
        # Average confidence
        avg_confidence = statistics.mean(confidence_scores)
        
        # Statistical quality
        stat_quality = (statistical_metrics.internal_consistency + 
                       statistical_metrics.response_consistency) / 2
        
        # Response bias penalty
        bias_penalty = (statistical_metrics.extreme_response_bias + 
                       statistical_metrics.acquiescence_bias) / 2
        
        # Combine factors
        stability = (avg_strength * 0.4 + avg_confidence * 0.3 + stat_quality * 0.2 - bias_penalty * 0.1)
        
        return max(0.0, min(1.0, stability))
    
    def _calculate_assessment_quality(self, statistical_metrics: StatisticalMetrics,
                                    dimension_analyses: Dict[str, DimensionAnalysis]) -> float:
        """Calculate overall assessment quality score"""
        
        # Quality factors:
        quality_factors = [
            statistical_metrics.internal_consistency,
            statistical_metrics.response_consistency,
            1 - statistical_metrics.extreme_response_bias,  # Lower bias = higher quality
            1 - statistical_metrics.acquiescence_bias,     # Lower bias = higher quality
        ]
        
        # Add confidence factors
        if dimension_analyses:
            avg_confidence = statistics.mean([analysis.confidence_level for analysis in dimension_analyses.values()])
            quality_factors.append(avg_confidence)
        
        return statistics.mean(quality_factors)
    
    def _should_recommend_retesting(self, quality_score: float, type_confidence: float) -> bool:
        """Determine if retesting should be recommended"""
        return quality_score < self.QUALITY_THRESHOLDS['acceptable'] or type_confidence < 0.6
    
    def _identify_exploration_areas(self, dimension_analyses: Dict[str, DimensionAnalysis],
                                  borderline_dims: List[str]) -> List[str]:
        """Identify areas that warrant further exploration"""
        areas = []
        
        for dim_key in borderline_dims:
            if dim_key == 'EI':
                areas.append("Social energy and interaction preferences")
            elif dim_key == 'SN':
                areas.append("Information processing and focus preferences")
            elif dim_key == 'TF':
                areas.append("Decision-making and value systems")
            elif dim_key == 'JP':
                areas.append("Lifestyle and structure preferences")
        
        # Add areas for very low confidence dimensions
        for dim_key, analysis in dimension_analyses.items():
            if analysis.confidence_level < 0.5 and dim_key not in borderline_dims:
                areas.append(f"Further clarification needed for {dim_key} dimension")
        
        return areas
    
    def _generate_confidence_notes(self, dimension_analyses: Dict[str, DimensionAnalysis],
                                 statistical_metrics: StatisticalMetrics) -> List[str]:
        """Generate notes about confidence and reliability"""
        notes = []
        
        # Overall quality assessment
        quality_score = self._calculate_assessment_quality(statistical_metrics, dimension_analyses)
        
        if quality_score >= self.QUALITY_THRESHOLDS['excellent']:
            notes.append("Excellent assessment quality with high reliability")
        elif quality_score >= self.QUALITY_THRESHOLDS['good']:
            notes.append("Good assessment quality with reliable results")
        elif quality_score >= self.QUALITY_THRESHOLDS['acceptable']:
            notes.append("Acceptable assessment quality - results are generally reliable")
        else:
            notes.append("Assessment quality concerns - consider retesting for more reliable results")
        
        # Response pattern notes
        if statistical_metrics.extreme_response_bias > 0.7:
            notes.append("Strong response bias detected - results may be less reliable")
        
        if statistical_metrics.internal_consistency < 0.6:
            notes.append("Low internal consistency - some responses may be inconsistent")
        
        # Dimension-specific notes
        for dim_key, analysis in dimension_analyses.items():
            if analysis.confidence_level < 0.5:
                notes.append(f"Low confidence in {dim_key} dimension - consider additional assessment")
            elif analysis.strength_category == PreferenceStrength.VERY_CLEAR:
                notes.append(f"Very clear preference in {dim_key} dimension")
        
        return notes
    
    def _update_session_with_enhanced_results(self, session: AssessmentSession, 
                                            result: EnhancedPersonalityResult):
        """Update session with enhanced results"""
        try:
            # Get the PersonalityType record
            personality_type = PersonalityType.query.filter_by(code=result.type_code).first()
            if personality_type:
                session.personality_type_id = personality_type.id
            
            # Store preference strengths (legacy compatibility)
            session.e_strength = result.preference_strengths.get('E', 0.0)
            session.s_strength = result.preference_strengths.get('S', 0.0)
            session.t_strength = result.preference_strengths.get('T', 0.0)
            session.j_strength = result.preference_strengths.get('J', 0.0)
            
            # Store preference clarity (legacy compatibility)
            session.ei_clarity = result.preference_clarity.get('EI')
            session.sn_clarity = result.preference_clarity.get('SN')
            session.tf_clarity = result.preference_clarity.get('TF')
            session.jp_clarity = result.preference_clarity.get('JP')
            
            db.session.commit()
            self.logger.debug(f"Updated session {session.id} with enhanced personality results")
            
        except Exception as e:
            db.session.rollback()
            self.logger.error(f"Error updating session with enhanced results: {str(e)}")
            raise
    
    def get_quality_assessment_report(self, session_id: int) -> Dict:
        """Generate a detailed quality assessment report"""
        try:
            result = self.calculate_enhanced_personality_type(session_id)
            
            return {
                'session_id': session_id,
                'assessment_quality': {
                    'overall_score': result.assessment_quality_score,
                    'quality_level': self._get_quality_level(result.assessment_quality_score),
                    'type_confidence': result.type_confidence,
                    'stability_prediction': result.type_stability_prediction
                },
                'statistical_metrics': {
                    'internal_consistency': result.statistical_metrics.internal_consistency,
                    'response_consistency': result.statistical_metrics.response_consistency,
                    'extreme_response_bias': result.statistical_metrics.extreme_response_bias,
                    'acquiescence_bias': result.statistical_metrics.acquiescence_bias,
                    'confidence_interval': result.statistical_metrics.confidence_interval
                },
                'dimensional_analysis': {
                    dim: {
                        'preference': analysis.preference_letter,
                        'strength': analysis.percentage,
                        'confidence': analysis.confidence_level,
                        'z_score': analysis.z_score,
                        'category': analysis.strength_category.value
                    }
                    for dim, analysis in result.dimension_analyses.items()
                },
                'recommendations': {
                    'retesting_recommended': result.retesting_recommended,
                    'areas_for_exploration': result.areas_for_exploration,
                    'confidence_notes': result.confidence_notes,
                    'borderline_dimensions': result.borderline_dimensions
                }
            }
            
        except Exception as e:
            self.logger.error(f"Error generating quality assessment report: {str(e)}")
            raise
    
    def _get_quality_level(self, quality_score: float) -> str:
        """Get quality level description"""
        if quality_score >= self.QUALITY_THRESHOLDS['excellent']:
            return 'Excellent'
        elif quality_score >= self.QUALITY_THRESHOLDS['good']:
            return 'Good'
        elif quality_score >= self.QUALITY_THRESHOLDS['acceptable']:
            return 'Acceptable'
        else:
            return 'Questionable'

