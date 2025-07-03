import api from './api';

export interface Language {
  code: string;
  name: string;
  native_name: string;
  direction: 'ltr' | 'rtl';
  locale: string;
  font_family: string;
}

export interface LanguageConfig {
  code: string;
  name: string;
  native_name: string;
  direction: 'ltr' | 'rtl';
  locale: string;
  font_family: string;
  date_format: string;
  time_format: string;
  decimal_separator: string;
  thousands_separator: string;
}

export interface TranslationResponse {
  success: boolean;
  language: string;
  translations: Record<string, Record<string, string>>;
  language_config: LanguageConfig;
}

export interface LocalizationStats {
  supported_languages: number;
  total_translations: number;
  default_language: string;
  translation_stats: Record<string, any>;
  language_configs: Record<string, LanguageConfig>;
}

class LocalizationService {
  async getSupportedLanguages(): Promise<{ success: boolean; languages: Language[]; default_language: string }> {
    const response = await api.get('/api/localization/languages');
    return response.data;
  }

  async getLanguageConfig(lang?: string): Promise<{ success: boolean; language_config: LanguageConfig }> {
    const params = lang ? { lang } : {};
    const response = await api.get('/api/localization/config', { params });
    return response.data;
  }

  async getTranslations(lang?: string, categories?: string[]): Promise<TranslationResponse> {
    const params: any = {};
    if (lang) params.lang = lang;
    if (categories && categories.length > 0) params.categories = categories.join(',');
    
    const response = await api.get('/api/localization/translations', { params });
    return response.data;
  }

  async translateText(keys: string[], language?: string, category?: string): Promise<{
    success: boolean;
    language: string;
    category: string;
    translations: Record<string, string>;
  }> {
    const response = await api.post('/api/localization/translate', {
      keys,
      language,
      category
    });
    return response.data;
  }

  async formatLocalizedContent(value: number, type: string = 'number', language?: string): Promise<{
    success: boolean;
    type: string;
    original_value: number;
    formatted_value: string;
    language: string;
  }> {
    const response = await api.post('/api/localization/format', {
      value,
      type,
      language
    });
    return response.data;
  }

  async localizeContent(content: Record<string, any>, language?: string): Promise<{
    success: boolean;
    language: string;
    original_content: Record<string, any>;
    localized_content: Record<string, any>;
    language_config: LanguageConfig;
  }> {
    const response = await api.post('/api/localization/localize', {
      content,
      language
    });
    return response.data;
  }

  async getLocalizationStats(): Promise<{ success: boolean; statistics: LocalizationStats }> {
    const response = await api.get('/api/localization/stats');
    return response.data;
  }
}

export const localizationService = new LocalizationService();
