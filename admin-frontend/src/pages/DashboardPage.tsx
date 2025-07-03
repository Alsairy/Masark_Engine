import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { 
  Users, 
  Activity, 
  FileText, 
  BarChart3,
  TrendingUp,
  Clock,
  CheckCircle,
  AlertCircle
} from 'lucide-react';
import { useLocalization } from '../contexts/LocalizationContext';

const DashboardPage: React.FC = () => {
  const { t, isRTL } = useLocalization();

  const stats = [
    {
      title: t('total_users', 'admin'),
      value: '0',
      icon: Users,
      color: 'text-blue-600 dark:text-blue-400',
      bgColor: 'bg-blue-50 dark:bg-blue-900/20',
      change: '+0%',
      changeType: 'neutral'
    },
    {
      title: t('active_sessions', 'admin'),
      value: '0',
      icon: Activity,
      color: 'text-green-600 dark:text-green-400',
      bgColor: 'bg-green-50 dark:bg-green-900/20',
      change: '+0%',
      changeType: 'neutral'
    },
    {
      title: t('assessments', 'admin'),
      value: '0',
      icon: FileText,
      color: 'text-purple-600 dark:text-purple-400',
      bgColor: 'bg-purple-50 dark:bg-purple-900/20',
      change: '+0%',
      changeType: 'neutral'
    },
    {
      title: t('reports', 'admin'),
      value: '0',
      icon: BarChart3,
      color: 'text-orange-600 dark:text-orange-400',
      bgColor: 'bg-orange-50 dark:bg-orange-900/20',
      change: '+0%',
      changeType: 'neutral'
    }
  ];

  const recentActivity = [
    {
      type: 'user_registered',
      message: t('new_user_registered', 'admin'),
      time: '2 minutes ago',
      status: 'success'
    },
    {
      type: 'assessment_completed',
      message: t('assessment_completed', 'admin'),
      time: '5 minutes ago',
      status: 'info'
    },
    {
      type: 'report_generated',
      message: t('report_generated', 'admin'),
      time: '10 minutes ago',
      status: 'success'
    }
  ];

  return (
    <div className="space-y-6">
      <div className={`flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 ${isRTL ? 'sm:flex-row-reverse' : ''}`}>
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
            {t('dashboard', 'admin')}
          </h1>
          <p className="text-gray-600 dark:text-gray-400 mt-1">
            {t('dashboard_overview', 'admin')}
          </p>
        </div>
        <div className={`flex items-center space-x-3 ${isRTL ? 'flex-row-reverse space-x-reverse' : ''}`}>
          <Badge variant="outline" className="flex items-center space-x-1">
            <CheckCircle className="h-3 w-3" />
            <span>{t('system_healthy', 'admin')}</span>
          </Badge>
          <Button variant="outline" size="sm">
            <TrendingUp className="h-4 w-4 mr-2" />
            {t('view_analytics', 'admin')}
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 lg:gap-6">
        {stats.map((stat, index) => {
          const Icon = stat.icon;
          return (
            <Card key={index} className="hover:shadow-md transition-shadow duration-200">
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium text-gray-600 dark:text-gray-400">
                  {stat.title}
                </CardTitle>
                <div className={`p-2 rounded-md ${stat.bgColor}`}>
                  <Icon className={`h-4 w-4 ${stat.color}`} />
                </div>
              </CardHeader>
              <CardContent>
                <div className="flex items-baseline justify-between">
                  <div className={`text-2xl font-bold ${stat.color}`}>
                    {stat.value}
                  </div>
                  <Badge 
                    variant={stat.changeType === 'positive' ? 'default' : 'secondary'}
                    className="text-xs"
                  >
                    {stat.change}
                  </Badge>
                </div>
              </CardContent>
            </Card>
          );
        })}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <Card className="lg:col-span-2">
          <CardHeader>
            <CardTitle className={`flex items-center space-x-2 ${isRTL ? 'flex-row-reverse space-x-reverse' : ''}`}>
              <Activity className="h-5 w-5" />
              <span>{t('recent_activity', 'admin')}</span>
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              {recentActivity.map((activity, index) => (
                <div key={index} className={`flex items-center space-x-3 p-3 rounded-lg bg-gray-50 dark:bg-gray-800 ${isRTL ? 'flex-row-reverse space-x-reverse' : ''}`}>
                  <div className={`p-1 rounded-full ${
                    activity.status === 'success' ? 'bg-green-100 dark:bg-green-900/20' :
                    activity.status === 'info' ? 'bg-blue-100 dark:bg-blue-900/20' :
                    'bg-gray-100 dark:bg-gray-700'
                  }`}>
                    {activity.status === 'success' ? (
                      <CheckCircle className="h-3 w-3 text-green-600 dark:text-green-400" />
                    ) : activity.status === 'info' ? (
                      <AlertCircle className="h-3 w-3 text-blue-600 dark:text-blue-400" />
                    ) : (
                      <Clock className="h-3 w-3 text-gray-600 dark:text-gray-400" />
                    )}
                  </div>
                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium text-gray-900 dark:text-white truncate">
                      {activity.message}
                    </p>
                    <p className="text-xs text-gray-500 dark:text-gray-400">
                      {activity.time}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className={`flex items-center space-x-2 ${isRTL ? 'flex-row-reverse space-x-reverse' : ''}`}>
              <BarChart3 className="h-5 w-5" />
              <span>{t('quick_stats', 'admin')}</span>
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-4">
              <div className="flex justify-between items-center">
                <span className="text-sm text-gray-600 dark:text-gray-400">
                  {t('completion_rate', 'admin')}
                </span>
                <span className="text-sm font-medium">0%</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm text-gray-600 dark:text-gray-400">
                  {t('avg_session_time', 'admin')}
                </span>
                <span className="text-sm font-medium">0 min</span>
              </div>
              <div className="flex justify-between items-center">
                <span className="text-sm text-gray-600 dark:text-gray-400">
                  {t('user_satisfaction', 'admin')}
                </span>
                <span className="text-sm font-medium">N/A</span>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  );
};

export default DashboardPage;
