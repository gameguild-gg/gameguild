'use server';

import { revalidateTag } from 'next/cache';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  getTestingRequests,
  postTestingRequests,
  deleteTestingRequestsById,
  getTestingRequestsById,
  putTestingRequestsById,
  getTestingRequestsByIdDetails,
  postTestingRequestsByIdRestore,
  getTestingRequestsByProjectVersionByProjectVersionId,
  getTestingRequestsByCreatorByCreatorId,
  getTestingRequestsByStatusByStatus,
  getTestingRequestsSearch,
  getTestingMyRequests,
  getTestingAvailableForTesting,
} from '@/lib/api/generated/sdk.gen';
import type { TestingRequest, TestingRequestStatus, InstructionType } from '@/lib/api/generated/types.gen';

// =============================================================================
// TESTING REQUEST CRUD OPERATIONS
// =============================================================================

/**
 * Get all testing requests with optional filtering
 */
export async function getTestingRequestsData(params?: { status?: string; projectVersionId?: string; creatorId?: string; skip?: number; take?: number }) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingRequests({
      query: params,
      headers: {
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
