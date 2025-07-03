import React from 'react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../components/ui/tabs';
import ApiKeyManagement from '../components/api-integration/ApiKeyManagement';
import ApiUsageMonitoring from '../components/api-integration/ApiUsageMonitoring';
import RateLimitingConfig from '../components/api-integration/RateLimitingConfig';
import ApiDocumentation from '../components/api-integration/ApiDocumentation';
import ApiEndpointDocumentation from '../components/api-integration/ApiEndpointDocumentation';

const ApiIntegrationPage: React.FC = () => {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-gray-900">API Integration Management</h1>
        <p className="text-gray-600 mt-2">
          Manage API keys, monitor usage, configure rate limits, and explore API documentation
        </p>
      </div>

      <Tabs defaultValue="keys" className="space-y-6">
        <TabsList className="grid w-full grid-cols-5">
          <TabsTrigger value="keys">API Keys</TabsTrigger>
          <TabsTrigger value="usage">Usage Monitoring</TabsTrigger>
          <TabsTrigger value="limits">Rate Limits</TabsTrigger>
          <TabsTrigger value="endpoints">API Reference</TabsTrigger>
          <TabsTrigger value="docs">Documentation</TabsTrigger>
        </TabsList>

        <TabsContent value="keys">
          <ApiKeyManagement />
        </TabsContent>

        <TabsContent value="usage">
          <ApiUsageMonitoring />
        </TabsContent>

        <TabsContent value="limits">
          <RateLimitingConfig />
        </TabsContent>

        <TabsContent value="endpoints">
          <ApiEndpointDocumentation />
        </TabsContent>

        <TabsContent value="docs">
          <ApiDocumentation />
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default ApiIntegrationPage;
