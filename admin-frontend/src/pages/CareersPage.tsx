import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { 
  Target, 
  Filter, 
  RefreshCw,
  Download,
  Settings,
  TrendingUp
} from 'lucide-react';
import { Button } from '../components/ui/button';
import { Card, CardContent } from '../components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../components/ui/tabs';
import CareerList from '../components/careers/CareerList';
import CareerSearch from '../components/careers/CareerSearch';
import ClusterManagement from '../components/careers/ClusterManagement';
import { careerService, Career, CareerCluster } from '../services/careerService';

const CareersPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState('overview');
  const [selectedCareer, setSelectedCareer] = useState<Career | null>(null);
  const [showCareerDetails, setShowCareerDetails] = useState(false);

  const { data: careerStats, isLoading: statsLoading, refetch: refetchStats } = useQuery({
    queryKey: ['career-stats'],
    queryFn: () => careerService.getCareerStats(),
    refetchInterval: 30000,
  });

  const { isLoading: clustersLoading } = useQuery({
    queryKey: ['career-clusters'],
    queryFn: () => careerService.getAllClusters(),
    refetchInterval: 30000,
  });

  const { data: sampleMatches, isLoading: matchesLoading } = useQuery({
    queryKey: ['sample-career-matches'],
    queryFn: () => careerService.getCareerMatches({
      personalityType: 'INTJ',
      limit: 6
    }),
  });

  const refreshAllData = () => {
    refetchStats();
  };

  const handleCareerSelect = (career: Career) => {
    setSelectedCareer(career);
    setShowCareerDetails(true);
  };

  const handleCreateCareer = () => {
    console.log('Create new career');
  };

  const handleEditCareer = (career: Career) => {
    console.log('Edit career:', career);
  };

  const handleDeleteCareer = (career: Career) => {
    console.log('Delete career:', career);
  };

  const handleCreateCluster = () => {
    console.log('Create new cluster');
  };

  const handleEditCluster = (cluster: CareerCluster) => {
    console.log('Edit cluster:', cluster);
  };

  const handleDeleteCluster = (cluster: CareerCluster) => {
    console.log('Delete cluster:', cluster);
  };

  const handleViewClusterCareers = (cluster: CareerCluster) => {
    console.log('View careers for cluster:', cluster);
  };

  if (statsLoading || clustersLoading || matchesLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <RefreshCw className="h-8 w-8 animate-spin mx-auto mb-4 text-blue-600" />
          <p className="text-gray-600">Loading career data...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Career Management</h1>
          <p className="text-gray-600 mt-1">
            Manage careers, clusters, and personality-career matching system
          </p>
        </div>
        <div className="flex items-center space-x-3">
          <Button onClick={refreshAllData} variant="outline" size="sm">
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
                <p className="text-sm font-medium text-gray-600">Total Careers</p>
                <p className="text-2xl font-bold text-gray-900">
                  {careerStats?.statistics?.total_careers?.toLocaleString() || '0'}
                </p>
              </div>
              <div className="p-3 bg-blue-100 rounded-lg">
                <Target className="h-6 w-6 text-blue-600" />
              </div>
            </div>
            <div className="mt-4 flex items-center text-sm">
              <span className="text-gray-600">across all clusters</span>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Career Clusters</p>
                <p className="text-2xl font-bold text-gray-900">
                  {careerStats?.statistics?.total_clusters?.toLocaleString() || '0'}
                </p>
              </div>
              <div className="p-3 bg-green-100 rounded-lg">
                <Filter className="h-6 w-6 text-green-600" />
              </div>
            </div>
            <div className="mt-4 flex items-center text-sm">
              <span className="text-gray-600">active clusters</span>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Matching Engine</p>
                <p className="text-2xl font-bold text-gray-900">Active</p>
              </div>
              <div className="p-3 bg-yellow-100 rounded-lg">
                <Settings className="h-6 w-6 text-yellow-600" />
              </div>
            </div>
            <div className="mt-4 flex items-center text-sm">
              <span className="text-green-600 font-medium">Operational</span>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Cache Performance</p>
                <p className="text-2xl font-bold text-gray-900">98.5%</p>
              </div>
              <div className="p-3 bg-purple-100 rounded-lg">
                <TrendingUp className="h-6 w-6 text-purple-600" />
              </div>
            </div>
            <div className="mt-4 flex items-center text-sm">
              <span className="text-green-600 font-medium">+2.1%</span>
              <span className="text-gray-600 ml-1">from last week</span>
            </div>
          </CardContent>
        </Card>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-6">
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="overview">Overview</TabsTrigger>
          <TabsTrigger value="search">Search & Match</TabsTrigger>
          <TabsTrigger value="careers">Career Management</TabsTrigger>
          <TabsTrigger value="clusters">Cluster Management</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-6">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <Card>
              <CardContent className="p-6">
                <h3 className="text-lg font-semibold text-gray-900 mb-4">Recent Career Matches</h3>
                {sampleMatches?.matches && sampleMatches.matches.length > 0 ? (
                  <div className="space-y-3">
                    {sampleMatches.matches.slice(0, 5).map((match: any) => (
                      <div key={match.career_id} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg">
                        <div className="flex-1 min-w-0">
                          <p className="text-sm font-medium text-gray-900 truncate">
                            {match.name}
                          </p>
                          <p className="text-xs text-gray-600">
                            {match.cluster.name}
                          </p>
                        </div>
                        <div className="ml-4 flex-shrink-0">
                          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
                            {match.match_score}%
                          </span>
                        </div>
                      </div>
                    ))}
                  </div>
                ) : (
                  <p className="text-gray-600 text-sm">No recent matches available</p>
                )}
              </CardContent>
            </Card>

            <Card>
              <CardContent className="p-6">
                <h3 className="text-lg font-semibold text-gray-900 mb-4">System Status</h3>
                <div className="space-y-3">
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">Career Database</span>
                    <span className="text-sm font-medium text-green-600">Online</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">Matching Engine</span>
                    <span className="text-sm font-medium text-green-600">Active</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">Cache System</span>
                    <span className="text-sm font-medium text-green-600">Operational</span>
                  </div>
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-gray-600">API Response Time</span>
                    <span className="text-sm font-medium text-blue-600">~150ms</span>
                  </div>
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        <TabsContent value="search" className="space-y-6">
          <CareerSearch onCareerSelect={handleCareerSelect} />
        </TabsContent>

        <TabsContent value="careers" className="space-y-6">
          <CareerList 
            careers={sampleMatches?.matches || []}
            loading={matchesLoading}
            onView={handleCareerSelect}
            onEdit={handleEditCareer}
            onDelete={handleDeleteCareer}
            onCreate={handleCreateCareer}
          />
        </TabsContent>

        <TabsContent value="clusters" className="space-y-6">
          <ClusterManagement 
            onCreateCluster={handleCreateCluster}
            onEditCluster={handleEditCluster}
            onDeleteCluster={handleDeleteCluster}
            onViewCareers={handleViewClusterCareers}
          />
        </TabsContent>
      </Tabs>

      {showCareerDetails && selectedCareer && (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
          <div className="bg-white rounded-lg shadow-xl w-full max-w-2xl max-h-[90vh] overflow-hidden">
            <div className="flex items-center justify-between p-6 border-b border-gray-200">
              <h2 className="text-xl font-semibold text-gray-900">Career Details</h2>
              <button
                onClick={() => {
                  setShowCareerDetails(false);
                  setSelectedCareer(null);
                }}
                className="text-gray-400 hover:text-gray-600"
              >
                ×
              </button>
            </div>
            <div className="p-6">
              <div className="space-y-4">
                <div>
                  <h3 className="text-lg font-semibold text-gray-900">{selectedCareer.name}</h3>
                  <p className="text-gray-600">{selectedCareer.cluster.name}</p>
                </div>
                <div>
                  <h4 className="font-medium text-gray-900 mb-2">Description</h4>
                  <p className="text-gray-600">{selectedCareer.description}</p>
                </div>
                {selectedCareer.ssoc_code && (
                  <div>
                    <h4 className="font-medium text-gray-900 mb-2">SSOC Code</h4>
                    <p className="text-gray-600">{selectedCareer.ssoc_code}</p>
                  </div>
                )}
                {selectedCareer.match_score && (
                  <div>
                    <h4 className="font-medium text-gray-900 mb-2">Match Score</h4>
                    <p className="text-gray-600">{selectedCareer.match_score}%</p>
                  </div>
                )}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default CareersPage;
