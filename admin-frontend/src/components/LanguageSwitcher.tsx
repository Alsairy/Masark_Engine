import React from 'react';
import { Button } from './ui/button';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from './ui/select';
import { Globe, ChevronDown } from 'lucide-react';
import { useLocalization } from '../contexts/LocalizationContext';

interface LanguageSwitcherProps {
  variant?: 'button' | 'select';
  className?: string;
}

export const LanguageSwitcher: React.FC<LanguageSwitcherProps> = ({ 
  variant = 'select', 
  className = '' 
}) => {
  const { 
    currentLanguage, 
    languages, 
    switchLanguage, 
    isLoading,
    isRTL 
  } = useLocalization();

  const currentLang = languages.find(lang => lang.code === currentLanguage);

  if (variant === 'button') {
    return (
      <div className={`relative ${className}`}>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => {
            const nextLang = currentLanguage === 'en' ? 'ar' : 'en';
            switchLanguage(nextLang);
          }}
          disabled={isLoading}
          className={`flex items-center space-x-2 ${isRTL ? 'flex-row-reverse space-x-reverse' : ''}`}
        >
          <Globe className="h-4 w-4" />
          <span className="text-sm font-medium">
            {currentLang?.native_name || currentLanguage.toUpperCase()}
          </span>
          <ChevronDown className="h-3 w-3" />
        </Button>
      </div>
    );
  }

  return (
    <div className={`flex items-center space-x-2 ${isRTL ? 'flex-row-reverse space-x-reverse' : ''} ${className}`}>
      <Globe className="h-4 w-4 text-gray-500" />
      <Select
        value={currentLanguage}
        onValueChange={switchLanguage}
        disabled={isLoading}
      >
        <SelectTrigger className="w-32">
          <SelectValue>
            {currentLang?.native_name || currentLanguage.toUpperCase()}
          </SelectValue>
        </SelectTrigger>
        <SelectContent>
          {languages.map((language) => (
            <SelectItem key={language.code} value={language.code}>
              <div className={`flex items-center space-x-2 ${isRTL ? 'flex-row-reverse space-x-reverse' : ''}`}>
                <span className="font-medium">{language.native_name}</span>
                <span className="text-sm text-gray-500">({language.name})</span>
              </div>
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  );
};

export default LanguageSwitcher;
