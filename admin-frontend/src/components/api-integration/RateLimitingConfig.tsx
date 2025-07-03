import React, { useState } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Button } from '../ui/button';
import { Badge } from '../ui/badge';
import { Input } from '../ui/input';
import { Label } from '../ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { AlertCircle, Plus, Trash2, Settings, Users, Globe } from 'lucide-react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiIntegrationService, RateLimitConfig, CreateRateLimitRequest } from '../../services/apiIntegrationService';

interface RateLimitingConfigProps {
  className?: string;
}

export const RateLimitingConfig: React.FC<RateLimitingConfigProps> = ({ className }) => {
  const [newConfig, setNewConfig] = useState<CreateRateLimitRequest>({
    name: '',
    requestsPerMinute: 60,
    requestsPerHour: 1000,
    requestsPerDay: 10000,
    burstLimit: 100,
    appliesTo: 'all',
    targetId: undefined
  });
  const queryClient = useQueryClient();

  const { data: rateLimitConfigs, isLoading, error } = useQuery({
    queryKey: ['rateLimitConfigs'],
    queryFn: apiIntegrationService.getRateLimitConfigs
  });

  const createConfigMutation = useMutation({
    mutationFn: apiIntegrationService.createRateLimit,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rateLimitConfigs'] });
      resetForm();
    },
    onError: (error) => {
      console.error('Error creating rate limit configuration:', error);
    }
  });


  const deleteConfigMutation = useMutation({
    mutationFn: apiIntegrationService.deleteRateLimit,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rateLimitConfigs'] });
    }
  });

  const toggleConfigMutation = useMutation({
    mutationFn: ({ configId, isActive }: { configId: number; isActive: boolean }) =>
      apiIntegrationService.toggleRateLimit(configId, isActive),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rateLimitConfigs'] });
    }
  });

  const resetForm = () => {
    setNewConfig({
      name: '',
      requestsPerMinute: 60,
      requestsPerHour: 1000,
      requestsPerDay: 10000,
      burstLimit: 100,
      appliesTo: 'all',
      targetId: undefined
    });
  };

  const handleCreateConfig = () => {
    if (newConfig.name.trim()) {
      createConfigMutation.mutate(newConfig);
    }
  };



  const getAppliesToIcon = (appliesTo: string) => {
    switch (appliesTo) {
      case 'user': return <Users className="h-4 w-4" />;
      case 'apikey': return <Globe className="h-4 w-4" />;
      default: return <Settings className="h-4 w-4" />;
    }
  };

  const getAppliesToLabel = (appliesTo: string) => {
    switch (appliesTo) {
      case 'user': return 'User';
      case 'apikey': return 'API Key';
      default: return 'All';
    }
  };

  if (isLoading) {
    return (
      <div className={`space-y-6 ${className}`}>
        <div className="animate-pulse space-y-4">
          {[...Array(3)].map((_, i) => (
            <Card key={i}>
              <CardContent className="p-6">
                <div className="h-4 bg-gray-200 rounded w-3/4 mb-2"></div>
                <div className="h-4 bg-gray-200 rounded w-1/2"></div>
              </CardContent>
            </Card>
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={`space-y-6 ${className}`}>
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center space-x-2 text-red-600">
              <AlertCircle className="h-5 w-5" />
              <span>Failed to load rate limit configurations</span>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className={`space-y-6 ${className}`}>
      <div className="flex justify-between items-center">
        <div>
          <h2 className="text-2xl font-bold">Rate Limiting Configuration</h2>
          <p className="text-gray-600">Configure API rate limits to protect your system</p>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Create New Rate Limit</CardTitle>
          <CardDescription>
            Set up rate limiting rules for different API endpoints or user groups
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            <div className="space-y-2">
              <Label htmlFor="name">Configuration Name</Label>
              <Input
                id="name"
                placeholder="e.g., Assessment API Limit"
                value={newConfig.name}
                onChange={(e) => setNewConfig({ ...newConfig, name: e.target.value })}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="appliesTo">Applies To</Label>
              <Select
                value={newConfig.appliesTo}
                onValueChange={(value) => setNewConfig({ ...newConfig, appliesTo: value as 'all' | 'user' | 'apikey' })}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">All Requests</SelectItem>
                  <SelectItem value="user">Per User</SelectItem>
                  <SelectItem value="apikey">Per API Key</SelectItem>
                  <SelectItem value="admin">Admin Only</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="requestsPerMinute">Requests per Minute</Label>
              <Input
                id="requestsPerMinute"
                type="number"
                value={newConfig.requestsPerMinute}
                onChange={(e) => setNewConfig({ ...newConfig, requestsPerMinute: parseInt(e.target.value) || 0 })}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="requestsPerHour">Requests per Hour</Label>
              <Input
                id="requestsPerHour"
                type="number"
                value={newConfig.requestsPerHour}
                onChange={(e) => setNewConfig({ ...newConfig, requestsPerHour: parseInt(e.target.value) || 0 })}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="requestsPerDay">Requests per Day</Label>
              <Input
                id="requestsPerDay"
                type="number"
                value={newConfig.requestsPerDay}
                onChange={(e) => setNewConfig({ ...newConfig, requestsPerDay: parseInt(e.target.value) || 0 })}
              />
            </div>

            <div className="space-y-2">
              <Label htmlFor="burstLimit">Burst Limit</Label>
              <Input
                id="burstLimit"
                type="number"
                value={newConfig.burstLimit}
                onChange={(e) => setNewConfig({ ...newConfig, burstLimit: parseInt(e.target.value) || 0 })}
              />
            </div>
          </div>

          <div className="flex justify-end mt-4">
            <Button
              onClick={handleCreateConfig}
              disabled={!newConfig.name.trim() || createConfigMutation.isPending}
              className="flex items-center space-x-2"
            >
              <Plus className="h-4 w-4" />
              <span>{createConfigMutation.isPending ? 'Creating...' : 'Create Configuration'}</span>
            </Button>
          </div>
        </CardContent>
      </Card>

      <div className="space-y-4">
        <h3 className="text-lg font-semibold">Existing Rate Limit Configurations</h3>
        
        {!rateLimitConfigs || rateLimitConfigs.length === 0 ? (
          <Card>
            <CardContent className="p-6">
              <p className="text-gray-500 text-center">No rate limit configurations found</p>
            </CardContent>
          </Card>
        ) : (
          rateLimitConfigs.map((config: RateLimitConfig) => (
            <Card key={config.id} className="relative">
              <CardContent className="p-6">
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <div className="flex items-center space-x-3 mb-2">
                      <h4 className="font-semibold text-lg">{config.name}</h4>
                      <Badge variant={config.isActive ? 'default' : 'secondary'}>
                        {config.isActive ? 'Active' : 'Inactive'}
                      </Badge>
                      <div className="flex items-center space-x-1">
                        {getAppliesToIcon(config.appliesTo)}
                        <span className="text-sm text-gray-600">
                          {getAppliesToLabel(config.appliesTo)}
                        </span>
                      </div>
                    </div>
                    
                    <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mt-4">
                      <div className="text-center">
                        <div className="text-2xl font-bold text-blue-600">{config.requestsPerMinute}</div>
                        <div className="text-sm text-gray-600">Per Minute</div>
                      </div>
                      <div className="text-center">
                        <div className="text-2xl font-bold text-green-600">{config.requestsPerHour}</div>
                        <div className="text-sm text-gray-600">Per Hour</div>
                      </div>
                      <div className="text-center">
                        <div className="text-2xl font-bold text-purple-600">{config.requestsPerDay}</div>
                        <div className="text-sm text-gray-600">Per Day</div>
                      </div>
                      <div className="text-center">
                        <div className="text-2xl font-bold text-orange-600">{config.burstLimit}</div>
                        <div className="text-sm text-gray-600">Burst Limit</div>
                      </div>
                    </div>

                    {config.targetId && (
                      <div className="mt-3">
                        <span className="text-sm text-gray-600">Target ID: {config.targetId}</span>
                      </div>
                    )}

                    <div className="mt-3 text-xs text-gray-500">
                      Created: {new Date(config.createdAt).toLocaleDateString()} | 
                      Updated: {new Date(config.updatedAt).toLocaleDateString()}
                    </div>
                  </div>
                  
                  <div className="flex space-x-2 ml-4">
                    
                    <Button
                      variant={config.isActive ? 'secondary' : 'default'}
                      size="sm"
                      onClick={() => toggleConfigMutation.mutate({ 
                        configId: config.id, 
                        isActive: !config.isActive 
                      })}
                      disabled={toggleConfigMutation.isPending}
                    >
                      {config.isActive ? 'Deactivate' : 'Activate'}
                    </Button>
                    
                    <Button
                      variant="destructive"
                      size="sm"
                      onClick={() => deleteConfigMutation.mutate(config.id)}
                      disabled={deleteConfigMutation.isPending}
                      className="flex items-center space-x-1"
                    >
                      <Trash2 className="h-4 w-4" />
                      <span>Delete</span>
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))
        )}
      </div>
    </div>
  );
};

export default RateLimitingConfig;
