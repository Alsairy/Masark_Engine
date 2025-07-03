import React, { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { 
  FileText, 
  Users, 
  Clock, 
  CheckCircle, 
  AlertCircle, 
  RefreshCw,
  Download
} from 'lucide-react';
import { Button } from '../components/ui/button';
import { Input } from '../components/ui/input';
import { Card, CardContent } from '../components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../components/ui/tabs';
import AssessmentSessionList from '../components/assessment/AssessmentSessionList';
import AssessmentStatistics from '../components/assessment/AssessmentStatistics';
import QuestionManagement from '../components/assessment/QuestionManagement';
import AssessmentConfiguration from '../components/assessment/AssessmentConfiguration';
import { assessmentApi } from '../services/api';

const AssessmentPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState('overview');
  const [searchTerm, setSearchTerm] = useState('');
  const [filterStatus, setFilterStatus] = useState('all');
  const queryClient = useQueryClient();

  const { data: assessmentStats, isLoading: statsLoading, error: statsError } = useQuery({
    queryKey: ['assessment-stats'],
    queryFn: () => assessmentApi.getAssessmentStatistics(),
    refetchInterval: 30000,
  });

  const { data: recentSessions, isLoading: sessionsLoading, error: sessionsError } = useQuery({
    queryKey: ['recent-sessions', searchTerm, filterStatus],
    queryFn: () => assessmentApi.getRecentSessions({ 
      search: searchTerm, 
      status: filterStatus,
      limit: 50 
    }),
    refetchInterval: 15000,
  });

  const { data: systemHealth, isLoading: healthLoading } = useQuery({
    queryKey: ['assessment-health'],
    queryFn: () => assessmentApi.getHealthCheck(),
    refetchInterval: 10000,
  });

  const refreshData = () => {
    queryClient.invalidateQueries({ queryKey: ['assessment-stats'] });
    queryClient.invalidateQueries({ queryKey: ['recent-sessions'] });
    queryClient.invalidateQueries({ queryKey: ['assessment-health'] });
  };

  const getHealthStatusIcon = (status: string) => {
    switch (status?.toLowerCase()) {
      case 'healthy':
        return <CheckCircle className="h-5 w-5 text-green-600" />;
      case 'warning':
        return <AlertCircle className="h-5 w-5 text-yellow-600" />;
      case 'error':
        return <AlertCircle className="h-5 w-5 text-red-600" />;
      default:
        return <Clock className="h-5 w-5 text-gray-600" />;
    }
  };

  if (statsLoading || sessionsLoading || healthLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <RefreshCw className="h-8 w-8 animate-spin mx-auto mb-4 text-blue-600" />
          <p className="text-gray-600">Loading assessment data...</p>
        </div>
      </div>
    );
  }

  if (statsError || sessionsError) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <AlertCircle className="h-8 w-8 mx-auto mb-4 text-red-600" />
          <p className="text-red-600 mb-4">Error loading assessment data</p>
          <Button onClick={refreshData} variant="outline">
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
          <h1 className="text-2xl font-bold text-gray-900">Assessment Management</h1>
          <p className="text-gray-600 mt-1">
            Monitor assessment sessions, manage questions, and view statistics
          </p>
        </div>
        <div className="flex items-center space-x-3">
          <Button onClick={refreshData} variant="outline" size="sm">
            <RefreshCw className="h-4 w-4 mr-2" />
            Refresh
          </Button>
          <Button variant="default" size="sm">
            <Download className="h-4 w-4 mr-2" />
            Export Data
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Total Sessions</p>
                <p className="text-2xl font-bold text-gray-900">
                  {assessmentStats?.totalSessions?.toLocaleString() || '0'}
                </p>
              </div>
              <div className="p-3 bg-blue-100 rounded-lg">
                <FileText className="h-6 w-6 text-blue-600" />
              </div>
            </div>
            <div className="mt-4 flex items-center text-sm">
              <span className="text-green-600 font-medium">
                +{(assessmentStats as any)?.newSessionsToday || 0}
              </span>
              <span className="text-gray-600 ml-1">today</span>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Completed</p>
                <p className="text-2xl font-bold text-gray-900">
                  {assessmentStats?.completedSessions?.toLocaleString() || '0'}
                </p>
              </div>
              <div className="p-3 bg-green-100 rounded-lg">
                <CheckCircle className="h-6 w-6 text-green-600" />
              </div>
            </div>
            <div className="mt-4 flex items-center text-sm">
              <span className="text-gray-600">
                {assessmentStats?.completionRate ? `${(assessmentStats.completionRate * 100).toFixed(1)}%` : '0%'} completion rate
              </span>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Active Sessions</p>
                <p className="text-2xl font-bold text-gray-900">
                  {assessmentStats?.activeSessions?.toLocaleString() || '0'}
                </p>
              </div>
              <div className="p-3 bg-yellow-100 rounded-lg">
                <Users className="h-6 w-6 text-yellow-600" />
              </div>
            </div>
            <div className="mt-4 flex items-center text-sm">
              <span className="text-gray-600">currently in progress</span>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">System Health</p>
                <p className="text-2xl font-bold text-gray-900">
                  {systemHealth?.status || 'Unknown'}
                </p>
              </div>
              <div className="p-3 bg-gray-100 rounded-lg">
                {getHealthStatusIcon(systemHealth?.status)}
              </div>
            </div>
            <div className="mt-4 flex items-center text-sm">
              <span className="text-gray-600">
                {systemHealth?.uptime ? `${Math.floor(systemHealth.uptime / 3600)}h uptime` : 'Status unknown'}
              </span>
            </div>
          </CardContent>
        </Card>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-6">
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="sessions">Sessions</TabsTrigger>
          <TabsTrigger value="questions">Questions</TabsTrigger>
          <TabsTrigger value="configuration">Configuration</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-6">
          <AssessmentStatistics data={assessmentStats} />
        </TabsContent>

        <TabsContent value="sessions" className="space-y-6">
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-4">
              <div className="relative">
                <Input
                  placeholder="Search sessions..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-10 w-80"
                />
                <div className="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
                  <Users className="h-4 w-4 text-gray-400" />
                </div>
              </div>
              <select
                value={filterStatus}
                onChange={(e) => setFilterStatus(e.target.value)}
                className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                <option value="all">All Sessions</option>
                <option value="completed">Completed</option>
                <option value="in_progress">In Progress</option>
                <option value="abandoned">Abandoned</option>
                <option value="paused">Paused</option>
              </select>
            </div>
          </div>

          <AssessmentSessionList 
            sessions={recentSessions?.sessions || []}
            searchTerm={searchTerm}
            filterStatus={filterStatus}
          />
        </TabsContent>

        <TabsContent value="questions" className="space-y-6">
          <QuestionManagement />
        </TabsContent>

        <TabsContent value="configuration" className="space-y-6">
          <AssessmentConfiguration />
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default AssessmentPage;
