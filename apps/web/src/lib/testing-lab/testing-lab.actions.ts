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
    const response = await postTestingSessions({
      baseUrl: environment.apiBaseUrl,
      body: sessionData as PostTestingSessionsData['body'],
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
