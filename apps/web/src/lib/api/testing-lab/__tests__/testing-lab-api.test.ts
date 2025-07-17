import { testingLabApi } from '@/lib/api/testing-lab/testing-lab-api';
import { apiClient } from '@/lib/api/api-client';

// Mock the API client
jest.mock('@/lib/api/api-client');
const mockApiClient = apiClient as jest.Mocked<typeof apiClient>;

describe('Testing Lab API', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('getAvailableTestingRequests', () => {
    it('should fetch available testing requests successfully', async () => {
      const mockRequests = [
        {
          id: '1',
          title: 'Alpha Build Testing',
          description: 'Test the core gameplay',
          projectVersionId: 'pv1',
          downloadUrl: 'https://example.com/game.zip',
          instructionsType: 'inline' as const,
          instructionsContent: 'Test thoroughly',
          feedbackFormContent: 'Rate the game',
          maxTesters: 8,
          currentTesterCount: 3,
          startDate: '2024-01-15',
          endDate: '2024-01-22',
          status: 'open' as const,
          createdBy: {
            id: 'u1',
            name: 'John Developer',
            email: 'john.dev@mymail.champlain.edu',
          },
          projectVersion: {
            id: 'pv1',
            versionNumber: 'v0.1.0-alpha',
            project: {
              id: 'p1',
              title: 'fa23-capstone-2023-24-t01',
            },
          },
        },
      ];

      mockApiClient.request.mockResolvedValueOnce(mockRequests);

      const result = await testingLabApi.getAvailableTestingRequests();

      expect(mockApiClient.request).toHaveBeenCalledWith('/testing/available-for-testing');
      expect(result).toEqual(mockRequests);
    });

    it('should handle API errors gracefully', async () => {
      const errorMessage = 'Network error';
      mockApiClient.request.mockRejectedValueOnce(new Error(errorMessage));

      await expect(testingLabApi.getAvailableTestingRequests()).rejects.toThrow(errorMessage);
    });
  });

  describe('createSimpleTestingRequest', () => {
    it('should create a testing request with required fields only', async () => {
      const requestData = {
        title: 'New Game Test',
        versionNumber: 'v1.0.0',
        downloadUrl: 'https://example.com/game.zip',
        instructionsType: 'inline' as const,
        instructionsContent: 'Test the game',
        feedbackFormContent: 'Please provide feedback',
        teamIdentifier: 'fa23-capstone-2023-24-t01',
      };

      const mockResponse = {
        id: '123',
        ...requestData,
        status: 'draft' as const,
        currentTesterCount: 0,
        startDate: '2024-01-15',
        endDate: '2024-01-22',
        createdBy: {
          id: 'u1',
          name: 'John Developer',
          email: 'john.dev@mymail.champlain.edu',
        },
        projectVersionId: 'pv1',
      };

      mockApiClient.request.mockResolvedValueOnce(mockResponse);

      const result = await testingLabApi.createSimpleTestingRequest(requestData);

      expect(mockApiClient.request).toHaveBeenCalledWith('/testing/submit-simple', {
        method: 'POST',
        body: JSON.stringify(requestData),
      });
      expect(result).toEqual(mockResponse);
    });
  });

  describe('submitFeedback', () => {
    it('should submit feedback successfully', async () => {
      const feedbackData = {
        testingRequestId: '123',
        feedbackResponses: JSON.stringify({ rating: 5, comments: 'Great game!' }),
        overallRating: 5,
        wouldRecommend: true,
        additionalNotes: 'Loved the graphics',
      };

      mockApiClient.request.mockResolvedValueOnce(undefined);

      await testingLabApi.submitFeedback(feedbackData);

      expect(mockApiClient.request).toHaveBeenCalledWith('/testing/feedback', {
        method: 'POST',
        body: JSON.stringify(feedbackData),
      });
    });
  });

  describe('getStudentAttendanceReport', () => {
    it('should fetch student attendance report', async () => {
      const mockAttendanceData = [
        {
          id: '1',
          name: 'John Developer',
          email: 'john.dev@mymail.champlain.edu',
          team: 'fa23-capstone-2023-24-t01',
          block1Sessions: 2,
          block2Sessions: 1,
          block3Sessions: 0,
          block4Sessions: 0,
          totalSessions: 3,
          gamesTested: 8,
          status: 'onTrack' as const,
        },
      ];

      mockApiClient.request.mockResolvedValueOnce(mockAttendanceData);

      const result = await testingLabApi.getStudentAttendanceReport();

      expect(mockApiClient.request).toHaveBeenCalledWith('/testing/attendance/students');
      expect(result).toEqual(mockAttendanceData);
    });
  });

  describe('updateAttendance', () => {
    it('should update session attendance', async () => {
      const sessionId = 'session-123';
      const userId = 'user-456';
      const status = 'completed';

      mockApiClient.request.mockResolvedValueOnce(undefined);

      await testingLabApi.updateAttendance(sessionId, userId, status);

      expect(mockApiClient.request).toHaveBeenCalledWith(`/testing/sessions/${sessionId}/attendance`, {
        method: 'POST',
        body: JSON.stringify({
          userId,
          attendanceStatus: status,
        }),
      });
    });
  });
});
