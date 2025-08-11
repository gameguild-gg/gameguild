'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  deleteTestingRequestsByRequestIdParticipantsByUserId,
  getTestingRequestsByRequestIdParticipants,
  getTestingRequestsByRequestIdParticipantsByUserIdCheck,
  postTestingRequestsByRequestIdParticipantsByUserId,
} from '@/lib/api/generated/sdk.gen';
import { revalidateTag } from 'next/cache';

// =============================================================================
// TESTING REQUEST PARTICIPANT MANAGEMENT
// =============================================================================

/**
 * Remove a participant from a testing request
 */
export async function removeParticipantFromRequest(requestId: string, userId: string) {
  await configureAuthenticatedClient();

  try {
    await deleteTestingRequestsByRequestIdParticipantsByUserId({
      path: { requestId, userId },
    });

    revalidateTag('testing-requests');
    return { success: true };
  } catch (error) {
    console.error('Error removing participant from request:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to remove participant from request');
  }
}

/**
 * Add a participant to a testing request
 */
export async function addParticipantToRequest(requestId: string, userId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postTestingRequestsByRequestIdParticipantsByUserId({
      path: { requestId, userId },
    });

    if (!response.data) {
      throw new Error('Failed to add participant to request');
    }

    revalidateTag('testing-requests');
    return response.data;
  } catch (error) {
    console.error('Error adding participant to request:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to add participant to request');
  }
}

/**
 * Get participants for a testing request
 */
export async function getTestingRequestParticipants(requestId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingRequestsByRequestIdParticipants({
      path: { requestId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch request participants');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching request participants:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch request participants');
  }
}

/**
 * Check if user is participant in testing request
 */
export async function checkUserParticipation(requestId: string, userId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingRequestsByRequestIdParticipantsByUserIdCheck({
      path: { requestId, userId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return !!response.data;
  } catch (error) {
    console.error('Error checking user participation:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to check user participation');
  }
}

// =============================================================================
// COMPONENT-FRIENDLY ALIASES
// =============================================================================

/**
 * Join a testing request (alias for addParticipantToRequest)
 */
export const joinTestingRequest = addParticipantToRequest;

/**
 * Leave a testing request (alias for removeParticipantFromRequest)
 */
export const leaveTestingRequest = removeParticipantFromRequest;

/**
 * Check if user has joined testing request (alias for checkUserParticipation)
 */
export const checkTestingRequestParticipation = checkUserParticipation;
