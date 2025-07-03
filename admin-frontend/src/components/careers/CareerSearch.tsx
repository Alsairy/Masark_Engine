import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { 
  Search, 
  Filter, 
  Download, 
  RefreshCw,
  Target,
  Users,
  TrendingUp
} from 'lucide-react';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Card, CardContent, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { careerService, Career } from '../../services/careerService';

interface CareerSearchProps {
  onCareerSelect?: (career: Career) => void;
}

const CareerSearch: React.FC<CareerSearchProps> = ({ onCareerSelect }) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedPersonalityType, setSelectedPersonalityType] = useState('INTJ');
  const [language, setLanguage] = useState('en');
  const [activeTab, setActiveTab] = useState('search');

  const { data: searchResults, isLoading: searchLoading, refetch: refetchSearch } = useQuery({
    queryKey: ['career-search', searchQuery, language],
    queryFn: () => careerService.searchCareers(searchQuery, language),
    enabled: searchQuery.length >= 2,
  });

  const { data: matchResults, isLoading: matchLoading, refetch: refetchMatches } = useQuery({
    queryKey: ['career-matches', selectedPersonalityType, language],
    queryFn: () => careerService.getCareerMatches({
      personalityType: selectedPersonalityType,
      language,
      limit: 20
    }),
  });

  const { data: careerStats } = useQuery({
    queryKey: ['career-stats'],
    queryFn: () => careerService.getCareerStats(),
    refetchInterval: 30000,
  });

  const personalityTypes = [
    'INTJ', 'INTP', 'ENTJ', 'ENTP',
    'INFJ', 'INFP', 'ENFJ', 'ENFP',
    'ISTJ', 'ISFJ', 'ESTJ', 'ESFJ',
    'ISTP', 'ISFP', 'ESTP', 'ESFP'
  ];

  const handleSearch = (query: string) => {
    setSearchQuery(query);
    if (query.length >= 2) {
      refetchSearch();
    }
  };

  const handlePersonalityTypeChange = (type: string) => {
    setSelectedPersonalityType(type);
    refetchMatches();
  };

  const exportResults = (data: any[], filename: string) => {
    const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${filename}-${new Date().toISOString().split('T')[0]}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  const renderCareerCard = (career: Career, showMatchScore = false) => (
    <Card 
      key={career.career_id} 
      className="hover:shadow-md transition-shadow cursor-pointer"
      onClick={() => onCareerSelect?.(career)}
    >
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
          
          {showMatchScore && career.match_score && (
            <Badge className="bg-blue-100 text-blue-800">
              <Target className="h-3 w-3 mr-1" />
              {career.match_score}%
            </Badge>
          )}
        </div>
      </CardHeader>
      
      <CardContent className="pt-0">
        <p className="text-sm text-gray-600 mb-4 line-clamp-3">
          {career.description}
        </p>
        
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
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
  );

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-gray-900">Career Search & Matching</h2>
          <p className="text-gray-600 mt-1">
            Search careers or find matches based on personality types
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
          
          <Button variant="outline" size="sm">
            <RefreshCw className="h-4 w-4 mr-2" />
            Refresh
          </Button>
        </div>
      </div>

      {careerStats && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600">Total Careers</p>
                  <p className="text-2xl font-bold text-gray-900">
                    {careerStats.statistics?.total_careers || 0}
                  </p>
                </div>
                <div className="p-3 bg-blue-100 rounded-lg">
                  <Target className="h-6 w-6 text-blue-600" />
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600">Career Clusters</p>
                  <p className="text-2xl font-bold text-gray-900">
                    {careerStats.statistics?.total_clusters || 0}
                  </p>
                </div>
                <div className="p-3 bg-green-100 rounded-lg">
                  <Filter className="h-6 w-6 text-green-600" />
                </div>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="p-6">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-600">Cache Status</p>
                  <p className="text-2xl font-bold text-gray-900">Active</p>
                </div>
                <div className="p-3 bg-yellow-100 rounded-lg">
                  <RefreshCw className="h-6 w-6 text-yellow-600" />
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      )}

      <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-6">
        <TabsList className="grid w-full grid-cols-2">
          <TabsTrigger value="search">Career Search</TabsTrigger>
          <TabsTrigger value="matching">Personality Matching</TabsTrigger>
        </TabsList>

        <TabsContent value="search" className="space-y-6">
          <div className="flex items-center space-x-4">
            <div className="relative flex-1">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
              <Input
                placeholder="Search careers by name, description, or SSOC code..."
                value={searchQuery}
                onChange={(e) => handleSearch(e.target.value)}
                className="pl-10"
              />
            </div>
            
            {searchResults?.careers && (
              <Button 
                variant="outline" 
                size="sm"
                onClick={() => exportResults(searchResults.careers, 'career-search')}
              >
                <Download className="h-4 w-4 mr-2" />
                Export
              </Button>
            )}
          </div>

          {searchLoading && (
            <div className="text-center py-8">
              <RefreshCw className="h-8 w-8 animate-spin mx-auto mb-4 text-blue-600" />
              <p className="text-gray-600">Searching careers...</p>
            </div>
          )}

          {searchResults && searchResults.careers && (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <p className="text-sm text-gray-600">
                  Found {searchResults.total_results} careers for "{searchQuery}"
                </p>
              </div>
              
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {searchResults.careers.map((career: Career) => renderCareerCard(career))}
              </div>
            </div>
          )}

          {searchQuery.length >= 2 && searchResults?.total_results === 0 && (
            <Card>
              <CardContent className="p-8 text-center">
                <Search className="h-12 w-12 mx-auto mb-4 text-gray-400" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">No careers found</h3>
                <p className="text-gray-600">
                  Try adjusting your search terms or check the spelling.
                </p>
              </CardContent>
            </Card>
          )}
        </TabsContent>

        <TabsContent value="matching" className="space-y-6">
          <div className="flex items-center space-x-4">
            <div className="flex-1">
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Personality Type
              </label>
              <select
                value={selectedPersonalityType}
                onChange={(e) => handlePersonalityTypeChange(e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
              >
                {personalityTypes.map(type => (
                  <option key={type} value={type}>{type}</option>
                ))}
              </select>
            </div>
            
            {matchResults?.matches && (
              <div className="pt-6">
                <Button 
                  variant="outline" 
                  size="sm"
                  onClick={() => exportResults(matchResults.matches, `career-matches-${selectedPersonalityType}`)}
                >
                  <Download className="h-4 w-4 mr-2" />
                  Export
                </Button>
              </div>
            )}
          </div>

          {matchLoading && (
            <div className="text-center py-8">
              <RefreshCw className="h-8 w-8 animate-spin mx-auto mb-4 text-blue-600" />
              <p className="text-gray-600">Finding career matches...</p>
            </div>
          )}

          {matchResults && matchResults.matches && (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <p className="text-sm text-gray-600">
                  Found {matchResults.total_matches} career matches for {selectedPersonalityType}
                </p>
                <Badge className="bg-blue-100 text-blue-800">
                  {matchResults.deployment_mode} Mode
                </Badge>
              </div>
              
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                {matchResults.matches.map((career: Career) => renderCareerCard(career, true))}
              </div>
            </div>
          )}

          {matchResults?.total_matches === 0 && (
            <Card>
              <CardContent className="p-8 text-center">
                <Target className="h-12 w-12 mx-auto mb-4 text-gray-400" />
                <h3 className="text-lg font-medium text-gray-900 mb-2">No matches found</h3>
                <p className="text-gray-600">
                  No career matches found for {selectedPersonalityType} personality type.
                </p>
              </CardContent>
            </Card>
          )}
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default CareerSearch;
