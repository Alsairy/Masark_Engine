"""
Report Generation Service for Masark Personality-Career Matching Engine
Generates comprehensive PDF reports with personality analysis and career recommendations
"""

from typing import Dict, List, Optional, Tuple
from dataclasses import dataclass
import os
from datetime import datetime
from reportlab.lib.pagesizes import A4, letter
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib.units import inch, cm
from reportlab.lib.colors import Color, black, blue, darkblue, grey
from reportlab.platypus import SimpleDocTemplate, Paragraph, Spacer, Table, TableStyle, PageBreak
from reportlab.platypus.flowables import HRFlowable
from reportlab.lib.enums import TA_CENTER, TA_LEFT, TA_RIGHT, TA_JUSTIFY
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
import logging

from src.models.masark_models import (
    AssessmentSession, PersonalityType, DeploymentMode
)
from src.services.personality_scoring import PersonalityScoringService
from src.services.career_matching import CareerMatchingService

logger = logging.getLogger(__name__)

@dataclass
class ReportData:
    """Data structure for report generation"""
    session_token: str
    student_name: Optional[str]
    personality_type: str
    personality_description: Dict
    preference_strengths: Dict[str, float]
    preference_clarity: Dict[str, str]
    career_matches: List[Dict]
    deployment_mode: str
    language: str
    generated_at: datetime

class ReportGenerationService:
    """
    Service class for generating personality and career reports
    Supports bilingual PDF generation with proper RTL support for Arabic
    """
    
    def __init__(self):
        self.logger = logging.getLogger(f"{__name__}.{self.__class__.__name__}")
        self.reports_dir = os.path.join(os.path.dirname(__file__), '..', 'reports')
        os.makedirs(self.reports_dir, exist_ok=True)
        
        # Initialize fonts for Arabic support
        self._setup_fonts()
    
    def _setup_fonts(self):
        """Setup fonts for bilingual support"""
        try:
            # For now, we'll use default fonts
            # In production, you would add Arabic fonts like:
            # pdfmetrics.registerFont(TTFont('Arabic', 'path/to/arabic-font.ttf'))
            self.arabic_font = 'Helvetica'  # Fallback to Helvetica
            self.english_font = 'Helvetica'
            self.logger.info("Fonts initialized for report generation")
        except Exception as e:
            self.logger.warning(f"Font setup warning: {str(e)}")
            self.arabic_font = 'Helvetica'
            self.english_font = 'Helvetica'
    
    def generate_comprehensive_report(self, session_token: str, 
                                    language: str = 'en',
                                    include_career_details: bool = True) -> Tuple[str, str]:
        """
        Generate a comprehensive personality and career report
        
        Args:
            session_token: Assessment session token
            language: 'en' or 'ar'
            include_career_details: Whether to include detailed career information
            
        Returns:
            Tuple of (file_path, filename)
        """
        try:
            # Gather report data
            report_data = self._gather_report_data(session_token, language)
            
            # Generate filename
            timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            student_name = report_data.student_name or "Student"
            safe_name = "".join(c for c in student_name if c.isalnum() or c in (' ', '-', '_')).rstrip()
            filename = f"Masark_Report_{safe_name}_{report_data.personality_type}_{timestamp}.pdf"
            file_path = os.path.join(self.reports_dir, filename)
            
            # Create PDF document
            doc = SimpleDocTemplate(
                file_path,
                pagesize=A4,
                rightMargin=2*cm,
                leftMargin=2*cm,
                topMargin=2*cm,
                bottomMargin=2*cm
            )
            
            # Build report content
            story = []
            
            # Add header
            story.extend(self._create_header(report_data))
            
            # Add personality section
            story.extend(self._create_personality_section(report_data))
            
            # Add career recommendations section
            story.extend(self._create_career_section(report_data, include_career_details))
            
            # Add pathways section
            story.extend(self._create_pathways_section(report_data))
            
            # Add footer
            story.extend(self._create_footer(report_data))
            
            # Build PDF
            doc.build(story)
            
            self.logger.info(f"Generated comprehensive report: {filename}")
            return file_path, filename
            
        except Exception as e:
            self.logger.error(f"Error generating comprehensive report: {str(e)}")
            raise
    
    def _gather_report_data(self, session_token: str, language: str) -> ReportData:
        """Gather all data needed for report generation"""
        try:
            # Get session
            session = AssessmentSession.query.filter_by(session_token=session_token).first()
            if not session:
                raise ValueError(f"Session {session_token} not found")
            
            if not session.is_completed or not session.personality_type_id:
                raise ValueError("Assessment must be completed and personality type calculated")
            
            # Get personality data
            scoring_service = PersonalityScoringService()
            personality_description = scoring_service.get_personality_description(
                session.personality_type.code, language
            )
            
            # Calculate preference strengths
            preference_strengths = {
                'E': float(session.e_strength * 100 if session.e_strength else 0),
                'I': float((1 - session.e_strength) * 100 if session.e_strength else 0),
                'S': float(session.s_strength * 100 if session.s_strength else 0),
                'N': float((1 - session.s_strength) * 100 if session.s_strength else 0),
                'T': float(session.t_strength * 100 if session.t_strength else 0),
                'F': float((1 - session.t_strength) * 100 if session.t_strength else 0),
                'J': float(session.j_strength * 100 if session.j_strength else 0),
                'P': float((1 - session.j_strength) * 100 if session.j_strength else 0)
            }
            
            # Get preference clarity
            preference_clarity = {
                'EI': session.ei_clarity.value if session.ei_clarity else 'SLIGHT',
                'SN': session.sn_clarity.value if session.sn_clarity else 'SLIGHT',
                'TF': session.tf_clarity.value if session.tf_clarity else 'SLIGHT',
                'JP': session.jp_clarity.value if session.jp_clarity else 'SLIGHT'
            }
            
            # Get career matches
            career_service = CareerMatchingService()
            career_result = career_service.get_career_matches(
                session.personality_type.code,
                session.deployment_mode,
                language,
                limit=10
            )
            
            return ReportData(
                session_token=session_token,
                student_name=session.student_name,
                personality_type=session.personality_type.code,
                personality_description=personality_description or {},
                preference_strengths=preference_strengths,
                preference_clarity=preference_clarity,
                career_matches=[
                    {
                        'name': match.career_name_en if language == 'en' else match.career_name_ar,
                        'match_score': match.match_score * 100,
                        'cluster': match.cluster_name_en if language == 'en' else match.cluster_name_ar,
                        'description': match.description_en if language == 'en' else match.description_ar,
                        'programs': match.programs,
                        'pathways': match.pathways
                    }
                    for match in career_result.top_matches
                ],
                deployment_mode=session.deployment_mode.value,
                language=language,
                generated_at=datetime.now()
            )
            
        except Exception as e:
            self.logger.error(f"Error gathering report data: {str(e)}")
            raise
    
    def _create_header(self, data: ReportData) -> List:
        """Create report header section"""
        styles = getSampleStyleSheet()
        story = []
        
        # Title
        if data.language == 'ar':
            title = "تقرير شخصية ومهنة مسارك"
            subtitle = f"تقرير شامل للطالب: {data.student_name or 'الطالب'}"
        else:
            title = "Masark Personality & Career Report"
            subtitle = f"Comprehensive Report for: {data.student_name or 'Student'}"
        
        title_style = ParagraphStyle(
            'CustomTitle',
            parent=styles['Title'],
            fontSize=24,
            spaceAfter=12,
            alignment=TA_CENTER,
            textColor=darkblue
        )
        
        subtitle_style = ParagraphStyle(
            'CustomSubtitle',
            parent=styles['Heading2'],
            fontSize=16,
            spaceAfter=20,
            alignment=TA_CENTER,
            textColor=blue
        )
        
        story.append(Paragraph(title, title_style))
        story.append(Paragraph(subtitle, subtitle_style))
        
        # Report info table
        report_info = [
            ['Personality Type' if data.language == 'en' else 'نوع الشخصية', data.personality_type],
            ['Generated On' if data.language == 'en' else 'تاريخ الإنشاء', data.generated_at.strftime('%Y-%m-%d %H:%M')],
            ['Deployment Mode' if data.language == 'en' else 'نمط النشر', data.deployment_mode],
            ['Language' if data.language == 'en' else 'اللغة', 'English' if data.language == 'en' else 'العربية']
        ]
        
        info_table = Table(report_info, colWidths=[4*cm, 6*cm])
        info_table.setStyle(TableStyle([
            ('BACKGROUND', (0, 0), (-1, -1), Color(0.95, 0.95, 0.95)),
            ('TEXTCOLOR', (0, 0), (-1, -1), black),
            ('ALIGN', (0, 0), (-1, -1), 'LEFT'),
            ('FONTNAME', (0, 0), (-1, -1), 'Helvetica'),
            ('FONTSIZE', (0, 0), (-1, -1), 10),
            ('GRID', (0, 0), (-1, -1), 1, black)
        ]))
        
        story.append(info_table)
        story.append(Spacer(1, 20))
        
        return story
    
    def _create_personality_section(self, data: ReportData) -> List:
        """Create personality analysis section"""
        styles = getSampleStyleSheet()
        story = []
        
        # Section title
        section_title = "Personality Analysis" if data.language == 'en' else "تحليل الشخصية"
        story.append(Paragraph(section_title, styles['Heading1']))
        story.append(Spacer(1, 12))
        
        # Personality type description
        if data.personality_description:
            desc = data.personality_description
            
            # Main description
            if desc.get('description'):
                story.append(Paragraph(f"<b>Description:</b> {desc['description']}", styles['Normal']))
                story.append(Spacer(1, 8))
            
            # Strengths
            if desc.get('strengths'):
                story.append(Paragraph(f"<b>Strengths:</b> {desc['strengths']}", styles['Normal']))
                story.append(Spacer(1, 8))
            
            # Challenges
            if desc.get('challenges'):
                story.append(Paragraph(f"<b>Challenges:</b> {desc['challenges']}", styles['Normal']))
                story.append(Spacer(1, 8))
        
        # Preference strengths table
        pref_title = "Preference Strengths" if data.language == 'en' else "قوة التفضيلات"
        story.append(Paragraph(pref_title, styles['Heading2']))
        story.append(Spacer(1, 8))
        
        # Create preference data
        pref_data = [
            ['Dimension' if data.language == 'en' else 'البُعد', 
             'Preference' if data.language == 'en' else 'التفضيل', 
             'Strength %' if data.language == 'en' else 'القوة %',
             'Clarity' if data.language == 'en' else 'الوضوح']
        ]
        
        # Add preference rows
        dimensions = [
            ('Extraversion/Introversion', 'E' if data.preference_strengths['E'] > data.preference_strengths['I'] else 'I'),
            ('Sensing/Intuition', 'S' if data.preference_strengths['S'] > data.preference_strengths['N'] else 'N'),
            ('Thinking/Feeling', 'T' if data.preference_strengths['T'] > data.preference_strengths['F'] else 'F'),
            ('Judging/Perceiving', 'J' if data.preference_strengths['J'] > data.preference_strengths['P'] else 'P')
        ]
        
        clarity_keys = ['EI', 'SN', 'TF', 'JP']
        
        for i, (dim_name, pref) in enumerate(dimensions):
            strength = max(data.preference_strengths[pref], data.preference_strengths[{'E':'I','I':'E','S':'N','N':'S','T':'F','F':'T','J':'P','P':'J'}[pref]])
            clarity = data.preference_clarity[clarity_keys[i]]
            
            pref_data.append([
                dim_name,
                pref,
                f"{strength:.1f}%",
                clarity
            ])
        
        pref_table = Table(pref_data, colWidths=[4*cm, 2*cm, 2*cm, 3*cm])
        pref_table.setStyle(TableStyle([
            ('BACKGROUND', (0, 0), (-1, 0), darkblue),
            ('TEXTCOLOR', (0, 0), (-1, 0), Color(1, 1, 1)),
            ('ALIGN', (0, 0), (-1, -1), 'CENTER'),
            ('FONTNAME', (0, 0), (-1, 0), 'Helvetica-Bold'),
            ('FONTNAME', (0, 1), (-1, -1), 'Helvetica'),
            ('FONTSIZE', (0, 0), (-1, -1), 10),
            ('GRID', (0, 0), (-1, -1), 1, black),
            ('ROWBACKGROUNDS', (0, 1), (-1, -1), [Color(0.98, 0.98, 0.98), Color(1, 1, 1)])
        ]))
        
        story.append(pref_table)
        story.append(Spacer(1, 20))
        
        return story
    
    def _create_career_section(self, data: ReportData, include_details: bool = True) -> List:
        """Create career recommendations section"""
        styles = getSampleStyleSheet()
        story = []
        
        # Section title
        section_title = "Career Recommendations" if data.language == 'en' else "التوصيات المهنية"
        story.append(Paragraph(section_title, styles['Heading1']))
        story.append(Spacer(1, 12))
        
        # Introduction
        intro_text = (
            f"Based on your personality type ({data.personality_type}), here are the top career matches:"
            if data.language == 'en' else
            f"بناءً على نوع شخصيتك ({data.personality_type})، إليك أفضل المهن المناسبة:"
        )
        story.append(Paragraph(intro_text, styles['Normal']))
        story.append(Spacer(1, 12))
        
        # Career matches table
        career_data = [
            ['Rank' if data.language == 'en' else 'الترتيب',
             'Career' if data.language == 'en' else 'المهنة',
             'Match %' if data.language == 'en' else 'التطابق %',
             'Cluster' if data.language == 'en' else 'المجموعة']
        ]
        
        for i, career in enumerate(data.career_matches[:10], 1):
            career_data.append([
                str(i),
                career['name'],
                f"{career['match_score']:.1f}%",
                career['cluster']
            ])
        
        career_table = Table(career_data, colWidths=[1.5*cm, 5*cm, 2*cm, 4*cm])
        career_table.setStyle(TableStyle([
            ('BACKGROUND', (0, 0), (-1, 0), darkblue),
            ('TEXTCOLOR', (0, 0), (-1, 0), Color(1, 1, 1)),
            ('ALIGN', (0, 0), (-1, -1), 'CENTER'),
            ('FONTNAME', (0, 0), (-1, 0), 'Helvetica-Bold'),
            ('FONTNAME', (0, 1), (-1, -1), 'Helvetica'),
            ('FONTSIZE', (0, 0), (-1, -1), 9),
            ('GRID', (0, 0), (-1, -1), 1, black),
            ('ROWBACKGROUNDS', (0, 1), (-1, -1), [Color(0.98, 0.98, 0.98), Color(1, 1, 1)])
        ]))
        
        story.append(career_table)
        story.append(Spacer(1, 15))
        
        # Detailed career descriptions if requested
        if include_details and len(data.career_matches) > 0:
            details_title = "Top Career Details" if data.language == 'en' else "تفاصيل أهم المهن"
            story.append(Paragraph(details_title, styles['Heading2']))
            story.append(Spacer(1, 10))
            
            for i, career in enumerate(data.career_matches[:5], 1):
                # Career name and match score
                career_header = f"{i}. {career['name']} ({career['match_score']:.1f}% match)"
                story.append(Paragraph(career_header, styles['Heading3']))
                
                # Description
                if career.get('description'):
                    story.append(Paragraph(career['description'], styles['Normal']))
                
                # Programs and pathways
                if career.get('programs') or career.get('pathways'):
                    if career.get('programs'):
                        programs_text = "Related Programs: " + ", ".join([p['name'] for p in career['programs'][:3]])
                        story.append(Paragraph(programs_text, styles['Normal']))
                    
                    if career.get('pathways'):
                        pathways_text = "Education Pathways: " + ", ".join([p['name'] for p in career['pathways'][:3]])
                        story.append(Paragraph(pathways_text, styles['Normal']))
                
                story.append(Spacer(1, 10))
        
        return story
    
    def _create_pathways_section(self, data: ReportData) -> List:
        """Create education pathways section"""
        styles = getSampleStyleSheet()
        story = []
        
        # Section title
        section_title = "Education Pathways" if data.language == 'en' else "المسارات التعليمية"
        story.append(Paragraph(section_title, styles['Heading1']))
        story.append(Spacer(1, 12))
        
        # Deployment mode specific content
        if data.deployment_mode == 'MAWHIBA':
            intro_text = (
                "As a Mawhiba student, you have access to both MOE and specialized Mawhiba pathways:"
                if data.language == 'en' else
                "كطالب موهبة، لديك إمكانية الوصول إلى مسارات وزارة التعليم ومسارات موهبة المتخصصة:"
            )
        else:
            intro_text = (
                "Based on your career interests, here are the recommended education pathways:"
                if data.language == 'en' else
                "بناءً على اهتماماتك المهنية، إليك المسارات التعليمية الموصى بها:"
            )
        
        story.append(Paragraph(intro_text, styles['Normal']))
        story.append(Spacer(1, 12))
        
        # Collect unique pathways from career matches
        all_pathways = []
        for career in data.career_matches:
            for pathway in career.get('pathways', []):
                if pathway not in all_pathways:
                    all_pathways.append(pathway)
        
        if all_pathways:
            pathway_data = [
                ['Pathway' if data.language == 'en' else 'المسار',
                 'Source' if data.language == 'en' else 'المصدر',
                 'Description' if data.language == 'en' else 'الوصف']
            ]
            
            for pathway in all_pathways[:8]:  # Limit to 8 pathways
                pathway_data.append([
                    pathway.get('name', 'N/A'),
                    pathway.get('source', 'N/A'),
                    pathway.get('description', 'N/A')[:100] + '...' if len(pathway.get('description', '')) > 100 else pathway.get('description', 'N/A')
                ])
            
            pathway_table = Table(pathway_data, colWidths=[4*cm, 2*cm, 6*cm])
            pathway_table.setStyle(TableStyle([
                ('BACKGROUND', (0, 0), (-1, 0), darkblue),
                ('TEXTCOLOR', (0, 0), (-1, 0), Color(1, 1, 1)),
                ('ALIGN', (0, 0), (-1, -1), 'LEFT'),
                ('FONTNAME', (0, 0), (-1, 0), 'Helvetica-Bold'),
                ('FONTNAME', (0, 1), (-1, -1), 'Helvetica'),
                ('FONTSIZE', (0, 0), (-1, -1), 9),
                ('GRID', (0, 0), (-1, -1), 1, black),
                ('ROWBACKGROUNDS', (0, 1), (-1, -1), [Color(0.98, 0.98, 0.98), Color(1, 1, 1)]),
                ('VALIGN', (0, 0), (-1, -1), 'TOP')
            ]))
            
            story.append(pathway_table)
        else:
            no_pathways_text = (
                "No specific pathways found for your career matches."
                if data.language == 'en' else
                "لم يتم العثور على مسارات محددة لمطابقاتك المهنية."
            )
            story.append(Paragraph(no_pathways_text, styles['Normal']))
        
        story.append(Spacer(1, 20))
        return story
    
    def _create_footer(self, data: ReportData) -> List:
        """Create report footer section"""
        styles = getSampleStyleSheet()
        story = []
        
        # Add horizontal line
        story.append(HRFlowable(width="100%", thickness=1, color=grey))
        story.append(Spacer(1, 10))
        
        # Footer text
        footer_text = (
            f"This report was generated by Masark Personality-Career Matching Engine on {data.generated_at.strftime('%Y-%m-%d at %H:%M')}. "
            f"The recommendations are based on your personality assessment results and should be used as guidance for career exploration."
            if data.language == 'en' else
            f"تم إنشاء هذا التقرير بواسطة محرك مطابقة الشخصية والمهنة مسارك في {data.generated_at.strftime('%Y-%m-%d في %H:%M')}. "
            f"التوصيات مبنية على نتائج تقييم شخصيتك ويجب استخدامها كدليل لاستكشاف المهن."
        )
        
        footer_style = ParagraphStyle(
            'Footer',
            parent=styles['Normal'],
            fontSize=8,
            textColor=grey,
            alignment=TA_CENTER
        )
        
        story.append(Paragraph(footer_text, footer_style))
        
        return story
    
    def generate_summary_report(self, session_token: str, language: str = 'en') -> Tuple[str, str]:
        """Generate a concise summary report (2-3 pages)"""
        try:
            # This would be a shorter version of the comprehensive report
            # For now, we'll use the comprehensive report
            return self.generate_comprehensive_report(session_token, language, include_career_details=False)
        except Exception as e:
            self.logger.error(f"Error generating summary report: {str(e)}")
            raise
    
    def list_generated_reports(self, limit: int = 50) -> List[Dict]:
        """List recently generated reports"""
        try:
            reports = []
            if os.path.exists(self.reports_dir):
                files = os.listdir(self.reports_dir)
                pdf_files = [f for f in files if f.endswith('.pdf')]
                pdf_files.sort(key=lambda x: os.path.getmtime(os.path.join(self.reports_dir, x)), reverse=True)
                
                for filename in pdf_files[:limit]:
                    file_path = os.path.join(self.reports_dir, filename)
                    stat = os.stat(file_path)
                    
                    reports.append({
                        'filename': filename,
                        'file_path': file_path,
                        'size_bytes': stat.st_size,
                        'created_at': datetime.fromtimestamp(stat.st_ctime).isoformat(),
                        'modified_at': datetime.fromtimestamp(stat.st_mtime).isoformat()
                    })
            
            return reports
        except Exception as e:
            self.logger.error(f"Error listing reports: {str(e)}")
            return []
    
    def delete_report(self, filename: str) -> bool:
        """Delete a generated report file"""
        try:
            file_path = os.path.join(self.reports_dir, filename)
            if os.path.exists(file_path) and filename.endswith('.pdf'):
                os.remove(file_path)
                self.logger.info(f"Deleted report: {filename}")
                return True
            return False
        except Exception as e:
            self.logger.error(f"Error deleting report {filename}: {str(e)}")
            return False

