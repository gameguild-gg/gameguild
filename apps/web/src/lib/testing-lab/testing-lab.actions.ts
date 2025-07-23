'use server';

import { revalidateTag } from 'next/cache';
import { auth } from '@/auth';
import { environment } from '@/configs/environment';
import {
  getTestingRequests,
  postTestingRequests,
  getTestingRequestsById,
  putTestingRequestsById,
  deleteTestingRequestsById,
  getTestingSessions,
  postTestingSessions,
  getTestingSessionsById,
  putTestingSessionsById,
  deleteTestingSessionsById,
  getTestingRequestsByProjectVersionByProjectVersionId,
  getTestingRequestsByCreatorByCreatorId,
  getTestingRequestsByStatusByStatus,
  getTestingRequestsSearch,
  getTestingSessionsByRequestByTestingRequestId,
  getTestingSessionsByLocationByLocationId,
  getTestingSessionsByStatusByStatus,
  getTestingSessionsByManagerByManagerId,
  getTestingSessionsSearch,
  getTestingRequestsByRequestIdParticipants,
  postTestingRequestsByRequestIdParticipantsByUserId,
  deleteTestingRequestsByRequestIdParticipantsByUserId,
  getTestingRequestsByRequestIdParticipantsByUserIdCheck,
  getTestingRequestsByRequestIdFeedback,
  postTestingRequestsByRequestIdFeedback,
  getTestingRequestsByRequestIdStatistics,
} from '@/lib/api/generated/sdk.gen';
import type {
  TestingRequest,
  TestingSession,
  PostTestingRequestsData,
  PutTestingRequestsByIdData,
  PostTestingSessionsData,
  PutTestingSessionsByIdData,
} from '@/lib/api/generated/types.gen';

// Get all testing requests
export async function getTestingRequestsData(params?: {
  status?: string;
  projectVersionId?: string;
  creatorId?: string;
  skip?: number;
  take?: number;
}) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await getTestingRequests({
      baseUrl: environment.apiBaseUrl,
      query: params as any,
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch testing requests');
    }

    return {
      testingRequests: response.data,
      total: response.data.length,
    };
  } catch (error) {
    console.error('Error fetching testing requests:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch testing requests');
  }
}

// Get a single testing request by ID
export async function getTestingRequestById(id: string) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await getTestingRequestsById({
      path: { id },
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Testing request not found');
    }

    return response.data as TestingRequest;
  } catch (error) {
    console.error('Error fetching testing request:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch testing request');
  }
}

// Create a new testing request
export async function createTestingRequest(requestData: {
  title: string;
  description?: string;
  projectVersionId: string;
  downloadUrl?: string;
  instructionsType: 'inline' | 'file' | 'url';
  instructionsContent?: string;
  instructionsUrl?: string;
  feedbackFormContent?: string;
  maxTesters?: number;
  startDate: string;
  endDate: string;
}) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await postTestingRequests({
      baseUrl: environment.apiBaseUrl,
      body: requestData as PostTestingRequestsData['body'],
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.data) {
      throw new Error('Failed to create testing request');
    }

    // Revalidate testing requests cache
    revalidateTag('testing-requests');

    return response.data as TestingRequest;
  } catch (error) {
    console.error('Error creating testing request:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to create testing request');
  }
}

// Update an existing testing request
export async function updateTestingRequest(
  id: string,
  requestData: {
    title?: string;
    description?: string;
    downloadUrl?: string;
    instructionsType?: 'inline' | 'file' | 'url';
    instructionsContent?: string;
    instructionsUrl?: string;
    feedbackFormContent?: string;
    maxTesters?: number;
    startDate?: string;
    endDate?: string;
    status?: number;
  },
) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await putTestingRequestsById({
      baseUrl: environment.apiBaseUrl,
      path: { id },
      body: requestData as PutTestingRequestsByIdData['body'],
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.data) {
      throw new Error('Failed to update testing request');
    }

    // Revalidate testing requests cache
    revalidateTag('testing-requests');

    return response.data as TestingRequest;
  } catch (error) {
    console.error('Error updating testing request:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to update testing request');
  }
}

// Delete a testing request
export async function deleteTestingRequest(id: string) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    await deleteTestingRequestsById({
      baseUrl: environment.apiBaseUrl,
      path: { id },
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
      },
    });

    // Revalidate testing requests cache
    revalidateTag('testing-requests');

    return { success: true };
  } catch (error) {
    console.error('Error deleting testing request:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to delete testing request');
  }
}

// Get all testing sessions
export async function getTestingSessionsData(params?: {
  testingRequestId?: string;
  locationId?: string;
  managerId?: string;
  status?: string;
  skip?: number;
  take?: number;
}) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await getTestingSessions({
      baseUrl: environment.apiBaseUrl,
      query: params as any,
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch testing sessions');
    }

    return {
      testingSessions: response.data,
      total: response.data.length,
    };
  } catch (error) {
    console.error('Error fetching testing sessions:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch testing sessions');
  }
}

// Get a single testing session by ID
export async function getTestingSessionById(id: string) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await getTestingSessionsById({
      path: { id },
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Testing session not found');
    }

    return response.data as TestingSession;
  } catch (error) {
    console.error('Error fetching testing session:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch testing session');
  }
}

// Create a new testing session
export async function createTestingSession(sessionData: {
  testingRequestId: string;
  locationId?: string;
  sessionName: string;
  sessionDate: string;
  startTime: string;
  endTime: string;
  maxTesters: number;
  managerId?: string;
}) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    // Convert time format to ISO datetime strings
    const sessionDateTime = new Date(sessionData.sessionDate);
    const [startHours, startMinutes] = sessionData.startTime.split(':');
    const [endHours, endMinutes] = sessionData.endTime.split(':');
    
    const startDateTime = new Date(sessionDateTime);
    startDateTime.setHours(parseInt(startHours), parseInt(startMinutes), 0, 0);
    
    const endDateTime = new Date(sessionDateTime);
    endDateTime.setHours(parseInt(endHours), parseInt(endMinutes), 0, 0);

    const testingSessionPayload: TestingSession = {
      testingRequestId: sessionData.testingRequestId,
      locationId: sessionData.locationId || undefined,
      sessionName: sessionData.sessionName,
      sessionDate: sessionDateTime.toISOString(),
      startTime: startDateTime.toISOString(),
      endTime: endDateTime.toISOString(),
      maxTesters: sessionData.maxTesters,
      status: 0, // SessionStatus.Scheduled
      managerId: sessionData.managerId || undefined,
      managerUserId: sessionData.managerId || undefined,
      registeredTesterCount: 0,
      registeredProjectMemberCount: 0,
      registeredProjectCount: 0,
    };

    const response = await postTestingSessions({
      baseUrl: environment.apiBaseUrl,
      body: testingSessionPayload,
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.data) {
      throw new Error('Failed to create testing session');
    }

    // Revalidate testing sessions cache
    revalidateTag('testing-sessions');

    return response.data as TestingSession;
  } catch (error) {
    console.error('Error creating testing session:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to create testing session');
  }
}

// Update an existing testing session
export async function updateTestingSession(
  id: string,
  sessionData: {
    sessionName?: string;
    sessionDate?: string;
    startTime?: string;
    endTime?: string;
    maxTesters?: number;
    status?: number;
  },
) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await putTestingSessionsById({
      baseUrl: environment.apiBaseUrl,
      path: { id },
      body: sessionData as PutTestingSessionsByIdData['body'],
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.data) {
      throw new Error('Failed to update testing session');
    }

    // Revalidate testing sessions cache
    revalidateTag('testing-sessions');

    return response.data as TestingSession;
  } catch (error) {
    console.error('Error updating testing session:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to update testing session');
  }
}

// Delete a testing session
export async function deleteTestingSession(id: string) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    await deleteTestingSessionsById({
      baseUrl: environment.apiBaseUrl,
      path: { id },
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
      },
    });

    // Revalidate testing sessions cache
    revalidateTag('testing-sessions');

    return { success: true };
  } catch (error) {
    console.error('Error deleting testing session:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to delete testing session');
  }
}

// Get testing requests by project version
export async function getTestingRequestsByProjectVersion(projectVersionId: string) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await getTestingRequestsByProjectVersionByProjectVersionId({
      baseUrl: environment.apiBaseUrl,
      path: { projectVersionId },
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch testing requests');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching testing requests by project version:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch testing requests');
  }
}

// Get testing requests by creator
export async function getTestingRequestsByCreator(creatorId: string) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await getTestingRequestsByCreatorByCreatorId({
      baseUrl: environment.apiBaseUrl,
      path: { creatorId },
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch testing requests');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching testing requests by creator:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch testing requests');
  }
}

// Get testing requests by status
export async function getTestingRequestsByStatus(status: string) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await getTestingRequestsByStatusByStatus({
      baseUrl: environment.apiBaseUrl,
      path: { status },
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch testing requests');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching testing requests by status:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch testing requests');
  }
}

// Search testing requests
export async function searchTestingRequests(searchTerm: string) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await getTestingRequestsSearch({
      baseUrl: environment.apiBaseUrl,
      query: { q: searchTerm },
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to search testing requests');
    }

    return response.data;
  } catch (error) {
    console.error('Error searching testing requests:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to search testing requests');
  }
}

// Get testing sessions by request
export async function getTestingSessionsByRequest(testingRequestId: string) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await getTestingSessionsByRequestByTestingRequestId({
      baseUrl: environment.apiBaseUrl,
      path: { testingRequestId },
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch testing sessions');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching testing sessions by request:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch testing sessions');
  }
}

// Get participants for a testing request
export async function getTestingRequestParticipants(requestId: string) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await getTestingRequestsByRequestIdParticipants({
      baseUrl: environment.apiBaseUrl,
      path: { requestId },
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch participants');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching testing request participants:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch participants');
  }
}

// Join a testing request
export async function joinTestingRequest(requestId: string, userId: string) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await postTestingRequestsByRequestIdParticipantsByUserId({
      baseUrl: environment.apiBaseUrl,
      path: { requestId, userId },
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.data) {
      throw new Error('Failed to join testing request');
    }

    // Revalidate cache
    revalidateTag('testing-requests');

    return response.data;
  } catch (error) {
    console.error('Error joining testing request:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to join testing request');
  }
}

// Leave a testing request
export async function leaveTestingRequest(requestId: string, userId: string) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    await deleteTestingRequestsByRequestIdParticipantsByUserId({
      baseUrl: environment.apiBaseUrl,
      path: { requestId, userId },
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
      },
    });

    // Revalidate cache
    revalidateTag('testing-requests');

    return { success: true };
  } catch (error) {
    console.error('Error leaving testing request:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to leave testing request');
  }
}

// Check if user has joined a testing request
export async function checkTestingRequestParticipation(requestId: string, userId: string) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await getTestingRequestsByRequestIdParticipantsByUserIdCheck({
      baseUrl: environment.apiBaseUrl,
      path: { requestId, userId },
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Cache-Control': 'no-store',
      },
    });

    return response.data || false;
  } catch (error) {
    console.error('Error checking testing request participation:', error);
    return false;
  }
}

// Get feedback for a testing request
export async function getTestingRequestFeedback(requestId: string) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await getTestingRequestsByRequestIdFeedback({
      baseUrl: environment.apiBaseUrl,
      path: { requestId },
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch feedback');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching testing request feedback:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch feedback');
  }
}

// Submit feedback for a testing request
export async function submitTestingRequestFeedback(
  requestId: string,
  feedbackData: {
    rating: number;
    comments: string;
    wouldRecommend: boolean;
    sessionId?: string;
  }
) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await postTestingRequestsByRequestIdFeedback({
      baseUrl: environment.apiBaseUrl,
      path: { requestId },
      body: feedbackData as any,
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
    });

    if (!response.data) {
      throw new Error('Failed to submit feedback');
    }

    // Revalidate cache
    revalidateTag('testing-requests');

    return response.data;
  } catch (error) {
    console.error('Error submitting testing request feedback:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to submit feedback');
  }
}

// Get testing request statistics
export async function getTestingRequestStatistics(requestId: string) {
  const session = await auth();

  if (!session?.accessToken) {
    throw new Error('Authentication required');
  }

  try {
    const response = await getTestingRequestsByRequestIdStatistics({
      baseUrl: environment.apiBaseUrl,
      path: { requestId },
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch statistics');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching testing request statistics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch statistics');
  }
}
