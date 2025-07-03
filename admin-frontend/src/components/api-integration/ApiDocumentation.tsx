import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { apiIntegrationService, ApiEndpoint } from '../../services/apiIntegrationService';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Badge } from '../ui/badge';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Label } from '../ui/label';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { Textarea } from '../ui/textarea';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Book, Code, Play, Copy, CheckCircle, XCircle, Search, Filter } from 'lucide-react';
import { toast } from 'sonner';

const ApiDocumentation: React.FC = () => {
  const [selectedEndpoint, setSelectedEndpoint] = useState<ApiEndpoint | null>(null);
  const [testRequest, setTestRequest] = useState<any>({});
  const [testResponse, setTestResponse] = useState<any>(null);
  const [testLoading, setTestLoading] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [methodFilter, setMethodFilter] = useState<string>('all');

  const { data: endpoints, isLoading: endpointsLoading } = useQuery({
    queryKey: ['apiEndpoints'],
    queryFn: apiIntegrationService.getApiEndpoints
  });

  const { data: apiSchema, isLoading: schemaLoading } = useQuery({
    queryKey: ['apiSchema'],
    queryFn: apiIntegrationService.getApiSchema
  });

  const { data: apiHealth } = useQuery({
    queryKey: ['apiHealth'],
    queryFn: apiIntegrationService.getApiHealth,
    refetchInterval: 30000
  });

  const filteredEndpoints = endpoints?.filter((endpoint: ApiEndpoint) => {
    const matchesSearch = endpoint.path.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         endpoint.description.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesMethod = methodFilter === 'all' || endpoint.method.toLowerCase() === methodFilter.toLowerCase();
    return matchesSearch && matchesMethod;
  });

  const handleTestEndpoint = async () => {
    if (!selectedEndpoint) return;
    
    setTestLoading(true);
    try {
      const response = await apiIntegrationService.testApiEndpoint(
        selectedEndpoint.path,
        selectedEndpoint.method,
        testRequest,
        { 'Content-Type': 'application/json' }
      );
      setTestResponse(response);
      toast.success('API test completed successfully');
    } catch (error: any) {
      setTestResponse({ error: error.message });
      toast.error(`API test failed: ${error.message}`);
    } finally {
      setTestLoading(false);
    }
  };

  const copyToClipboard = (text: string) => {
    navigator.clipboard.writeText(text);
    toast.success('Copied to clipboard');
  };

  const getMethodColor = (method: string) => {
    switch (method.toUpperCase()) {
      case 'GET': return 'bg-green-100 text-green-800';
      case 'POST': return 'bg-blue-100 text-blue-800';
      case 'PUT': return 'bg-yellow-100 text-yellow-800';
      case 'DELETE': return 'bg-red-100 text-red-800';
      case 'PATCH': return 'bg-purple-100 text-purple-800';
      default: return 'bg-gray-100 text-gray-800';
    }
  };

  const getStatusColor = (statusCode: number) => {
    if (statusCode >= 200 && statusCode < 300) return 'text-green-600';
    if (statusCode >= 400 && statusCode < 500) return 'text-yellow-600';
    if (statusCode >= 500) return 'text-red-600';
    return 'text-gray-600';
  };

  if (endpointsLoading || schemaLoading) {
    return (
      <div className="flex items-center justify-center p-8">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold text-gray-900">API Documentation</h2>
          <p className="text-gray-600">Explore and test the Masark API endpoints</p>
        </div>
        <div className="flex items-center space-x-2">
          <Badge variant={apiHealth?.status === 'healthy' ? 'default' : 'destructive'}>
            {apiHealth?.status === 'healthy' ? (
              <>
                <CheckCircle className="h-3 w-3 mr-1" />
                API Healthy
              </>
            ) : (
              <>
                <XCircle className="h-3 w-3 mr-1" />
                API Issues
              </>
            )}
          </Badge>
          <Button variant="outline" onClick={() => window.location.reload()}>
            Refresh
          </Button>
        </div>
      </div>

      <Tabs defaultValue="endpoints" className="space-y-6">
        <TabsList>
          <TabsTrigger value="endpoints">Endpoints</TabsTrigger>
          <TabsTrigger value="schema">Schema</TabsTrigger>
          <TabsTrigger value="testing">API Testing</TabsTrigger>
        </TabsList>

        <TabsContent value="endpoints" className="space-y-6">
          <div className="flex items-center space-x-4">
            <div className="flex-1">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
                <Input
                  placeholder="Search endpoints..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-10"
                />
              </div>
            </div>
            <Select value={methodFilter} onValueChange={setMethodFilter}>
              <SelectTrigger className="w-32">
                <Filter className="h-4 w-4 mr-2" />
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Methods</SelectItem>
                <SelectItem value="get">GET</SelectItem>
                <SelectItem value="post">POST</SelectItem>
                <SelectItem value="put">PUT</SelectItem>
                <SelectItem value="delete">DELETE</SelectItem>
                <SelectItem value="patch">PATCH</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="grid gap-4">
            {filteredEndpoints?.length === 0 ? (
              <Card>
                <CardContent className="flex flex-col items-center justify-center py-12">
                  <Book className="h-12 w-12 text-gray-400 mb-4" />
                  <h3 className="text-lg font-medium text-gray-900 mb-2">No Endpoints Found</h3>
                  <p className="text-gray-600 text-center">
                    {searchTerm || methodFilter !== 'all' 
                      ? 'No endpoints match your search criteria'
                      : 'API documentation is not available'
                    }
                  </p>
                </CardContent>
              </Card>
            ) : (
              filteredEndpoints?.map((endpoint: ApiEndpoint, index: number) => (
                <Card key={index} className="hover:shadow-md transition-shadow">
                  <CardHeader>
                    <div className="flex items-center justify-between">
                      <div className="flex items-center space-x-3">
                        <Badge className={getMethodColor(endpoint.method)}>
                          {endpoint.method.toUpperCase()}
                        </Badge>
                        <div>
                          <CardTitle className="text-lg font-mono">{endpoint.path}</CardTitle>
                          <CardDescription>{endpoint.description}</CardDescription>
                        </div>
                      </div>
                      <div className="flex items-center space-x-2">
                        {endpoint.authentication && (
                          <Badge variant="outline">Auth Required</Badge>
                        )}
                        {endpoint.rateLimit && (
                          <Badge variant="outline">Rate Limited</Badge>
                        )}
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => setSelectedEndpoint(endpoint)}
                        >
                          <Play className="h-4 w-4 mr-1" />
                          Test
                        </Button>
                      </div>
                    </div>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-4">
                      {endpoint.parameters?.length > 0 && (
                        <div>
                          <Label className="text-sm font-medium">Parameters</Label>
                          <div className="mt-2 space-y-2">
                            {endpoint.parameters.map((param, paramIndex) => (
                              <div key={paramIndex} className="flex items-center justify-between p-2 bg-gray-50 rounded">
                                <div className="flex items-center space-x-2">
                                  <code className="text-sm font-mono">{param.name}</code>
                                  <Badge variant={param.required ? 'destructive' : 'secondary'} className="text-xs">
                                    {param.required ? 'Required' : 'Optional'}
                                  </Badge>
                                  <span className="text-xs text-gray-500">{param.type}</span>
                                </div>
                                <span className="text-sm text-gray-600">{param.description}</span>
                              </div>
                            ))}
                          </div>
                        </div>
                      )}

                      {endpoint.responses?.length > 0 && (
                        <div>
                          <Label className="text-sm font-medium">Responses</Label>
                          <div className="mt-2 space-y-2">
                            {endpoint.responses.map((response, responseIndex) => (
                              <div key={responseIndex} className="flex items-center justify-between p-2 bg-gray-50 rounded">
                                <div className="flex items-center space-x-2">
                                  <Badge className={`${getStatusColor(response.statusCode)} bg-transparent border`}>
                                    {response.statusCode}
                                  </Badge>
                                </div>
                                <span className="text-sm text-gray-600">{response.description}</span>
                              </div>
                            ))}
                          </div>
                        </div>
                      )}

                      {endpoint.examples?.length > 0 && (
                        <div>
                          <Label className="text-sm font-medium">Examples</Label>
                          <div className="mt-2 space-y-2">
                            {endpoint.examples.map((example, exampleIndex) => (
                              <div key={exampleIndex} className="p-3 bg-gray-50 rounded">
                                <div className="flex items-center justify-between mb-2">
                                  <span className="text-sm font-medium">{example.title}</span>
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    onClick={() => copyToClipboard(JSON.stringify(example.request, null, 2))}
                                  >
                                    <Copy className="h-3 w-3" />
                                  </Button>
                                </div>
                                <pre className="text-xs bg-white p-2 rounded border overflow-x-auto">
                                  <code>{JSON.stringify(example.request, null, 2)}</code>
                                </pre>
                              </div>
                            ))}
                          </div>
                        </div>
                      )}
                    </div>
                  </CardContent>
                </Card>
              ))
            )}
          </div>
        </TabsContent>

        <TabsContent value="schema" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>API Schema</CardTitle>
              <CardDescription>Complete API schema definition</CardDescription>
            </CardHeader>
            <CardContent>
              {apiSchema ? (
                <div className="space-y-4">
                  <div className="flex items-center justify-between">
                    <span className="text-sm font-medium">Schema Version: {apiSchema.version || 'N/A'}</span>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => copyToClipboard(JSON.stringify(apiSchema, null, 2))}
                    >
                      <Copy className="h-4 w-4 mr-1" />
                      Copy Schema
                    </Button>
                  </div>
                  <pre className="bg-gray-50 p-4 rounded border overflow-x-auto text-sm">
                    <code>{JSON.stringify(apiSchema, null, 2)}</code>
                  </pre>
                </div>
              ) : (
                <p className="text-gray-500 text-center py-8">API schema not available</p>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="testing" className="space-y-6">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <Card>
              <CardHeader>
                <CardTitle>Test API Endpoint</CardTitle>
                <CardDescription>
                  {selectedEndpoint 
                    ? `Testing ${selectedEndpoint.method.toUpperCase()} ${selectedEndpoint.path}`
                    : 'Select an endpoint from the Endpoints tab to test'
                  }
                </CardDescription>
              </CardHeader>
              <CardContent>
                {selectedEndpoint ? (
                  <div className="space-y-4">
                    <div>
                      <Label htmlFor="testRequest">Request Body (JSON)</Label>
                      <Textarea
                        id="testRequest"
                        value={JSON.stringify(testRequest, null, 2)}
                        onChange={(e) => {
                          try {
                            setTestRequest(JSON.parse(e.target.value || '{}'));
                          } catch {
                          }
                        }}
                        placeholder="Enter request body as JSON"
                        rows={8}
                        className="font-mono text-sm"
                      />
                    </div>
                    <Button 
                      onClick={handleTestEndpoint}
                      disabled={testLoading}
                      className="w-full"
                    >
                      {testLoading ? (
                        <>
                          <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2"></div>
                          Testing...
                        </>
                      ) : (
                        <>
                          <Play className="h-4 w-4 mr-2" />
                          Test Endpoint
                        </>
                      )}
                    </Button>
                  </div>
                ) : (
                  <div className="text-center py-8">
                    <Code className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                    <p className="text-gray-500">Select an endpoint to start testing</p>
                  </div>
                )}
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Response</CardTitle>
                <CardDescription>API response will appear here</CardDescription>
              </CardHeader>
              <CardContent>
                {testResponse ? (
                  <div className="space-y-4">
                    <div className="flex items-center justify-between">
                      <span className="text-sm font-medium">
                        Status: {testResponse.error ? 'Error' : 'Success'}
                      </span>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => copyToClipboard(JSON.stringify(testResponse, null, 2))}
                      >
                        <Copy className="h-4 w-4 mr-1" />
                        Copy
                      </Button>
                    </div>
                    <pre className={`p-4 rounded border overflow-x-auto text-sm ${
                      testResponse.error ? 'bg-red-50 border-red-200' : 'bg-green-50 border-green-200'
                    }`}>
                      <code>{JSON.stringify(testResponse, null, 2)}</code>
                    </pre>
                  </div>
                ) : (
                  <div className="text-center py-8">
                    <div className="text-gray-400 mb-2">No response yet</div>
                    <p className="text-sm text-gray-500">Test an endpoint to see the response</p>
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default ApiDocumentation;
