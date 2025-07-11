import React from 'react';
import { useLocalization } from '../contexts/LocalizationContext';
import { useRTL } from '../hooks/useRTL';
import { Input } from './ui/input';

interface LocalizedInputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  placeholderKey?: string;
  labelKey?: string;
  category?: string;
  className?: string;
}

export const LocalizedInput: React.FC<LocalizedInputProps> = ({
  placeholderKey,
  labelKey,
  category = 'system',
  className = '',
  ...props
}) => {
  const { t, isRTL } = useLocalization();
  const { getRTLClasses, getTextAlign } = useRTL();

  const placeholder = placeholderKey ? t(placeholderKey, category) : props.placeholder;
  const label = labelKey ? t(labelKey, category) : undefined;
  
  const inputClasses = getRTLClasses(`input-rtl ${getTextAlign()} ${className}`);

  return (
    <div className="space-y-2">
      {label && (
        <label className={getRTLClasses('block text-sm font-medium text-gray-700 dark:text-gray-300')}>
          {label}
        </label>
      )}
      <Input
        {...props}
        placeholder={placeholder}
        className={inputClasses}
        dir={isRTL ? 'rtl' : 'ltr'}
      />
    </div>
  );
};

export default LocalizedInput;
