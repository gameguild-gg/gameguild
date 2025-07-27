'use server';

import { revalidateTag } from 'next/cache';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import type {
  SubmitFeedbackDto,
  RateFeedbackQualityDto,
} from '@/lib/api/generated/types.gen';
import {
  // Testing Feedback
  postTestingFeedback,
  // Feedback Quality Management
  postTestingFeedbackByFeedbackIdReport,
  postTestingFeedbackByFeedbackIdQuality,
} from '@/lib/api/generated/sdk.gen';

// =============================================================================
// USER TESTING FEEDBACK SUBMISSION
// =============================================================================

/**
 * Submit comprehensive feedback for testing
 */
export async function submitTestingFeedback(feedbackData: SubmitFeedbackDto) {
  await configureAuthenticatedClient();

  try {
    const response = await postTestingFeedback({
      body: feedbackData,
    });

    if (!response.data) {
      throw new Error('Failed to submit testing feedback');
    }

    revalidateTag('testing-feedback');
    revalidateTag('user-feedback');
    return response.data;
  } catch (error) {
    console.error('Error submitting testing feedback:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to submit testing feedback');
  }
}

// =============================================================================
// FEEDBACK QUALITY MANAGEMENT
// =============================================================================

/**
 * Report feedback for quality review
 */
export async function reportFeedbackQuality(
  feedbackId: string,
  reportData: {
    reason?: string;
    category?: string;
    description?: string;
    metadata?: Record<string, unknown>;
  },
) {
  await configureAuthenticatedClient();

  try {
    const response = await postTestingFeedbackByFeedbackIdReport({
      path: { feedbackId },
      body: reportData,
    });

    if (!response.data) {
      throw new Error('Failed to report feedback');
    }

    revalidateTag('testing-feedback');
    revalidateTag('feedback-reports');
    return response.data;
  } catch (error) {
    console.error('Error reporting feedback:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to report feedback');
  }
}

/**
 * Rate feedback quality
 */
export async function rateFeedbackQuality(feedbackId: string, qualityData: RateFeedbackQualityDto) {
  await configureAuthenticatedClient();

  try {
    const response = await postTestingFeedbackByFeedbackIdQuality({
      path: { feedbackId },
      body: qualityData,
    });

    if (!response.data) {
      throw new Error('Failed to rate feedback quality');
    }

    revalidateTag('testing-feedback');
    revalidateTag('feedback-quality');
    return response.data;
  } catch (error) {
    console.error('Error rating feedback quality:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to rate feedback quality');
  }
}

// =============================================================================
// ENHANCED USER FEEDBACK WORKFLOWS
// =============================================================================

/**
 * Submit detailed feedback with session completion
 */
export async function completeTestingSession(sessionData: {
  testingRequestId: string;
  sessionId?: string;
  feedbackResponses: string;
  overallRating?: number;
  wouldRecommend?: boolean;
  additionalNotes?: string;
}) {
  await configureAuthenticatedClient();

  try {
    const feedbackData: SubmitFeedbackDto = {
      testingRequestId: sessionData.testingRequestId,
      feedbackResponses: sessionData.feedbackResponses,
      overallRating: sessionData.overallRating,
      wouldRecommend: sessionData.wouldRecommend,
      additionalNotes: sessionData.additionalNotes,
      sessionId: sessionData.sessionId,
    };

    const response = await submitTestingFeedback(feedbackData);

    return {
      feedback: response,
      completedAt: new Date().toISOString(),
    };
  } catch (error) {
    console.error('Error completing testing session:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to complete testing session');
  }
}

/**
 * Submit quick feedback for testing
 */
export async function submitQuickTestingFeedback(quickFeedbackData: {
  testingRequestId: string;
  sessionId?: string;
  overallRating: number;
  quickNotes?: string;
  wouldRecommend?: boolean;
}) {
  await configureAuthenticatedClient();

  try {
    const feedbackData: SubmitFeedbackDto = {
      testingRequestId: quickFeedbackData.testingRequestId,
      feedbackResponses: quickFeedbackData.quickNotes || 'Quick feedback submission',
      overallRating: quickFeedbackData.overallRating,
      wouldRecommend: quickFeedbackData.wouldRecommend,
      additionalNotes: 'Submitted via quick feedback form',
      sessionId: quickFeedbackData.sessionId,
    };

    const response = await submitTestingFeedback(feedbackData);

    return response;
  } catch (error) {
    console.error('Error submitting quick feedback:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to submit quick feedback');
  }
}
