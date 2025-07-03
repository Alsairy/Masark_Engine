import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { 
  FileText, 
  Plus, 
  BarChart3, 
  Download,
  RefreshCw,
  TrendingUp,
  Users,
  Calendar
} from 'lucide-react';
import { Button } from '../components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card';
import { Badge } from '../components/ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../components/ui/tabs';
import ReportGenerator from '../components/reports/ReportGenerator';
import ReportList from '../components/reports/ReportList';
import ReportViewer from '../components/reports/ReportViewer';
import { reportService, Report } from '../services/reportService';

const ReportsPage: React.FC = () => {
  const [selectedReport, setSelectedReport] = useState<Report | null>(null);
  const [activeTab, setActiveTab] = useState('overview');

  const { data: statsData, isLoading: statsLoading, refetch: refetchStats } = useQuery({
    queryKey: ['report-stats'],
    queryFn: () => reportService.getReportStats(),
    refetchInterval: 30000,
  });

  const stats = statsData?.statistics || {
    total_reports: 0,
    total_size_mb: 0,
    reports_last_7_days: 0,
    average_report_size_kb: 0
  };

  const handleReportGenerated = () => {
    refetchStats();
    setActiveTab('list');
  };

  const handleReportSelect = (report: Report) => {
    setSelectedReport(report);
    setActiveTab('viewer');
  };

  const formatSize = (sizeInMb: number) => {
    if (sizeInMb < 1) {
      return `${(sizeInMb * 1024).toFixed(1)} KB`;
    }
    return `${sizeInMb.toFixed(1)} MB`;
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Report Management</h1>
          <p className="text-gray-600 mt-1">
            Generate, manage, and view assessment reports
          </p>
        </div>
        
        <div className="flex items-center space-x-2">
          <Button variant="outline" size="sm" onClick={() => refetchStats()}>
            <RefreshCw className="h-4 w-4 mr-2" />
            Refresh
          </Button>
          
          <Button onClick={() => setActiveTab('generate')}>
            <Plus className="h-4 w-4 mr-2" />
            Generate Report
          </Button>
        </div>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-6">
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="generate">Generate</TabsTrigger>
          <TabsTrigger value="list">Reports</TabsTrigger>
          <TabsTrigger value="viewer">Viewer</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            <Card>
              <CardContent className="p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-gray-600">Total Reports</p>
                    <p className="text-2xl font-bold text-blue-600">
                      {statsLoading ? '...' : stats.total_reports}
                    </p>
                    <p className="text-xs text-gray-500 mt-1">across all sessions</p>
                  </div>
                  <FileText className="h-8 w-8 text-blue-600" />
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-gray-600">Storage Used</p>
                    <p className="text-2xl font-bold text-green-600">
                      {statsLoading ? '...' : formatSize(stats.total_size_mb)}
                    </p>
                    <p className="text-xs text-gray-500 mt-1">total file size</p>
                  </div>
                  <BarChart3 className="h-8 w-8 text-green-600" />
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-gray-600">Recent Activity</p>
                    <p className="text-2xl font-bold text-purple-600">
                      {statsLoading ? '...' : stats.reports_last_7_days}
                    </p>
                    <p className="text-xs text-gray-500 mt-1">reports last 7 days</p>
                  </div>
                  <TrendingUp className="h-8 w-8 text-purple-600" />
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardContent className="p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-medium text-gray-600">Avg. Size</p>
                    <p className="text-2xl font-bold text-orange-600">
                      {statsLoading ? '...' : `${stats.average_report_size_kb.toFixed(1)} KB`}
                    </p>
                    <p className="text-xs text-gray-500 mt-1">per report</p>
                  </div>
                  <Download className="h-8 w-8 text-orange-600" />
                </div>
              </CardContent>
            </Card>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center space-x-2">
                  <BarChart3 className="h-5 w-5" />
                  <span>Report Statistics</span>
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">Total Reports Generated</span>
                    <Badge variant="outline">{stats.total_reports}</Badge>
                  </div>
                  
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">Storage Utilization</span>
                    <Badge variant="outline">{formatSize(stats.total_size_mb)}</Badge>
                  </div>
                  
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">Average Report Size</span>
                    <Badge variant="outline">{stats.average_report_size_kb.toFixed(1)} KB</Badge>
                  </div>
                  
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">Recent Activity (7 days)</span>
                    <Badge className="bg-green-100 text-green-800">
                      {stats.reports_last_7_days} reports
                    </Badge>
                  </div>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle className="flex items-center space-x-2">
                  <Users className="h-5 w-5" />
                  <span>System Status</span>
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">Report Generation</span>
                    <Badge className="bg-green-100 text-green-800">Active</Badge>
                  </div>
                  
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">PDF Engine</span>
                    <Badge className="bg-green-100 text-green-800">Operational</Badge>
                  </div>
                  
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">File Storage</span>
                    <Badge className="bg-green-100 text-green-800">Available</Badge>
                  </div>
                  
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">Multi-language Support</span>
                    <Badge className="bg-blue-100 text-blue-800">EN/AR</Badge>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="generate" className="space-y-6">
          <ReportGenerator onReportGenerated={handleReportGenerated} />
        </TabsContent>

        <TabsContent value="list" className="space-y-6">
          <ReportList onReportSelect={handleReportSelect} />
        </TabsContent>

        <TabsContent value="viewer" className="space-y-6">
          {selectedReport ? (
            <div className="space-y-6">
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center space-x-2">
                    <FileText className="h-5 w-5" />
                    <span>Report: {selectedReport.filename}</span>
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                    <div className="flex items-center space-x-2">
                      <Users className="h-4 w-4 text-gray-500" />
                      <span className="text-sm text-gray-600">Student:</span>
                      <span className="font-medium">{selectedReport.student_name}</span>
                    </div>
                    
                    <div className="flex items-center space-x-2">
                      <Badge>{selectedReport.personality_type}</Badge>
                    </div>
                    
                    <div className="flex items-center space-x-2">
                      <Calendar className="h-4 w-4 text-gray-500" />
                      <span className="text-sm">
                        {new Date(selectedReport.generated_at).toLocaleDateString()}
                      </span>
                    </div>
                    
                    <div className="flex items-center space-x-2">
                      <Badge variant="outline">
                        {selectedReport.language === 'en' ? 'English' : 'العربية'}
                      </Badge>
                    </div>
                  </div>
                </CardContent>
              </Card>
              
              <ReportViewer />
            </div>
          ) : (
            <Card>
              <CardContent className="p-8 text-center">
                <FileText className="h-12 w-12 mx-auto mb-4 text-gray-400" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">No report selected</h3>
                <p className="text-gray-600">
                  Select a report from the Reports tab to view its details.
                </p>
                <Button 
                  onClick={() => setActiveTab('list')} 
                  className="mt-4"
                >
                  Browse Reports
                </Button>
              </CardContent>
            </Card>
          )}
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default ReportsPage;
