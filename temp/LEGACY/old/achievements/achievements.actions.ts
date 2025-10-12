'use server';

import { revalidateTag } from 'next/cache';
import type { Achievement, AchievementsPageDto, CreateAchievementCommand, UpdateAchievementCommand, AwardAchievementRequest, BulkAwardAchievementRequest, AchievementStatisticsDto } from '@/lib/core/api/generated/types.gen';
import {
  getApiAchievements,
  postApiAchievements,
  getApiAchievementsByAchievementId,
  putApiAchievementsByAchievementId,
  deleteApiAchievementsByAchievementId,
  postApiAchievementsByAchievementIdAward,
  postApiAchievementsByAchievementIdBulkAward,
  getApiAchievementsStatistics,
  getApiAchievementsByAchievementIdStatistics,
} from '@/lib/core/api/generated/sdk.gen';

// =============================================================================
// TYPES & INTERFACES
// =============================================================================

export interface AchievementActionResult<T = unknown> {
  success: boolean;
  data?: T;
  error?: string;
}

// =============================================================================
// VALIDATION UTILITIES
// =============================================================================

function validateAchievementData(data: CreateAchievementCommand | UpdateAchievementCommand): string[] {
  const errors: string[] = [];

  if (!data.name || data.name.trim().length === 0) {
    errors.push('Achievement name is required');
  }

  if (data.name && data.name.length > 100) {
    errors.push('Achievement name must be less than 100 characters');
  }

  if (data.description && data.description.length > 500) {
    errors.push('Achievement description must be less than 500 characters');
  }

  if (data.points !== undefined && data.points !== null && data.points < 0) {
    errors.push('Achievement points must be a positive number');
  }

  if (data.displayOrder !== undefined && data.displayOrder !== null && data.displayOrder < 0) {
    errors.push('Display order must be a positive number');
  }

  return errors;
}

// =============================================================================
// ERROR HANDLING
// =============================================================================

function handleApiError<T = unknown>(error: unknown, operation: string): AchievementActionResult<T> {
  console.error(`Error in ${operation}:`, error);

  if (error instanceof Error) {
    return {
      success: false,
      error: error.message,
    };
  }

  return {
    success: false,
    error: `Failed to ${operation}`,
  };
}

// =============================================================================
// CACHE MANAGEMENT
// =============================================================================

function invalidateAchievementCache(achievementId?: string) {
  revalidateTag('achievements');
  revalidateTag('achievement-statistics');
  if (achievementId) {
    revalidateTag(`achievement-${achievementId}`);
  }
}

// =============================================================================
// ACHIEVEMENT ACTIONS
// =============================================================================

/**
 * Retrieves a paginated list of achievements with optional filtering
 */
export async function getAchievements(params?: {
  pageNumber?: number;
  pageSize?: number;
  category?: string;
  type?: string;
  isActive?: boolean;
  isSecret?: boolean;
  searchTerm?: string;
  orderBy?: string;
  descending?: boolean;
}): Promise<AchievementActionResult<AchievementsPageDto>> {
  try {
    const query = {
      pageNumber: params?.pageNumber ?? 1,
      pageSize: params?.pageSize ?? 10,
      category: params?.category,
      type: params?.type,
      isActive: params?.isActive,
      isSecret: params?.isSecret,
      searchTerm: params?.searchTerm,
      orderBy: params?.orderBy,
      descending: params?.descending,
    };

    const result = await getApiAchievements({ query });

    if (!result.data) {
      return {
        success: false,
        error: 'No data received from server',
      };
    }

    return {
      success: true,
      data: result.data,
    };
  } catch (error) {
    return handleApiError<AchievementsPageDto>(error, 'fetch achievements');
  }
}

/**
 * Retrieves overall achievement statistics
 */
export async function getAchievementsStatistics(): Promise<AchievementActionResult<AchievementStatisticsDto>> {
  try {
    const result = await getApiAchievementsStatistics();

    if (!result.data) {
      return {
        success: false,
        error: 'No data received from server',
      };
    }

    return {
      success: true,
      data: result.data,
    };
  } catch (error) {
    return handleApiError<AchievementStatisticsDto>(error, 'fetch achievement statistics');
  }
}

/**
 * Creates a new achievement
 */
export async function createAchievement(data: CreateAchievementCommand): Promise<AchievementActionResult<Achievement>> {
  try {
    // Validate input data
    const validationErrors = validateAchievementData(data);
    if (validationErrors.length > 0) {
      return {
        success: false,
        error: validationErrors.join(', '),
      };
    }

    const result = await postApiAchievements({ body: data });

    if (!result.data) {
      return {
        success: false,
        error: 'No data received from server',
      };
    }

    // Invalidate cache
    invalidateAchievementCache();

    return {
      success: true,
      data: result.data,
    };
  } catch (error) {
    return handleApiError<Achievement>(error, 'create achievement');
  }
}

/**
 * Deletes an achievement by ID
 */
export async function deleteAchievement(achievementId: string): Promise<AchievementActionResult<unknown>> {
  try {
    if (!achievementId || achievementId.trim().length === 0) {
      return {
        success: false,
        error: 'Achievement ID is required',
      };
    }

    await deleteApiAchievementsByAchievementId({
      path: { achievementId },
    });

    // Invalidate cache
    invalidateAchievementCache(achievementId);

    return {
      success: true,
    };
  } catch (error) {
    return handleApiError(error, 'delete achievement');
  }
}

/**
 * Retrieves a single achievement by ID
 */
export async function getAchievementById(achievementId: string, options?: { includeLevels?: boolean; includePrerequisites?: boolean }): Promise<AchievementActionResult<Achievement>> {
  try {
    if (!achievementId || achievementId.trim().length === 0) {
      return {
        success: false,
        error: 'Achievement ID is required',
      };
    }

    const result = await getApiAchievementsByAchievementId({
      path: { achievementId },
      query: {
        includeLevels: options?.includeLevels,
        includePrerequisites: options?.includePrerequisites,
      },
    });

    if (!result.data) {
      return {
        success: false,
        error: 'Achievement not found',
      };
    }

    return {
      success: true,
      data: result.data,
    };
  } catch (error) {
    return handleApiError<Achievement>(error, 'fetch achievement');
  }
}

/**
 * Updates an existing achievement
 */
export async function updateAchievement(achievementId: string, data: UpdateAchievementCommand): Promise<AchievementActionResult<Achievement>> {
  try {
    if (!achievementId || achievementId.trim().length === 0) {
      return {
        success: false,
        error: 'Achievement ID is required',
      };
    }

    // Validate input data
    const validationErrors = validateAchievementData(data);
    if (validationErrors.length > 0) {
      return {
        success: false,
        error: validationErrors.join(', '),
      };
    }

    const result = await putApiAchievementsByAchievementId({
      path: { achievementId },
      body: data,
    });

    if (!result.data) {
      return {
        success: false,
        error: 'No data received from server',
      };
    }

    // Invalidate cache
    invalidateAchievementCache(achievementId);

    return {
      success: true,
      data: result.data,
    };
  } catch (error) {
    return handleApiError<Achievement>(error, 'update achievement');
  }
}

/**
 * Awards an achievement to a specific user
 */
export async function awardAchievement(
  achievementId: string,
  userId: string,
  options?: {
    level?: number;
    progress?: number;
    maxProgress?: number;
    context?: string;
    notifyUser?: boolean;
  },
): Promise<AchievementActionResult<unknown>> {
  try {
    if (!achievementId || !userId) {
      return {
        success: false,
        error: 'Achievement ID and User ID are required',
      };
    }

    const requestData: AwardAchievementRequest = {
      userId,
      level: options?.level,
      progress: options?.progress ?? 1,
      maxProgress: options?.maxProgress ?? 1,
      context: options?.context,
      notifyUser: options?.notifyUser ?? true,
    };

    await postApiAchievementsByAchievementIdAward({
      path: { achievementId },
      body: requestData,
    });

    // Invalidate cache
    invalidateAchievementCache(achievementId);

    return {
      success: true,
    };
  } catch (error) {
    return handleApiError(error, 'award achievement');
  }
}

/**
 * Awards an achievement to multiple users
 */
export async function bulkAwardAchievement(
  achievementId: string,
  userIds: string[],
  options?: {
    progress?: number;
    maxProgress?: number;
    context?: string;
    notifyUsers?: boolean;
  },
): Promise<AchievementActionResult<unknown>> {
  try {
    if (!achievementId || !userIds || userIds.length === 0) {
      return {
        success: false,
        error: 'Achievement ID and at least one User ID are required',
      };
    }

    const requestData: BulkAwardAchievementRequest = {
      userIds,
      context: options?.context,
      notifyUsers: options?.notifyUsers ?? true,
    };

    await postApiAchievementsByAchievementIdBulkAward({
      path: { achievementId },
      body: requestData,
    });

    // Invalidate cache
    invalidateAchievementCache(achievementId);

    return {
      success: true,
    };
  } catch (error) {
    return handleApiError(error, 'bulk award achievement');
  }
}

/**
 * Retrieves statistics for a specific achievement
 */
export async function getAchievementStatistics(achievementId: string): Promise<AchievementActionResult<AchievementStatisticsDto>> {
  try {
    if (!achievementId || achievementId.trim().length === 0) {
      return {
        success: false,
        error: 'Achievement ID is required',
      };
    }

    const result = await getApiAchievementsByAchievementIdStatistics({
      path: { achievementId },
    });

    if (!result.data) {
      return {
        success: false,
        error: 'No statistics data received from server',
      };
    }

    return {
      success: true,
      data: result.data,
    };
  } catch (error) {
    return handleApiError<AchievementStatisticsDto>(error, 'fetch achievement statistics');
  }
}
