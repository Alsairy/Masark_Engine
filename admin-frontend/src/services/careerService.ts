import api from './api';

export interface Career {
  career_id: number;
  name: string;
  description: string;
  match_score?: number;
  cluster: {
    name: string;
  };
  ssoc_code: string;
  programs: any[];
  pathways: any[];
}

export interface CareerCluster {
  id: number;
  name: string;
  description?: string;
  careers?: Career[];
}

export interface CareerMatchRequest {
  sessionToken?: string;
  personalityType: string;
  deploymentMode?: string;
  language?: string;
  limit?: number;
  tenantId?: number;
}

export interface CareerStats {
  total_careers: number;
  total_clusters: number;
  cluster_breakdown: any[];
  cache_stats: any;
}

export interface ClusterRating {
  id: number;
  value: number;
  description: string;
}

export const careerService = {
  async getCareerMatches(request: CareerMatchRequest) {
    const response = await api.post('/api/careers/match', request);
    return response.data;
  },

  async searchCareers(query: string, language = 'en', limit = 20) {
    const response = await api.get('/api/careers/search', {
      params: { q: query, language, limit }
    });
    return response.data;
  },

  async getCareerDetails(careerId: number, language = 'en') {
    const response = await api.get(`/api/careers/${careerId}`, {
      params: { language }
    });
    return response.data;
  },

  async getCareersByCluster(clusterId: number, language = 'en') {
    const response = await api.get(`/api/careers/clusters/${clusterId}/careers`, {
      params: { language }
    });
    return response.data;
  },

  async getAllClusters(language = 'en') {
    const response = await api.get('/api/careers/clusters', {
      params: { language }
    });
    return response.data;
  },

  async getCareerStats() {
    const response = await api.get('/api/careers/stats');
    return response.data;
  },

  async getClusterRatings(language = 'en') {
    const response = await api.get('/api/careers/cluster-ratings', {
      params: { language }
    });
    return response.data;
  },

  async rateCareerCluster(assessmentId: number, careerClusterId: number, ratingId: number) {
    const response = await api.post(`/api/careers/assessments/${assessmentId}/career-cluster-ratings`, {
      careerClusterId,
      careerClusterRatingId: ratingId
    });
    return response.data;
  },

  async getAssessmentClusterRatings(assessmentId: number) {
    const response = await api.get(`/api/careers/assessments/${assessmentId}/career-cluster-ratings`);
    return response.data;
  }
};
