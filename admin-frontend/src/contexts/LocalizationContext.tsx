import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { localizationService, Language, LanguageConfig } from '../services/localizationService';

interface LocalizationContextType {
  currentLanguage: string;
  languages: Language[];
  languageConfig: LanguageConfig | null;
  translations: Record<string, Record<string, string>>;
  isLoading: boolean;
  error: string | null;
  switchLanguage: (languageCode: string) => Promise<void>;
  t: (key: string, category?: string) => string;
  formatNumber: (value: number, type?: string) => Promise<string>;
  isRTL: boolean;
}

const LocalizationContext = createContext<LocalizationContextType | undefined>(undefined);

interface LocalizationProviderProps {
  children: ReactNode;
}

export const LocalizationProvider: React.FC<LocalizationProviderProps> = ({ children }) => {
  const [currentLanguage, setCurrentLanguage] = useState<string>('en');
  const [languages, setLanguages] = useState<Language[]>([]);
  const [languageConfig, setLanguageConfig] = useState<LanguageConfig | null>(null);
  const [translations, setTranslations] = useState<Record<string, Record<string, string>>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadLanguages = async () => {
    try {
      const response = await localizationService.getSupportedLanguages();
      if (response.success) {
        setLanguages(response.languages);
        setCurrentLanguage(response.default_language);
      }
    } catch (err) {
      setError('Failed to load supported languages');
      console.error('Error loading languages:', err);
    }
  };

  const loadLanguageConfig = async (lang: string) => {
    try {
      const response = await localizationService.getLanguageConfig(lang);
      if (response.success) {
        setLanguageConfig(response.language_config);
      }
    } catch (err) {
      setError('Failed to load language configuration');
      console.error('Error loading language config:', err);
    }
  };

  const loadTranslations = async (lang: string) => {
    try {
      const categories = ['system', 'auth', 'admin', 'assessment', 'careers', 'reports'];
      const response = await localizationService.getTranslations(lang, categories);
      if (response.success) {
        setTranslations(response.translations);
        
        const config = await localizationService.getLanguageConfig(lang);
        if (config.success) {
          const isRtlLang = config.language_config.direction === 'rtl';
          document.documentElement.dir = config.language_config.direction;
          document.documentElement.lang = lang;
          
          if (isRtlLang && lang.startsWith('ar')) {
            document.body.classList.add('font-arabic');
            document.body.style.fontFamily = config.language_config.font_family;
          } else {
            document.body.classList.remove('font-arabic');
            document.body.style.fontFamily = '';
          }
          
          if (isRtlLang) {
            document.body.classList.add('rtl-support');
            document.body.classList.remove('ltr-support');
          } else {
            document.body.classList.add('ltr-support');
            document.body.classList.remove('rtl-support');
          }
        }
      }
    } catch (err) {
      setError('Failed to load translations');
      console.error('Error loading translations:', err);
    }
  };

  const switchLanguage = async (languageCode: string) => {
    setIsLoading(true);
    setError(null);
    
    try {
      setCurrentLanguage(languageCode);
      localStorage.setItem('masark_language', languageCode);
      
      await Promise.all([
        loadLanguageConfig(languageCode),
        loadTranslations(languageCode)
      ]);
      
      const config = await localizationService.getLanguageConfig(languageCode);
      if (config.success) {
        document.documentElement.dir = config.language_config.direction;
        document.documentElement.lang = languageCode;
        document.body.style.fontFamily = config.language_config.font_family;
      }
    } catch (err) {
      setError('Failed to switch language');
      console.error('Error switching language:', err);
    } finally {
      setIsLoading(false);
    }
  };

  const t = (key: string, category: string = 'system'): string => {
    if (translations[category] && translations[category][key]) {
      return translations[category][key];
    }
    
    return key.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase());
  };

  const formatNumber = async (value: number, type: string = 'number'): Promise<string> => {
    try {
      const response = await localizationService.formatLocalizedContent(value, type, currentLanguage);
      return response.success ? response.formatted_value : value.toString();
    } catch (err) {
      console.error('Error formatting number:', err);
      return value.toString();
    }
  };

  const isRTL = languageConfig?.direction === 'rtl';

  useEffect(() => {
    const initializeLocalization = async () => {
      setIsLoading(true);
      
      const savedLanguage = localStorage.getItem('masark_language') || 'en';
      
      try {
        await loadLanguages();
        await switchLanguage(savedLanguage);
      } catch (err) {
        setError('Failed to initialize localization');
        console.error('Error initializing localization:', err);
      } finally {
        setIsLoading(false);
      }
    };

    initializeLocalization();
  }, [switchLanguage]);

  const value: LocalizationContextType = {
    currentLanguage,
    languages,
    languageConfig,
    translations,
    isLoading,
    error,
    switchLanguage,
    t,
    formatNumber,
    isRTL
  };

  return (
    <LocalizationContext.Provider value={value}>
      {children}
    </LocalizationContext.Provider>
  );
};

export const useLocalization = (): LocalizationContextType => {
  const context = useContext(LocalizationContext);
  if (context === undefined) {
    throw new Error('useLocalization must be used within a LocalizationProvider');
  }
  return context;
};
