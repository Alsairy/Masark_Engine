import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { 
  Plus, 
  Edit, 
  Trash2, 
  Users, 
  Star,
  RefreshCw,
  Filter,
  Search
} from 'lucide-react';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '../ui/dropdown-menu';
import { careerService, CareerCluster, ClusterRating } from '../../services/careerService';

interface ClusterManagementProps {
  onCreateCluster?: () => void;
  onEditCluster?: (cluster: CareerCluster) => void;
  onDeleteCluster?: (cluster: CareerCluster) => void;
  onViewCareers?: (cluster: CareerCluster) => void;
}

const ClusterManagement: React.FC<ClusterManagementProps> = ({
  onCreateCluster,
  onEditCluster,
  onDeleteCluster,
  onViewCareers
}) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [language, setLanguage] = useState('en');
  const [activeTab, setActiveTab] = useState('clusters');

  const { data: clustersData, isLoading: clustersLoading, refetch: refetchClusters } = useQuery({
    queryKey: ['career-clusters', language],
    queryFn: () => careerService.getAllClusters(language),
    refetchInterval: 30000,
  });

  const { data: ratingsData, isLoading: ratingsLoading } = useQuery({
    queryKey: ['cluster-ratings', language],
    queryFn: () => careerService.getClusterRatings(language),
  });

  const clusters = clustersData?.clusters || [];
  const ratings = ratingsData?.career_cluster_ratings || [];

  const filteredClusters = clusters.filter((cluster: CareerCluster) =>
    cluster.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    cluster.description?.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const renderClusterCard = (cluster: CareerCluster) => (
    <Card key={cluster.id} className="hover:shadow-md transition-shadow">
      <CardHeader className="pb-3">
        <div className="flex items-start justify-between">
          <div className="flex-1 min-w-0">
            <CardTitle className="text-lg font-semibold text-gray-900 truncate">
              {cluster.name}
            </CardTitle>
            <p className="text-sm text-gray-600 mt-1">
              {cluster.careers?.length || 0} careers
            </p>
          </div>
          
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="sm">
                <Edit className="h-4 w-4" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {onViewCareers && (
                <DropdownMenuItem onClick={() => onViewCareers(cluster)}>
                  <Users className="h-4 w-4 mr-2" />
                  View Careers
                </DropdownMenuItem>
              )}
              {onEditCluster && (
                <DropdownMenuItem onClick={() => onEditCluster(cluster)}>
                  <Edit className="h-4 w-4 mr-2" />
                  Edit Cluster
                </DropdownMenuItem>
              )}
              {onDeleteCluster && (
                <DropdownMenuItem 
                  onClick={() => onDeleteCluster(cluster)}
                  className="text-red-600"
                >
                  <Trash2 className="h-4 w-4 mr-2" />
                  Delete Cluster
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </CardHeader>
      
      <CardContent className="pt-0">
        {cluster.description && (
          <p className="text-sm text-gray-600 mb-4 line-clamp-3">
            {cluster.description}
          </p>
        )}
        
        <div className="flex items-center justify-between">
          <Badge variant="outline" className="text-xs">
            ID: {cluster.id}
          </Badge>
          
          <div className="flex items-center space-x-1 text-xs text-gray-500">
            <Users className="h-3 w-3" />
            <span>{cluster.careers?.length || 0} careers</span>
          </div>
        </div>
      </CardContent>
    </Card>
  );

  const renderRatingCard = (rating: ClusterRating) => (
    <Card key={rating.id} className="hover:shadow-md transition-shadow">
      <CardContent className="p-6">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center space-x-3">
            <div className="p-2 bg-blue-100 rounded-lg">
              <Star className="h-5 w-5 text-blue-600" />
            </div>
            <div>
              <h3 className="font-semibold text-gray-900">Rating {rating.value}</h3>
              <p className="text-sm text-gray-600">Level {rating.id}</p>
            </div>
          </div>
          
          <Badge className="bg-blue-100 text-blue-800">
            {rating.value}/5
          </Badge>
        </div>
        
        <p className="text-sm text-gray-600">
          {rating.description}
        </p>
      </CardContent>
    </Card>
  );

  if (clustersLoading || ratingsLoading) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <RefreshCw className="h-8 w-8 animate-spin mx-auto mb-4 text-blue-600" />
          <p className="text-gray-600">Loading cluster data...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-gray-900">Cluster Management</h2>
          <p className="text-gray-600 mt-1">
            Manage career clusters and rating systems
          </p>
        </div>
        
        <div className="flex items-center space-x-2">
          <select
            value={language}
            onChange={(e) => setLanguage(e.target.value)}
            className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="en">English</option>
            <option value="ar">العربية</option>
          </select>
          
          <Button variant="outline" size="sm" onClick={() => refetchClusters()}>
            <RefreshCw className="h-4 w-4 mr-2" />
            Refresh
          </Button>
          
          {onCreateCluster && (
            <Button onClick={onCreateCluster}>
              <Plus className="h-4 w-4 mr-2" />
              Add Cluster
            </Button>
          )}
        </div>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-6">
        <TabsList className="grid w-full grid-cols-2">
          <TabsTrigger value="clusters">Career Clusters</TabsTrigger>
          <TabsTrigger value="ratings">Rating System</TabsTrigger>
        </TabsList>

        <TabsContent value="clusters" className="space-y-6">
          <div className="flex items-center space-x-4">
            <div className="relative flex-1 max-w-md">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
              <Input
                placeholder="Search clusters..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="pl-10"
              />
            </div>
          </div>

          {filteredClusters.length === 0 ? (
            <Card>
              <CardContent className="p-8 text-center">
                <Filter className="h-12 w-12 mx-auto mb-4 text-gray-400" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">No clusters found</h3>
                <p className="text-gray-600">
                  {searchTerm 
                    ? 'Try adjusting your search criteria.'
                    : 'No career clusters have been created yet.'}
                </p>
                {onCreateCluster && !searchTerm && (
                  <Button onClick={onCreateCluster} className="mt-4">
                    <Plus className="h-4 w-4 mr-2" />
                    Create First Cluster
                  </Button>
                )}
              </CardContent>
            </Card>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {filteredClusters.map(renderClusterCard)}
            </div>
          )}
        </TabsContent>

        <TabsContent value="ratings" className="space-y-6">
          <div className="mb-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-2">Career Cluster Rating Scale</h3>
            <p className="text-gray-600">
              The rating system used for career cluster assessments and user preferences.
            </p>
          </div>

          {ratings.length === 0 ? (
            <Card>
              <CardContent className="p-8 text-center">
                <Star className="h-12 w-12 mx-auto mb-4 text-gray-400" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">No ratings configured</h3>
                <p className="text-gray-600">
                  The rating system has not been configured yet.
                </p>
              </CardContent>
            </Card>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {ratings.map(renderRatingCard)}
            </div>
          )}
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default ClusterManagement;
