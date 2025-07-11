import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  Settings, 
  Save, 
  RefreshCw, 
  AlertCircle, 
  CheckCircle,
  Users,
  Globe,
  Shield,
  Database,
  Zap,
  FileText
} from 'lucide-react';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { systemApi } from '../../services/api';

interface SystemConfig {
  maxConcurrentSessions: number;
  sessionTimeoutMinutes: number;
  supportedLanguages: string[];
  defaultLanguage: string;
  enableMultiTenant: boolean;
  enableCaching: boolean;
  cacheExpirationMinutes: number;
  enableRateLimiting: boolean;
  rateLimitRequestsPerMinute: number;
  enableAuditLogging: boolean;
  enablePerformanceMonitoring: boolean;
  assessmentSettings: {
    questionsPerSession: number;
    enableTieBreaker: boolean;
    enableCareerClusterRating: boolean;
    enableAssessmentRating: boolean;
    minCompletionPercentage: number;
  };
  deploymentModes: string[];
  personalityTypes: number;
  careerClusters: number;
  pathways: number;
}

const AssessmentConfiguration: React.FC = () => {
  const [activeTab, setActiveTab] = useState('general');
  const [hasChanges, setHasChanges] = useState(false);
  const [config, setConfig] = useState<SystemConfig | null>(null);

  const queryClient = useQueryClient();

  const { data: systemConfig, isLoading, error } = useQuery({
    queryKey: ['system-config'],
    queryFn: () => systemApi.getSystemConfig(),
  });

  React.useEffect(() => {
    if (systemConfig) {
      setConfig(systemConfig);
    }
  }, [systemConfig]);

  const updateConfigMutation = useMutation({
    mutationFn: (configData: Partial<SystemConfig>) => systemApi.updateSystemConfig(configData),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['system-config'] });
      setHasChanges(false);
    },
  });

  const handleConfigChange = (key: string, value: string | number | boolean) => {
    if (!config) return;
    
    const newConfig = { ...config };
    if (key.includes('.')) {
      const [parent, child] = key.split('.');
      const parentObj = newConfig[parent as keyof SystemConfig] as Record<string, unknown>;
      if (parentObj && typeof parentObj === 'object') {
        (newConfig as Record<string, unknown>)[parent] = {
          ...parentObj,
          [child]: value
        };
      }
    } else {
      (newConfig as Record<string, unknown>)[key] = value;
    }
    
    setConfig(newConfig);
    setHasChanges(true);
  };

  const handleSave = () => {
    if (config) {
      updateConfigMutation.mutate(config);
    }
  };

  const handleReset = () => {
    if (systemConfig) {
      setConfig(systemConfig);
      setHasChanges(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <Settings className="h-8 w-8 animate-spin mx-auto mb-4 text-blue-600" />
          <p className="text-gray-600">Loading configuration...</p>
        </div>
      </div>
    );
  }

  if (error || !config) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <AlertCircle className="h-8 w-8 mx-auto mb-4 text-red-600" />
          <p className="text-red-600 mb-4">Error loading configuration</p>
          <Button onClick={() => queryClient.invalidateQueries({ queryKey: ['system-config'] })} variant="outline">
            <RefreshCw className="h-4 w-4 mr-2" />
            Retry
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-semibold text-gray-900">Assessment Configuration</h2>
          <p className="text-gray-600 mt-1">
            Configure system settings and assessment parameters
          </p>
        </div>
        <div className="flex items-center space-x-3">
          {hasChanges && (
            <Badge variant="outline" className="bg-yellow-50 text-yellow-700 border-yellow-200">
              Unsaved Changes
            </Badge>
          )}
          <Button onClick={handleReset} variant="outline" disabled={!hasChanges}>
            Reset
          </Button>
          <Button 
            onClick={handleSave} 
            disabled={!hasChanges || updateConfigMutation.isPending}
          >
            <Save className="h-4 w-4 mr-2" />
            Save Changes
          </Button>
        </div>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-6">
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="general">General</TabsTrigger>
          <TabsTrigger value="assessment">Assessment</TabsTrigger>
          <TabsTrigger value="performance">Performance</TabsTrigger>
          <TabsTrigger value="security">Security</TabsTrigger>
        </TabsList>

        <TabsContent value="general" className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center space-x-2">
                  <Users className="h-5 w-5" />
                  <span>Session Management</span>
                </CardTitle>
                <CardDescription>
                  Configure concurrent sessions and timeouts
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Max Concurrent Sessions
                  </label>
                  <Input
                    type="number"
                    value={config.maxConcurrentSessions}
                    onChange={(e) => handleConfigChange('maxConcurrentSessions', parseInt(e.target.value))}
                    min={1}
                    max={1000000}
                  />
                  <p className="text-xs text-gray-500 mt-1">
                    Maximum number of simultaneous assessment sessions
                  </p>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Session Timeout (minutes)
                  </label>
                  <Input
                    type="number"
                    value={config.sessionTimeoutMinutes}
                    onChange={(e) => handleConfigChange('sessionTimeoutMinutes', parseInt(e.target.value))}
                    min={5}
                    max={180}
                  />
                  <p className="text-xs text-gray-500 mt-1">
                    Automatic session timeout for inactive users
                  </p>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="flex items-center space-x-2">
                  <Globe className="h-5 w-5" />
                  <span>Localization</span>
                </CardTitle>
                <CardDescription>
                  Language and regional settings
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Supported Languages
                  </label>
                  <div className="flex flex-wrap gap-2">
                    {(config.supportedLanguages || []).map((lang) => (
                      <Badge key={lang} variant="outline" className="uppercase">
                        {lang}
                      </Badge>
                    ))}
                  </div>
                  <p className="text-xs text-gray-500 mt-1">
                    Languages available for assessments
                  </p>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Default Language
                  </label>
                  <select
                    value={config.defaultLanguage}
                    onChange={(e) => handleConfigChange('defaultLanguage', e.target.value)}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  >
                    {(config.supportedLanguages || []).map((lang) => (
                      <option key={lang} value={lang}>
                        {lang.toUpperCase()}
                      </option>
                    ))}
                  </select>
                  <p className="text-xs text-gray-500 mt-1">
                    Default language for new assessments
                  </p>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="assessment" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center space-x-2">
                <FileText className="h-5 w-5" />
                <span>Assessment Settings</span>
              </CardTitle>
              <CardDescription>
                Configure assessment behavior and requirements
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Questions Per Session
                  </label>
                  <Input
                    type="number"
                    value={config.assessmentSettings.questionsPerSession}
                    onChange={(e) => handleConfigChange('assessmentSettings.questionsPerSession', parseInt(e.target.value))}
                    min={10}
                    max={100}
                  />
                  <p className="text-xs text-gray-500 mt-1">
                    Number of questions in each assessment
                  </p>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Min Completion Percentage
                  </label>
                  <Input
                    type="number"
                    value={config.assessmentSettings.minCompletionPercentage}
                    onChange={(e) => handleConfigChange('assessmentSettings.minCompletionPercentage', parseInt(e.target.value))}
                    min={50}
                    max={100}
                  />
                  <p className="text-xs text-gray-500 mt-1">
                    Minimum completion required for results
                  </p>
                </div>
              </div>

              <div className="space-y-3">
                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="enableTieBreaker"
                    checked={config.assessmentSettings.enableTieBreaker}
                    onChange={(e) => handleConfigChange('assessmentSettings.enableTieBreaker', e.target.checked)}
                    className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                  />
                  <label htmlFor="enableTieBreaker" className="text-sm text-gray-700">
                    Enable Tie Breaker Questions
                  </label>
                </div>

                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="enableCareerClusterRating"
                    checked={config.assessmentSettings.enableCareerClusterRating}
                    onChange={(e) => handleConfigChange('assessmentSettings.enableCareerClusterRating', e.target.checked)}
                    className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                  />
                  <label htmlFor="enableCareerClusterRating" className="text-sm text-gray-700">
                    Enable Career Cluster Rating
                  </label>
                </div>

                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="enableAssessmentRating"
                    checked={config.assessmentSettings.enableAssessmentRating}
                    onChange={(e) => handleConfigChange('assessmentSettings.enableAssessmentRating', e.target.checked)}
                    className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                  />
                  <label htmlFor="enableAssessmentRating" className="text-sm text-gray-700">
                    Enable Assessment Rating
                  </label>
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>Data Overview</CardTitle>
              <CardDescription>
                Current assessment data statistics
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                <div className="text-center">
                  <div className="text-3xl font-bold text-purple-600">
                    {config.personalityTypes}
                  </div>
                  <div className="text-sm text-gray-600">Personality Types</div>
                </div>
                <div className="text-center">
                  <div className="text-3xl font-bold text-blue-600">
                    {config.careerClusters}
                  </div>
                  <div className="text-sm text-gray-600">Career Clusters</div>
                </div>
                <div className="text-center">
                  <div className="text-3xl font-bold text-green-600">
                    {config.pathways}
                  </div>
                  <div className="text-sm text-gray-600">Career Pathways</div>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="performance" className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center space-x-2">
                  <Zap className="h-5 w-5" />
                  <span>Performance Settings</span>
                </CardTitle>
                <CardDescription>
                  Configure caching and performance optimization
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="enableCaching"
                    checked={config.enableCaching}
                    onChange={(e) => handleConfigChange('enableCaching', e.target.checked)}
                    className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                  />
                  <label htmlFor="enableCaching" className="text-sm text-gray-700">
                    Enable Caching
                  </label>
                </div>

                {config.enableCaching && (
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Cache Expiration (minutes)
                    </label>
                    <Input
                      type="number"
                      value={config.cacheExpirationMinutes}
                      onChange={(e) => handleConfigChange('cacheExpirationMinutes', parseInt(e.target.value))}
                      min={1}
                      max={1440}
                    />
                  </div>
                )}

                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="enablePerformanceMonitoring"
                    checked={config.enablePerformanceMonitoring}
                    onChange={(e) => handleConfigChange('enablePerformanceMonitoring', e.target.checked)}
                    className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                  />
                  <label htmlFor="enablePerformanceMonitoring" className="text-sm text-gray-700">
                    Enable Performance Monitoring
                  </label>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="flex items-center space-x-2">
                  <Database className="h-5 w-5" />
                  <span>Multi-Tenancy</span>
                </CardTitle>
                <CardDescription>
                  Configure tenant isolation settings
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="enableMultiTenant"
                    checked={config.enableMultiTenant}
                    onChange={(e) => handleConfigChange('enableMultiTenant', e.target.checked)}
                    className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                  />
                  <label htmlFor="enableMultiTenant" className="text-sm text-gray-700">
                    Enable Multi-Tenant Support
                  </label>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-2">
                    Deployment Modes
                  </label>
                  <div className="flex flex-wrap gap-2">
                    {(config.deploymentModes || []).map((mode) => (
                      <Badge key={mode} variant="outline" className={
                        mode === 'MAWHIBA' ? 'bg-green-50 text-green-700 border-green-200' : 'bg-blue-50 text-blue-700 border-blue-200'
                      }>
                        {mode}
                      </Badge>
                    ))}
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="security" className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center space-x-2">
                  <Shield className="h-5 w-5" />
                  <span>Rate Limiting</span>
                </CardTitle>
                <CardDescription>
                  Configure API rate limiting and security
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="enableRateLimiting"
                    checked={config.enableRateLimiting}
                    onChange={(e) => handleConfigChange('enableRateLimiting', e.target.checked)}
                    className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                  />
                  <label htmlFor="enableRateLimiting" className="text-sm text-gray-700">
                    Enable Rate Limiting
                  </label>
                </div>

                {config.enableRateLimiting && (
                  <div>
                    <label className="block text-sm font-medium text-gray-700 mb-2">
                      Requests Per Minute
                    </label>
                    <Input
                      type="number"
                      value={config.rateLimitRequestsPerMinute}
                      onChange={(e) => handleConfigChange('rateLimitRequestsPerMinute', parseInt(e.target.value))}
                      min={10}
                      max={1000}
                    />
                  </div>
                )}
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="flex items-center space-x-2">
                  <AlertCircle className="h-5 w-5" />
                  <span>Audit & Logging</span>
                </CardTitle>
                <CardDescription>
                  Configure audit logging and monitoring
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex items-center space-x-2">
                  <input
                    type="checkbox"
                    id="enableAuditLogging"
                    checked={config.enableAuditLogging}
                    onChange={(e) => handleConfigChange('enableAuditLogging', e.target.checked)}
                    className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                  />
                  <label htmlFor="enableAuditLogging" className="text-sm text-gray-700">
                    Enable Audit Logging
                  </label>
                </div>

                <div className="bg-gray-50 p-4 rounded-lg">
                  <h4 className="text-sm font-medium text-gray-700 mb-2">Security Features</h4>
                  <ul className="text-sm text-gray-600 space-y-1">
                    <li>• JWT token authentication</li>
                    <li>• Role-based access control</li>
                    <li>• Request rate limiting</li>
                    <li>• Audit trail logging</li>
                    <li>• Data encryption at rest</li>
                  </ul>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>
      </Tabs>

      {updateConfigMutation.isError && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <div className="flex items-center space-x-2">
            <AlertCircle className="h-5 w-5 text-red-600" />
            <span className="text-red-800 font-medium">Error saving configuration</span>
          </div>
          <p className="text-red-700 text-sm mt-1">
            {updateConfigMutation.error?.message || 'Failed to update configuration'}
          </p>
        </div>
      )}

      {updateConfigMutation.isSuccess && (
        <div className="bg-green-50 border border-green-200 rounded-lg p-4">
          <div className="flex items-center space-x-2">
            <CheckCircle className="h-5 w-5 text-green-600" />
            <span className="text-green-800 font-medium">Configuration saved successfully</span>
          </div>
        </div>
      )}
    </div>
  );
};

export default AssessmentConfiguration;
