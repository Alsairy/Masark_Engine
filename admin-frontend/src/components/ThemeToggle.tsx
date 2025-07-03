import React from 'react';
import { Button } from './ui/button';
import { Moon, Sun } from 'lucide-react';
import { useTheme } from '../contexts/ThemeContext';

interface ThemeToggleProps {
  variant?: 'button' | 'icon';
  className?: string;
}

export const ThemeToggle: React.FC<ThemeToggleProps> = ({ 
  variant = 'icon', 
  className = '' 
}) => {
  const { theme, toggleTheme } = useTheme();

  if (variant === 'button') {
    return (
      <Button
        variant="outline"
        size="sm"
        onClick={toggleTheme}
        className={`flex items-center space-x-2 ${className}`}
      >
        {theme === 'light' ? (
          <>
            <Moon className="h-4 w-4" />
            <span>Dark</span>
          </>
        ) : (
          <>
            <Sun className="h-4 w-4" />
            <span>Light</span>
          </>
        )}
      </Button>
    );
  }

  return (
    <Button
      variant="ghost"
      size="icon"
      onClick={toggleTheme}
      className={`h-9 w-9 ${className}`}
      title={`Switch to ${theme === 'light' ? 'dark' : 'light'} mode`}
    >
      {theme === 'light' ? (
        <Moon className="h-4 w-4" />
      ) : (
        <Sun className="h-4 w-4" />
      )}
    </Button>
  );
};

export default ThemeToggle;
