"""
Personality Scoring Service for Masark Personality-Career Matching Engine
Implements MBTI-style personality type calculation from 36 forced-choice questions
"""

from typing import Dict, List, Tuple, Optional
from dataclasses import dataclass
from src.models.masark_models import (
    AssessmentSession, AssessmentAnswer, Question, PersonalityType,
    PersonalityDimension, PreferenceStrength, db
)
import logging

logger = logging.getLogger(__name__)

@dataclass
class PersonalityScores:
    """Data class to hold personality dimension scores"""
    e_score: int = 0  # Extraversion score
    i_score: int = 0  # Introversion score
    s_score: int = 0  # Sensing score
    n_score: int = 0  # Intuition score
    t_score: int = 0  # Thinking score
    f_score: int = 0  # Feeling score
    j_score: int = 0  # Judging score
    p_score: int = 0  # Perceiving score

@dataclass
class PersonalityResult:
    """Complete personality assessment result"""
    personality_type: str
    type_code: str
    dimension_scores: PersonalityScores
    preference_strengths: Dict[str, float]  # E.g., {'E': 0.67, 'S': 0.56, ...}
    preference_clarity: Dict[str, PreferenceStrength]
    borderline_dimensions: List[str]  # Dimensions that were close calls
    total_questions_per_dimension: Dict[str, int]

class PersonalityScoringService:
    """
    Service class for calculating MBTI personality types from assessment answers
    Implements the algorithm specified in the requirements document
    """
    
    # Tie-breaking rules as specified in the document
    TIE_BREAKING_RULES = {
        'EI': 'I',  # If E = I, assign I
        'SN': 'N',  # If S = N, assign N  
        'TF': 'F',  # If T = F, assign F
        'JP': 'P'   # If J = P, assign P
    }
    
    # Preference strength thresholds
    STRENGTH_THRESHOLDS = {
        PreferenceStrength.SLIGHT: 0.60,      # <60% = slight
        PreferenceStrength.MODERATE: 0.75,    # 60-75% = moderate
        PreferenceStrength.CLEAR: 0.90,       # 76-90% = clear
        PreferenceStrength.VERY_CLEAR: 1.0    # >90% = very clear
    }
    
    def __init__(self):
        self.logger = logging.getLogger(f"{__name__}.{self.__class__.__name__}")
    
    def calculate_personality_type(self, session_id: int) -> PersonalityResult:
        """
        Calculate personality type for a completed assessment session
        
        Args:
            session_id: ID of the completed assessment session
            
        Returns:
            PersonalityResult with complete analysis
            
        Raises:
            ValueError: If session is invalid or incomplete
            Exception: For other calculation errors
        """
        try:
            # Validate session
            session = AssessmentSession.query.get(session_id)
            if not session:
                raise ValueError(f"Session {session_id} not found")
            
            if not session.is_completed:
                raise ValueError(f"Session {session_id} is not completed")
            
            # Get all answers for this session
            answers = AssessmentAnswer.query.filter_by(session_id=session_id).all()
            if len(answers) != 36:
                raise ValueError(f"Expected 36 answers, got {len(answers)}")
            
            # Get questions with their metadata
            questions = {q.id: q for q in Question.query.filter_by(is_active=True).all()}
            
            # Calculate scores for each dimension
            scores = self._calculate_dimension_scores(answers, questions)
            
            # Determine personality type using tie-breaking rules
            type_code = self._determine_personality_type(scores)
            
            # Calculate preference strengths and clarity
            strengths = self._calculate_preference_strengths(scores)
            clarity = self._calculate_preference_clarity(strengths)
            
            # Identify borderline dimensions (close to 50-50)
            borderline = self._identify_borderline_dimensions(strengths)
            
            # Get questions per dimension count
            questions_per_dimension = self._count_questions_per_dimension(list(questions.values()))
            
            # Get personality type name
            personality_type = PersonalityType.query.filter_by(code=type_code).first()
            type_name = personality_type.name_en if personality_type else f"Type {type_code}"
            
            result = PersonalityResult(
                personality_type=type_name,
                type_code=type_code,
                dimension_scores=scores,
                preference_strengths=strengths,
                preference_clarity=clarity,
                borderline_dimensions=borderline,
                total_questions_per_dimension=questions_per_dimension
            )
            
            # Update session with results
            self._update_session_with_results(session, result)
            
            self.logger.info(f"Calculated personality type {type_code} for session {session_id}")
            return result
            
        except Exception as e:
            self.logger.error(f"Error calculating personality type for session {session_id}: {str(e)}")
            raise
    
    def _calculate_dimension_scores(self, answers: List[AssessmentAnswer], 
                                  questions: Dict[int, Question]) -> PersonalityScores:
        """Calculate raw scores for each personality dimension"""
        scores = PersonalityScores()
        
        for answer in answers:
            question = questions.get(answer.question_id)
            if not question:
                continue
            
            # Determine which dimension this answer contributes to
            dimension = question.dimension
            selected_option = answer.selected_option
            
            # Check if the selected option maps to the first letter of the dimension
            maps_to_first = (selected_option == 'A' and question.option_a_maps_to_first) or \
                           (selected_option == 'B' and not question.option_a_maps_to_first)
            
            # Increment appropriate score based on dimension and mapping
            if dimension == PersonalityDimension.EI:
                if maps_to_first:
                    scores.e_score += 1
                else:
                    scores.i_score += 1
            elif dimension == PersonalityDimension.SN:
                if maps_to_first:
                    scores.s_score += 1
                else:
                    scores.n_score += 1
            elif dimension == PersonalityDimension.TF:
                if maps_to_first:
                    scores.t_score += 1
                else:
                    scores.f_score += 1
            elif dimension == PersonalityDimension.JP:
                if maps_to_first:
                    scores.j_score += 1
                else:
                    scores.p_score += 1
        
        return scores
    
    def _determine_personality_type(self, scores: PersonalityScores) -> str:
        """Determine 4-letter personality type using tie-breaking rules"""
        type_letters = []
        
        # Extraversion vs Introversion
        if scores.e_score > scores.i_score:
            type_letters.append('E')
        elif scores.i_score > scores.e_score:
            type_letters.append('I')
        else:
            # Tie - use tie-breaking rule (favor I)
            type_letters.append(self.TIE_BREAKING_RULES['EI'])
            self.logger.debug("Applied tie-breaking rule for E-I dimension")
        
        # Sensing vs Intuition
        if scores.s_score > scores.n_score:
            type_letters.append('S')
        elif scores.n_score > scores.s_score:
            type_letters.append('N')
        else:
            # Tie - use tie-breaking rule (favor N)
            type_letters.append(self.TIE_BREAKING_RULES['SN'])
            self.logger.debug("Applied tie-breaking rule for S-N dimension")
        
        # Thinking vs Feeling
        if scores.t_score > scores.f_score:
            type_letters.append('T')
        elif scores.f_score > scores.t_score:
            type_letters.append('F')
        else:
            # Tie - use tie-breaking rule (favor F)
            type_letters.append(self.TIE_BREAKING_RULES['TF'])
            self.logger.debug("Applied tie-breaking rule for T-F dimension")
        
        # Judging vs Perceiving
        if scores.j_score > scores.p_score:
            type_letters.append('J')
        elif scores.p_score > scores.j_score:
            type_letters.append('P')
        else:
            # Tie - use tie-breaking rule (favor P)
            type_letters.append(self.TIE_BREAKING_RULES['JP'])
            self.logger.debug("Applied tie-breaking rule for J-P dimension")
        
        return ''.join(type_letters)
    
    def _calculate_preference_strengths(self, scores: PersonalityScores) -> Dict[str, float]:
        """Calculate preference strength as percentages for each dimension"""
        strengths = {}
        
        # Calculate total questions per dimension (assuming roughly 9 each)
        ei_total = scores.e_score + scores.i_score
        sn_total = scores.s_score + scores.n_score
        tf_total = scores.t_score + scores.f_score
        jp_total = scores.j_score + scores.p_score
        
        # Calculate strengths (percentage of the winning side)
        if ei_total > 0:
            e_strength = scores.e_score / ei_total
            i_strength = scores.i_score / ei_total
            strengths['E'] = e_strength
            strengths['I'] = i_strength
        
        if sn_total > 0:
            s_strength = scores.s_score / sn_total
            n_strength = scores.n_score / sn_total
            strengths['S'] = s_strength
            strengths['N'] = n_strength
        
        if tf_total > 0:
            t_strength = scores.t_score / tf_total
            f_strength = scores.f_score / tf_total
            strengths['T'] = t_strength
            strengths['F'] = f_strength
        
        if jp_total > 0:
            j_strength = scores.j_score / jp_total
            p_strength = scores.p_score / jp_total
            strengths['J'] = j_strength
            strengths['P'] = p_strength
        
        return strengths
    
    def _calculate_preference_clarity(self, strengths: Dict[str, float]) -> Dict[str, PreferenceStrength]:
        """Calculate preference clarity categories for each dimension"""
        clarity = {}
        
        # For each dimension, use the strength of the dominant preference
        dimensions = [
            ('EI', max(strengths.get('E', 0), strengths.get('I', 0))),
            ('SN', max(strengths.get('S', 0), strengths.get('N', 0))),
            ('TF', max(strengths.get('T', 0), strengths.get('F', 0))),
            ('JP', max(strengths.get('J', 0), strengths.get('P', 0)))
        ]
        
        for dim_name, strength in dimensions:
            if strength < self.STRENGTH_THRESHOLDS[PreferenceStrength.SLIGHT]:
                clarity[dim_name] = PreferenceStrength.SLIGHT
            elif strength < self.STRENGTH_THRESHOLDS[PreferenceStrength.MODERATE]:
                clarity[dim_name] = PreferenceStrength.MODERATE
            elif strength < self.STRENGTH_THRESHOLDS[PreferenceStrength.CLEAR]:
                clarity[dim_name] = PreferenceStrength.CLEAR
            else:
                clarity[dim_name] = PreferenceStrength.VERY_CLEAR
        
        return clarity
    
    def _identify_borderline_dimensions(self, strengths: Dict[str, float], 
                                      threshold: float = 0.55) -> List[str]:
        """Identify dimensions where the preference was borderline (close to 50-50)"""
        borderline = []
        
        dimensions = [
            ('EI', max(strengths.get('E', 0), strengths.get('I', 0))),
            ('SN', max(strengths.get('S', 0), strengths.get('N', 0))),
            ('TF', max(strengths.get('T', 0), strengths.get('F', 0))),
            ('JP', max(strengths.get('J', 0), strengths.get('P', 0)))
        ]
        
        for dim_name, strength in dimensions:
            if strength < threshold:  # Less than 55% means it was close
                borderline.append(dim_name)
        
        return borderline
    
    def _count_questions_per_dimension(self, questions: List[Question]) -> Dict[str, int]:
        """Count how many questions target each dimension"""
        counts = {'EI': 0, 'SN': 0, 'TF': 0, 'JP': 0}
        
        for question in questions:
            dim_key = question.dimension.value.replace('-', '')
            if dim_key in counts:
                counts[dim_key] += 1
        
        return counts
    
    def _update_session_with_results(self, session: AssessmentSession, result: PersonalityResult):
        """Update the session with calculated results"""
        try:
            # Get the PersonalityType record
            personality_type = PersonalityType.query.filter_by(code=result.type_code).first()
            if personality_type:
                session.personality_type_id = personality_type.id
            
            # Store preference strengths
            strengths = result.preference_strengths
            session.e_strength = strengths.get('E', 0.0)
            session.s_strength = strengths.get('S', 0.0)
            session.t_strength = strengths.get('T', 0.0)
            session.j_strength = strengths.get('J', 0.0)
            
            # Store preference clarity
            clarity = result.preference_clarity
            session.ei_clarity = clarity.get('EI')
            session.sn_clarity = clarity.get('SN')
            session.tf_clarity = clarity.get('TF')
            session.jp_clarity = clarity.get('JP')
            
            db.session.commit()
            self.logger.debug(f"Updated session {session.id} with personality results")
            
        except Exception as e:
            db.session.rollback()
            self.logger.error(f"Error updating session with results: {str(e)}")
            raise
    
    def get_personality_description(self, type_code: str, language: str = 'en') -> Optional[Dict]:
        """Get personality type description in specified language"""
        personality_type = PersonalityType.query.filter_by(code=type_code).first()
        if personality_type:
            return personality_type.to_dict(language)
        return None
    
    def calculate_personality_type_from_responses(self, responses: List[int]) -> PersonalityResult:
        """
        Calculate personality type directly from response array (for testing/validation)
        
        Args:
            responses: List of 36 responses (1-5 scale)
            
        Returns:
            PersonalityResult with complete analysis
            
        Raises:
            ValueError: If responses are invalid
        """
        try:
            if len(responses) != 36:
                raise ValueError(f"Expected 36 responses, got {len(responses)}")
            
            # Validate response values
            for i, response in enumerate(responses):
                if not isinstance(response, int) or response < 1 or response > 5:
                    raise ValueError(f"Invalid response at position {i}: {response}. Must be integer 1-5")
            
            questions = Question.query.filter_by(is_active=True).order_by(Question.id).all()
            if len(questions) != 36:
                raise ValueError(f"Expected 36 active questions, found {len(questions)}")
            
            # Calculate scores using simplified mapping
            scores = PersonalityScores()
            
            for i, (response, question) in enumerate(zip(responses, questions)):
                if response <= 2:
                    selected_option = 'A'
                elif response >= 4:
                    selected_option = 'B'
                else:
                    import random
                    selected_option = random.choice(['A', 'B'])
                
                # Determine which dimension this contributes to
                dimension = question.dimension
                maps_to_first = (selected_option == 'A' and question.option_a_maps_to_first) or \
                               (selected_option == 'B' and not question.option_a_maps_to_first)
                
                # Increment appropriate score
                if dimension == PersonalityDimension.EI:
                    if maps_to_first:
                        scores.e_score += 1
                    else:
                        scores.i_score += 1
                elif dimension == PersonalityDimension.SN:
                    if maps_to_first:
                        scores.s_score += 1
                    else:
                        scores.n_score += 1
                elif dimension == PersonalityDimension.TF:
                    if maps_to_first:
                        scores.t_score += 1
                    else:
                        scores.f_score += 1
                elif dimension == PersonalityDimension.JP:
                    if maps_to_first:
                        scores.j_score += 1
                    else:
                        scores.p_score += 1
            
            # Determine personality type using tie-breaking rules
            type_code = self._determine_personality_type(scores)
            
            # Calculate preference strengths and clarity
            strengths = self._calculate_preference_strengths(scores)
            clarity = self._calculate_preference_clarity(strengths)
            
            # Identify borderline dimensions
            borderline = self._identify_borderline_dimensions(strengths)
            
            # Get questions per dimension count
            questions_per_dimension = self._count_questions_per_dimension(questions)
            
            # Get personality type name
            personality_type = PersonalityType.query.filter_by(code=type_code).first()
            type_name = personality_type.name_en if personality_type else f"Type {type_code}"
            
            result = PersonalityResult(
                personality_type=type_name,
                type_code=type_code,
                dimension_scores=scores,
                preference_strengths=strengths,
                preference_clarity=clarity,
                borderline_dimensions=borderline,
                total_questions_per_dimension=questions_per_dimension
            )
            
            self.logger.info(f"Calculated personality type {type_code} from direct responses")
            return result
            
        except Exception as e:
            self.logger.error(f"Error calculating personality type from responses: {str(e)}")
            raise

    def validate_answers_completeness(self, session_id: int) -> Tuple[bool, str]:
        """Validate that all required answers are present for scoring"""
        try:
            answers = AssessmentAnswer.query.filter_by(session_id=session_id).all()
            
            if len(answers) != 36:
                return False, f"Expected 36 answers, got {len(answers)}"
            
            # Check that we have answers for all dimensions
            questions = {q.id: q for q in Question.query.filter_by(is_active=True).all()}
            dimension_counts = {'EI': 0, 'SN': 0, 'TF': 0, 'JP': 0}
            
            for answer in answers:
                question = questions.get(answer.question_id)
                if question:
                    dim_key = question.dimension.value.replace('-', '')
                    if dim_key in dimension_counts:
                        dimension_counts[dim_key] += 1
            
            # Check that each dimension has at least some questions
            for dim, count in dimension_counts.items():
                if count == 0:
                    return False, f"No answers found for dimension {dim}"
            
            return True, "All answers present and valid"
            
        except Exception as e:
            return False, f"Error validating answers: {str(e)}"

