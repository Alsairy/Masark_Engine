import React from 'react';
import { useLocalization } from '../contexts/LocalizationContext';
import { useRTL } from '../hooks/useRTL';
import { Button } from './ui/button';

interface LocalizedButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  textKey?: string;
  category?: string;
  variant?: 'default' | 'destructive' | 'outline' | 'secondary' | 'ghost' | 'link';
  size?: 'default' | 'sm' | 'lg' | 'icon';
  children?: React.ReactNode;
}

export const LocalizedButton: React.FC<LocalizedButtonProps> = ({
  textKey,
  category = 'system',
  variant = 'default',
  size = 'default',
  className = '',
  children,
  ...props
}) => {
  const { t, isRTL } = useLocalization();
  const { getRTLClasses } = useRTL();

  const buttonText = textKey ? t(textKey, category) : undefined;
  const buttonClasses = getRTLClasses(`btn-rtl ${className}`);

  return (
    <Button
      {...props}
      variant={variant}
      size={size}
      className={buttonClasses}
      dir={isRTL ? 'rtl' : 'ltr'}
    >
      {buttonText || children}
    </Button>
  );
};

export default LocalizedButton;
