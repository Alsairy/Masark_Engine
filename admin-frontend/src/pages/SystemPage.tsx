import React, { useState, useEffect } from 'react';
import { 
  Server, 
  Database, 
  Activity, 
  Settings, 
  Users, 
  FileText, 
  Globe,
  CheckCircle,
  AlertCircle,
  XCircle,
  RefreshCw,
  BarChart3
} from 'lucide-react';
import { systemApi } from '../services/api';

interface SystemInfo {
  system: {
    name: string;
    version: string;
    description: string;
    features: string[];
    deployment_modes: string[];
    supported_languages: string[];
  };
  statistics: {
    personality_types: number;
    career_clusters: number;
    questions: number;
    pathways: number;
    total_sessions: number;
    completed_sessions: number;
  };
  api_endpoints: any;
  timestamp: string;
}

interface HealthCheck {
  status: string;
  timestamp: string;
  checks: {
    database: string;
    personality_types: string;
    questions: string;
  };
  details: {
    personality_types_count: number;
    questions_count: number;
    database_connected: boolean;
  };
}

interface SystemConfig {
  configurations: {
    max_concurrent_sessions: {
      value: string;
      description: string;
    };
    session_timeout_minutes: {
      value: string;
      description: string;
    };
    supported_languages: {
      value: string;
      description: string;
    };
  };
  timestamp: string;
}

const SystemPage: React.FC = () => {
  const [systemInfo, setSystemInfo] = useState<SystemInfo | null>(null);
  const [healthCheck, setHealthCheck] = useState<HealthCheck | null>(null);
  const [systemConfig, setSystemConfig] = useState<SystemConfig | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastRefresh, setLastRefresh] = useState<Date>(new Date());

  const fetchSystemData = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const [infoResponse, healthResponse, configResponse] = await Promise.all([
        systemApi.getSystemInfo(),
        systemApi.getHealthCheck(),
        systemApi.getSystemConfig()
      ]);

      setSystemInfo(infoResponse);
      setHealthCheck(healthResponse);
      setSystemConfig(configResponse);
      setLastRefresh(new Date());
    } catch (err: any) {
      console.error('Error fetching system data:', err);
      setError(err.response?.data?.message || 'Failed to fetch system data');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSystemData();
    
    const interval = setInterval(fetchSystemData, 30000);
    return () => clearInterval(interval);
  }, []);

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'healthy':
        return <CheckCircle className="h-5 w-5 text-green-500" />;
      case 'warning':
        return <AlertCircle className="h-5 w-5 text-yellow-500" />;
      case 'unhealthy':
        return <XCircle className="h-5 w-5 text-red-500" />;
      default:
        return <AlertCircle className="h-5 w-5 text-gray-500" />;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'healthy':
        return 'bg-green-100 text-green-800 border-green-200';
      case 'warning':
        return 'bg-yellow-100 text-yellow-800 border-yellow-200';
      case 'unhealthy':
        return 'bg-red-100 text-red-800 border-red-200';
      default:
        return 'bg-gray-100 text-gray-800 border-gray-200';
    }
  };

  if (loading && !systemInfo) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="flex items-center space-x-2">
          <RefreshCw className="h-6 w-6 animate-spin text-blue-600" />
          <span className="text-lg text-gray-600">Loading system information...</span>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">System Management</h1>
          <p className="text-gray-600 mt-1">Monitor system health, statistics, and configuration</p>
        </div>
        <div className="flex items-center space-x-4">
          <div className="text-sm text-gray-500">
            Last updated: {lastRefresh.toLocaleTimeString()}
          </div>
          <button
            onClick={fetchSystemData}
            disabled={loading}
            className="flex items-center space-x-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 disabled:opacity-50"
          >
            <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
            <span>Refresh</span>
          </button>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4">
          <div className="flex items-center space-x-2">
            <XCircle className="h-5 w-5 text-red-500" />
            <span className="text-red-800 font-medium">Error loading system data</span>
          </div>
          <p className="text-red-700 mt-1">{error}</p>
        </div>
      )}

      {healthCheck && (
        <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
          <div className={`p-6 rounded-lg border-2 ${getStatusColor(healthCheck.status)}`}>
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium">Overall Status</p>
                <p className="text-2xl font-bold capitalize">{healthCheck.status}</p>
              </div>
              {getStatusIcon(healthCheck.status)}
            </div>
          </div>

          <div className={`p-6 rounded-lg border-2 ${getStatusColor(healthCheck.checks.database)}`}>
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium">Database</p>
                <p className="text-2xl font-bold capitalize">{healthCheck.checks.database}</p>
              </div>
              <Database className="h-8 w-8 text-gray-600" />
            </div>
          </div>

          <div className={`p-6 rounded-lg border-2 ${getStatusColor(healthCheck.checks.personality_types)}`}>
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium">Personality Types</p>
                <p className="text-2xl font-bold">{healthCheck.details.personality_types_count}</p>
              </div>
              <Users className="h-8 w-8 text-gray-600" />
            </div>
          </div>

          <div className={`p-6 rounded-lg border-2 ${getStatusColor(healthCheck.checks.questions)}`}>
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium">Questions</p>
                <p className="text-2xl font-bold">{healthCheck.details.questions_count}</p>
              </div>
              <FileText className="h-8 w-8 text-gray-600" />
            </div>
          </div>
        </div>
      )}

      {systemInfo && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <div className="bg-white border border-gray-200 rounded-lg p-6">
            <div className="flex items-center space-x-3 mb-4">
              <Server className="h-6 w-6 text-blue-600" />
              <h2 className="text-lg font-semibold text-gray-900">System Information</h2>
            </div>
            
            <div className="space-y-4">
              <div>
                <h3 className="font-medium text-gray-900">{systemInfo.system.name}</h3>
                <p className="text-sm text-gray-600">Version {systemInfo.system.version}</p>
                <p className="text-sm text-gray-600 mt-1">{systemInfo.system.description}</p>
              </div>

              <div>
                <h4 className="font-medium text-gray-900 mb-2">Deployment Modes</h4>
                <div className="flex space-x-2">
                  {systemInfo.system.deployment_modes.map((mode) => (
                    <span
                      key={mode}
                      className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800"
                    >
                      {mode}
                    </span>
                  ))}
                </div>
              </div>

              <div>
                <h4 className="font-medium text-gray-900 mb-2">Supported Languages</h4>
                <div className="flex space-x-2">
                  {systemInfo.system.supported_languages.map((lang) => (
                    <span
                      key={lang}
                      className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800"
                    >
                      <Globe className="h-3 w-3 mr-1" />
                      {lang.toUpperCase()}
                    </span>
                  ))}
                </div>
              </div>
            </div>
          </div>

          <div className="bg-white border border-gray-200 rounded-lg p-6">
            <div className="flex items-center space-x-3 mb-4">
              <BarChart3 className="h-6 w-6 text-green-600" />
              <h2 className="text-lg font-semibold text-gray-900">System Statistics</h2>
            </div>
            
            <div className="grid grid-cols-2 gap-4">
              <div className="text-center p-4 bg-gray-50 rounded-lg">
                <div className="text-2xl font-bold text-gray-900">{systemInfo.statistics.personality_types}</div>
                <div className="text-sm text-gray-600">Personality Types</div>
              </div>
              <div className="text-center p-4 bg-gray-50 rounded-lg">
                <div className="text-2xl font-bold text-gray-900">{systemInfo.statistics.career_clusters}</div>
                <div className="text-sm text-gray-600">Career Clusters</div>
              </div>
              <div className="text-center p-4 bg-gray-50 rounded-lg">
                <div className="text-2xl font-bold text-gray-900">{systemInfo.statistics.questions}</div>
                <div className="text-sm text-gray-600">Questions</div>
              </div>
              <div className="text-center p-4 bg-gray-50 rounded-lg">
                <div className="text-2xl font-bold text-gray-900">{systemInfo.statistics.pathways}</div>
                <div className="text-sm text-gray-600">Pathways</div>
              </div>
              <div className="text-center p-4 bg-gray-50 rounded-lg">
                <div className="text-2xl font-bold text-gray-900">{systemInfo.statistics.total_sessions}</div>
                <div className="text-sm text-gray-600">Total Sessions</div>
              </div>
              <div className="text-center p-4 bg-gray-50 rounded-lg">
                <div className="text-2xl font-bold text-gray-900">{systemInfo.statistics.completed_sessions}</div>
                <div className="text-sm text-gray-600">Completed Sessions</div>
              </div>
            </div>
          </div>
        </div>
      )}

      {systemConfig && (
        <div className="bg-white border border-gray-200 rounded-lg p-6">
          <div className="flex items-center space-x-3 mb-4">
            <Settings className="h-6 w-6 text-purple-600" />
            <h2 className="text-lg font-semibold text-gray-900">System Configuration</h2>
          </div>
          
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="p-4 bg-gray-50 rounded-lg">
              <h3 className="font-medium text-gray-900 mb-2">Max Concurrent Sessions</h3>
              <div className="text-2xl font-bold text-blue-600">{systemConfig.configurations.max_concurrent_sessions.value}</div>
              <p className="text-sm text-gray-600 mt-1">{systemConfig.configurations.max_concurrent_sessions.description}</p>
            </div>
            
            <div className="p-4 bg-gray-50 rounded-lg">
              <h3 className="font-medium text-gray-900 mb-2">Session Timeout</h3>
              <div className="text-2xl font-bold text-orange-600">{systemConfig.configurations.session_timeout_minutes.value} min</div>
              <p className="text-sm text-gray-600 mt-1">{systemConfig.configurations.session_timeout_minutes.description}</p>
            </div>
            
            <div className="p-4 bg-gray-50 rounded-lg">
              <h3 className="font-medium text-gray-900 mb-2">Supported Languages</h3>
              <div className="text-lg font-bold text-green-600">{systemConfig.configurations.supported_languages.value}</div>
              <p className="text-sm text-gray-600 mt-1">{systemConfig.configurations.supported_languages.description}</p>
            </div>
          </div>
        </div>
      )}

      {systemInfo && (
        <div className="bg-white border border-gray-200 rounded-lg p-6">
          <div className="flex items-center space-x-3 mb-4">
            <Activity className="h-6 w-6 text-indigo-600" />
            <h2 className="text-lg font-semibold text-gray-900">Key Features</h2>
          </div>
          
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            {systemInfo.system.features.map((feature, index) => (
              <div key={index} className="flex items-center space-x-3 p-3 bg-gray-50 rounded-lg">
                <CheckCircle className="h-5 w-5 text-green-500 flex-shrink-0" />
                <span className="text-sm text-gray-700">{feature}</span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};

export default SystemPage;
