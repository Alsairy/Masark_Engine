import { useLocalization } from '../contexts/LocalizationContext';

export const useRTL = () => {
  const { isRTL, currentLanguage, languageConfig } = useLocalization();

  const getRTLClasses = (baseClasses: string = '') => {
    const rtlClasses = isRTL ? 'rtl-support font-arabic' : 'ltr-support';
    return `${baseClasses} ${rtlClasses}`.trim();
  };

  const getDirectionalClasses = (ltrClasses: string, rtlClasses: string) => {
    return isRTL ? rtlClasses : ltrClasses;
  };

  const getTextAlign = () => {
    return isRTL ? 'text-right' : 'text-left';
  };

  const getFlexDirection = () => {
    return isRTL ? 'flex-row-reverse' : 'flex-row';
  };

  const getSpacing = (property: 'margin' | 'padding', side: 'left' | 'right', value: string) => {
    if (isRTL) {
      const oppositeSide = side === 'left' ? 'right' : 'left';
      return `${property}-${oppositeSide}-${value}`;
    }
    return `${property}-${side}-${value}`;
  };

  const getBorderSide = (side: 'left' | 'right') => {
    if (isRTL) {
      return side === 'left' ? 'border-r' : 'border-l';
    }
    return side === 'left' ? 'border-l' : 'border-r';
  };

  const getPosition = (side: 'left' | 'right') => {
    if (isRTL) {
      return side === 'left' ? 'right-0' : 'left-0';
    }
    return side === 'left' ? 'left-0' : 'right-0';
  };

  return {
    isRTL,
    currentLanguage,
    languageConfig,
    getRTLClasses,
    getDirectionalClasses,
    getTextAlign,
    getFlexDirection,
    getSpacing,
    getBorderSide,
    getPosition,
  };
};

export default useRTL;
