import React, { useState, useEffect } from 'react';
import { Settings, Globe, GraduationCap, AlertCircle, CheckCircle } from 'lucide-react';
import api from '../../services/api';

interface DeploymentModeConfig {
  mode: 'STANDARD' | 'MAWHIBA';
  description: string;
  features: string[];
  pathwayVisibility: string;
}

const deploymentModes: Record<string, DeploymentModeConfig> = {
  STANDARD: {
    mode: 'STANDARD',
    description: 'Standard deployment for general educational institutions',
    features: [
      'MOE pathway recommendations only',
      'Standard career guidance interface',
      'General education focus',
      'Suitable for public schools'
    ],
    pathwayVisibility: 'MOE pathways only (5 tracks)'
  },
  MAWHIBA: {
    mode: 'MAWHIBA',
    description: 'Enhanced deployment for gifted education programs',
    features: [
      'Both MOE and Mawhiba pathway recommendations',
      'Advanced career planning tools',
      'Gifted education focus',
      'Specialized aptitude considerations'
    ],
    pathwayVisibility: 'MOE + Mawhiba pathways (9 tracks total)'
  }
};

interface DeploymentModeSelectorProps {
  onModeChange?: (mode: 'STANDARD' | 'MAWHIBA') => void;
  disabled?: boolean;
}

const DeploymentModeSelector: React.FC<DeploymentModeSelectorProps> = ({ 
  onModeChange, 
  disabled = false 
}) => {
  const [currentMode, setCurrentMode] = useState<'STANDARD' | 'MAWHIBA'>('STANDARD');
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  useEffect(() => {
    fetchCurrentMode();
  }, []);

  const fetchCurrentMode = async () => {
    try {
      setLoading(true);
      const response = await api.get('/system/deployment-mode');
      setCurrentMode(response.data.deploymentMode || 'STANDARD');
    } catch (err) {
      console.error('Failed to fetch deployment mode:', err);
      setError('Failed to load current deployment mode');
    } finally {
      setLoading(false);
    }
  };

  const handleModeChange = async (newMode: 'STANDARD' | 'MAWHIBA') => {
    if (newMode === currentMode || disabled) return;

    try {
      setSaving(true);
      setError(null);
      setSuccess(false);

      await api.put('/system/deployment-mode', { deploymentMode: newMode });
      
      setCurrentMode(newMode);
      setSuccess(true);
      onModeChange?.(newMode);

      setTimeout(() => setSuccess(false), 3000);
    } catch (err: any) {
      console.error('Failed to update deployment mode:', err);
      setError(err.response?.data?.message || 'Failed to update deployment mode');
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="flex items-center space-x-3 mb-4">
          <Settings className="h-6 w-6 text-gray-400 animate-spin" />
          <h3 className="text-lg font-medium text-gray-900">Loading Deployment Configuration...</h3>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center space-x-3">
          <Settings className="h-6 w-6 text-gray-600" />
          <h3 className="text-lg font-medium text-gray-900">Deployment Mode Configuration</h3>
        </div>
        {success && (
          <div className="flex items-center space-x-2 text-green-600">
            <CheckCircle className="h-5 w-5" />
            <span className="text-sm font-medium">Configuration updated successfully</span>
          </div>
        )}
      </div>

      {error && (
        <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg">
          <div className="flex items-center space-x-2">
            <AlertCircle className="h-5 w-5 text-red-600" />
            <span className="text-sm text-red-800">{error}</span>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {Object.values(deploymentModes).map((config) => {
          const isSelected = currentMode === config.mode;
          const Icon = config.mode === 'STANDARD' ? Globe : GraduationCap;

          return (
            <div
              key={config.mode}
              className={`relative border-2 rounded-lg p-6 cursor-pointer transition-all duration-200 ${
                isSelected
                  ? 'border-blue-500 bg-blue-50'
                  : 'border-gray-200 hover:border-gray-300 bg-white'
              } ${disabled ? 'opacity-50 cursor-not-allowed' : ''}`}
              onClick={() => !disabled && handleModeChange(config.mode)}
            >
              {isSelected && (
                <div className="absolute top-4 right-4">
                  <div className="w-6 h-6 bg-blue-500 rounded-full flex items-center justify-center">
                    <CheckCircle className="h-4 w-4 text-white" />
                  </div>
                </div>
              )}

              <div className="flex items-center space-x-3 mb-4">
                <div className={`p-3 rounded-lg ${
                  isSelected ? 'bg-blue-100' : 'bg-gray-100'
                }`}>
                  <Icon className={`h-6 w-6 ${
                    isSelected ? 'text-blue-600' : 'text-gray-600'
                  }`} />
                </div>
                <div>
                  <h4 className={`text-lg font-semibold ${
                    isSelected ? 'text-blue-900' : 'text-gray-900'
                  }`}>
                    {config.mode} Mode
                  </h4>
                  <p className={`text-sm ${
                    isSelected ? 'text-blue-700' : 'text-gray-600'
                  }`}>
                    {config.description}
                  </p>
                </div>
              </div>

              <div className="space-y-3">
                <div>
                  <h5 className="text-sm font-medium text-gray-900 mb-2">Pathway Visibility:</h5>
                  <p className={`text-sm ${
                    isSelected ? 'text-blue-700' : 'text-gray-600'
                  }`}>
                    {config.pathwayVisibility}
                  </p>
                </div>

                <div>
                  <h5 className="text-sm font-medium text-gray-900 mb-2">Features:</h5>
                  <ul className="space-y-1">
                    {config.features.map((feature, index) => (
                      <li key={index} className={`text-sm flex items-start space-x-2 ${
                        isSelected ? 'text-blue-700' : 'text-gray-600'
                      }`}>
                        <span className="text-xs mt-1">•</span>
                        <span>{feature}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              </div>

              {saving && isSelected && (
                <div className="absolute inset-0 bg-white bg-opacity-75 flex items-center justify-center rounded-lg">
                  <div className="flex items-center space-x-2">
                    <Settings className="h-5 w-5 text-blue-600 animate-spin" />
                    <span className="text-sm text-blue-600 font-medium">Updating configuration...</span>
                  </div>
                </div>
              )}
            </div>
          );
        })}
      </div>

      <div className="mt-6 p-4 bg-gray-50 rounded-lg">
        <h5 className="text-sm font-medium text-gray-900 mb-2">Current Configuration Impact:</h5>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 text-sm text-gray-600">
          <div>
            <span className="font-medium">Assessment Results:</span>
            <span className="ml-2">
              {currentMode === 'STANDARD' ? 'MOE pathways only' : 'MOE + Mawhiba pathways'}
            </span>
          </div>
          <div>
            <span className="font-medium">Career Recommendations:</span>
            <span className="ml-2">
              {currentMode === 'STANDARD' ? '5 pathway options' : '9 pathway options'}
            </span>
          </div>
          <div>
            <span className="font-medium">Target Audience:</span>
            <span className="ml-2">
              {currentMode === 'STANDARD' ? 'General education' : 'Gifted programs'}
            </span>
          </div>
          <div>
            <span className="font-medium">Scoring Algorithm:</span>
            <span className="ml-2">
              {currentMode === 'STANDARD' ? 'MOE boost (+0.3)' : 'MOE (+0.3) + Mawhiba (+0.2)'}
            </span>
          </div>
        </div>
      </div>
    </div>
  );
};

export default DeploymentModeSelector;
