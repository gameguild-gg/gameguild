'use server';

import { environment } from '@/configs/environment';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  deleteTestingRequestsById,
  getTestingAvailableForTesting,
  getTestingMyRequests,
  getTestingRequests,
  getTestingRequestsByCreatorByCreatorId,
  getTestingRequestsById,
  getTestingRequestsByIdDetails,
  getTestingRequestsByProjectVersionByProjectVersionId,
  getTestingRequestsByStatusByStatus,
  getTestingRequestsSearch,
  postTestingRequests,
  postTestingRequestsByIdRestore,
  putTestingRequestsById,
} from '@/lib/api/generated/sdk.gen';
import type { InstructionType, TestingRequest, TestingRequestStatus } from '@/lib/api/generated/types.gen';
import { revalidateTag } from 'next/cache';

// =============================================================================
// TESTING REQUEST CRUD OPERATIONS
// =============================================================================

/**
 * Get all testing requests with optional filtering
 */
export async function getTestingRequestsData(params?: { status?: string; projectVersionId?: string; creatorId?: string; skip?: number; take?: number }) {
  const session = await configureAuthenticatedClient();

  try {
    console.log('Making request to getTestingRequests with params:', params);
    console.log('Session token exists:', !!session?.api?.accessToken);
    console.log('Current tenant ID:', session?.currentTenant?.id);

    const response = await getTestingRequests({
      query: params,
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    console.log('Response received:', response);
    console.log('Response status:', response?.response?.status);
    console.log('Response data length:', response?.data?.length);

    if (!response.data) {
      console.log('No data in response, but no error thrown - this might be normal if no requests exist');
      return {
        testingRequests: [],
        total: 0,
      };
    }

    return {
      testingRequests: response.data,
      total: response.data.length,
    };
  } catch (error) {
    console.error('Error fetching testing requests:', error);
    console.error('Error details:', {
      message: error instanceof Error ? error.message : 'Unknown error',
      stack: error instanceof Error ? error.stack : undefined,
      cause: error instanceof Error ? error.cause : undefined,
      name: error instanceof Error ? error.name : undefined,
    });
    console.error('User ID:', session?.user?.id);
    console.error('API Base URL from config:', environment.apiBaseUrl);

    // If it's a 401 error, it's likely authentication issue
    if (error instanceof Error && error.message.includes('401')) {
      throw new Error('Authentication failed - please try signing in again');
    }

    throw new Error(error instanceof Error ? error.message : 'Failed to fetch testing requests');
  }
}

/**
 * Create a new testing request
 */
export async function createTestingRequest(requestData: {
  title: string;
  description?: string;
  projectVersionId: string;
  downloadUrl?: string;
  instructionsContent?: string;
  instructionsUrl?: string;
  feedbackFormContent?: string;
  maxTesters?: number;
  startDate: string;
  endDate: string;
  teamIdentifier: string;
  instructionsType?: InstructionType;
  status?: TestingRequestStatus;
}) {
  await configureAuthenticatedClient();

  try {
    const response = await postTestingRequests({
      body: {
        title: requestData.title,
        description: requestData.description,
        downloadUrl: requestData.downloadUrl,
        instructionsContent: requestData.instructionsContent,
        instructionsUrl: requestData.instructionsUrl,
        feedbackFormContent: requestData.feedbackFormContent,
        maxTesters: requestData.maxTesters || 10,
        startDate: new Date(requestData.startDate).toISOString(),
        endDate: new Date(requestData.endDate).toISOString(),
        projectVersionId: requestData.projectVersionId,
        instructionsType: requestData.instructionsType ?? 1, // Default to MARKDOWN
        status: requestData.status ?? 1, // Default to DRAFT
      },
    });

    if (!response.data) {
      throw new Error('Failed to create testing request');
    }

    revalidateTag('testing-requests');
    return response.data as TestingRequest;
  } catch (error) {
    console.error('Error creating testing request:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to create testing request');
  }
}

/**
 * Delete a testing request by ID
 */
export async function deleteTestingRequest(id: string) {
  await configureAuthenticatedClient();

  try {
    await deleteTestingRequestsById({
      path: { id },
    });

    revalidateTag('testing-requests');
    return { success: true };
  } catch (error) {
    console.error('Error deleting testing request:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to delete testing request');
  }
}

/**
 * Get a single testing request by ID
 */
export async function getTestingRequestById(id: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingRequestsById({
      path: { id },
      headers: {
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

/**
 * Update an existing testing request
 */
export async function updateTestingRequest(
  id: string,
  requestData: {
    title?: string;
    description?: string;
    instructionsType?: 'inline' | 'file' | 'url';
    instructionsContent?: string;
    instructionsUrl?: string;
    feedbackFormContent?: string;
    maxTesters?: number;
    startDate?: string;
    endDate?: string;
    status?: TestingRequestStatus;
  },
) {
  await configureAuthenticatedClient();

  try {
    const response = await putTestingRequestsById({
      path: { id },
      body: requestData as unknown as TestingRequest,
    });

    if (!response.data) {
      throw new Error('Failed to update testing request');
    }

    revalidateTag('testing-requests');
    return response.data as TestingRequest;
  } catch (error) {
    console.error('Error updating testing request:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to update testing request');
  }
}

/**
 * Get detailed information about a testing request
 */
export async function getTestingRequestDetails(id: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingRequestsByIdDetails({
      path: { id },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Testing request details not found');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching testing request details:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch testing request details');
  }
}

/**
 * Restore a deleted testing request
 */
export async function restoreTestingRequest(id: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postTestingRequestsByIdRestore({
      path: { id },
    });

    if (!response.data) {
      throw new Error('Failed to restore testing request');
    }

    revalidateTag('testing-requests');
    return response.data;
  } catch (error) {
    console.error('Error restoring testing request:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to restore testing request');
  }
}

// =============================================================================
// TESTING REQUEST FILTERING & SEARCH
// =============================================================================

/**
 * Get testing requests by project version
 */
export async function getTestingRequestsByProjectVersion(projectVersionId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingRequestsByProjectVersionByProjectVersionId({
      path: { projectVersionId },
      headers: {
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

/**
 * Get testing requests by creator
 */
export async function getTestingRequestsByCreator(creatorId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingRequestsByCreatorByCreatorId({
      path: { creatorId },
      headers: {
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

/**
 * Get testing requests by status
 */
export async function getTestingRequestsByStatus(status: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingRequestsByStatusByStatus({
      path: { status: status as unknown as TestingRequestStatus },
      headers: {
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

/**
 * Search testing requests
 */
export async function searchTestingRequests(searchTerm: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingRequestsSearch({
      query: { searchTerm },
      headers: {
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

/**
 * Get current user's testing requests
 */
export async function getMyTestingRequests() {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingMyRequests({
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch user testing requests');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching user testing requests:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user testing requests');
  }
}

/**
 * Get available testing opportunities for the current user
 */
export async function getAvailableTestingOpportunities() {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingAvailableForTesting({
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch available testing opportunities');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching available testing opportunities:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch available testing opportunities');
  }
}

// =============================================================================
// COMPONENT-FRIENDLY ALIASES
// =============================================================================

/**
 * Get all testing requests (alias for getTestingRequestsData)
 */
export const getAllTestingRequests = getTestingRequestsData;

/**
 * Get testing requests by session slug/ID
 */
export async function getTestingRequestsBySession(sessionSlug: string) {
  await configureAuthenticatedClient();

  try {
    console.log('Fetching testing requests by session:', sessionSlug);

    // Get all testing requests and filter by session ID if available
    const response = await getTestingRequests({
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      return {
        testingRequests: [],
        total: 0,
      };
    }

    // For now, return all requests since we don't have a direct session-to-request relationship endpoint
    // This could be enhanced when such an endpoint becomes available
    return {
      testingRequests: response.data,
      total: response.data.length,
    };
  } catch (error) {
    console.error('Error fetching testing requests by session:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch testing requests by session');
  }
}
