'use server';

import { revalidateTag } from 'next/cache';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  // Content Interaction Management
  postApiContentinteractionStart,
  putApiContentinteractionByInteractionIdProgress,
  postApiContentinteractionByInteractionIdSubmit,
  postApiContentinteractionByInteractionIdComplete,
  putApiContentinteractionByInteractionIdTimeSpent,
  getApiContentinteractionUserByProgramUserIdContentByContentId,
  getApiContentinteractionUserByProgramUserId,
} from '@/lib/api/generated/sdk.gen';
import type {
  StartContentRequest,
  UpdateProgressRequest,
  SubmitContentRequest,
  CompleteContentRequest,
  UpdateTimeSpentRequest,
  ContentInteractionDto,
} from '@/lib/api/generated/types.gen';

// =============================================================================
// CONTENT INTERACTION MANAGEMENT
// =============================================================================

/**
 * Start a new content interaction
 */
export async function startContentInteraction(interactionData: { contentId: string; programUserId: string; programId?: string }) {
  await configureAuthenticatedClient();

  try {
    const startRequest: StartContentRequest = {
      programUserId: interactionData.programUserId,
      contentId: interactionData.contentId,
    };

    const response = await postApiContentinteractionStart({
      body: startRequest,
      query: interactionData.programId ? { programId: interactionData.programId } : undefined,
    });

    if (!response.data) {
      throw new Error('Failed to start content interaction');
    }

    revalidateTag('content-interactions');
    revalidateTag('user-progress');
    return response.data;
  } catch (error) {
    console.error('Error starting content interaction:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to start content interaction');
  }
}

/**
 * Update progress for a content interaction
 */
export async function updateContentInteractionProgress(
  interactionId: string,
  progressData: {
    programUserId: string;
    contentId: string;
    completionPercentage: number;
  },
) {
  await configureAuthenticatedClient();

  try {
    const updateRequest: UpdateProgressRequest = {
      programUserId: progressData.programUserId,
      contentId: progressData.contentId,
      completionPercentage: progressData.completionPercentage,
    };

    const response = await putApiContentinteractionByInteractionIdProgress({
      path: { interactionId },
      body: updateRequest,
    });

    if (!response.data) {
      throw new Error('Failed to update progress');
    }

    revalidateTag('content-interactions');
    revalidateTag('user-progress');
    revalidateTag(`interaction-${interactionId}`);
    return response.data;
  } catch (error) {
    console.error('Error updating content interaction progress:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to update progress');
  }
}

/**
 * Submit content interaction (for assignments, quizzes, etc.)
 */
export async function submitContentInteraction(
  interactionId: string,
  submissionData: {
    programUserId: string;
    contentId: string;
    submissionData?: string;
  },
) {
  await configureAuthenticatedClient();

  try {
    const submitRequest: SubmitContentRequest = {
      programUserId: submissionData.programUserId,
      contentId: submissionData.contentId,
      submissionData: submissionData.submissionData,
    };

    const response = await postApiContentinteractionByInteractionIdSubmit({
      path: { interactionId },
      body: submitRequest,
    });

    if (!response.data) {
      throw new Error('Failed to submit content interaction');
    }

    revalidateTag('content-interactions');
    revalidateTag('user-progress');
    revalidateTag(`interaction-${interactionId}`);
    revalidateTag('submissions');
    return response.data;
  } catch (error) {
    console.error('Error submitting content interaction:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to submit content interaction');
  }
}

/**
 * Complete a content interaction
 */
export async function completeContentInteraction(
  interactionId: string,
  completionData: {
    programUserId: string;
    contentId: string;
  },
) {
  await configureAuthenticatedClient();

  try {
    const completeRequest: CompleteContentRequest = {
      programUserId: completionData.programUserId,
      contentId: completionData.contentId,
    };

    const response = await postApiContentinteractionByInteractionIdComplete({
      path: { interactionId },
      body: completeRequest,
    });

    if (!response.data) {
      throw new Error('Failed to complete content interaction');
    }

    revalidateTag('content-interactions');
    revalidateTag('user-progress');
    revalidateTag(`interaction-${interactionId}`);
    revalidateTag('completions');
    return response.data;
  } catch (error) {
    console.error('Error completing content interaction:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to complete content interaction');
  }
}

/**
 * Update time spent on content interaction
 */
export async function updateContentInteractionTimeSpent(
  interactionId: string,
  timeData: {
    programUserId: string;
    contentId: string;
    additionalMinutes: number;
  },
) {
  await configureAuthenticatedClient();

  try {
    const timeRequest: UpdateTimeSpentRequest = {
      programUserId: timeData.programUserId,
      contentId: timeData.contentId,
      additionalMinutes: timeData.additionalMinutes,
    };

    const response = await putApiContentinteractionByInteractionIdTimeSpent({
      path: { interactionId },
      body: timeRequest,
    });

    if (!response.data) {
      throw new Error('Failed to update time spent');
    }

    revalidateTag('content-interactions');
    revalidateTag(`interaction-${interactionId}`);
    revalidateTag('time-tracking');
    return response.data;
  } catch (error) {
    console.error('Error updating time spent:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to update time spent');
  }
}

/**
 * Get user interactions for specific content
 */
export async function getUserContentInteraction(programUserId: string, contentId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiContentinteractionUserByProgramUserIdContentByContentId({
      path: { programUserId, contentId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return response.data;
  } catch (error) {
    console.error('Error fetching user content interaction:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user content interaction');
  }
}

/**
 * Get all interactions for a user
 */
export async function getUserInteractions(programUserId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiContentinteractionUserByProgramUserId({
      path: { programUserId },
      headers: {
        'Cache-Control': 'no-store',
      },
    });

    return Array.isArray(response.data) ? response.data : [];
  } catch (error) {
    console.error('Error fetching user interactions:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user interactions');
  }
}

// =============================================================================
// COMPREHENSIVE INTERACTION ANALYTICS
// =============================================================================

/**
 * Get comprehensive interaction analytics for a user
 */
export async function getUserInteractionAnalytics(programUserId: string) {
  await configureAuthenticatedClient();

  try {
    const interactions = await getUserInteractions(programUserId);

    // Calculate analytics from interactions
    const analytics = {
      totalInteractions: interactions.length,
      completedInteractions: interactions.filter((i: ContentInteractionDto) => i.status === 2).length, // Assuming status 2 is completed
      inProgressInteractions: interactions.filter((i: ContentInteractionDto) => i.status === 1).length, // Assuming status 1 is in progress
      totalTimeSpent: interactions.reduce((sum: number, i: ContentInteractionDto) => sum + (i.timeSpentMinutes || 0), 0),
      averageProgress:
        interactions.length > 0
          ? interactions.reduce((sum: number, i: ContentInteractionDto) => sum + (i.completionPercentage || 0), 0) / interactions.length
          : 0,
      lastActivity: interactions.reduce(
        (latest: string | null, i: ContentInteractionDto) => {
          const interactionDate = i.lastAccessedAt;
          return !latest || (interactionDate && interactionDate > latest) ? interactionDate || null : latest;
        },
        null as string | null,
      ),
    };

    return {
      analytics,
      interactions,
      summary: {
        programUserId,
        generatedAt: new Date().toISOString(),
        completionRate: analytics.totalInteractions > 0 ? (analytics.completedInteractions / analytics.totalInteractions) * 100 : 0,
      },
    };
  } catch (error) {
    console.error('Error generating user interaction analytics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to generate user interaction analytics');
  }
}

/**
 * Get content interaction summary for specific content
 */
export async function getContentInteractionSummary(contentId: string, userIds: string[]) {
  await configureAuthenticatedClient();

  try {
    const userInteractions = await Promise.all(
      userIds.map(async (userId) => {
        try {
          const interaction = await getUserContentInteraction(userId, contentId);
          return { userId, interaction };
        } catch {
          return { userId, interaction: null };
        }
      }),
    );

    const validInteractions = userInteractions.filter((ui) => ui.interaction !== null);

    const summary = {
      contentId,
      totalUsers: userIds.length,
      interactingUsers: validInteractions.length,
      completedUsers: validInteractions.filter((ui) => ui.interaction?.status === 2).length,
      averageProgress:
        validInteractions.length > 0
          ? validInteractions.reduce((sum, ui) => sum + (ui.interaction?.completionPercentage || 0), 0) / validInteractions.length
          : 0,
      totalTimeSpent: validInteractions.reduce((sum, ui) => sum + (ui.interaction?.timeSpentMinutes || 0), 0),
      engagementRate: userIds.length > 0 ? (validInteractions.length / userIds.length) * 100 : 0,
      completionRate:
        validInteractions.length > 0 ? (validInteractions.filter((ui) => ui.interaction?.status === 2).length / validInteractions.length) * 100 : 0,
    };

    return {
      summary,
      userInteractions: validInteractions,
      metadata: {
        generatedAt: new Date().toISOString(),
        requestedUsers: userIds.length,
        responseUsers: validInteractions.length,
      },
    };
  } catch (error) {
    console.error('Error generating content interaction summary:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to generate content interaction summary');
  }
}
