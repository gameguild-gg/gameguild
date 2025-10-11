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

export interface StudentAttendanceData {
  id: string;
  name: string;
  email: string;
  team: string;
  block1Sessions: number;
  block2Sessions: number;
  block3Sessions: number;
  block4Sessions: number;
  totalSessions: number;
  gamesTested: number;
  status: 'onTrack' | 'atRisk' | 'failing';
}

export interface SessionAttendanceData {
  id: string;
  sessionName: string;
  date: string;
  location: string;
  totalCapacity: number;
  studentsRegistered: number;
  studentsAttended: number;
  attendanceRate: number;
  gamesTested: number;
}

export const testingLabApi = {
  // Testing Requests
  async getAvailableTestingRequests(accessToken?: string): Promise<TestingRequest[]> {
    try {
      if (accessToken) {
        const response = await apiClient.authenticatedRequest<TestingRequest[]>('/testing/available-for-testing', accessToken);
        return response;
      } else {
        const response = await apiClient.request<TestingRequest[]>('/testing/available-for-testing');
        return response;
      }
    } catch (error) {
      console.error('Error fetching available testing requests:', error);
      throw error;
    }
  },

  async getMyTestingRequests(accessToken?: string): Promise<TestingRequest[]> {
    try {
      if (accessToken) {
        const response = await apiClient.authenticatedRequest<TestingRequest[]>('/testing/my-requests', accessToken);
        return response;
      } else {
        const response = await apiClient.request<TestingRequest[]>('/testing/my-requests');
        return response;
      }
    } catch (error) {
      console.error('Error fetching my testing requests:', error);
      throw error;
    }
  },

  async getTestingRequest(id: string): Promise<TestingRequest> {
    try {
      const response = await apiClient.request<TestingRequest>(`/testing/requests/${id}`);
      return response;
    } catch (error) {
      console.error('Error fetching testing request:', error);
      throw error;
    }
  },

  async createSimpleTestingRequest(data: CreateSimpleTestingRequestDto): Promise<TestingRequest> {
    try {
      const response = await apiClient.request<TestingRequest>('/testing/submit-simple', {
        method: 'POST',
        body: JSON.stringify(data),
      });
      return response;
    } catch (error) {
      console.error('Error creating testing request:', error);
      throw error;
    }
  },

  async submitFeedback(data: SubmitFeedbackDto): Promise<void> {
    try {
      await apiClient.request('/testing/feedback', {
        method: 'POST',
        body: JSON.stringify(data),
      });
    } catch (error) {
      console.error('Error submitting feedback:', error);
      throw error;
    }
  },

  // Testing Sessions
  async getTestingSessions(accessToken?: string): Promise<TestingSession[]> {
    try {
      if (accessToken) {
        const response = await apiClient.authenticatedRequest<TestingSession[]>('/testing/sessions', accessToken);
        return response;
      } else {
        const response = await apiClient.request<TestingSession[]>('/testing/sessions');
        return response;
      }
    } catch (error) {
      console.error('Error fetching testing sessions:', error);
      throw error;
    }
  },

  async getTestingSession(id: string): Promise<TestingSession> {
    try {
      const response = await apiClient.request<TestingSession>(`/testing/sessions/${id}`);
      return response;
    } catch (error) {
      console.error('Error fetching testing session:', error);
      throw error;
    }
  },

  // Attendance Reports
  async getStudentAttendanceReport(): Promise<StudentAttendanceData[]> {
    try {
      const response = await apiClient.request<StudentAttendanceData[]>('/testing/attendance/students');
      return response;
    } catch (error) {
      console.error('Error fetching student attendance report:', error);
      throw error;
    }
  },

  async getSessionAttendanceReport(): Promise<SessionAttendanceData[]> {
    try {
      const response = await apiClient.request<SessionAttendanceData[]>('/testing/attendance/sessions');
      return response;
    } catch (error) {
      console.error('Error fetching session attendance report:', error);
      throw error;
    }
  },

  async updateAttendance(sessionId: string, userId: string, status: string): Promise<void> {
    try {
      await apiClient.request(`/testing/sessions/${sessionId}/attendance`, {
        method: 'POST',
        body: JSON.stringify({
          userId,
          attendanceStatus: status,
        }),
      });
    } catch (error) {
      console.error('Error updating attendance:', error);
      throw error;
    }
  },
};
