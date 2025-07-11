import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { apiIntegrationService, ApiKey, CreateApiKeyRequest } from '../../services/apiIntegrationService';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Label } from '../ui/label';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';
import { Switch } from '../ui/switch';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '../ui/dialog';
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle, AlertDialogTrigger } from '../ui/alert-dialog';
import { Key, Copy, Trash2, RefreshCw, Plus, Eye, EyeOff } from 'lucide-react';
import { toast } from 'sonner';

interface ApiKeyManagementProps {
  userId?: number;
}

const ApiKeyManagement: React.FC<ApiKeyManagementProps> = ({ userId }) => {
  const queryClient = useQueryClient();
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [showKeyValue, setShowKeyValue] = useState<{ [key: number]: boolean }>({});
  const [newKey, setNewKey] = useState<CreateApiKeyRequest>({
    name: '',
    userId: userId || 0,
    permissions: [],
    rateLimit: 1000,
    expiresAt: ''
  });

  const { data: apiKeys, isLoading, error } = useQuery({
    queryKey: ['apiKeys', userId],
    queryFn: () => apiIntegrationService.getApiKeys(userId)
  });

  const createKeyMutation = useMutation({
    mutationFn: apiIntegrationService.createApiKey,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['apiKeys'] });
      setShowCreateDialog(false);
      setNewKey({
        name: '',
        userId: userId || 0,
        permissions: [],
        rateLimit: 1000,
        expiresAt: ''
      });
      toast.success('API key created successfully');
    },
    onError: (error: Error) => {
      toast.error(`Failed to create API key: ${error.message}`);
    }
  });

  const deleteKeyMutation = useMutation({
    mutationFn: apiIntegrationService.deleteApiKey,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['apiKeys'] });
      toast.success('API key deleted successfully');
    },
    onError: (error: Error) => {
      toast.error(`Failed to delete API key: ${error.message}`);
    }
  });

  const toggleKeyMutation = useMutation({
    mutationFn: ({ keyId, isActive }: { keyId: number; isActive: boolean }) =>
      apiIntegrationService.toggleApiKey(keyId, isActive),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['apiKeys'] });
      toast.success('API key status updated');
    },
    onError: (error: Error) => {
      toast.error(`Failed to update API key: ${error.message}`);
    }
  });

  const regenerateKeyMutation = useMutation({
    mutationFn: apiIntegrationService.regenerateApiKey,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['apiKeys'] });
      toast.success('API key regenerated successfully');
    },
    onError: (error: Error) => {
      toast.error(`Failed to regenerate API key: ${error.message}`);
    }
  });

  const handleCreateKey = () => {
    if (!newKey.name.trim()) {
      toast.error('Please enter a name for the API key');
      return;
    }
    createKeyMutation.mutate(newKey);
  };

  const handleCopyKey = (key: string) => {
    navigator.clipboard.writeText(key);
    toast.success('API key copied to clipboard');
  };

  const toggleKeyVisibility = (keyId: number) => {
    setShowKeyValue(prev => ({
      ...prev,
      [keyId]: !prev[keyId]
    }));
  };

  const availablePermissions = [
    'assessment:read',
    'assessment:write',
    'careers:read',
    'careers:write',
    'reports:read',
    'reports:write',
    'users:read',
    'users:write',
    'system:read',
    'system:write'
  ];

  if (isLoading) {
    return (
      <div className="flex items-center justify-center p-8">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center p-8">
        <p className="text-red-600">Failed to load API keys</p>
        <Button onClick={() => queryClient.invalidateQueries({ queryKey: ['apiKeys'] })} className="mt-4">
          Retry
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-gray-900">API Key Management</h2>
          <p className="text-gray-600">Manage API keys for secure access to the Masark API</p>
        </div>
        <Dialog open={showCreateDialog} onOpenChange={setShowCreateDialog}>
          <DialogTrigger asChild>
            <Button className="flex items-center space-x-2">
              <Plus className="h-4 w-4" />
              <span>Create API Key</span>
            </Button>
          </DialogTrigger>
          <DialogContent className="max-w-md">
            <DialogHeader>
              <DialogTitle>Create New API Key</DialogTitle>
              <DialogDescription>
                Create a new API key for secure access to the Masark API
              </DialogDescription>
            </DialogHeader>
            <div className="space-y-4">
              <div>
                <Label htmlFor="keyName">Key Name</Label>
                <Input
                  id="keyName"
                  value={newKey.name}
                  onChange={(e) => setNewKey({ ...newKey, name: e.target.value })}
                  placeholder="Enter a descriptive name"
                />
              </div>
              <div>
                <Label htmlFor="rateLimit">Rate Limit (requests/hour)</Label>
                <Input
                  id="rateLimit"
                  type="number"
                  value={newKey.rateLimit}
                  onChange={(e) => setNewKey({ ...newKey, rateLimit: parseInt(e.target.value) || 1000 })}
                />
              </div>
              <div>
                <Label htmlFor="expiresAt">Expiration Date (optional)</Label>
                <Input
                  id="expiresAt"
                  type="datetime-local"
                  value={newKey.expiresAt}
                  onChange={(e) => setNewKey({ ...newKey, expiresAt: e.target.value })}
                />
              </div>
              <div>
                <Label>Permissions</Label>
                <div className="grid grid-cols-2 gap-2 mt-2">
                  {availablePermissions.map((permission) => (
                    <label key={permission} className="flex items-center space-x-2">
                      <input
                        type="checkbox"
                        checked={newKey.permissions.includes(permission)}
                        onChange={(e) => {
                          if (e.target.checked) {
                            setNewKey({
                              ...newKey,
                              permissions: [...newKey.permissions, permission]
                            });
                          } else {
                            setNewKey({
                              ...newKey,
                              permissions: newKey.permissions.filter(p => p !== permission)
                            });
                          }
                        }}
                        className="rounded"
                      />
                      <span className="text-sm">{permission}</span>
                    </label>
                  ))}
                </div>
              </div>
            </div>
            <DialogFooter>
              <Button variant="outline" onClick={() => setShowCreateDialog(false)}>
                Cancel
              </Button>
              <Button onClick={handleCreateKey} disabled={createKeyMutation.isPending}>
                {createKeyMutation.isPending ? 'Creating...' : 'Create Key'}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </div>

      <div className="grid gap-4">
        {apiKeys?.length === 0 ? (
          <Card>
            <CardContent className="flex flex-col items-center justify-center py-12">
              <Key className="h-12 w-12 text-gray-400 mb-4" />
              <h3 className="text-lg font-medium text-gray-900 mb-2">No API Keys</h3>
              <p className="text-gray-600 text-center mb-4">
                Create your first API key to start using the Masark API
              </p>
              <Button onClick={() => setShowCreateDialog(true)}>
                Create API Key
              </Button>
            </CardContent>
          </Card>
        ) : (
          apiKeys?.map((apiKey: ApiKey) => (
            <Card key={apiKey.id} className="hover:shadow-md transition-shadow">
              <CardHeader>
                <div className="flex items-center justify-between">
                  <div className="flex items-center space-x-3">
                    <Key className="h-5 w-5 text-blue-600" />
                    <div>
                      <CardTitle className="text-lg">{apiKey.name}</CardTitle>
                      <CardDescription>
                        Created {new Date(apiKey.createdAt).toLocaleDateString()}
                        {apiKey.lastUsed && (
                          <span className="ml-2">
                            • Last used {new Date(apiKey.lastUsed).toLocaleDateString()}
                          </span>
                        )}
                      </CardDescription>
                    </div>
                  </div>
                  <div className="flex items-center space-x-2">
                    <Badge variant={apiKey.isActive ? 'default' : 'secondary'}>
                      {apiKey.isActive ? 'Active' : 'Inactive'}
                    </Badge>
                    <Switch
                      checked={apiKey.isActive}
                      onCheckedChange={(checked) =>
                        toggleKeyMutation.mutate({ keyId: apiKey.id, isActive: checked })
                      }
                    />
                  </div>
                </div>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div>
                    <Label className="text-sm font-medium">API Key</Label>
                    <div className="flex items-center space-x-2 mt-1">
                      <Input
                        value={showKeyValue[apiKey.id] ? apiKey.key : '••••••••••••••••••••••••••••••••'}
                        readOnly
                        className="font-mono text-sm"
                      />
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => toggleKeyVisibility(apiKey.id)}
                      >
                        {showKeyValue[apiKey.id] ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => handleCopyKey(apiKey.key)}
                      >
                        <Copy className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>

                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <Label className="text-sm font-medium">Usage Count</Label>
                      <p className="text-sm text-gray-600">{apiKey.usageCount.toLocaleString()} requests</p>
                    </div>
                    <div>
                      <Label className="text-sm font-medium">Rate Limit</Label>
                      <p className="text-sm text-gray-600">{apiKey.rateLimit}/hour</p>
                    </div>
                  </div>

                  {apiKey.expiresAt && (
                    <div>
                      <Label className="text-sm font-medium">Expires</Label>
                      <p className="text-sm text-gray-600">
                        {new Date(apiKey.expiresAt).toLocaleDateString()}
                      </p>
                    </div>
                  )}

                  <div>
                    <Label className="text-sm font-medium">Permissions</Label>
                    <div className="flex flex-wrap gap-1 mt-1">
                      {apiKey.permissions.map((permission) => (
                        <Badge key={permission} variant="outline" className="text-xs">
                          {permission}
                        </Badge>
                      ))}
                    </div>
                  </div>

                  <div className="flex items-center justify-between pt-4 border-t">
                    <div className="flex items-center space-x-2">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => regenerateKeyMutation.mutate(apiKey.id)}
                        disabled={regenerateKeyMutation.isPending}
                      >
                        <RefreshCw className="h-4 w-4 mr-1" />
                        Regenerate
                      </Button>
                    </div>
                    <AlertDialog>
                      <AlertDialogTrigger asChild>
                        <Button variant="destructive" size="sm">
                          <Trash2 className="h-4 w-4 mr-1" />
                          Delete
                        </Button>
                      </AlertDialogTrigger>
                      <AlertDialogContent>
                        <AlertDialogHeader>
                          <AlertDialogTitle>Delete API Key</AlertDialogTitle>
                          <AlertDialogDescription>
                            Are you sure you want to delete the API key "{apiKey.name}"? This action cannot be undone and will immediately revoke access for any applications using this key.
                          </AlertDialogDescription>
                        </AlertDialogHeader>
                        <AlertDialogFooter>
                          <AlertDialogCancel>Cancel</AlertDialogCancel>
                          <AlertDialogAction
                            onClick={() => deleteKeyMutation.mutate(apiKey.id)}
                            className="bg-red-600 hover:bg-red-700"
                          >
                            Delete Key
                          </AlertDialogAction>
                        </AlertDialogFooter>
                      </AlertDialogContent>
                    </AlertDialog>
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

export default ApiKeyManagement;
