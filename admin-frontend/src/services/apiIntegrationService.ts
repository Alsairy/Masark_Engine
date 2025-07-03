import api from './api';

export interface ApiKey {
  id: number;
  name: string;
  key: string;
  userId: number;
  userName?: string;
  isActive: boolean;
  permissions: string[];
  rateLimit: number;
  usageCount: number;
  lastUsed?: string;
  expiresAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface ApiUsageStats {
  totalRequests: number;
  successfulRequests: number;
  failedRequests: number;
  averageResponseTime: number;
  requestsLast24Hours: number;
  requestsLast7Days: number;
  requestsLast30Days: number;
  topEndpoints: Array<{
    endpoint: string;
    count: number;
    averageResponseTime: number;
  }>;
  usageByHour: Array<{
    hour: string;
    requests: number;
  }>;
  errorsByType: Array<{
    errorType: string;
    count: number;
  }>;
}

export interface RateLimitConfig {
  id: number;
  name: string;
  requestsPerMinute: number;
  requestsPerHour: number;
  requestsPerDay: number;
  burstLimit: number;
  isActive: boolean;
  appliesTo: 'all' | 'user' | 'apikey';
  targetId?: number;
  createdAt: string;
  updatedAt: string;
}

export interface ApiEndpoint {
  path: string;
  method: string;
  description: string;
  category: string;
  summary: string;
  parameters: Array<{
    name: string;
    type: string;
    required: boolean;
    description: string;
  }>;
  responses: Array<{
    statusCode: number;
    description: string;
    schema?: any;
  }>;
  authentication: boolean;
  rateLimit?: string;
  examples: Array<{
    title: string;
    request: any;
    response: any;
  }>;
}

export interface CreateApiKeyRequest {
  name: string;
  userId: number;
  permissions: string[];
  rateLimit?: number;
  expiresAt?: string;
}

export interface UpdateApiKeyRequest {
  name?: string;
  permissions?: string[];
  rateLimit?: number;
  isActive?: boolean;
  expiresAt?: string;
}

export interface CreateRateLimitRequest {
  name: string;
  requestsPerMinute: number;
  requestsPerHour: number;
  requestsPerDay: number;
  burstLimit: number;
  appliesTo: 'all' | 'user' | 'apikey';
  targetId?: number;
}

export const apiIntegrationService = {
  async getApiKeys(userId?: number) {
    const response = await api.get('/api/api-keys', {
      params: userId ? { userId } : {}
    });
    return response.data;
  },

  async createApiKey(request: CreateApiKeyRequest) {
    const response = await api.post('/api/api-keys', request);
    return response.data;
  },

  async updateApiKey(keyId: number, request: UpdateApiKeyRequest) {
    const response = await api.put(`/api/api-keys/${keyId}`, request);
    return response.data;
  },

  async deleteApiKey(keyId: number) {
    const response = await api.delete(`/api/api-keys/${keyId}`);
    return response.data;
  },

  async regenerateApiKey(keyId: number) {
    const response = await api.post(`/api/api-keys/${keyId}/regenerate`);
    return response.data;
  },

  async toggleApiKey(keyId: number, isActive: boolean) {
    const response = await api.patch(`/api/api-keys/${keyId}/toggle`, { isActive });
    return response.data;
  },

  async getApiUsageStats(timeRange = '7d', userId?: number, apiKeyId?: number) {
    const response = await api.get('/api/api-usage/stats', {
      params: { timeRange, userId, apiKeyId }
    });
    return response.data;
  },

  async getApiUsageHistory(timeRange = '7d', userId?: number, apiKeyId?: number) {
    const response = await api.get('/api/api-usage/history', {
      params: { timeRange, userId, apiKeyId }
    });
    return response.data;
  },

  async getApiUsageByEndpoint(timeRange = '7d') {
    const response = await api.get('/api/api-usage/by-endpoint', {
      params: { timeRange }
    });
    return response.data;
  },

  async getApiErrorLogs(limit = 100, severity?: string) {
    const response = await api.get('/api/api-usage/errors', {
      params: { limit, severity }
    });
    return response.data;
  },

  async getRateLimitConfigs() {
    const response = await api.get('/api/rate-limits');
    return response.data;
  },

  async createRateLimit(request: CreateRateLimitRequest) {
    const response = await api.post('/api/rate-limits', request);
    return response.data;
  },

  async updateRateLimit(configId: number, request: Partial<CreateRateLimitRequest>) {
    const response = await api.put(`/api/rate-limits/${configId}`, request);
    return response.data;
  },

  async deleteRateLimit(configId: number) {
    const response = await api.delete(`/api/rate-limits/${configId}`);
    return response.data;
  },

  async toggleRateLimit(configId: number, isActive: boolean) {
    const response = await api.patch(`/api/rate-limits/${configId}/toggle`, { isActive });
    return response.data;
  },

  async getApiDocumentation() {
    const response = await api.get('/api/documentation');
    return response.data;
  },

  async getApiEndpoints() {
    const response = await api.get('/api/documentation/endpoints');
    return response.data;
  },

  async getApiSchema() {
    const response = await api.get('/api/documentation/schema');
    return response.data;
  },

  async testApiEndpoint(endpoint: string, method: string, data?: any, headers?: any) {
    const response = await api.request({
      url: `/api/test/${endpoint}`,
      method: method.toLowerCase(),
      data,
      headers
    });
    return response.data;
  },

  async getApiHealth() {
    const response = await api.get('/api/health');
    return response.data;
  },

  async getApiMetrics() {
    const response = await api.get('/api/metrics');
    return response.data;
  },

  async getWebhooks(userId?: number) {
    const response = await api.get('/api/webhooks', {
      params: userId ? { userId } : {}
    });
    return response.data;
  },

  async createWebhook(request: {
    url: string;
    events: string[];
    userId: number;
    secret?: string;
    isActive?: boolean;
  }) {
    const response = await api.post('/api/webhooks', request);
    return response.data;
  },

  async updateWebhook(webhookId: number, request: {
    url?: string;
    events?: string[];
    secret?: string;
    isActive?: boolean;
  }) {
    const response = await api.put(`/api/webhooks/${webhookId}`, request);
    return response.data;
  },

  async deleteWebhook(webhookId: number) {
    const response = await api.delete(`/api/webhooks/${webhookId}`);
    return response.data;
  },

  async testWebhook(webhookId: number) {
    const response = await api.post(`/api/webhooks/${webhookId}/test`);
    return response.data;
  }
};
