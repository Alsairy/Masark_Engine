import React, { useState } from 'react';
import { 
  Search, 
  Filter, 
  Eye, 
  Edit, 
  Trash2, 
  Plus,
  Star,
  Building,
  Users,
  TrendingUp
} from 'lucide-react';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Badge } from '../ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '../ui/card';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '../ui/dropdown-menu';
import { Career } from '../../services/careerService';

interface CareerListProps {
  careers: Career[];
  loading?: boolean;
  onSearch?: (query: string) => void;
  onFilter?: (filters: any) => void;
  onView?: (career: Career) => void;
  onEdit?: (career: Career) => void;
  onDelete?: (career: Career) => void;
  onCreate?: () => void;
}

const CareerList: React.FC<CareerListProps> = ({
  careers,
  loading = false,
  onSearch,
  onFilter,
  onView,
  onEdit,
  onDelete,
  onCreate
}) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCluster, setSelectedCluster] = useState('all');

  const handleSearch = (value: string) => {
    setSearchTerm(value);
    onSearch?.(value);
  };

  const handleClusterFilter = (cluster: string) => {
    setSelectedCluster(cluster);
    onFilter?.({ cluster });
  };

  const getMatchScoreColor = (score?: number) => {
    if (!score) return 'bg-gray-100 text-gray-800';
    if (score >= 80) return 'bg-green-100 text-green-800';
    if (score >= 60) return 'bg-yellow-100 text-yellow-800';
    return 'bg-red-100 text-red-800';
  };

  if (loading) {
    return (
      <div className="space-y-4">
        {[...Array(5)].map((_, i) => (
          <Card key={i} className="animate-pulse">
            <CardContent className="p-6">
              <div className="h-4 bg-gray-200 rounded w-3/4 mb-2"></div>
              <div className="h-3 bg-gray-200 rounded w-1/2 mb-4"></div>
              <div className="flex space-x-2">
                <div className="h-6 bg-gray-200 rounded w-20"></div>
                <div className="h-6 bg-gray-200 rounded w-16"></div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4 flex-1">
          <div className="relative flex-1 max-w-md">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
            <Input
              placeholder="Search careers..."
              value={searchTerm}
              onChange={(e) => handleSearch(e.target.value)}
              className="pl-10"
            />
          </div>
          
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" size="sm">
                <Filter className="h-4 w-4 mr-2" />
                Cluster: {selectedCluster === 'all' ? 'All' : selectedCluster}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent>
              <DropdownMenuItem onClick={() => handleClusterFilter('all')}>
                All Clusters
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => handleClusterFilter('technology')}>
                Technology
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => handleClusterFilter('healthcare')}>
                Healthcare
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => handleClusterFilter('business')}>
                Business
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => handleClusterFilter('education')}>
                Education
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>

        {onCreate && (
          <Button onClick={onCreate}>
            <Plus className="h-4 w-4 mr-2" />
            Add Career
          </Button>
        )}
      </div>

      {careers.length === 0 ? (
        <Card>
          <CardContent className="p-8 text-center">
            <Building className="h-12 w-12 mx-auto mb-4 text-gray-400" />
            <h3 className="text-lg font-medium text-gray-900 mb-2">No careers found</h3>
            <p className="text-gray-600">
              {searchTerm || selectedCluster !== 'all' 
                ? 'Try adjusting your search or filter criteria.'
                : 'No careers have been added yet.'}
            </p>
            {onCreate && (
              <Button onClick={onCreate} className="mt-4">
                <Plus className="h-4 w-4 mr-2" />
                Add First Career
              </Button>
            )}
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {careers.map((career) => (
            <Card key={career.career_id} className="hover:shadow-md transition-shadow">
              <CardHeader className="pb-3">
                <div className="flex items-start justify-between">
                  <div className="flex-1 min-w-0">
                    <CardTitle className="text-lg font-semibold text-gray-900 truncate">
                      {career.name}
                    </CardTitle>
                    <p className="text-sm text-gray-600 mt-1">
                      {career.cluster.name}
                    </p>
                  </div>
                  
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="ghost" size="sm">
                        <Eye className="h-4 w-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      {onView && (
                        <DropdownMenuItem onClick={() => onView(career)}>
                          <Eye className="h-4 w-4 mr-2" />
                          View Details
                        </DropdownMenuItem>
                      )}
                      {onEdit && (
                        <DropdownMenuItem onClick={() => onEdit(career)}>
                          <Edit className="h-4 w-4 mr-2" />
                          Edit Career
                        </DropdownMenuItem>
                      )}
                      {onDelete && (
                        <DropdownMenuItem 
                          onClick={() => onDelete(career)}
                          className="text-red-600"
                        >
                          <Trash2 className="h-4 w-4 mr-2" />
                          Delete Career
                        </DropdownMenuItem>
                      )}
                    </DropdownMenuContent>
                  </DropdownMenu>
                </div>
              </CardHeader>
              
              <CardContent className="pt-0">
                <p className="text-sm text-gray-600 mb-4 line-clamp-3">
                  {career.description}
                </p>
                
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-2">
                    {career.match_score && (
                      <Badge className={getMatchScoreColor(career.match_score)}>
                        <Star className="h-3 w-3 mr-1" />
                        {career.match_score}%
                      </Badge>
                    )}
                    
                    {career.ssoc_code && (
                      <Badge variant="outline" className="text-xs">
                        {career.ssoc_code}
                      </Badge>
                    )}
                  </div>
                  
                  <div className="flex items-center space-x-1 text-xs text-gray-500">
                    <Users className="h-3 w-3" />
                    <span>{career.programs?.length || 0}</span>
                    <TrendingUp className="h-3 w-3 ml-2" />
                    <span>{career.pathways?.length || 0}</span>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
};

export default CareerList;
