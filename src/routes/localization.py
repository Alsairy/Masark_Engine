"""
Localization API Routes for Masark Personality-Career Matching Engine
Handles language configuration, translations, and localization services
"""

from flask import Blueprint, request, jsonify, current_app
from src.services.localization import (
    localization_service, 
    get_localized_text, 
    get_current_language, 
    get_language_config,
    Language
)

localization_bp = Blueprint('localization', __name__)

@localization_bp.route('/languages', methods=['GET'])
def get_supported_languages():
    """
    Get list of supported languages with their configurations
    """
    try:
        languages = localization_service.get_supported_languages()
        
        return jsonify({
            'success': True,
            'languages': languages,
            'default_language': localization_service.default_language.value
        }), 200
        
    except Exception as e:
        current_app.logger.error(f"Get supported languages error: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to get supported languages',
            'message': str(e)
        }), 500

@localization_bp.route('/config', methods=['GET'])
def get_language_config_endpoint():
    """
    Get configuration for current or specified language
    Query parameters:
    - lang: Language code (en, ar)
    """
    try:
        # Get language from query parameter or auto-detect
        lang_param = request.args.get('lang', '').lower()
        language = None
        
        if lang_param == 'ar' or lang_param == 'arabic':
            language = Language.ARABIC
        elif lang_param == 'en' or lang_param == 'english':
            language = Language.ENGLISH
        
        config = get_language_config(language)
        
        return jsonify({
            'success': True,
            'language_config': config
        }), 200
        
    except Exception as e:
        current_app.logger.error(f"Get language config error: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to get language configuration',
            'message': str(e)
        }), 500

@localization_bp.route('/translations', methods=['GET'])
def get_translations():
    """
    Get translations for a specific category and language
    Query parameters:
    - lang: Language code (en, ar)
    - category: Translation category (system, auth, assessment, careers, reports, admin, personality_types)
    - categories: Comma-separated list of categories (alternative to category)
    """
    try:
        # Get language from query parameter or auto-detect
        lang_param = request.args.get('lang', '').lower()
        language = None
        
        if lang_param == 'ar' or lang_param == 'arabic':
            language = Language.ARABIC
        elif lang_param == 'en' or lang_param == 'english':
            language = Language.ENGLISH
        else:
            language = get_current_language()
        
        # Get categories to translate
        category = request.args.get('category', '')
        categories_param = request.args.get('categories', '')
        
        if categories_param:
            categories = [cat.strip() for cat in categories_param.split(',')]
        elif category:
            categories = [category]
        else:
            # Return all categories
            categories = ['system', 'auth', 'assessment', 'careers', 'reports', 'admin', 'personality_types']
        
        # Get translations for requested categories
        translations = {}
        for cat in categories:
            if language in localization_service.translations:
                category_translations = localization_service.translations[language].get(cat, {})
                if category_translations:
                    translations[cat] = category_translations
        
        return jsonify({
            'success': True,
            'language': language.value,
            'translations': translations,
            'language_config': get_language_config(language)
        }), 200
        
    except Exception as e:
        current_app.logger.error(f"Get translations error: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to get translations',
            'message': str(e)
        }), 500

@localization_bp.route('/translate', methods=['POST'])
def translate_text():
    """
    Translate specific text keys
    Expected payload:
    {
        "keys": ["key1", "key2"],
        "category": "system",
        "language": "ar"
    }
    """
    try:
        data = request.get_json() or {}
        
        keys = data.get('keys', [])
        category = data.get('category', 'system')
        lang_param = data.get('language', '').lower()
        
        # Validate input
        if not keys or not isinstance(keys, list):
            return jsonify({
                'success': False,
                'error': 'Keys array is required'
            }), 400
        
        # Determine language
        language = None
        if lang_param == 'ar' or lang_param == 'arabic':
            language = Language.ARABIC
        elif lang_param == 'en' or lang_param == 'english':
            language = Language.ENGLISH
        else:
            language = get_current_language()
        
        # Translate keys
        translations = {}
        for key in keys:
            translations[key] = get_localized_text(key, category, language)
        
        return jsonify({
            'success': True,
            'language': language.value,
            'category': category,
            'translations': translations
        }), 200
        
    except Exception as e:
        current_app.logger.error(f"Translate text error: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to translate text',
            'message': str(e)
        }), 500

@localization_bp.route('/format', methods=['POST'])
def format_localized_content():
    """
    Format numbers, percentages, and other locale-specific content
    Expected payload:
    {
        "type": "number" | "percentage",
        "value": 123.45,
        "language": "ar"
    }
    """
    try:
        data = request.get_json() or {}
        
        format_type = data.get('type', 'number')
        value = data.get('value')
        lang_param = data.get('language', '').lower()
        
        # Validate input
        if value is None:
            return jsonify({
                'success': False,
                'error': 'Value is required'
            }), 400
        
        try:
            value = float(value)
        except (ValueError, TypeError):
            return jsonify({
                'success': False,
                'error': 'Value must be a number'
            }), 400
        
        # Determine language
        language = None
        if lang_param == 'ar' or lang_param == 'arabic':
            language = Language.ARABIC
        elif lang_param == 'en' or lang_param == 'english':
            language = Language.ENGLISH
        else:
            language = get_current_language()
        
        # Format value
        if format_type == 'percentage':
            formatted_value = localization_service.format_percentage(value, language)
        else:
            formatted_value = localization_service.format_number(value, language)
        
        return jsonify({
            'success': True,
            'type': format_type,
            'original_value': value,
            'formatted_value': formatted_value,
            'language': language.value
        }), 200
        
    except Exception as e:
        current_app.logger.error(f"Format localized content error: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to format content',
            'message': str(e)
        }), 500

@localization_bp.route('/localize', methods=['POST'])
def localize_content():
    """
    Localize content object with language-specific fields
    Expected payload:
    {
        "content": {
            "title_en": "English Title",
            "title_ar": "العنوان العربي",
            "description_en": "English Description",
            "description_ar": "الوصف العربي",
            "other_field": "Non-localized field"
        },
        "language": "ar"
    }
    """
    try:
        data = request.get_json() or {}
        
        content = data.get('content', {})
        lang_param = data.get('language', '').lower()
        
        # Validate input
        if not content or not isinstance(content, dict):
            return jsonify({
                'success': False,
                'error': 'Content object is required'
            }), 400
        
        # Determine language
        language = None
        if lang_param == 'ar' or lang_param == 'arabic':
            language = Language.ARABIC
        elif lang_param == 'en' or lang_param == 'english':
            language = Language.ENGLISH
        else:
            language = get_current_language()
        
        # Localize content
        localized_content = localization_service.localize_content(content, language)
        
        return jsonify({
            'success': True,
            'language': language.value,
            'original_content': content,
            'localized_content': localized_content,
            'language_config': get_language_config(language)
        }), 200
        
    except Exception as e:
        current_app.logger.error(f"Localize content error: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to localize content',
            'message': str(e)
        }), 500

@localization_bp.route('/stats', methods=['GET'])
def get_localization_stats():
    """Get localization statistics and system information"""
    try:
        # Count translations
        total_translations = 0
        translation_stats = {}
        
        for language, categories in localization_service.translations.items():
            lang_total = 0
            lang_stats = {}
            
            for category, translations in categories.items():
                category_count = len(translations)
                lang_stats[category] = category_count
                lang_total += category_count
            
            translation_stats[language.value] = {
                'categories': lang_stats,
                'total': lang_total
            }
            total_translations += lang_total
        
        return jsonify({
            'success': True,
            'statistics': {
                'supported_languages': len(localization_service.supported_languages),
                'total_translations': total_translations,
                'default_language': localization_service.default_language.value,
                'translation_stats': translation_stats,
                'language_configs': {
                    lang.value: {
                        'name': localization_service.language_config[lang]['name'],
                        'native_name': localization_service.language_config[lang]['native_name'],
                        'direction': localization_service.language_config[lang]['direction'].value,
                        'locale': localization_service.language_config[lang]['locale'],
                        'font_family': localization_service.language_config[lang]['font_family']
                    }
                    for lang in localization_service.supported_languages
                }
            }
        }), 200
        
    except Exception as e:
        current_app.logger.error(f"Get localization stats error: {str(e)}")
        return jsonify({
            'success': False,
            'error': 'Failed to get localization statistics',
            'message': str(e)
        }), 500

