import api from './api';

export interface Report {
  filename: string;
  file_path: string;
  file_size_bytes: number;
  report_type: string;
  language: string;
  session_token: string;
  personality_type: string;
  student_name: string;
  generated_at: string;
}

export interface ReportStats {
  total_reports: number;
  total_size_bytes: number;
  total_size_mb: number;
  reports_last_7_days: number;
  reports_by_date: any;
  average_report_size_kb: number;
}

export interface GenerateReportRequest {
  sessionToken: string;
  language?: string;
  reportType?: string;
  includeCareerDetails?: boolean;
}

export interface ReportElement {
  id: number;
  parent_element_id?: number;
  element_type: string;
  title: string;
  content: string;
  order_index: number;
  is_interactive: boolean;
  child_elements?: ReportElement[];
  activity_data?: string;
  graph_data?: string;
}

export interface ReportAnswer {
  id: number;
  report_element_question_id: number;
  assessment_session_id: number;
  answer_text?: string;
  answer_rating?: number;
  answer_boolean?: boolean;
  answer_choice?: string;
  answered_at: string;
}

export interface ReportElementRating {
  id: number;
  report_element_id: number;
  assessment_session_id: number;
  rating: number;
  comment?: string;
  rated_at: string;
}

export interface AchieveWorksReport {
  report_id: number;
  language: string;
  report_type: string;
  personality_type: string;
  student_name: string;
  generated_at: string;
  report_elements: ReportElement[];
  career_matches: any[];
}

export const reportService = {
  async generateReport(request: GenerateReportRequest) {
    const response = await api.post('/api/reports/generate', request);
    return response.data;
  },

  async downloadReport(filename: string) {
    const response = await api.get(`/api/reports/download/${filename}`, {
      responseType: 'blob'
    });
    return response.data;
  },

  async listReports(limit = 50) {
    const response = await api.get('/api/reports/list', {
      params: { limit }
    });
    return response.data;
  },

  async deleteReport(filename: string) {
    const response = await api.delete(`/api/reports/delete/${filename}`);
    return response.data;
  },

  async getSessionReports(sessionToken: string) {
    const response = await api.get(`/api/reports/session/${sessionToken}`);
    return response.data;
  },

  async getReportStats() {
    const response = await api.get('/api/reports/stats');
    return response.data;
  },

  async getReportElements(assessmentId: number, language = 'en') {
    const response = await api.get(`/api/reports/assessments/${assessmentId}/elements`, {
      params: { language }
    });
    return response.data;
  },

  async submitReportAnswer(elementId: number, request: {
    assessmentSessionId: number;
    questionId?: number;
    answerText?: string;
    answerRating?: number;
    answerBoolean?: boolean;
    answerChoice?: string;
  }) {
    const response = await api.post(`/api/reports/elements/${elementId}/answers`, request);
    return response.data;
  },

  async rateReportElement(elementId: number, request: {
    assessmentSessionId: number;
    rating: number;
    comment?: string;
  }) {
    const response = await api.post(`/api/reports/elements/${elementId}/ratings`, request);
    return response.data;
  },

  async getReportFeedback(assessmentId: number) {
    const response = await api.get(`/api/reports/assessments/${assessmentId}/feedback`);
    return response.data;
  },

  async getAchieveWorksReport(reportId: number, language = 'en') {
    const response = await api.get(`/api/achieve_works_reports/${reportId}`, {
      params: { language }
    });
    return response.data;
  },

  async getReportCareers(reportId: number, language = 'en') {
    const response = await api.get(`/api/achieve_works_reports/${reportId}/careers`, {
      params: { language }
    });
    return response.data;
  },

  async getCareerProgramMatches(reportId: number, language = 'en') {
    const response = await api.get(`/api/achieve_works_reports/${reportId}/career_program_matches`, {
      params: { language }
    });
    return response.data;
  },

  async getReportElementRatings(reportId: number) {
    const response = await api.get(`/api/achieve_works_reports/${reportId}/report_element_ratings`);
    return response.data;
  },

  async createReportUserAnswer(request: {
    reportElementQuestionId: number;
    assessmentSessionId: number;
    answerText?: string;
    answerRating?: number;
    answerBoolean?: boolean;
    answerChoice?: string;
  }) {
    const response = await api.post('/api/report_user_answers', request);
    return response.data;
  },

  async updateReportUserAnswer(answerId: number, request: {
    answerText?: string;
    answerRating?: number;
    answerBoolean?: boolean;
    answerChoice?: string;
  }) {
    const response = await api.put(`/api/report_user_answers/${answerId}`, request);
    return response.data;
  },

  async deleteReportUserAnswer(answerId: number) {
    const response = await api.delete(`/api/report_user_answers/${answerId}`);
    return response.data;
  },

  async createReportElementRating(request: {
    reportElementId: number;
    assessmentSessionId: number;
    rating: number;
    comment?: string;
  }) {
    const response = await api.post('/api/report_element_ratings', request);
    return response.data;
  },

  async updateReportElementRating(ratingId: number, request: {
    rating: number;
    comment?: string;
  }) {
    const response = await api.put(`/api/report_element_ratings/${ratingId}`, request);
    return response.data;
  },

  async createCareerUserRating(request: {
    careerId: number;
    userId: number;
    ratingId: number;
    ratingValue: number;
  }) {
    const response = await api.post('/api/career_user_ratings', request);
    return response.data;
  },

  async getCareerUserRating(ratingId: number) {
    const response = await api.get(`/api/career_user_ratings/${ratingId}`);
    return response.data;
  },

  async updateCareerUserRating(ratingId: number, request: {
    ratingId: number;
    ratingValue: number;
  }) {
    const response = await api.put(`/api/career_user_ratings/${ratingId}`, request);
    return response.data;
  },

  async deleteCareerUserRating(ratingId: number) {
    const response = await api.delete(`/api/career_user_ratings/${ratingId}`);
    return response.data;
  }
};
