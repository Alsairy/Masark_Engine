import React from 'react';
import { useLocalization } from '../contexts/LocalizationContext';
import { useRTL } from '../hooks/useRTL';

interface LocalizedTextProps {
  translationKey: string;
  category?: string;
  fallback?: string;
  className?: string;
  children?: React.ReactNode;
  as?: keyof JSX.IntrinsicElements;
}

export const LocalizedText: React.FC<LocalizedTextProps> = ({
  translationKey,
  category = 'system',
  fallback,
  className = '',
  children,
  as: Component = 'span'
}) => {
  const { t, isRTL } = useLocalization();
  const { getRTLClasses } = useRTL();

  const translatedText = t(translationKey, category) || fallback || translationKey;
  const rtlClasses = getRTLClasses(className);

  return (
    <Component className={rtlClasses}>
      {translatedText}
      {children}
    </Component>
  );
};

export default LocalizedText;
