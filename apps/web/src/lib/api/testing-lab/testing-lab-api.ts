import { apiClient } from '@/lib/api/api-client';

export interface TestingRequest {
  id: string;
  title: string;
  description?: string;
  projectVersionId: string;
  downloadUrl?: string;
  instructionsType: 'inline' | 'file' | 'url';
  instructionsContent?: string;
  instructionsUrl?: string;
  feedbackFormContent?: string;
  maxTesters?: number;
  currentTesterCount: number;
  startDate: string;
  endDate: string;
  status: 'draft' | 'open' | 'inProgress' | 'completed' | 'cancelled';
  createdBy: {
    id: string;
    name: string;
    email: string;
  };
  projectVersion?: {
    id: string;
    versionNumber: string;
    project: {
      id: string;
      title: string;
    };
  };
}

export interface CreateSimpleTestingRequestDto {
  title: string;
  description?: string;
  versionNumber: string;
  downloadUrl?: string;
  instructionsType: 'inline' | 'file' | 'url';
  instructionsContent?: string;
  instructionsUrl?: string;
  feedbackFormContent?: string;
  maxTesters?: number;
  startDate?: string;
  endDate?: string;
  teamIdentifier: string;
}

export interface SubmitFeedbackDto {
  testingRequestId: string;
  feedbackResponses: string; // JSON string
  overallRating?: number;
  wouldRecommend?: boolean;
  additionalNotes?: string;
  sessionId?: string;
}

export interface TestingSession {
  id: string;
  sessionName: string;
  sessionDate: string;
  startTime: string;
  endTime: string;
  maxTesters: number;
  registeredTesterCount: number;
  status: 'scheduled' | 'inProgress' | 'completed' | 'cancelled';
  location: {
    id: string;
    name: string;
    capacity: number;
  };
}

export const testingLabApi = {
  // Testing Requests
  async getAvailableTestingRequests(): Promise<TestingRequest[]> {
    try {
      const response = await apiClient.get('/testing/available-for-testing');
      return response.data;
    } catch (error) {
      console.error('Error fetching available testing requests:', error);
      throw error;
    }
  },

  async getMyTestingRequests(): Promise<TestingRequest[]> {
    try {
      const response = await apiClient.get('/testing/my-requests');
      return response.data;
    } catch (error) {
      console.error('Error fetching my testing requests:', error);
      throw error;
    }
  },

  async getTestingRequest(id: string): Promise<TestingRequest> {
    try {
      const response = await apiClient.get(`/testing/requests/${id}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching testing request:', error);
      throw error;
    }
  },

  async createSimpleTestingRequest(data: CreateSimpleTestingRequestDto): Promise<TestingRequest> {
    try {
      const response = await apiClient.post('/testing/submit-simple', data);
      return response.data;
    } catch (error) {
      console.error('Error creating testing request:', error);
      throw error;
    }
  },

  async submitFeedback(data: SubmitFeedbackDto): Promise<void> {
    try {
      await apiClient.post('/testing/feedback', data);
    } catch (error) {
      console.error('Error submitting feedback:', error);
      throw error;
    }
  },

  // Testing Sessions
  async getTestingSessions(): Promise<TestingSession[]> {
    try {
      const response = await apiClient.get('/testing/sessions');
      return response.data;
    } catch (error) {
      console.error('Error fetching testing sessions:', error);
      throw error;
    }
  },

  async getTestingSession(id: string): Promise<TestingSession> {
    try {
      const response = await apiClient.get(`/testing/sessions/${id}`);
      return response.data;
    } catch (error) {
      console.error('Error fetching testing session:', error);
      throw error;
    }
  },
};
