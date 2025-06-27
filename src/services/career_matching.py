"""
Career Matching Service for Masark Personality-Career Matching Engine
Implements career recommendation algorithm based on personality-career fit matrix
"""

from typing import List, Dict, Optional, Tuple
from dataclasses import dataclass
from src.models.masark_models import (
    db, Career, PersonalityType, PersonalityCareerMatch, CareerCluster,
    Program, Pathway, CareerProgram, CareerPathway, DeploymentMode
)
import logging
from functools import lru_cache
import json

logger = logging.getLogger(__name__)

@dataclass
class CareerMatch:
    """Data class for career match results"""
    career_id: int
    career_name_en: str
    career_name_ar: str
    match_score: float
    cluster_name_en: str
    cluster_name_ar: str
    programs: List[Dict]
    pathways: List[Dict]
    ssoc_code: Optional[str] = None
    description_en: Optional[str] = None
    description_ar: Optional[str] = None

@dataclass
class CareerMatchResult:
    """Complete career matching result"""
    personality_type: str
    total_careers: int
    top_matches: List[CareerMatch]
    deployment_mode: str
    language: str
    cached: bool = False

class CareerMatchingService:
    """
    Service class for matching careers to personality types
    Uses personality-career fit matrix for scoring and ranking
    """
    
    def __init__(self):
        self.logger = logging.getLogger(f"{__name__}.{self.__class__.__name__}")
        self._cache = {}  # Simple in-memory cache
    
    def get_career_matches(self, personality_type_code: str, 
                          deployment_mode: DeploymentMode = DeploymentMode.STANDARD,
                          language: str = 'en',
                          limit: int = 10) -> CareerMatchResult:
        """
        Get ranked career matches for a personality type
        
        Args:
            personality_type_code: 4-letter personality type (e.g., 'INTJ')
            deployment_mode: STANDARD or MAWHIBA
            language: 'en' or 'ar'
            limit: Number of top matches to return
            
        Returns:
            CareerMatchResult with ranked career matches
        """
        try:
            # Check cache first
            cache_key = f"{personality_type_code}_{deployment_mode.value}_{language}_{limit}"
            if cache_key in self._cache:
                result = self._cache[cache_key]
                result.cached = True
                self.logger.debug(f"Returning cached results for {personality_type_code}")
                return result
            
            # Get personality type
            personality_type = PersonalityType.query.filter_by(code=personality_type_code).first()
            if not personality_type:
                raise ValueError(f"Personality type {personality_type_code} not found")
            
            # Get all career matches for this personality type, sorted by score
            matches = PersonalityCareerMatch.query.filter_by(
                personality_type_id=personality_type.id
            ).order_by(PersonalityCareerMatch.match_score.desc()).limit(limit).all()
            
            if not matches:
                # If no matches exist, create default matches
                self.logger.warning(f"No career matches found for {personality_type_code}, creating defaults")
                matches = self._create_default_matches(personality_type.id, limit)
            
            # Convert to CareerMatch objects with full details
            career_matches = []
            for match in matches:
                career_match = self._build_career_match(match, deployment_mode, language)
                if career_match:
                    career_matches.append(career_match)
            
            # Create result
            result = CareerMatchResult(
                personality_type=personality_type_code,
                total_careers=len(career_matches),
                top_matches=career_matches,
                deployment_mode=deployment_mode.value,
                language=language,
                cached=False
            )
            
            # Cache the result
            self._cache[cache_key] = result
            
            self.logger.info(f"Generated {len(career_matches)} career matches for {personality_type_code}")
            return result
            
        except Exception as e:
            self.logger.error(f"Error getting career matches for {personality_type_code}: {str(e)}")
            raise
    
    def _build_career_match(self, match: PersonalityCareerMatch, 
                           deployment_mode: DeploymentMode, 
                           language: str) -> Optional[CareerMatch]:
        """Build a complete CareerMatch object with all related data"""
        try:
            career = match.career
            if not career or not career.is_active:
                return None
            
            # Get cluster information
            cluster = career.cluster
            cluster_name_en = cluster.name_en if cluster else "Uncategorized"
            cluster_name_ar = cluster.name_ar if cluster else "غير مصنف"
            
            # Get associated programs
            programs = []
            career_programs = CareerProgram.query.filter_by(career_id=career.id).all()
            for cp in career_programs:
                program = cp.program
                programs.append({
                    'id': program.id,
                    'name': program.name_en if language == 'en' else program.name_ar,
                    'description': program.description_en if language == 'en' else program.description_ar
                })
            
            # Get associated pathways (filtered by deployment mode)
            pathways = []
            career_pathways = CareerPathway.query.filter_by(career_id=career.id).all()
            for cp in career_pathways:
                pathway = cp.pathway
                # Filter pathways based on deployment mode
                if deployment_mode == DeploymentMode.STANDARD:
                    # Show only MOE pathways for standard mode
                    if pathway.source.value == 'MOE':
                        pathways.append({
                            'id': pathway.id,
                            'name': pathway.name_en if language == 'en' else pathway.name_ar,
                            'source': pathway.source.value,
                            'description': pathway.description_en if language == 'en' else pathway.description_ar
                        })
                else:
                    # Show both MOE and Mawhiba pathways for Mawhiba mode
                    pathways.append({
                        'id': pathway.id,
                        'name': pathway.name_en if language == 'en' else pathway.name_ar,
                        'source': pathway.source.value,
                        'description': pathway.description_en if language == 'en' else pathway.description_ar
                    })
            
            return CareerMatch(
                career_id=career.id,
                career_name_en=career.name_en,
                career_name_ar=career.name_ar,
                match_score=match.match_score,
                cluster_name_en=cluster_name_en,
                cluster_name_ar=cluster_name_ar,
                programs=programs,
                pathways=pathways,
                ssoc_code=career.ssoc_code,
                description_en=career.description_en,
                description_ar=career.description_ar
            )
            
        except Exception as e:
            self.logger.error(f"Error building career match for career {match.career_id}: {str(e)}")
            return None
    
    def _create_default_matches(self, personality_type_id: int, limit: int) -> List[PersonalityCareerMatch]:
        """Create default career matches if none exist in the database"""
        try:
            # Get random careers to create default matches
            careers = Career.query.filter_by(is_active=True).limit(limit * 2).all()
            
            matches = []
            for i, career in enumerate(careers[:limit]):
                # Create a default match score (decreasing from 0.9 to 0.5)
                score = 0.9 - (i * 0.4 / limit)
                
                match = PersonalityCareerMatch(
                    personality_type_id=personality_type_id,
                    career_id=career.id,
                    match_score=score
                )
                db.session.add(match)
                matches.append(match)
            
            db.session.commit()
            self.logger.info(f"Created {len(matches)} default career matches")
            return matches
            
        except Exception as e:
            db.session.rollback()
            self.logger.error(f"Error creating default matches: {str(e)}")
            return []
    
    def get_career_details(self, career_id: int, language: str = 'en') -> Optional[Dict]:
        """Get detailed information about a specific career"""
        try:
            career = Career.query.get(career_id)
            if not career or not career.is_active:
                return None
            
            # Get cluster
            cluster = career.cluster
            
            # Get programs
            programs = []
            career_programs = CareerProgram.query.filter_by(career_id=career.id).all()
            for cp in career_programs:
                program = cp.program
                programs.append({
                    'id': program.id,
                    'name': program.name_en if language == 'en' else program.name_ar,
                    'description': program.description_en if language == 'en' else program.description_ar
                })
            
            # Get pathways
            pathways = []
            career_pathways = CareerPathway.query.filter_by(career_id=career.id).all()
            for cp in career_pathways:
                pathway = cp.pathway
                pathways.append({
                    'id': pathway.id,
                    'name': pathway.name_en if language == 'en' else pathway.name_ar,
                    'source': pathway.source.value,
                    'description': pathway.description_en if language == 'en' else pathway.description_ar
                })
            
            return {
                'id': career.id,
                'name': career.name_en if language == 'en' else career.name_ar,
                'description': career.description_en if language == 'en' else career.description_ar,
                'ssoc_code': career.ssoc_code,
                'cluster': {
                    'id': cluster.id,
                    'name': cluster.name_en if language == 'en' else cluster.name_ar,
                    'description': cluster.description_en if language == 'en' else cluster.description_ar
                } if cluster else None,
                'programs': programs,
                'pathways': pathways,
                'created_at': career.created_at.isoformat() if career.created_at else None,
                'updated_at': career.updated_at.isoformat() if career.updated_at else None
            }
            
        except Exception as e:
            self.logger.error(f"Error getting career details for {career_id}: {str(e)}")
            return None
    
    def search_careers(self, query: str, language: str = 'en', limit: int = 20) -> List[Dict]:
        """Search careers by name or description"""
        try:
            if language == 'en':
                careers = Career.query.filter(
                    db.or_(
                        Career.name_en.ilike(f'%{query}%'),
                        Career.description_en.ilike(f'%{query}%')
                    ),
                    Career.is_active == True
                ).limit(limit).all()
            else:
                careers = Career.query.filter(
                    db.or_(
                        Career.name_ar.ilike(f'%{query}%'),
                        Career.description_ar.ilike(f'%{query}%')
                    ),
                    Career.is_active == True
                ).limit(limit).all()
            
            results = []
            for career in careers:
                results.append({
                    'id': career.id,
                    'name': career.name_en if language == 'en' else career.name_ar,
                    'description': career.description_en if language == 'en' else career.description_ar,
                    'cluster': career.cluster.name_en if career.cluster and language == 'en' else career.cluster.name_ar if career.cluster else None,
                    'ssoc_code': career.ssoc_code
                })
            
            return results
            
        except Exception as e:
            self.logger.error(f"Error searching careers with query '{query}': {str(e)}")
            return []
    
    def get_careers_by_cluster(self, cluster_id: int, language: str = 'en') -> List[Dict]:
        """Get all careers in a specific cluster"""
        try:
            careers = Career.query.filter_by(
                cluster_id=cluster_id,
                is_active=True
            ).order_by(Career.name_en).all()
            
            results = []
            for career in careers:
                results.append({
                    'id': career.id,
                    'name': career.name_en if language == 'en' else career.name_ar,
                    'description': career.description_en if language == 'en' else career.description_ar,
                    'ssoc_code': career.ssoc_code
                })
            
            return results
            
        except Exception as e:
            self.logger.error(f"Error getting careers for cluster {cluster_id}: {str(e)}")
            return []
    
    def update_match_score(self, personality_type_code: str, career_id: int, 
                          new_score: float) -> bool:
        """Update the match score for a personality type-career pair"""
        try:
            if not (0.0 <= new_score <= 1.0):
                raise ValueError("Match score must be between 0.0 and 1.0")
            
            personality_type = PersonalityType.query.filter_by(code=personality_type_code).first()
            if not personality_type:
                raise ValueError(f"Personality type {personality_type_code} not found")
            
            career = Career.query.get(career_id)
            if not career:
                raise ValueError(f"Career {career_id} not found")
            
            # Find or create the match record
            match = PersonalityCareerMatch.query.filter_by(
                personality_type_id=personality_type.id,
                career_id=career_id
            ).first()
            
            if match:
                match.match_score = new_score
            else:
                match = PersonalityCareerMatch(
                    personality_type_id=personality_type.id,
                    career_id=career_id,
                    match_score=new_score
                )
                db.session.add(match)
            
            db.session.commit()
            
            # Clear cache for this personality type
            self._clear_cache_for_personality_type(personality_type_code)
            
            self.logger.info(f"Updated match score for {personality_type_code}-{career_id}: {new_score}")
            return True
            
        except Exception as e:
            db.session.rollback()
            self.logger.error(f"Error updating match score: {str(e)}")
            return False
    
    def _clear_cache_for_personality_type(self, personality_type_code: str):
        """Clear cache entries for a specific personality type"""
        keys_to_remove = [key for key in self._cache.keys() if key.startswith(personality_type_code)]
        for key in keys_to_remove:
            del self._cache[key]
        self.logger.debug(f"Cleared {len(keys_to_remove)} cache entries for {personality_type_code}")
    
    def clear_all_cache(self):
        """Clear all cached results"""
        cache_size = len(self._cache)
        self._cache.clear()
        self.logger.info(f"Cleared all cache ({cache_size} entries)")
    
    def get_cache_stats(self) -> Dict:
        """Get cache statistics"""
        return {
            'total_entries': len(self._cache),
            'cache_keys': list(self._cache.keys())
        }
    
    def bulk_update_match_scores(self, personality_type_code: str, 
                                scores: Dict[int, float]) -> Tuple[int, int]:
        """
        Bulk update match scores for a personality type
        
        Args:
            personality_type_code: 4-letter personality type
            scores: Dict mapping career_id to match_score
            
        Returns:
            Tuple of (successful_updates, failed_updates)
        """
        try:
            personality_type = PersonalityType.query.filter_by(code=personality_type_code).first()
            if not personality_type:
                raise ValueError(f"Personality type {personality_type_code} not found")
            
            successful = 0
            failed = 0
            
            for career_id, score in scores.items():
                try:
                    if not (0.0 <= score <= 1.0):
                        self.logger.warning(f"Invalid score {score} for career {career_id}")
                        failed += 1
                        continue
                    
                    # Find or create the match record
                    match = PersonalityCareerMatch.query.filter_by(
                        personality_type_id=personality_type.id,
                        career_id=career_id
                    ).first()
                    
                    if match:
                        match.match_score = score
                    else:
                        match = PersonalityCareerMatch(
                            personality_type_id=personality_type.id,
                            career_id=career_id,
                            match_score=score
                        )
                        db.session.add(match)
                    
                    successful += 1
                    
                except Exception as e:
                    self.logger.error(f"Error updating score for career {career_id}: {str(e)}")
                    failed += 1
            
            db.session.commit()
            
            # Clear cache for this personality type
            self._clear_cache_for_personality_type(personality_type_code)
            
            self.logger.info(f"Bulk update completed: {successful} successful, {failed} failed")
            return successful, failed
            
        except Exception as e:
            db.session.rollback()
            self.logger.error(f"Error in bulk update: {str(e)}")
            return 0, len(scores)

