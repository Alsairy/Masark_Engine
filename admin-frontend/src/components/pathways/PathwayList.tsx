import React, { useState, useEffect } from 'react';
import { Search, Filter, Plus, Edit, Trash2, Eye, Globe, GraduationCap, Clock, BarChart3 } from 'lucide-react';
import { pathwayService, Pathway } from '../../services/pathwayService';

interface PathwayListProps {
  onEditPathway?: (pathway: Pathway) => void;
  onCreatePathway?: () => void;
  selectedSource?: 'MOE' | 'MAWHIBA' | 'ALL';
}

const PathwayList: React.FC<PathwayListProps> = ({ 
  onEditPathway, 
  onCreatePathway,
  selectedSource = 'ALL'
}) => {
  const [pathways, setPathways] = useState<Pathway[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [sourceFilter, setSourceFilter] = useState<'ALL' | 'MOE' | 'MAWHIBA'>(selectedSource);
  const [statusFilter, setStatusFilter] = useState<'ALL' | 'ACTIVE' | 'INACTIVE'>('ALL');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);

  useEffect(() => {
    fetchPathways();
  }, [sourceFilter, statusFilter, currentPage, searchTerm]);

  const fetchPathways = async () => {
    try {
      setLoading(true);
      setError(null);

      interface FetchParams {
        page?: number;
        limit?: number;
        source?: 'MAWHIBA' | 'MOE';
        isActive?: boolean;
        search?: string;
      }

      const params: FetchParams = {
        page: currentPage,
        limit: 10,
      };

      if (sourceFilter !== 'ALL') {
        params.source = sourceFilter;
      }

      if (statusFilter !== 'ALL') {
        params.isActive = statusFilter === 'ACTIVE';
      }

      if (searchTerm.trim()) {
        params.search = searchTerm.trim();
      }

      const response = await pathwayService.getPathways(params);
      setPathways(response.pathways);
      setTotalPages(Math.ceil(response.total / 10));
    } catch (err: unknown) {
      console.error('Failed to fetch pathways:', err);
      setError('Failed to load pathways');
    } finally {
      setLoading(false);
    }
  };

  const handleDeletePathway = async (pathway: Pathway) => {
    if (!confirm(`Are you sure you want to delete "${pathway.nameEn}"? This action cannot be undone.`)) {
      return;
    }

    try {
      await pathwayService.deletePathway(pathway.id);
      fetchPathways();
    } catch (err: unknown) {
      console.error('Failed to delete pathway:', err);
      alert('Failed to delete pathway. Please try again.');
    }
  };

  const getSourceIcon = (source: string) => {
    return source === 'MOE' ? Globe : GraduationCap;
  };

  const getSourceColor = (source: string) => {
    return source === 'MOE' 
      ? 'bg-blue-100 text-blue-800 border-blue-200'
      : 'bg-purple-100 text-purple-800 border-purple-200';
  };

  const getPathwayCharacteristics = (source: string) => {
    if (source === 'MOE') {
      return {
        duration: '2-4 years',
        difficulty: 'Moderate to High',
        prerequisites: 'High school diploma, minimum GPA'
      };
    } else {
      return {
        duration: '1-2 years',
        difficulty: 'High',
        prerequisites: 'Exceptional performance, aptitude tests'
      };
    }
  };

  if (loading) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <div className="animate-pulse space-y-4">
          <div className="h-4 bg-gray-200 rounded w-1/4"></div>
          <div className="space-y-3">
            {[...Array(5)].map((_, i) => (
              <div key={i} className="h-16 bg-gray-200 rounded"></div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-lg border border-gray-200">
      {/* Header */}
      <div className="p-6 border-b border-gray-200">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-medium text-gray-900">Pathway Management</h3>
          {onCreatePathway && (
            <button
              onClick={onCreatePathway}
              className="inline-flex items-center px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 transition-colors"
            >
              <Plus className="h-4 w-4 mr-2" />
              Add Pathway
            </button>
          )}
        </div>

        {/* Filters */}
        <div className="flex flex-col sm:flex-row gap-4">
          <div className="flex-1">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
              <input
                type="text"
                placeholder="Search pathways..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
            </div>
          </div>

          <div className="flex gap-2">
            <select
              value={sourceFilter}
              onChange={(e) => setSourceFilter(e.target.value as 'ALL' | 'MOE' | 'MAWHIBA')}
              className="px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            >
              <option value="ALL">All Sources</option>
              <option value="MOE">MOE Only</option>
              <option value="MAWHIBA">Mawhiba Only</option>
            </select>

            <select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value as 'ALL' | 'ACTIVE' | 'INACTIVE')}
              className="px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            >
              <option value="ALL">All Status</option>
              <option value="ACTIVE">Active Only</option>
              <option value="INACTIVE">Inactive Only</option>
            </select>
          </div>
        </div>
      </div>

      {/* Error State */}
      {error && (
        <div className="p-6 border-b border-gray-200">
          <div className="bg-red-50 border border-red-200 rounded-lg p-4">
            <p className="text-red-800">{error}</p>
            <button
              onClick={fetchPathways}
              className="mt-2 text-red-600 hover:text-red-800 text-sm font-medium"
            >
              Try again
            </button>
          </div>
        </div>
      )}

      {/* Pathway List */}
      <div className="divide-y divide-gray-200">
        {pathways.length === 0 ? (
          <div className="p-12 text-center">
            <Filter className="h-12 w-12 text-gray-400 mx-auto mb-4" />
            <h4 className="text-lg font-medium text-gray-900 mb-2">No pathways found</h4>
            <p className="text-gray-600 mb-4">
              {searchTerm || sourceFilter !== 'ALL' || statusFilter !== 'ALL'
                ? 'Try adjusting your search criteria or filters.'
                : 'Get started by creating your first pathway.'}
            </p>
            {onCreatePathway && (
              <button
                onClick={onCreatePathway}
                className="inline-flex items-center px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 transition-colors"
              >
                <Plus className="h-4 w-4 mr-2" />
                Create Pathway
              </button>
            )}
          </div>
        ) : (
          pathways.map((pathway) => {
            const SourceIcon = getSourceIcon(pathway.source);
            const characteristics = getPathwayCharacteristics(pathway.source);

            return (
              <div key={pathway.id} className="p-6 hover:bg-gray-50 transition-colors">
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center space-x-3 mb-2">
                      <div className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium border ${getSourceColor(pathway.source)}`}>
                        <SourceIcon className="h-3 w-3 mr-1" />
                        {pathway.source}
                      </div>
                      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                        pathway.isActive 
                          ? 'bg-green-100 text-green-800 border border-green-200'
                          : 'bg-gray-100 text-gray-800 border border-gray-200'
                      }`}>
                        {pathway.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </div>

                    <h4 className="text-lg font-medium text-gray-900 mb-1">
                      {pathway.nameEn}
                    </h4>
                    {pathway.nameAr && (
                      <h5 className="text-md text-gray-600 mb-2 font-arabic">
                        {pathway.nameAr}
                      </h5>
                    )}
                    
                    <p className="text-gray-600 mb-3 line-clamp-2">
                      {pathway.descriptionEn}
                    </p>

                    {/* Pathway Characteristics */}
                    <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 text-sm">
                      <div className="flex items-center space-x-2">
                        <Clock className="h-4 w-4 text-gray-400" />
                        <span className="text-gray-600">Duration: {characteristics.duration}</span>
                      </div>
                      <div className="flex items-center space-x-2">
                        <BarChart3 className="h-4 w-4 text-gray-400" />
                        <span className="text-gray-600">Difficulty: {characteristics.difficulty}</span>
                      </div>
                      <div className="flex items-center space-x-2">
                        <Eye className="h-4 w-4 text-gray-400" />
                        <span className="text-gray-600">ID: {pathway.id}</span>
                      </div>
                    </div>
                  </div>

                  <div className="flex items-center space-x-2 ml-4">
                    {onEditPathway && (
                      <button
                        onClick={() => onEditPathway(pathway)}
                        className="p-2 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition-colors"
                        title="Edit pathway"
                      >
                        <Edit className="h-4 w-4" />
                      </button>
                    )}
                    <button
                      onClick={() => handleDeletePathway(pathway)}
                      className="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                      title="Delete pathway"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                </div>
              </div>
            );
          })
        )}
      </div>

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="p-6 border-t border-gray-200">
          <div className="flex items-center justify-between">
            <p className="text-sm text-gray-700">
              Page {currentPage} of {totalPages}
            </p>
            <div className="flex space-x-2">
              <button
                onClick={() => setCurrentPage(Math.max(1, currentPage - 1))}
                disabled={currentPage === 1}
                className="px-3 py-1 text-sm border border-gray-300 rounded hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Previous
              </button>
              <button
                onClick={() => setCurrentPage(Math.min(totalPages, currentPage + 1))}
                disabled={currentPage === totalPages}
                className="px-3 py-1 text-sm border border-gray-300 rounded hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Next
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default PathwayList;
