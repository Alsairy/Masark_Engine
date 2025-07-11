import React, { useState, useEffect } from 'react';
import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, PieChart, Pie, Cell, ResponsiveContainer } from 'recharts';
import { GraduationCap, TrendingUp, Users, Award, Download, Calendar } from 'lucide-react';
import { reportService } from '../../services/reportService';

interface MawhibaStats {
  totalMawhibaRecommendations: number;
  totalMoeRecommendations: number;
  mawhibaPathwayDistribution: Array<{
    pathwayName: string;
    pathwayNameAr: string;
    count: number;
    percentage: number;
  }>;
  moePathwayDistribution: Array<{
    pathwayName: string;
    pathwayNameAr: string;
    count: number;
    percentage: number;
  }>;
  comparisonMetrics: {
    averageMawhibaScore: number;
    averageMoeScore: number;
    mawhibaEngagementRate: number;
    moeEngagementRate: number;
  };
  monthlyTrends: Array<{
    month: string;
    mawhibaRecommendations: number;
    moeRecommendations: number;
  }>;
}

interface MawhibaReportSectionProps {
  dateRange?: {
    startDate: string;
    endDate: string;
  };
  deploymentMode?: 'STANDARD' | 'MAWHIBA';
}

const COLORS = ['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#8B5CF6', '#06B6D4', '#84CC16', '#F97316', '#EC4899'];

const MawhibaReportSection: React.FC<MawhibaReportSectionProps> = ({ 
  dateRange, 
  deploymentMode = 'MAWHIBA' 
}) => {
  const [stats, setStats] = useState<MawhibaStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selectedView, setSelectedView] = useState<'overview' | 'pathways' | 'trends' | 'comparison'>('overview');
  const [language, setLanguage] = useState<'en' | 'ar'>('en');

  useEffect(() => {
    fetchMawhibaStats();
  }, [dateRange, deploymentMode]);

  const fetchMawhibaStats = async () => {
    try {
      setLoading(true);
      setError(null);
      
      const response = await reportService.getMawhibaAnalytics({
        startDate: dateRange?.startDate,
        endDate: dateRange?.endDate,
        deploymentMode
      });
      
      setStats(response.data);
    } catch (err: unknown) {
      console.error('Failed to fetch Mawhiba statistics:', err);
      const errorMessage = (err as any)?.response?.data?.message || 'Failed to load Mawhiba analytics';
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleExportReport = async () => {
    try {
      const response = await reportService.exportMawhibaReport({
        startDate: dateRange?.startDate,
        endDate: dateRange?.endDate,
        deploymentMode,
        format: 'pdf'
      });
      
      const blob = new Blob([response.data], { type: 'application/pdf' });
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `mawhiba-report-${new Date().toISOString().split('T')[0]}.pdf`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    } catch (err) {
      console.error('Failed to export report:', err);
    }
  };

  if (loading) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="flex items-center space-x-3 mb-4">
          <GraduationCap className="h-6 w-6 text-blue-600 animate-pulse" />
          <h3 className="text-lg font-medium text-gray-900">Loading Mawhiba Analytics...</h3>
        </div>
        <div className="animate-pulse space-y-4">
          <div className="h-4 bg-gray-200 rounded w-3/4"></div>
          <div className="h-4 bg-gray-200 rounded w-1/2"></div>
          <div className="h-32 bg-gray-200 rounded"></div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="bg-white rounded-lg border border-red-200 p-6">
        <div className="flex items-center space-x-3 mb-4">
          <GraduationCap className="h-6 w-6 text-red-600" />
          <h3 className="text-lg font-medium text-red-900">Mawhiba Analytics Error</h3>
        </div>
        <p className="text-red-700">{error}</p>
        <button
          onClick={fetchMawhibaStats}
          className="mt-4 px-4 py-2 bg-red-600 text-white rounded-lg hover:bg-red-700 transition-colors"
        >
          Retry
        </button>
      </div>
    );
  }

  if (!stats) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="flex items-center space-x-3 mb-4">
          <GraduationCap className="h-6 w-6 text-gray-400" />
          <h3 className="text-lg font-medium text-gray-900">No Mawhiba Data Available</h3>
        </div>
        <p className="text-gray-600">No assessment data found for the selected period.</p>
      </div>
    );
  }

  const totalRecommendations = stats.totalMawhibaRecommendations + stats.totalMoeRecommendations;
  const mawhibaPercentage = totalRecommendations > 0 ? (stats.totalMawhibaRecommendations / totalRecommendations) * 100 : 0;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="flex items-center justify-between mb-6">
          <div className="flex items-center space-x-3">
            <GraduationCap className="h-8 w-8 text-blue-600" />
            <div>
              <h2 className="text-2xl font-bold text-gray-900">Mawhiba Analytics Dashboard</h2>
              <p className="text-gray-600">Specialized reporting for gifted education pathways</p>
            </div>
          </div>
          <div className="flex items-center space-x-3">
            <select
              value={language}
              onChange={(e) => setLanguage(e.target.value as 'en' | 'ar')}
              className="px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            >
              <option value="en">English</option>
              <option value="ar">العربية</option>
            </select>
            <button
              onClick={handleExportReport}
              className="flex items-center space-x-2 px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
            >
              <Download className="h-4 w-4" />
              <span>Export Report</span>
            </button>
          </div>
        </div>

        {/* Navigation Tabs */}
        <div className="flex space-x-1 bg-gray-100 rounded-lg p-1">
          {[
            { key: 'overview', label: 'Overview', icon: TrendingUp },
            { key: 'pathways', label: 'Pathway Analysis', icon: Award },
            { key: 'trends', label: 'Trends', icon: Calendar },
            { key: 'comparison', label: 'MOE vs Mawhiba', icon: Users }
          ].map(({ key, label, icon: Icon }) => (
            <button
              key={key}
              onClick={() => setSelectedView(key as any)}
              className={`flex items-center space-x-2 px-4 py-2 rounded-md transition-colors ${
                selectedView === key
                  ? 'bg-white text-blue-600 shadow-sm'
                  : 'text-gray-600 hover:text-gray-900'
              }`}
            >
              <Icon className="h-4 w-4" />
              <span>{label}</span>
            </button>
          ))}
        </div>
      </div>

      {/* Overview Section */}
      {selectedView === 'overview' && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Total Mawhiba Recommendations</p>
                <p className="text-3xl font-bold text-blue-600">{stats.totalMawhibaRecommendations.toLocaleString()}</p>
              </div>
              <GraduationCap className="h-8 w-8 text-blue-600" />
            </div>
            <div className="mt-4">
              <div className="flex items-center text-sm text-gray-600">
                <span>{mawhibaPercentage.toFixed(1)}% of total recommendations</span>
              </div>
            </div>
          </div>

          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Total MOE Recommendations</p>
                <p className="text-3xl font-bold text-green-600">{stats.totalMoeRecommendations.toLocaleString()}</p>
              </div>
              <Users className="h-8 w-8 text-green-600" />
            </div>
            <div className="mt-4">
              <div className="flex items-center text-sm text-gray-600">
                <span>{(100 - mawhibaPercentage).toFixed(1)}% of total recommendations</span>
              </div>
            </div>
          </div>

          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Avg Mawhiba Score</p>
                <p className="text-3xl font-bold text-purple-600">{stats.comparisonMetrics.averageMawhibaScore.toFixed(2)}</p>
              </div>
              <Award className="h-8 w-8 text-purple-600" />
            </div>
            <div className="mt-4">
              <div className="flex items-center text-sm text-gray-600">
                <span>Higher difficulty, specialized focus</span>
              </div>
            </div>
          </div>

          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Engagement Rate</p>
                <p className="text-3xl font-bold text-orange-600">{stats.comparisonMetrics.mawhibaEngagementRate.toFixed(1)}%</p>
              </div>
              <TrendingUp className="h-8 w-8 text-orange-600" />
            </div>
            <div className="mt-4">
              <div className="flex items-center text-sm text-gray-600">
                <span>Mawhiba pathway engagement</span>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Pathway Analysis Section */}
      {selectedView === 'pathways' && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-4">Mawhiba Pathway Distribution</h3>
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={stats.mawhibaPathwayDistribution}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={({ pathwayName, percentage }) => `${pathwayName}: ${percentage.toFixed(1)}%`}
                  outerRadius={80}
                  fill="#8884d8"
                  dataKey="count"
                >
                  {stats.mawhibaPathwayDistribution.map((_, index) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          </div>

          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-4">MOE Pathway Distribution</h3>
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={stats.moePathwayDistribution}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={({ pathwayName, percentage }) => `${pathwayName}: ${percentage.toFixed(1)}%`}
                  outerRadius={80}
                  fill="#8884d8"
                  dataKey="count"
                >
                  {stats.moePathwayDistribution.map((_, index) => (
                    <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                  ))}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </div>
      )}

      {/* Trends Section */}
      {selectedView === 'trends' && (
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h3 className="text-lg font-medium text-gray-900 mb-4">Monthly Recommendation Trends</h3>
          <ResponsiveContainer width="100%" height={400}>
            <BarChart data={stats.monthlyTrends}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="month" />
              <YAxis />
              <Tooltip />
              <Legend />
              <Bar dataKey="mawhibaRecommendations" fill="#3B82F6" name="Mawhiba Recommendations" />
              <Bar dataKey="moeRecommendations" fill="#10B981" name="MOE Recommendations" />
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}

      {/* Comparison Section */}
      {selectedView === 'comparison' && (
        <div className="space-y-6">
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <h3 className="text-lg font-medium text-gray-900 mb-6">MOE vs Mawhiba Comparison</h3>
            
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="space-y-4">
                <h4 className="font-medium text-gray-900">Recommendation Volume</h4>
                <div className="space-y-3">
                  <div>
                    <div className="flex justify-between text-sm">
                      <span>Mawhiba Pathways</span>
                      <span>{stats.totalMawhibaRecommendations}</span>
                    </div>
                    <div className="w-full bg-gray-200 rounded-full h-2">
                      <div 
                        className="bg-blue-600 h-2 rounded-full" 
                        style={{ width: `${mawhibaPercentage}%` }}
                      ></div>
                    </div>
                  </div>
                  <div>
                    <div className="flex justify-between text-sm">
                      <span>MOE Pathways</span>
                      <span>{stats.totalMoeRecommendations}</span>
                    </div>
                    <div className="w-full bg-gray-200 rounded-full h-2">
                      <div 
                        className="bg-green-600 h-2 rounded-full" 
                        style={{ width: `${100 - mawhibaPercentage}%` }}
                      ></div>
                    </div>
                  </div>
                </div>
              </div>

              <div className="space-y-4">
                <h4 className="font-medium text-gray-900">Performance Metrics</h4>
                <div className="space-y-3">
                  <div className="flex justify-between">
                    <span className="text-sm text-gray-600">Average Score (Mawhiba)</span>
                    <span className="font-medium">{stats.comparisonMetrics.averageMawhibaScore.toFixed(2)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm text-gray-600">Average Score (MOE)</span>
                    <span className="font-medium">{stats.comparisonMetrics.averageMoeScore.toFixed(2)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm text-gray-600">Engagement Rate (Mawhiba)</span>
                    <span className="font-medium">{stats.comparisonMetrics.mawhibaEngagementRate.toFixed(1)}%</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm text-gray-600">Engagement Rate (MOE)</span>
                    <span className="font-medium">{stats.comparisonMetrics.moeEngagementRate.toFixed(1)}%</span>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div className="bg-blue-50 rounded-lg border border-blue-200 p-6">
            <h4 className="font-medium text-blue-900 mb-3">Key Insights</h4>
            <ul className="space-y-2 text-sm text-blue-800">
              <li>• Mawhiba pathways represent {mawhibaPercentage.toFixed(1)}% of total recommendations, indicating specialized targeting</li>
              <li>• Higher average scores for Mawhiba reflect the advanced nature of gifted education programs</li>
              <li>• Engagement rates show student interest levels in specialized vs. general education pathways</li>
              <li>• Duration difference: Mawhiba (1-2 years) vs MOE (2-4 years) reflects intensive vs. comprehensive approaches</li>
            </ul>
          </div>
        </div>
      )}
    </div>
  );
};

export default MawhibaReportSection;
