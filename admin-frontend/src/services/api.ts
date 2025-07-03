import axios from 'axios';
import { LoginRequest, LoginResponse, User, CreateUserRequest, UpdateUserRequest } from '../types/auth';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5282';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('authToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('authToken');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export const authApi = {
  login: async (credentials: LoginRequest): Promise<LoginResponse> => {
    const response = await api.post('/api/auth/login', credentials);
    return response.data;
  },
  
  getCurrentUser: async () => {
    const response = await api.get('/api/auth/me');
    return response.data;
  },
  
  logout: async () => {
    const response = await api.post('/api/auth/logout');
    return response.data;
  },
};

export const userApi = {
  getUsers: async (): Promise<User[]> => {
    const response = await api.get('/api/auth/users');
    return response.data.users || [];
  },
  
  createUser: async (userData: CreateUserRequest): Promise<User> => {
    const response = await api.post('/api/auth/users', userData);
    return response.data.user;
  },
  
  updateUser: async (userData: UpdateUserRequest): Promise<User> => {
    const response = await api.put(`/api/user/users/${userData.id}`, userData);
    return response.data.user;
  },
  
  deleteUser: async (userId: string): Promise<void> => {
    await api.delete(`/api/user/users/${userId}`);
  },
  
  deactivateUser: async (userId: string): Promise<void> => {
    await api.post(`/api/auth/users/${userId}/deactivate`);
  },
};

export const systemApi = {
  getSystemInfo: async () => {
    const response = await api.get('/api/system/info');
    return response.data;
  },
  
  getHealthCheck: async () => {
    const response = await api.get('/api/system/health');
    return response.data;
  },
  
  getSystemConfig: async () => {
    const response = await api.get('/api/system/config');
    return response.data;
  },
  
  getPersonalityTypes: async (language = 'en') => {
    const response = await api.get(`/api/system/personality-types?language=${language}`);
    return response.data;
  },
  
  getCareerClusters: async (language = 'en') => {
    const response = await api.get(`/api/system/career-clusters?language=${language}`);
    return response.data;
  },
  
  getPathways: async (language = 'en', source?: string) => {
    const response = await api.get(`/api/system/pathways?language=${language}${source ? `&source=${source}` : ''}`);
    return response.data;
  },
  
  updateSystemConfig: async (configData: any) => {
    const response = await api.put('/api/system/config', configData);
    return response.data;
  },
};

export const assessmentApi = {
  getAssessmentStatistics: async () => {
    const response = await api.get('/api/assessment/statistics');
    return response.data;
  },
  
  getRecentSessions: async (params: { search?: string; status?: string; limit?: number }) => {
    const queryParams = new URLSearchParams();
    if (params.search) queryParams.append('search', params.search);
    if (params.status && params.status !== 'all') queryParams.append('status', params.status);
    if (params.limit) queryParams.append('limit', params.limit.toString());
    
    const response = await api.get(`/api/assessment/sessions?${queryParams.toString()}`);
    return response.data;
  },
  
  getHealthCheck: async () => {
    const response = await api.get('/api/assessment/health');
    return response.data;
  },
  
  getAssessmentResults: async (sessionId: number, includeStatistics = false) => {
    const response = await api.get(`/api/assessment/results/${sessionId}?includeStatistics=${includeStatistics}`);
    return response.data;
  },
  
  getAssessmentState: async (sessionId: number) => {
    const response = await api.get(`/api/assessment/${sessionId}/state`);
    return response.data;
  },
  
  getQuestions: async (language = 'en') => {
    const response = await api.get(`/api/assessment/questions?language=${language}`);
    return response.data.questions || [];
  },
  
  createQuestion: async (questionData: any) => {
    const response = await api.post('/api/assessment/questions', questionData);
    return response.data;
  },
  
  updateQuestion: async (id: number, questionData: any) => {
    const response = await api.put(`/api/assessment/questions/${id}`, questionData);
    return response.data;
  },
  
  deleteQuestion: async (id: number) => {
    const response = await api.delete(`/api/assessment/questions/${id}`);
    return response.data;
  },
  
  updateSystemConfig: async (configData: any) => {
    const response = await api.put('/api/system/config', configData);
    return response.data;
  },
};

export default api;
