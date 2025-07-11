import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { apiIntegrationService } from '../../services/apiIntegrationService';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Badge } from '../ui/badge';
import { Button } from '../ui/button';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { BarChart3, TrendingUp, Clock, AlertTriangle, Activity } from 'lucide-react';

interface UsageByHour {
  hour: string;
  requests: number;
}

interface TopEndpoint {
  endpoint: string;
  count: number;
  averageResponseTime: number;
}

interface EndpointUsage {
  method: string;
  path: string;
  totalRequests: number;
  successfulRequests: number;
  averageResponseTime: number;
  errorCount: number;
}

interface ErrorLog {
  statusCode: number;
  method: string;
  endpoint: string;
  timestamp: string;
  message: string;
  userAgent?: string;
}

interface ErrorsByType {
  errorType: string;
  count: number;
}

const ApiUsageMonitoring: React.FC = () => {
  const [timeRange, setTimeRange] = useState('7d');
  const [selectedUserId] = useState<number | undefined>();
  const [selectedApiKeyId] = useState<number | undefined>();

  const { data: usageStats, isLoading: statsLoading } = useQuery({
    queryKey: ['apiUsageStats', timeRange, selectedUserId, selectedApiKeyId],
    queryFn: () => apiIntegrationService.getApiUsageStats(timeRange, selectedUserId, selectedApiKeyId)
  });


  const { data: endpointUsage, isLoading: endpointLoading } = useQuery({
    queryKey: ['apiUsageByEndpoint', timeRange],
    queryFn: () => apiIntegrationService.getApiUsageByEndpoint(timeRange)
  });

  const { data: errorLogs, isLoading: errorsLoading } = useQuery({
    queryKey: ['apiErrorLogs'],
    queryFn: () => apiIntegrationService.getApiErrorLogs(50)
  });

  const timeRangeOptions = [
    { value: '1h', label: 'Last Hour' },
    { value: '24h', label: 'Last 24 Hours' },
    { value: '7d', label: 'Last 7 Days' },
    { value: '30d', label: 'Last 30 Days' },
    { value: '90d', label: 'Last 90 Days' }
  ];

  const formatNumber = (num: number) => {
    if (num >= 1000000) return `${(num / 1000000).toFixed(1)}M`;
    if (num >= 1000) return `${(num / 1000).toFixed(1)}K`;
    return num.toString();
  };

  const formatDuration = (ms: number) => {
    if (ms < 1000) return `${ms}ms`;
    return `${(ms / 1000).toFixed(2)}s`;
  };

  if (statsLoading) {
    return (
      <div className="flex items-center justify-center p-8">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-gray-900">API Usage Monitoring</h2>
          <p className="text-gray-600">Monitor API usage, performance, and errors</p>
        </div>
        <div className="flex items-center space-x-4">
          <Select value={timeRange} onValueChange={setTimeRange}>
            <SelectTrigger className="w-40">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {timeRangeOptions.map((option) => (
                <SelectItem key={option.value} value={option.value}>
                  {option.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <Button variant="outline" onClick={() => window.location.reload()}>
            Refresh
          </Button>
        </div>
      </div>

      <Tabs defaultValue="overview" className="space-y-6">
        <TabsList>
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="endpoints">Endpoints</TabsTrigger>
          <TabsTrigger value="errors">Errors</TabsTrigger>
          <TabsTrigger value="performance">Performance</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Total Requests</CardTitle>
                <Activity className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{formatNumber(usageStats?.totalRequests || 0)}</div>
                <p className="text-xs text-muted-foreground">
                  +{formatNumber(usageStats?.requestsLast24Hours || 0)} in last 24h
                </p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Success Rate</CardTitle>
                <TrendingUp className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">
                  {usageStats?.totalRequests ? 
                    ((usageStats.successfulRequests / usageStats.totalRequests) * 100).toFixed(1) : 0}%
                </div>
                <p className="text-xs text-muted-foreground">
                  {formatNumber(usageStats?.successfulRequests || 0)} successful requests
                </p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Avg Response Time</CardTitle>
                <Clock className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">
                  {formatDuration(usageStats?.averageResponseTime || 0)}
                </div>
                <p className="text-xs text-muted-foreground">
                  Across all endpoints
                </p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Failed Requests</CardTitle>
                <AlertTriangle className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{formatNumber(usageStats?.failedRequests || 0)}</div>
                <p className="text-xs text-muted-foreground">
                  {usageStats?.totalRequests ? 
                    ((usageStats.failedRequests / usageStats.totalRequests) * 100).toFixed(1) : 0}% error rate
                </p>
              </CardContent>
            </Card>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <Card>
              <CardHeader>
                <CardTitle>Usage by Hour</CardTitle>
                <CardDescription>Request volume over time</CardDescription>
              </CardHeader>
              <CardContent>
                {usageStats?.usageByHour?.length > 0 ? (
                  <div className="space-y-2">
                    {usageStats.usageByHour.slice(-12).map((item: UsageByHour, index: number) => (
                      <div key={index} className="flex items-center justify-between">
                        <span className="text-sm text-gray-600">{item.hour}</span>
                        <div className="flex items-center space-x-2">
                          <div className="w-32 bg-gray-200 rounded-full h-2">
                            <div
                              className="bg-blue-600 h-2 rounded-full"
                              style={{
                                width: `${Math.min((item.requests / Math.max(...usageStats.usageByHour.map((h: UsageByHour) => h.requests))) * 100, 100)}%`
                              }}
                            />
                          </div>
                          <span className="text-sm font-medium w-12 text-right">{item.requests}</span>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-gray-500 text-center py-8">No usage data available</p>
                )}
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Top Endpoints</CardTitle>
                <CardDescription>Most frequently used API endpoints</CardDescription>
              </CardHeader>
              <CardContent>
                {usageStats?.topEndpoints?.length > 0 ? (
                  <div className="space-y-3">
                    {usageStats.topEndpoints.slice(0, 8).map((endpoint: TopEndpoint, index: number) => (
                      <div key={index} className="flex items-center justify-between">
                        <div className="flex-1">
                          <p className="text-sm font-medium truncate">{endpoint.endpoint}</p>
                          <p className="text-xs text-gray-500">
                            Avg: {formatDuration(endpoint.averageResponseTime)}
                          </p>
                        </div>
                        <Badge variant="outline">{formatNumber(endpoint.count)}</Badge>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-gray-500 text-center py-8">No endpoint data available</p>
                )}
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="endpoints" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Endpoint Usage Statistics</CardTitle>
              <CardDescription>Detailed usage statistics for each API endpoint</CardDescription>
            </CardHeader>
            <CardContent>
              {endpointLoading ? (
                <div className="flex items-center justify-center py-8">
                  <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600"></div>
                </div>
              ) : endpointUsage?.length > 0 ? (
                <div className="space-y-4">
                  {endpointUsage.map((endpoint: EndpointUsage, index: number) => (
                    <div key={index} className="border rounded-lg p-4">
                      <div className="flex items-center justify-between mb-2">
                        <div className="flex items-center space-x-2">
                          <Badge variant="outline">{endpoint.method}</Badge>
                          <span className="font-medium">{endpoint.path}</span>
                        </div>
                        <Badge>{formatNumber(endpoint.totalRequests)} requests</Badge>
                      </div>
                      <div className="grid grid-cols-3 gap-4 text-sm">
                        <div>
                          <span className="text-gray-500">Success Rate:</span>
                          <span className="ml-2 font-medium">
                            {((endpoint.successfulRequests / endpoint.totalRequests) * 100).toFixed(1)}%
                          </span>
                        </div>
                        <div>
                          <span className="text-gray-500">Avg Response:</span>
                          <span className="ml-2 font-medium">{formatDuration(endpoint.averageResponseTime)}</span>
                        </div>
                        <div>
                          <span className="text-gray-500">Errors:</span>
                          <span className="ml-2 font-medium">{endpoint.errorCount}</span>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-gray-500 text-center py-8">No endpoint usage data available</p>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="errors" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Recent API Errors</CardTitle>
              <CardDescription>Latest errors and issues in API requests</CardDescription>
            </CardHeader>
            <CardContent>
              {errorsLoading ? (
                <div className="flex items-center justify-center py-8">
                  <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600"></div>
                </div>
              ) : errorLogs?.length > 0 ? (
                <div className="space-y-3">
                  {errorLogs.map((error: ErrorLog, index: number) => (
                    <div key={index} className="border-l-4 border-red-500 bg-red-50 p-4 rounded">
                      <div className="flex items-center justify-between mb-2">
                        <div className="flex items-center space-x-2">
                          <Badge variant="destructive">{error.statusCode}</Badge>
                          <span className="font-medium">{error.method} {error.endpoint}</span>
                        </div>
                        <span className="text-sm text-gray-500">
                          {new Date(error.timestamp).toLocaleString()}
                        </span>
                      </div>
                      <p className="text-sm text-gray-700">{error.message}</p>
                      {error.userAgent && (
                        <p className="text-xs text-gray-500 mt-1">User Agent: {error.userAgent}</p>
                      )}
                    </div>
                  ))}
                </div>
              ) : (
                <p className="text-gray-500 text-center py-8">No recent errors</p>
              )}
            </CardContent>
          </Card>

          {usageStats?.errorsByType?.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle>Error Distribution</CardTitle>
                <CardDescription>Breakdown of errors by type</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  {usageStats.errorsByType.map((errorType: ErrorsByType, index: number) => (
                    <div key={index} className="flex items-center justify-between">
                      <span className="text-sm">{errorType.errorType}</span>
                      <div className="flex items-center space-x-2">
                        <div className="w-32 bg-gray-200 rounded-full h-2">
                          <div
                            className="bg-red-500 h-2 rounded-full"
                            style={{
                              width: `${Math.min((errorType.count / Math.max(...usageStats.errorsByType.map((e: ErrorsByType) => e.count))) * 100, 100)}%`
                            }}
                          />
                        </div>
                        <Badge variant="outline">{errorType.count}</Badge>
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}
        </TabsContent>

        <TabsContent value="performance" className="space-y-6">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <Card>
              <CardHeader>
                <CardTitle>Response Time Trends</CardTitle>
                <CardDescription>API performance over time</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="text-center py-8">
                  <BarChart3 className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                  <p className="text-gray-500">Performance charts coming soon</p>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>System Health</CardTitle>
                <CardDescription>Overall API system status</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <span className="text-sm">API Status</span>
                    <Badge className="bg-green-100 text-green-800">Operational</Badge>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-sm">Database</span>
                    <Badge className="bg-green-100 text-green-800">Healthy</Badge>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-sm">Cache</span>
                    <Badge className="bg-green-100 text-green-800">Active</Badge>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-sm">Rate Limiting</span>
                    <Badge className="bg-green-100 text-green-800">Enabled</Badge>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default ApiUsageMonitoring;
