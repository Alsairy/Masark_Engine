import React, { useState, useEffect } from 'react';
import { BarChart3, Globe, GraduationCap, Plus, Settings, TrendingUp } from 'lucide-react';
import PathwayList from './PathwayList';
import PathwayForm from './PathwayForm';
import DeploymentModeSelector from '../system/DeploymentModeSelector';
import { pathwayService, Pathway, PathwayStats } from '../../services/pathwayService';

type TabType = 'overview' | 'pathways' | 'deployment' | 'create';

const PathwayManagement: React.FC = () => {
  const [activeTab, setActiveTab] = useState<TabType>('overview');
  const [stats, setStats] = useState<PathwayStats | null>(null);
  const [selectedPathway, setSelectedPathway] = useState<Pathway | null>(null);
  const [showEditModal, setShowEditModal] = useState(false);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchStats();
  }, []);

  const fetchStats = async () => {
    try {
      setLoading(true);
      const pathwayStats = await pathwayService.getPathwayStats();
      setStats(pathwayStats);
    } catch (err) {
      console.error('Failed to fetch pathway stats:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleEditPathway = (pathway: Pathway) => {
    setSelectedPathway(pathway);
    setShowEditModal(true);
  };

  const handleCreatePathway = () => {
    setActiveTab('create');
  };

  const handlePathwaySaved = (_pathway: Pathway) => {
    setShowEditModal(false);
    setSelectedPathway(null);
    fetchStats();
    
    if (activeTab === 'create') {
      setActiveTab('pathways');
    }
  };

  const handleModalClose = () => {
    setShowEditModal(false);
    setSelectedPathway(null);
  };

  const tabs = [
    { id: 'overview' as TabType, name: 'Overview', icon: BarChart3 },
    { id: 'pathways' as TabType, name: 'Manage Pathways', icon: Settings },
    { id: 'deployment' as TabType, name: 'Deployment Mode', icon: Globe },
    { id: 'create' as TabType, name: 'Create Pathway', icon: Plus },
  ];

  const renderOverview = () => {
    if (loading) {
      return (
        <div className="animate-pulse space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
            {[...Array(4)].map((_, i) => (
              <div key={i} className="bg-gray-200 h-24 rounded-lg"></div>
            ))}
          </div>
          <div className="h-64 bg-gray-200 rounded-lg"></div>
        </div>
      );
    }

    return (
      <div className="space-y-6">
        {/* Statistics Cards */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Total Pathways</p>
                <p className="text-2xl font-bold text-gray-900">{stats?.totalPathways || 0}</p>
              </div>
              <div className="p-3 bg-blue-100 rounded-lg">
                <BarChart3 className="h-6 w-6 text-blue-600" />
              </div>
            </div>
          </div>

          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">MOE Pathways</p>
                <p className="text-2xl font-bold text-blue-900">{stats?.moePathways || 0}</p>
              </div>
              <div className="p-3 bg-blue-100 rounded-lg">
                <Globe className="h-6 w-6 text-blue-600" />
              </div>
            </div>
          </div>

          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Mawhiba Pathways</p>
                <p className="text-2xl font-bold text-purple-900">{stats?.mawhibaPathways || 0}</p>
              </div>
              <div className="p-3 bg-purple-100 rounded-lg">
                <GraduationCap className="h-6 w-6 text-purple-600" />
              </div>
            </div>
          </div>

          <div className="bg-white rounded-lg border border-gray-200 p-6">
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium text-gray-600">Active Pathways</p>
                <p className="text-2xl font-bold text-green-900">{stats?.activePathways || 0}</p>
              </div>
              <div className="p-3 bg-green-100 rounded-lg">
                <TrendingUp className="h-6 w-6 text-green-600" />
              </div>
            </div>
          </div>
        </div>

        {/* Pathway Distribution Chart */}
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h4 className="text-lg font-medium text-gray-900 mb-4">Pathway Distribution</h4>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-3">
                  <div className="w-4 h-4 bg-blue-500 rounded"></div>
                  <span className="text-sm font-medium text-gray-700">MOE Pathways</span>
                </div>
                <span className="text-sm text-gray-600">{stats?.moePathways || 0}</span>
              </div>
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-3">
                  <div className="w-4 h-4 bg-purple-500 rounded"></div>
                  <span className="text-sm font-medium text-gray-700">Mawhiba Pathways</span>
                </div>
                <span className="text-sm text-gray-600">{stats?.mawhibaPathways || 0}</span>
              </div>
            </div>
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-3">
                  <div className="w-4 h-4 bg-green-500 rounded"></div>
                  <span className="text-sm font-medium text-gray-700">Active</span>
                </div>
                <span className="text-sm text-gray-600">{stats?.activePathways || 0}</span>
              </div>
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-3">
                  <div className="w-4 h-4 bg-gray-400 rounded"></div>
                  <span className="text-sm font-medium text-gray-700">Inactive</span>
                </div>
                <span className="text-sm text-gray-600">{stats?.inactivePathways || 0}</span>
              </div>
            </div>
          </div>
        </div>

        {/* Quick Actions */}
        <div className="bg-white rounded-lg border border-gray-200 p-6">
          <h4 className="text-lg font-medium text-gray-900 mb-4">Quick Actions</h4>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <button
              onClick={() => setActiveTab('create')}
              className="flex items-center justify-center space-x-2 p-4 border-2 border-dashed border-gray-300 rounded-lg hover:border-blue-500 hover:bg-blue-50 transition-colors"
            >
              <Plus className="h-5 w-5 text-gray-400" />
              <span className="text-sm font-medium text-gray-600">Create New Pathway</span>
            </button>
            <button
              onClick={() => setActiveTab('deployment')}
              className="flex items-center justify-center space-x-2 p-4 border-2 border-dashed border-gray-300 rounded-lg hover:border-purple-500 hover:bg-purple-50 transition-colors"
            >
              <Settings className="h-5 w-5 text-gray-400" />
              <span className="text-sm font-medium text-gray-600">Configure Deployment</span>
            </button>
            <button
              onClick={() => setActiveTab('pathways')}
              className="flex items-center justify-center space-x-2 p-4 border-2 border-dashed border-gray-300 rounded-lg hover:border-green-500 hover:bg-green-50 transition-colors"
            >
              <BarChart3 className="h-5 w-5 text-gray-400" />
              <span className="text-sm font-medium text-gray-600">Manage Pathways</span>
            </button>
          </div>
        </div>
      </div>
    );
  };

  const renderContent = () => {
    switch (activeTab) {
      case 'overview':
        return renderOverview();
      case 'pathways':
        return (
          <PathwayList
            onEditPathway={handleEditPathway}
            onCreatePathway={handleCreatePathway}
          />
        );
      case 'deployment':
        return <DeploymentModeSelector />;
      case 'create':
        return (
          <PathwayForm
            onSave={handlePathwaySaved}
            onCancel={() => setActiveTab('overview')}
          />
        );
      default:
        return renderOverview();
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-gray-900">Pathway Management</h2>
          <p className="text-gray-600">Manage MOE and Mawhiba educational pathways</p>
        </div>
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200">
        <nav className="-mb-px flex space-x-8">
          {tabs.map((tab) => {
            const Icon = tab.icon;
            const isActive = activeTab === tab.id;
            
            return (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`flex items-center space-x-2 py-2 px-1 border-b-2 font-medium text-sm transition-colors ${
                  isActive
                    ? 'border-blue-500 text-blue-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                }`}
              >
                <Icon className="h-4 w-4" />
                <span>{tab.name}</span>
              </button>
            );
          })}
        </nav>
      </div>

      {/* Content */}
      <div className="min-h-[600px]">
        {renderContent()}
      </div>

      {/* Edit Modal */}
      {showEditModal && selectedPathway && (
        <PathwayForm
          pathway={selectedPathway}
          onSave={handlePathwaySaved}
          onCancel={handleModalClose}
          isModal={true}
        />
      )}
    </div>
  );
};

export default PathwayManagement;
