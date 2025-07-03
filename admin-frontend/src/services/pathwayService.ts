import api from './api';

export interface Pathway {
  id: number;
  nameEn: string;
  nameAr: string;
  descriptionEn: string;
  descriptionAr: string;
  source: 'MOE' | 'MAWHIBA';
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface PathwayCreateRequest {
  nameEn: string;
  nameAr: string;
  descriptionEn: string;
  descriptionAr: string;
  source: 'MOE' | 'MAWHIBA';
  isActive: boolean;
}

export interface PathwayUpdateRequest extends PathwayCreateRequest {
  id: number;
}

export interface PathwayListResponse {
  success: boolean;
  pathways: Pathway[];
  total: number;
  page: number;
  limit: number;
}

export interface PathwayResponse {
  success: boolean;
  pathway: Pathway;
}

export interface PathwayStats {
  totalPathways: number;
  moePathways: number;
  mawhibaPathways: number;
  activePathways: number;
  inactivePathways: number;
}

export const pathwayService = {
  async getPathways(params?: {
    source?: 'MOE' | 'MAWHIBA';
    isActive?: boolean;
    page?: number;
    limit?: number;
    search?: string;
  }): Promise<PathwayListResponse> {
    const queryParams = new URLSearchParams();
    if (params?.source) queryParams.append('source', params.source);
    if (params?.isActive !== undefined) queryParams.append('isActive', params.isActive.toString());
    if (params?.page) queryParams.append('page', params.page.toString());
    if (params?.limit) queryParams.append('limit', params.limit.toString());
    if (params?.search) queryParams.append('search', params.search);

    const response = await api.get(`/pathways?${queryParams.toString()}`);
    return response.data;
  },

  async getPathway(id: number): Promise<PathwayResponse> {
    const response = await api.get(`/pathways/${id}`);
    return response.data;
  },

  async createPathway(pathway: PathwayCreateRequest): Promise<PathwayResponse> {
    const response = await api.post('/pathways', pathway);
    return response.data;
  },

  async updatePathway(pathway: PathwayUpdateRequest): Promise<PathwayResponse> {
    const response = await api.put(`/pathways/${pathway.id}`, pathway);
    return response.data;
  },

  async deletePathway(id: number): Promise<{ success: boolean }> {
    const response = await api.delete(`/pathways/${id}`);
    return response.data;
  },

  async getPathwayStats(): Promise<PathwayStats> {
    const response = await api.get('/pathways/stats');
    return response.data.statistics;
  },

  async getPathwayCareers(pathwayId: number): Promise<any[]> {
    const response = await api.get(`/pathways/${pathwayId}/careers`);
    return response.data.careers;
  },

  async associateCareerWithPathway(pathwayId: number, careerId: number, recommendationScore?: number): Promise<{ success: boolean }> {
    const response = await api.post(`/pathways/${pathwayId}/careers`, {
      careerId,
      recommendationScore: recommendationScore || 0.5
    });
    return response.data;
  },

  async removeCareerFromPathway(pathwayId: number, careerId: number): Promise<{ success: boolean }> {
    const response = await api.delete(`/pathways/${pathwayId}/careers/${careerId}`);
    return response.data;
  }
};
