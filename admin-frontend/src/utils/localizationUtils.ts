import { useLocalization } from '../contexts/LocalizationContext';

export const formatLocalizedNumber = (
  value: number,
  locale?: string,
  options?: Intl.NumberFormatOptions
): string => {
  const defaultLocale = locale || 'en-US';
  return new Intl.NumberFormat(defaultLocale, options).format(value);
};

export const formatLocalizedDate = (
  date: Date,
  locale?: string,
  options?: Intl.DateTimeFormatOptions
): string => {
  const defaultLocale = locale || 'en-US';
  return new Intl.DateTimeFormat(defaultLocale, options).format(date);
};

export const formatLocalizedCurrency = (
  amount: number,
  currency: string = 'USD',
  locale?: string
): string => {
  const defaultLocale = locale || 'en-US';
  return new Intl.NumberFormat(defaultLocale, {
    style: 'currency',
    currency: currency,
  }).format(amount);
};

export const formatLocalizedPercentage = (
  value: number,
  locale?: string,
  minimumFractionDigits: number = 0,
  maximumFractionDigits: number = 2
): string => {
  const defaultLocale = locale || 'en-US';
  return new Intl.NumberFormat(defaultLocale, {
    style: 'percent',
    minimumFractionDigits,
    maximumFractionDigits,
  }).format(value / 100);
};

export const getLocalizedDirection = (languageCode: string): 'ltr' | 'rtl' => {
  const rtlLanguages = ['ar', 'he', 'fa', 'ur'];
  const baseLanguage = languageCode.split('-')[0].toLowerCase();
  return rtlLanguages.includes(baseLanguage) ? 'rtl' : 'ltr';
};

export const getLocalizedFontFamily = (languageCode: string): string => {
  const baseLanguage = languageCode.split('-')[0].toLowerCase();
  
  switch (baseLanguage) {
    case 'ar':
      return "'Tahoma', 'Noto Sans Arabic', 'Arial', sans-serif";
    case 'he':
      return "'Noto Sans Hebrew', 'Arial', sans-serif";
    case 'fa':
      return "'Noto Sans Persian', 'Arial', sans-serif";
    case 'ur':
      return "'Noto Sans Urdu', 'Arial', sans-serif";
    case 'zh':
      return "'Noto Sans CJK SC', 'Microsoft YaHei', 'Arial', sans-serif";
    case 'ja':
      return "'Noto Sans CJK JP', 'Hiragino Sans', 'Arial', sans-serif";
    case 'ko':
      return "'Noto Sans CJK KR', 'Malgun Gothic', 'Arial', sans-serif";
    default:
      return "'Arial', sans-serif";
  }
};

export const useLocalizedFormatting = () => {
  const { currentLanguage, languageConfig } = useLocalization();
  
  const formatNumber = (value: number, type: 'number' | 'currency' | 'percentage' = 'number') => {
    const locale = languageConfig?.locale || currentLanguage || 'en-US';
    
    switch (type) {
      case 'currency':
        return formatLocalizedCurrency(value, 'USD', locale);
      case 'percentage':
        return formatLocalizedPercentage(value, locale);
      default:
        return formatLocalizedNumber(value, locale);
    }
  };
  
  const formatDate = (date: Date, options?: Intl.DateTimeFormatOptions) => {
    const locale = languageConfig?.locale || currentLanguage || 'en-US';
    return formatLocalizedDate(date, locale, options);
  };
  
  return {
    formatNumber,
    formatDate,
    currentLanguage,
    languageConfig,
  };
};
