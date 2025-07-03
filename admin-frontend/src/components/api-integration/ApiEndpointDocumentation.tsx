import React, { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Input } from '@/components/ui/input';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { AlertCircle, Search, Code, Play, Book, Globe, Lock } from 'lucide-react';
import { apiIntegrationService, ApiEndpoint } from '@/services/apiIntegrationService';

interface ApiEndpointDocumentationProps {
  className?: string;
}

export const ApiEndpointDocumentation: React.FC<ApiEndpointDocumentationProps> = ({ className }) => {
  const [documentation, setDocumentation] = useState<any | null>(null);
  const [endpoints, setEndpoints] = useState<ApiEndpoint[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>('all');
  const [selectedEndpoint, setSelectedEndpoint] = useState<ApiEndpoint | null>(null);
  const [testResult, setTestResult] = useState<any>(null);
  const [testLoading, setTestLoading] = useState(false);

  const fetchDocumentation = async () => {
    try {
      setLoading(true);
      setError(null);

      const [docResponse, endpointsResponse] = await Promise.all([
        apiIntegrationService.getApiDocumentation(),
        apiIntegrationService.getApiEndpoints()
      ]);

      setDocumentation(docResponse);
      setEndpoints(endpointsResponse);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load API documentation');
      console.error('Error fetching API documentation:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchDocumentation();
  }, []);

  const filteredEndpoints = endpoints.filter(endpoint => {
    const matchesSearch = endpoint.path.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         endpoint.description.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesCategory = selectedCategory === 'all' || endpoint.category === selectedCategory;
    return matchesSearch && matchesCategory;
  });

  const categories = [...new Set(endpoints.map(e => e.category))];

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

  const testEndpoint = async (endpoint: ApiEndpoint) => {
    try {
      setTestLoading(true);
      const result = await apiIntegrationService.testApiEndpoint(endpoint.path, endpoint.method);
      setTestResult(result);
    } catch (err) {
      setTestResult({ error: err instanceof Error ? err.message : 'Test failed' });
    } finally {
      setTestLoading(false);
    }
  };

  if (loading) {
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
              <span>{error}</span>
            </div>
            <Button onClick={fetchDocumentation} className="mt-4">
              Retry
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className={`space-y-6 ${className}`}>
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center space-y-4 sm:space-y-0">
        <div>
          <h2 className="text-2xl font-bold">API Documentation</h2>
          <p className="text-gray-600">Comprehensive API reference and testing interface</p>
        </div>
        
        <div className="flex flex-col sm:flex-row space-y-2 sm:space-y-0 sm:space-x-4">
          <div className="flex items-center space-x-2">
            <Search className="h-4 w-4 text-gray-400" />
            <Input
              placeholder="Search endpoints..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-64"
            />
          </div>
          
          <Select value={selectedCategory} onValueChange={setSelectedCategory}>
            <SelectTrigger className="w-40">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Categories</SelectItem>
              {categories.map(category => (
                <SelectItem key={category} value={category}>
                  {category.charAt(0).toUpperCase() + category.slice(1)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      {documentation && (
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center space-x-2">
              <Book className="h-5 w-5" />
              <span>API Overview</span>
            </CardTitle>
            <CardDescription>
              {documentation.description}
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="flex items-center space-x-2">
                <Globe className="h-4 w-4 text-blue-600" />
                <div>
                  <p className="font-medium">Base URL</p>
                  <p className="text-sm text-gray-600">{documentation.baseUrl}</p>
                </div>
              </div>
              
              <div className="flex items-center space-x-2">
                <Code className="h-4 w-4 text-green-600" />
                <div>
                  <p className="font-medium">Version</p>
                  <p className="text-sm text-gray-600">{documentation.version}</p>
                </div>
              </div>
              
              <div className="flex items-center space-x-2">
                <Lock className="h-4 w-4 text-yellow-600" />
                <div>
                  <p className="font-medium">Authentication</p>
                  <p className="text-sm text-gray-600">API Key Required</p>
                </div>
              </div>
            </div>
          </CardContent>
        </Card>
      )}

      <Tabs defaultValue="endpoints" className="space-y-4">
        <TabsList>
          <TabsTrigger value="endpoints">Endpoints</TabsTrigger>
          <TabsTrigger value="testing">API Testing</TabsTrigger>
          <TabsTrigger value="examples">Code Examples</TabsTrigger>
        </TabsList>

        <TabsContent value="endpoints" className="space-y-4">
          <div className="space-y-4">
            {filteredEndpoints.length === 0 ? (
              <Card>
                <CardContent className="p-6">
                  <p className="text-gray-500 text-center">No endpoints found matching your criteria</p>
                </CardContent>
              </Card>
            ) : (
              filteredEndpoints.map((endpoint) => (
                <Card key={`${endpoint.method}-${endpoint.path}`} className="cursor-pointer hover:shadow-md transition-shadow">
                  <CardContent className="p-6">
                    <div className="flex items-start justify-between">
                      <div className="flex-1">
                        <div className="flex items-center space-x-3 mb-2">
                          <Badge className={getMethodColor(endpoint.method)}>
                            {endpoint.method.toUpperCase()}
                          </Badge>
                          <code className="text-sm font-mono bg-gray-100 px-2 py-1 rounded">
                            {endpoint.path}
                          </code>
                          <Badge variant="outline">{endpoint.category}</Badge>
                        </div>
                        
                        <h3 className="font-semibold mb-1">{endpoint.summary}</h3>
                        <p className="text-gray-600 text-sm mb-3">{endpoint.description}</p>
                        
                        {endpoint.parameters && endpoint.parameters.length > 0 && (
                          <div className="mb-3">
                            <h4 className="font-medium text-sm mb-2">Parameters:</h4>
                            <div className="space-y-1">
                              {endpoint.parameters.map((param, index) => (
                                <div key={index} className="flex items-center space-x-2 text-sm">
                                  <code className="bg-gray-100 px-1 rounded">{param.name}</code>
                                  <Badge variant={param.required ? 'default' : 'secondary'} className="text-xs">
                                    {param.required ? 'Required' : 'Optional'}
                                  </Badge>
                                  <span className="text-gray-600">{param.type}</span>
                                  {param.description && (
                                    <span className="text-gray-500">- {param.description}</span>
                                  )}
                                </div>
                              ))}
                            </div>
                          </div>
                        )}
                        
                        {endpoint.responses && (
                          <div>
                            <h4 className="font-medium text-sm mb-2">Responses:</h4>
                            <div className="space-y-1">
                              {Object.entries(endpoint.responses).map(([status, response]) => (
                                <div key={status} className="flex items-center space-x-2 text-sm">
                                  <Badge variant={status.startsWith('2') ? 'default' : 'destructive'} className="text-xs">
                                    {status}
                                  </Badge>
                                  <span className="text-gray-600">{response.description}</span>
                                </div>
                              ))}
                            </div>
                          </div>
                        )}
                      </div>
                      
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => setSelectedEndpoint(endpoint)}
                        className="ml-4"
                      >
                        <Play className="h-4 w-4 mr-1" />
                        Test
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              ))
            )}
          </div>
        </TabsContent>

        <TabsContent value="testing" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>API Testing Interface</CardTitle>
              <CardDescription>
                Test API endpoints directly from the documentation
              </CardDescription>
            </CardHeader>
            <CardContent>
              {selectedEndpoint ? (
                <div className="space-y-4">
                  <div className="flex items-center space-x-3">
                    <Badge className={getMethodColor(selectedEndpoint.method)}>
                      {selectedEndpoint.method.toUpperCase()}
                    </Badge>
                    <code className="text-sm font-mono bg-gray-100 px-2 py-1 rounded">
                      {selectedEndpoint.path}
                    </code>
                  </div>
                  
                  <div>
                    <h4 className="font-medium mb-2">{selectedEndpoint.summary}</h4>
                    <p className="text-gray-600 text-sm">{selectedEndpoint.description}</p>
                  </div>
                  
                  <div className="flex space-x-2">
                    <Button
                      onClick={() => testEndpoint(selectedEndpoint)}
                      disabled={testLoading}
                      className="flex items-center space-x-2"
                    >
                      <Play className="h-4 w-4" />
                      <span>{testLoading ? 'Testing...' : 'Test Endpoint'}</span>
                    </Button>
                    <Button
                      variant="outline"
                      onClick={() => setSelectedEndpoint(null)}
                    >
                      Clear
                    </Button>
                  </div>
                  
                  {testResult && (
                    <div className="mt-4">
                      <h4 className="font-medium mb-2">Test Result:</h4>
                      <pre className="bg-gray-100 p-3 rounded text-sm overflow-auto">
                        {JSON.stringify(testResult, null, 2)}
                      </pre>
                    </div>
                  )}
                </div>
              ) : (
                <p className="text-gray-500">Select an endpoint from the list above to test it</p>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="examples" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>Code Examples</CardTitle>
              <CardDescription>
                Sample code for integrating with the Masark API
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-6">
                <div>
                  <h4 className="font-medium mb-2">Authentication</h4>
                  <pre className="bg-gray-100 p-3 rounded text-sm overflow-auto">
{`// JavaScript/TypeScript
const apiKey = 'your-api-key-here';
const headers = {
  'Authorization': \`Bearer \${apiKey}\`,
  'Content-Type': 'application/json'
};

curl -H "Authorization: Bearer your-api-key-here" \\
     -H "Content-Type: application/json" \\
     https://api.masark.com/api/endpoint`}
                  </pre>
                </div>
                
                <div>
                  <h4 className="font-medium mb-2">Start Assessment Session</h4>
                  <pre className="bg-gray-100 p-3 rounded text-sm overflow-auto">
{`// JavaScript/TypeScript
const response = await fetch('/api/assessment/start', {
  method: 'POST',
  headers: headers,
  body: JSON.stringify({
    userId: 123,
    language: 'en'
  })
});

const session = await response.json();
console.log('Session ID:', session.sessionId);`}
                  </pre>
                </div>
                
                <div>
                  <h4 className="font-medium mb-2">Submit Answer</h4>
                  <pre className="bg-gray-100 p-3 rounded text-sm overflow-auto">
{`// JavaScript/TypeScript
const response = await fetch('/api/assessment/answer', {
  method: 'POST',
  headers: headers,
  body: JSON.stringify({
    sessionId: 'session-uuid',
    questionId: 1,
    selectedOption: 'A'
  })
});

const result = await response.json();`}
                  </pre>
                </div>
                
                <div>
                  <h4 className="font-medium mb-2">Get Career Matches</h4>
                  <pre className="bg-gray-100 p-3 rounded text-sm overflow-auto">
{`// JavaScript/TypeScript
const response = await fetch('/api/careers/matches/user-123', {
  method: 'GET',
  headers: headers
});

const matches = await response.json();
console.log('Career matches:', matches);`}
                  </pre>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default ApiEndpointDocumentation;
