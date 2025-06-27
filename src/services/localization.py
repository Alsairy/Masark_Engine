"""
Localization Service for Masark Personality-Career Matching Engine
Handles bilingual support (Arabic/English), RTL text direction, and localization
"""

import json
import os
from enum import Enum
from typing import Dict, Any, Optional, List
from flask import current_app, request

class Language(Enum):
    ENGLISH = "en"
    ARABIC = "ar"

class TextDirection(Enum):
    LTR = "ltr"  # Left-to-Right (English)
    RTL = "rtl"  # Right-to-Left (Arabic)

class LocalizationService:
    """Service for handling localization and bilingual support"""
    
    def __init__(self):
        self.translations = {}
        self.default_language = Language.ENGLISH
        self.supported_languages = [Language.ENGLISH, Language.ARABIC]
        self.language_config = {
            Language.ENGLISH: {
                'name': 'English',
                'native_name': 'English',
                'direction': TextDirection.LTR,
                'locale': 'en-US',
                'font_family': 'Arial, sans-serif'
            },
            Language.ARABIC: {
                'name': 'Arabic',
                'native_name': 'العربية',
                'direction': TextDirection.RTL,
                'locale': 'ar-SA',
                'font_family': 'Arial, "Noto Sans Arabic", sans-serif'
            }
        }
        self.load_translations()
    
    def load_translations(self):
        """Load translation files for all supported languages"""
        try:
            # English translations (base language)
            self.translations[Language.ENGLISH] = {
                # System Messages
                'system': {
                    'welcome': 'Welcome to Masark',
                    'loading': 'Loading...',
                    'error': 'An error occurred',
                    'success': 'Operation completed successfully',
                    'save': 'Save',
                    'cancel': 'Cancel',
                    'delete': 'Delete',
                    'edit': 'Edit',
                    'view': 'View',
                    'download': 'Download',
                    'upload': 'Upload',
                    'search': 'Search',
                    'filter': 'Filter',
                    'sort': 'Sort',
                    'next': 'Next',
                    'previous': 'Previous',
                    'close': 'Close',
                    'confirm': 'Confirm',
                    'yes': 'Yes',
                    'no': 'No'
                },
                
                # Authentication
                'auth': {
                    'login': 'Login',
                    'logout': 'Logout',
                    'username': 'Username',
                    'password': 'Password',
                    'email': 'Email',
                    'full_name': 'Full Name',
                    'role': 'Role',
                    'login_success': 'Login successful',
                    'login_failed': 'Login failed',
                    'logout_success': 'Logout successful',
                    'invalid_credentials': 'Invalid username or password',
                    'access_denied': 'Access denied',
                    'token_expired': 'Session expired, please login again',
                    'change_password': 'Change Password',
                    'current_password': 'Current Password',
                    'new_password': 'New Password',
                    'password_changed': 'Password changed successfully'
                },
                
                # Assessment
                'assessment': {
                    'personality_assessment': 'Personality Assessment',
                    'start_assessment': 'Start Assessment',
                    'question': 'Question',
                    'of': 'of',
                    'next_question': 'Next Question',
                    'previous_question': 'Previous Question',
                    'submit_assessment': 'Submit Assessment',
                    'assessment_complete': 'Assessment Complete',
                    'your_personality_type': 'Your Personality Type',
                    'personality_description': 'Personality Description',
                    'strengths': 'Strengths',
                    'challenges': 'Challenges',
                    'career_recommendations': 'Career Recommendations',
                    'preference_strength': 'Preference Strength',
                    'slight': 'Slight',
                    'moderate': 'Moderate',
                    'clear': 'Clear',
                    'very_clear': 'Very Clear'
                },
                
                # Careers
                'careers': {
                    'careers': 'Careers',
                    'career': 'Career',
                    'career_title': 'Career Title',
                    'career_description': 'Career Description',
                    'career_cluster': 'Career Cluster',
                    'education_requirements': 'Education Requirements',
                    'skills_required': 'Skills Required',
                    'salary_range': 'Salary Range',
                    'job_outlook': 'Job Outlook',
                    'related_careers': 'Related Careers',
                    'personality_match': 'Personality Match',
                    'match_percentage': 'Match Percentage',
                    'highly_recommended': 'Highly Recommended',
                    'recommended': 'Recommended',
                    'suitable': 'Suitable',
                    'search_careers': 'Search Careers'
                },
                
                # Reports
                'reports': {
                    'reports': 'Reports',
                    'generate_report': 'Generate Report',
                    'download_report': 'Download Report',
                    'report_generated': 'Report generated successfully',
                    'personality_report': 'Personality Report',
                    'career_report': 'Career Report',
                    'comprehensive_report': 'Comprehensive Report',
                    'report_date': 'Report Date',
                    'student_name': 'Student Name',
                    'assessment_results': 'Assessment Results',
                    'career_matches': 'Career Matches',
                    'education_pathways': 'Education Pathways'
                },
                
                # Admin Panel
                'admin': {
                    'admin_panel': 'Admin Panel',
                    'dashboard': 'Dashboard',
                    'users': 'Users',
                    'questions': 'Questions',
                    'settings': 'Settings',
                    'statistics': 'Statistics',
                    'total_users': 'Total Users',
                    'active_users': 'Active Users',
                    'total_assessments': 'Total Assessments',
                    'total_reports': 'Total Reports',
                    'system_health': 'System Health',
                    'online': 'Online',
                    'offline': 'Offline',
                    'user_management': 'User Management',
                    'create_user': 'Create User',
                    'edit_user': 'Edit User',
                    'delete_user': 'Delete User',
                    'user_created': 'User created successfully',
                    'user_updated': 'User updated successfully',
                    'user_deleted': 'User deleted successfully'
                },
                
                # Personality Types
                'personality_types': {
                    'INTJ': 'The Strategist',
                    'INTP': 'The Logician',
                    'ENTJ': 'The Commander',
                    'ENTP': 'The Debater',
                    'INFJ': 'The Advocate',
                    'INFP': 'The Mediator',
                    'ENFJ': 'The Protagonist',
                    'ENFP': 'The Campaigner',
                    'ISTJ': 'The Logistician',
                    'ISFJ': 'The Protector',
                    'ESTJ': 'The Executive',
                    'ESFJ': 'The Consul',
                    'ISTP': 'The Virtuoso',
                    'ISFP': 'The Adventurer',
                    'ESTP': 'The Entrepreneur',
                    'ESFP': 'The Entertainer'
                }
            }
            
            # Arabic translations
            self.translations[Language.ARABIC] = {
                # System Messages
                'system': {
                    'welcome': 'مرحباً بك في مسارك',
                    'loading': 'جاري التحميل...',
                    'error': 'حدث خطأ',
                    'success': 'تمت العملية بنجاح',
                    'save': 'حفظ',
                    'cancel': 'إلغاء',
                    'delete': 'حذف',
                    'edit': 'تعديل',
                    'view': 'عرض',
                    'download': 'تحميل',
                    'upload': 'رفع',
                    'search': 'بحث',
                    'filter': 'تصفية',
                    'sort': 'ترتيب',
                    'next': 'التالي',
                    'previous': 'السابق',
                    'close': 'إغلاق',
                    'confirm': 'تأكيد',
                    'yes': 'نعم',
                    'no': 'لا'
                },
                
                # Authentication
                'auth': {
                    'login': 'تسجيل الدخول',
                    'logout': 'تسجيل الخروج',
                    'username': 'اسم المستخدم',
                    'password': 'كلمة المرور',
                    'email': 'البريد الإلكتروني',
                    'full_name': 'الاسم الكامل',
                    'role': 'الدور',
                    'login_success': 'تم تسجيل الدخول بنجاح',
                    'login_failed': 'فشل تسجيل الدخول',
                    'logout_success': 'تم تسجيل الخروج بنجاح',
                    'invalid_credentials': 'اسم المستخدم أو كلمة المرور غير صحيحة',
                    'access_denied': 'تم رفض الوصول',
                    'token_expired': 'انتهت صلاحية الجلسة، يرجى تسجيل الدخول مرة أخرى',
                    'change_password': 'تغيير كلمة المرور',
                    'current_password': 'كلمة المرور الحالية',
                    'new_password': 'كلمة المرور الجديدة',
                    'password_changed': 'تم تغيير كلمة المرور بنجاح'
                },
                
                # Assessment
                'assessment': {
                    'personality_assessment': 'تقييم الشخصية',
                    'start_assessment': 'بدء التقييم',
                    'question': 'السؤال',
                    'of': 'من',
                    'next_question': 'السؤال التالي',
                    'previous_question': 'السؤال السابق',
                    'submit_assessment': 'إرسال التقييم',
                    'assessment_complete': 'اكتمل التقييم',
                    'your_personality_type': 'نوع شخصيتك',
                    'personality_description': 'وصف الشخصية',
                    'strengths': 'نقاط القوة',
                    'challenges': 'التحديات',
                    'career_recommendations': 'توصيات المهن',
                    'preference_strength': 'قوة التفضيل',
                    'slight': 'طفيف',
                    'moderate': 'متوسط',
                    'clear': 'واضح',
                    'very_clear': 'واضح جداً'
                },
                
                # Careers
                'careers': {
                    'careers': 'المهن',
                    'career': 'المهنة',
                    'career_title': 'عنوان المهنة',
                    'career_description': 'وصف المهنة',
                    'career_cluster': 'مجموعة المهن',
                    'education_requirements': 'المتطلبات التعليمية',
                    'skills_required': 'المهارات المطلوبة',
                    'salary_range': 'نطاق الراتب',
                    'job_outlook': 'توقعات الوظيفة',
                    'related_careers': 'المهن ذات الصلة',
                    'personality_match': 'توافق الشخصية',
                    'match_percentage': 'نسبة التوافق',
                    'highly_recommended': 'موصى به بشدة',
                    'recommended': 'موصى به',
                    'suitable': 'مناسب',
                    'search_careers': 'البحث في المهن'
                },
                
                # Reports
                'reports': {
                    'reports': 'التقارير',
                    'generate_report': 'إنشاء تقرير',
                    'download_report': 'تحميل التقرير',
                    'report_generated': 'تم إنشاء التقرير بنجاح',
                    'personality_report': 'تقرير الشخصية',
                    'career_report': 'تقرير المهن',
                    'comprehensive_report': 'تقرير شامل',
                    'report_date': 'تاريخ التقرير',
                    'student_name': 'اسم الطالب',
                    'assessment_results': 'نتائج التقييم',
                    'career_matches': 'توافق المهن',
                    'education_pathways': 'المسارات التعليمية'
                },
                
                # Admin Panel
                'admin': {
                    'admin_panel': 'لوحة الإدارة',
                    'dashboard': 'لوحة المعلومات',
                    'users': 'المستخدمون',
                    'questions': 'الأسئلة',
                    'settings': 'الإعدادات',
                    'statistics': 'الإحصائيات',
                    'total_users': 'إجمالي المستخدمين',
                    'active_users': 'المستخدمون النشطون',
                    'total_assessments': 'إجمالي التقييمات',
                    'total_reports': 'إجمالي التقارير',
                    'system_health': 'حالة النظام',
                    'online': 'متصل',
                    'offline': 'غير متصل',
                    'user_management': 'إدارة المستخدمين',
                    'create_user': 'إنشاء مستخدم',
                    'edit_user': 'تعديل المستخدم',
                    'delete_user': 'حذف المستخدم',
                    'user_created': 'تم إنشاء المستخدم بنجاح',
                    'user_updated': 'تم تحديث المستخدم بنجاح',
                    'user_deleted': 'تم حذف المستخدم بنجاح'
                },
                
                # Personality Types
                'personality_types': {
                    'INTJ': 'الاستراتيجي',
                    'INTP': 'المنطقي',
                    'ENTJ': 'القائد',
                    'ENTP': 'المناقش',
                    'INFJ': 'المدافع',
                    'INFP': 'الوسيط',
                    'ENFJ': 'البطل',
                    'ENFP': 'المناضل',
                    'ISTJ': 'اللوجستي',
                    'ISFJ': 'الحامي',
                    'ESTJ': 'التنفيذي',
                    'ESFJ': 'القنصل',
                    'ISTP': 'الفنان',
                    'ISFP': 'المغامر',
                    'ESTP': 'ريادي الأعمال',
                    'ESFP': 'المسلي'
                }
            }
            
            print("✅ Localization service initialized with bilingual support")
            
        except Exception as e:
            print(f"❌ Error loading translations: {str(e)}")
            # Fallback to English only
            self.supported_languages = [Language.ENGLISH]
    
    def get_language_from_request(self) -> Language:
        """Get language preference from request headers or parameters"""
        try:
            # Check query parameter first
            lang_param = request.args.get('lang', '').lower()
            if lang_param == 'ar' or lang_param == 'arabic':
                return Language.ARABIC
            elif lang_param == 'en' or lang_param == 'english':
                return Language.ENGLISH
            
            # Check Accept-Language header
            accept_language = request.headers.get('Accept-Language', '')
            if 'ar' in accept_language.lower():
                return Language.ARABIC
            
            return self.default_language
            
        except:
            return self.default_language
    
    def translate(self, key: str, language: Optional[Language] = None, category: str = 'system') -> str:
        """
        Get translated text for a given key
        
        Args:
            key: Translation key
            language: Target language (auto-detect if None)
            category: Translation category (system, auth, assessment, etc.)
        
        Returns:
            Translated text or key if translation not found
        """
        try:
            if language is None:
                language = self.get_language_from_request()
            
            # Get translation from the specified category
            translations = self.translations.get(language, {})
            category_translations = translations.get(category, {})
            
            # Return translation or fallback to English
            if key in category_translations:
                return category_translations[key]
            
            # Fallback to English
            english_translations = self.translations.get(Language.ENGLISH, {})
            english_category = english_translations.get(category, {})
            if key in english_category:
                return english_category[key]
            
            # Return key if no translation found
            return key
            
        except Exception as e:
            print(f"Translation error for key '{key}': {str(e)}")
            return key
    
    def get_language_config(self, language: Optional[Language] = None) -> Dict[str, Any]:
        """Get configuration for a specific language"""
        if language is None:
            language = self.get_language_from_request()
        
        config = self.language_config.get(language, self.language_config[Language.ENGLISH])
        return {
            'code': language.value,
            'name': config['name'],
            'native_name': config['native_name'],
            'direction': config['direction'].value,
            'locale': config['locale'],
            'font_family': config['font_family'],
            'is_rtl': config['direction'] == TextDirection.RTL
        }
    
    def get_supported_languages(self) -> List[Dict[str, Any]]:
        """Get list of all supported languages with their configurations"""
        languages = []
        for lang in self.supported_languages:
            config = self.get_language_config(lang)
            languages.append(config)
        return languages
    
    def format_number(self, number: float, language: Optional[Language] = None) -> str:
        """Format numbers according to language conventions"""
        if language is None:
            language = self.get_language_from_request()
        
        if language == Language.ARABIC:
            # Arabic number formatting (can be customized)
            return f"{number:,.1f}".replace(',', '٬')
        else:
            # English number formatting
            return f"{number:,.1f}"
    
    def format_percentage(self, percentage: float, language: Optional[Language] = None) -> str:
        """Format percentages according to language conventions"""
        if language is None:
            language = self.get_language_from_request()
        
        formatted_number = self.format_number(percentage, language)
        
        if language == Language.ARABIC:
            return f"{formatted_number}٪"
        else:
            return f"{formatted_number}%"
    
    def get_text_direction_class(self, language: Optional[Language] = None) -> str:
        """Get CSS class for text direction"""
        config = self.get_language_config(language)
        return 'rtl' if config['is_rtl'] else 'ltr'
    
    def localize_content(self, content: Dict[str, Any], language: Optional[Language] = None) -> Dict[str, Any]:
        """
        Localize content object with language-specific fields
        
        Args:
            content: Content object with _en and _ar suffixed fields
            language: Target language
        
        Returns:
            Localized content object
        """
        if language is None:
            language = self.get_language_from_request()
        
        localized = {}
        suffix = '_ar' if language == Language.ARABIC else '_en'
        
        for key, value in content.items():
            if key.endswith('_en') or key.endswith('_ar'):
                # Skip language-specific fields, they'll be handled separately
                continue
            elif f"{key}_en" in content and f"{key}_ar" in content:
                # Use language-specific version
                localized[key] = content.get(f"{key}{suffix}", content.get(f"{key}_en", value))
            else:
                # Keep original value
                localized[key] = value
        
        return localized

# Global localization service instance
localization_service = LocalizationService()

def get_localized_text(key: str, category: str = 'system', language: Optional[Language] = None) -> str:
    """Helper function to get localized text"""
    return localization_service.translate(key, language, category)

def get_current_language() -> Language:
    """Helper function to get current language from request"""
    return localization_service.get_language_from_request()

def get_language_config(language: Optional[Language] = None) -> Dict[str, Any]:
    """Helper function to get language configuration"""
    return localization_service.get_language_config(language)

