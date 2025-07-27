'use server';

import { revalidateTag } from 'next/cache';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  getApiUsersByUserIdAchievements,
  getApiUsersByUserIdAchievementsProgress,
  getApiUsersByUserIdAchievementsSummary,
  getApiUsersByUserIdAchievementsAvailable,
  postApiUsersByUserIdAchievementsByAchievementIdProgress,
  getApiUsersByUserIdAchievementsByAchievementIdPrerequisites,
  postApiUsersByUserIdAchievementsByUserAchievementIdMarkNotified,
  deleteApiUsersByUserIdAchievementsByUserAchievementId,
} from '@/lib/api/generated/sdk.gen';

// =============================================================================
// USER ACHIEVEMENT OPERATIONS
// =============================================================================

/**
 * Get all achievements for a specific user
 */
export async function getUserAchievements(
  userId: string,
  params?: {
    pageNumber?: number;
    pageSize?: number;
    category?: string;
    type?: string;
    isCompleted?: boolean;
    earnedAfter?: string;
    earnedBefore?: string;
    orderBy?: string;
    descending?: boolean;
  },
) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiUsersByUserIdAchievements({
      path: { userId },
      query: {
        pageNumber: params?.pageNumber || 1,
        pageSize: params?.pageSize || 50,
        category: params?.category,
        type: params?.type,
        isCompleted: params?.isCompleted,
        earnedAfter: params?.earnedAfter,
        earnedBefore: params?.earnedBefore,
        orderBy: params?.orderBy,
        descending: params?.descending,
      },
    });

    if (response.error) {
      console.error('Error fetching user achievements:', response.error);
      return { error: response.error };
    }

    return { data: response.data };
  } catch (error) {
    console.error('Error in getUserAchievements:', error);
    return { error: 'Failed to fetch user achievements' };
  }
}

/**
 * Get achievement progress for a specific user
 */
export async function getUserAchievementProgress(userId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiUsersByUserIdAchievementsProgress({
      path: { userId },
    });

    if (response.error) {
      console.error('Error fetching user achievement progress:', response.error);
      return { error: response.error };
    }

    return { data: response.data };
  } catch (error) {
    console.error('Error in getUserAchievementProgress:', error);
    return { error: 'Failed to fetch user achievement progress' };
  }
}

/**
 * Get achievement summary for a specific user
 */
export async function getUserAchievementSummary(userId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiUsersByUserIdAchievementsSummary({
      path: { userId },
    });

    if (response.error) {
      console.error('Error fetching user achievement summary:', response.error);
      return { error: response.error };
    }

    return { data: response.data };
  } catch (error) {
    console.error('Error in getUserAchievementSummary:', error);
    return { error: 'Failed to fetch user achievement summary' };
  }
}

/**
 * Get available achievements for a specific user
 */
export async function getUserAvailableAchievements(
  userId: string,
  params?: {
    pageNumber?: number;
    pageSize?: number;
    category?: string;
    onlyEligible?: boolean;
  },
) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiUsersByUserIdAchievementsAvailable({
      path: { userId },
      query: {
        pageNumber: params?.pageNumber || 1,
        pageSize: params?.pageSize || 50,
        category: params?.category,
        onlyEligible: params?.onlyEligible,
      },
    });

    if (response.error) {
      console.error('Error fetching available achievements:', response.error);
      return { error: response.error };
    }

    return { data: response.data };
  } catch (error) {
    console.error('Error in getUserAvailableAchievements:', error);
    return { error: 'Failed to fetch available achievements' };
  }
}

// =============================================================================
// ACHIEVEMENT PROGRESS OPERATIONS
// =============================================================================

/**
 * Update progress for a specific achievement
 */
export async function updateAchievementProgress(
  userId: string,
  achievementId: string,
  progressData: {
    progressIncrement?: number;
    context?: string | null;
    autoAward?: boolean;
  },
) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiUsersByUserIdAchievementsByAchievementIdProgress({
      path: { userId, achievementId },
      body: progressData,
    });

    if (response.error) {
      console.error('Error updating achievement progress:', response.error);
      return { error: response.error };
    }

    revalidateTag(`user-${userId}-achievements`);
    revalidateTag(`achievement-${achievementId}-progress`);
    return { data: response.data };
  } catch (error) {
    console.error('Error in updateAchievementProgress:', error);
    return { error: 'Failed to update achievement progress' };
  }
}

/**
 * Get prerequisites for a specific achievement
 */
export async function getAchievementPrerequisites(userId: string, achievementId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiUsersByUserIdAchievementsByAchievementIdPrerequisites({
      path: { userId, achievementId },
    });

    if (response.error) {
      console.error('Error fetching achievement prerequisites:', response.error);
      return { error: response.error };
    }

    return { data: response.data };
  } catch (error) {
    console.error('Error in getAchievementPrerequisites:', error);
    return { error: 'Failed to fetch achievement prerequisites' };
  }
}

// =============================================================================
// ACHIEVEMENT NOTIFICATION OPERATIONS
// =============================================================================

/**
 * Mark an achievement as notified (user has been notified of earning it)
 */
export async function markAchievementAsNotified(userId: string, userAchievementId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiUsersByUserIdAchievementsByUserAchievementIdMarkNotified({
      path: { userId, userAchievementId },
    });

    if (response.error) {
      console.error('Error marking achievement as notified:', response.error);
      return { error: response.error };
    }

    revalidateTag(`user-${userId}-achievements`);
    return { data: response.data };
  } catch (error) {
    console.error('Error in markAchievementAsNotified:', error);
    return { error: 'Failed to mark achievement as notified' };
  }
}

/**
 * Remove/revoke a user achievement
 */
export async function removeUserAchievement(userId: string, userAchievementId: string, reason?: string) {
  await configureAuthenticatedClient();

  try {
    const response = await deleteApiUsersByUserIdAchievementsByUserAchievementId({
      path: { userId, userAchievementId },
      body: reason ? { reason } : undefined,
    });

    if (response.error) {
      console.error('Error removing user achievement:', response.error);
      return { error: response.error };
    }

    revalidateTag(`user-${userId}-achievements`);
    return { data: response.data };
  } catch (error) {
    console.error('Error in removeUserAchievement:', error);
    return { error: 'Failed to remove user achievement' };
  }
}

// =============================================================================
// UTILITY FUNCTIONS
// =============================================================================

/**
 * Get comprehensive achievement data for a user (summary + progress + available)
 */
export async function getComprehensiveUserAchievements(userId: string) {
  const [summary, progress, available] = await Promise.all([
    getUserAchievementSummary(userId),
    getUserAchievementProgress(userId),
    getUserAvailableAchievements(userId),
  ]);

  return {
    summary: summary.data || null,
    progress: progress.data || null,
    available: available.data || null,
    errors: [
      summary.error && { type: 'summary', error: summary.error },
      progress.error && { type: 'progress', error: progress.error },
      available.error && { type: 'available', error: available.error },
    ].filter(Boolean),
  };
}

/**
 * Check if a user can pursue a specific achievement (prerequisites met)
 */
export async function canUserPursueAchievement(userId: string, achievementId: string) {
  const prerequisites = await getAchievementPrerequisites(userId, achievementId);

  if (prerequisites.error) {
    return { canPursue: false, error: prerequisites.error };
  }

  // Assuming the API returns prerequisite status
  // This logic may need adjustment based on actual API response structure
  const unmetPrerequisites = prerequisites.data
    ? Object.values(prerequisites.data).filter((prereq: unknown) => typeof prereq === 'object' && prereq !== null && 'isMet' in prereq && !prereq.isMet)
    : [];

  return {
    canPursue: unmetPrerequisites.length === 0,
    unmetPrerequisites,
    prerequisites: prerequisites.data,
  };
}
