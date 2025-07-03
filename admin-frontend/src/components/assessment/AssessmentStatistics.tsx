import React from 'react';
import { 
  BarChart3, 
  TrendingUp, 
  Users, 
  Clock, 
  Globe,
  Monitor,
  Star,
  Target
} from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';

interface AssessmentStatsData {
  totalSessions: number;
  completedSessions: number;
  activeSessions: number;
  averageCompletionTime: number;
  completionRate: number;
  popularPersonalityTypes: Array<{
    type: string;
    count: number;
    percentage: number;
  }>;
  sessionsByLanguage: Array<{
    language: string;
    count: number;
  }>;
  sessionsByDeploymentMode: Array<{
    mode: string;
    count: number;
  }>;
  dailyStats?: Array<{
    date: string;
    sessions: number;
    completions: number;
  }>;
  averageRating?: number;
  topCareerClusters?: Array<{
    cluster: string;
    count: number;
  }>;
}

interface AssessmentStatisticsProps {
  data: AssessmentStatsData | undefined;
}

const AssessmentStatistics: React.FC<AssessmentStatisticsProps> = ({ data }) => {
  if (!data) {
    return (
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {[...Array(6)].map((_, i) => (
          <Card key={i} className="animate-pulse">
            <CardContent className="p-6">
              <div className="h-4 bg-gray-200 rounded w-3/4 mb-2"></div>
              <div className="h-8 bg-gray-200 rounded w-1/2 mb-4"></div>
              <div className="h-3 bg-gray-200 rounded w-full"></div>
            </CardContent>
          </Card>
        ))}
      </div>
    );
  }

  const formatTime = (minutes: number) => {
    if (minutes < 60) {
      return `${Math.round(minutes)}m`;
    } else {
      const hours = Math.floor(minutes / 60);
      const mins = Math.round(minutes % 60);
      return `${hours}h ${mins}m`;
    }
  };

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Completion Rate</p>
                <p className="text-2xl font-bold text-gray-900">
                  {data.completionRate.toFixed(1)}%
                </p>
              </div>
              <div className="p-3 bg-green-100 rounded-lg">
                <Target className="h-6 w-6 text-green-600" />
              </div>
            </div>
            <div className="mt-4">
              <div className="w-full bg-gray-200 rounded-full h-2">
                <div 
                  className="bg-green-600 h-2 rounded-full"
                  style={{ width: `${data.completionRate}%` }}
                />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Avg. Completion Time</p>
                <p className="text-2xl font-bold text-gray-900">
                  {formatTime(data.averageCompletionTime)}
                </p>
              </div>
              <div className="p-3 bg-blue-100 rounded-lg">
                <Clock className="h-6 w-6 text-blue-600" />
              </div>
            </div>
            <div className="mt-4 text-sm text-gray-600">
              Based on completed sessions
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Active Sessions</p>
                <p className="text-2xl font-bold text-gray-900">
                  {data.activeSessions.toLocaleString()}
                </p>
              </div>
              <div className="p-3 bg-yellow-100 rounded-lg">
                <Users className="h-6 w-6 text-yellow-600" />
              </div>
            </div>
            <div className="mt-4 text-sm text-gray-600">
              Currently in progress
            </div>
          </CardContent>
        </Card>

        {data.averageRating && (
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600">Average Rating</p>
                  <p className="text-2xl font-bold text-gray-900">
                    {data.averageRating.toFixed(1)}/5
                  </p>
                </div>
                <div className="p-3 bg-purple-100 rounded-lg">
                  <Star className="h-6 w-6 text-purple-600" />
                </div>
              </div>
              <div className="mt-4 flex items-center">
                {[...Array(5)].map((_, i) => (
                  <Star
                    key={i}
                    className={`h-4 w-4 ${
                      i < Math.floor(data.averageRating!)
                        ? 'text-yellow-400 fill-current'
                        : 'text-gray-300'
                    }`}
                  />
                ))}
              </div>
            </CardContent>
          </Card>
        )}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <BarChart3 className="h-5 w-5" />
              <span>Popular Personality Types</span>
            </CardTitle>
            <CardDescription>
              Most common assessment results
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {(data.popularPersonalityTypes || []).slice(0, 8).map((type, index) => (
                <div key={type.type} className="flex items-center justify-between">
                  <div className="flex items-center space-x-3">
                    <div className="flex items-center justify-center w-8 h-8 bg-purple-100 text-purple-600 rounded-lg font-semibold text-sm">
                      {index + 1}
                    </div>
                    <div>
                      <p className="font-medium text-gray-900">{type.type}</p>
                      <p className="text-sm text-gray-600">{type.count} assessments</p>
                    </div>
                  </div>
                  <div className="text-right">
                    <p className="font-medium text-gray-900">{type.percentage.toFixed(1)}%</p>
                    <div className="w-20 bg-gray-200 rounded-full h-2 mt-1">
                      <div 
                        className="bg-purple-600 h-2 rounded-full"
                        style={{ width: `${type.percentage}%` }}
                      />
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <Globe className="h-5 w-5" />
              <span>Language Distribution</span>
            </CardTitle>
            <CardDescription>
              Assessment sessions by language preference
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {(data.sessionsByLanguage || []).map((lang) => {
                const percentage = (lang.count / data.totalSessions) * 100;
                return (
                  <div key={lang.language} className="flex items-center justify-between">
                    <div className="flex items-center space-x-3">
                      <Badge variant="outline" className="uppercase">
                        {lang.language}
                      </Badge>
                      <span className="text-gray-900">{lang.count} sessions</span>
                    </div>
                    <div className="text-right">
                      <p className="font-medium text-gray-900">{percentage.toFixed(1)}%</p>
                      <div className="w-16 bg-gray-200 rounded-full h-2 mt-1">
                        <div 
                          className="bg-blue-600 h-2 rounded-full"
                          style={{ width: `${percentage}%` }}
                        />
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <Monitor className="h-5 w-5" />
              <span>Deployment Modes</span>
            </CardTitle>
            <CardDescription>
              Sessions by deployment configuration
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {(data.sessionsByDeploymentMode || []).map((mode) => {
                const percentage = (mode.count / data.totalSessions) * 100;
                return (
                  <div key={mode.mode} className="flex items-center justify-between">
                    <div className="flex items-center space-x-3">
                      <Badge 
                        variant="outline" 
                        className={mode.mode === 'MAWHIBA' ? 'bg-green-50 text-green-700 border-green-200' : 'bg-blue-50 text-blue-700 border-blue-200'}
                      >
                        {mode.mode}
                      </Badge>
                      <span className="text-gray-900">{mode.count} sessions</span>
                    </div>
                    <div className="text-right">
                      <p className="font-medium text-gray-900">{percentage.toFixed(1)}%</p>
                      <div className="w-16 bg-gray-200 rounded-full h-2 mt-1">
                        <div 
                          className={`h-2 rounded-full ${
                            mode.mode === 'MAWHIBA' ? 'bg-green-600' : 'bg-blue-600'
                          }`}
                          style={{ width: `${percentage}%` }}
                        />
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </CardContent>
        </Card>

        {data.topCareerClusters && (
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center space-x-2">
                <TrendingUp className="h-5 w-5" />
                <span>Top Career Clusters</span>
              </CardTitle>
              <CardDescription>
                Most popular career interest areas
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {(data.topCareerClusters || []).slice(0, 6).map((cluster, index) => {
                  const percentage = (cluster.count / data.completedSessions) * 100;
                  return (
                    <div key={cluster.cluster} className="flex items-center justify-between">
                      <div className="flex items-center space-x-3">
                        <div className="flex items-center justify-center w-6 h-6 bg-orange-100 text-orange-600 rounded font-semibold text-xs">
                          {index + 1}
                        </div>
                        <span className="text-gray-900 text-sm">{cluster.cluster}</span>
                      </div>
                      <div className="text-right">
                        <p className="font-medium text-gray-900 text-sm">{cluster.count}</p>
                        <div className="w-12 bg-gray-200 rounded-full h-1.5 mt-1">
                          <div 
                            className="bg-orange-600 h-1.5 rounded-full"
                            style={{ width: `${Math.min(percentage, 100)}%` }}
                          />
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            </CardContent>
          </Card>
        )}
      </div>

      {data.dailyStats && data.dailyStats.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <BarChart3 className="h-5 w-5" />
              <span>Daily Activity</span>
            </CardTitle>
            <CardDescription>
              Assessment sessions and completions over time
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="space-y-2">
              {(data.dailyStats || []).slice(-7).map((day) => (
                <div key={day.date} className="flex items-center justify-between py-2">
                  <div className="text-sm text-gray-600">
                    {new Date(day.date).toLocaleDateString()}
                  </div>
                  <div className="flex items-center space-x-4">
                    <div className="text-sm">
                      <span className="text-gray-900 font-medium">{day.sessions}</span>
                      <span className="text-gray-600 ml-1">started</span>
                    </div>
                    <div className="text-sm">
                      <span className="text-green-600 font-medium">{day.completions}</span>
                      <span className="text-gray-600 ml-1">completed</span>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  );
};

export default AssessmentStatistics;
