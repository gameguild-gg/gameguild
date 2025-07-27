'use server';

import { revalidateTag } from 'next/cache';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import { getTestingRequestsByRequestIdFeedback, postTestingRequestsByRequestIdFeedback } from '@/lib/api/generated/sdk.gen';
import type { FeedbackRequest } from '@/lib/api/generated/types.gen';

// =============================================================================
// TESTING REQUEST FEEDBACK MANAGEMENT
// =============================================================================

/**
 * Get feedback for a specific testing request
 */
export async function getTestingRequestFeedback(requestId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getTestingRequestsByRequestIdFeedback({
      path: { requestId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    if (!response.data) {
      throw new Error('Failed to fetch request feedback');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching testing request feedback:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch request feedback');
  }
}

/**
 * Submit feedback for a specific testing request
 */
export async function submitTestingRequestFeedback(
  requestId: string,
  feedbackData: {
    rating: number;
    comments: string;
    suggestions?: string;
    foundBugs?: boolean;
    bugDetails?: string;
  },
) {
  await configureAuthenticatedClient();

  try {
    const response = await postTestingRequestsByRequestIdFeedback({
      path: { requestId },
      body: feedbackData as unknown as FeedbackRequest,
    });

    if (!response.data) {
      throw new Error('Failed to submit feedback');
    }

    revalidateTag('testing-requests');
    return response.data;
  } catch (error) {
    console.error('Error submitting testing request feedback:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to submit feedback');
  }
}
