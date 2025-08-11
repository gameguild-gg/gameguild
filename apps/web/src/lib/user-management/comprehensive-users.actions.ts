'use server';

import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  deleteApiUsersById,
  deleteApiUsersByUserIdAchievementsByUserAchievementId,
  getApiUsers,
  getApiUsersById,
  getApiUsersByUserIdAchievements,
  getApiUsersByUserIdAchievementsAvailable,
  getApiUsersByUserIdAchievementsByAchievementIdPrerequisites,
  getApiUsersByUserIdAchievementsProgress,
  getApiUsersByUserIdAchievementsSummary,
  getApiUsersSearch,
  getApiUsersStatistics,
  postApiUsers,
  postApiUsersBulk,
  postApiUsersByIdRestore,
  postApiUsersByUserIdAchievementsByAchievementIdProgress,
  postApiUsersByUserIdAchievementsByUserAchievementIdMarkNotified,
  putApiUsersById,
  putApiUsersByIdBalance,
} from '@/lib/api/generated/sdk.gen';
import type { CreateUserDto, UpdateAchievementProgressRequest, UpdateUserBalanceDto, UpdateUserDto } from '@/lib/api/generated/types.gen';
import { revalidateTag } from 'next/cache';

// =============================================================================
// CORE USER CRUD OPERATIONS
// =============================================================================

/**
 * Get all users with advanced filtering and pagination
 */
export async function getUsers(params?: { skip?: number; take?: number; includeDeleted?: boolean; isActive?: boolean; sortBy?: string; sortOrder?: 'asc' | 'desc' }) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiUsers({
      query: {
        skip: params?.skip,
        take: params?.take,
        includeDeleted: params?.includeDeleted,
        isActive: params?.isActive,
      },
    });

    if (response.error || !response.data) {
      throw new Error('Failed to fetch users');
    }

    revalidateTag('users');
    return response.data;
  } catch (error) {
    console.error('Error fetching users:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch users');
  }
}

/**
 * Get user by ID with full details
 */
export async function getUserById(id: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiUsersById({
      path: { id },
    });

    if (response.error || !response.data) {
      throw new Error('Failed to fetch user');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching user:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user');
  }
}

/**
 * Create a new user
 */
export async function createUser(userData: CreateUserDto) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiUsers({
      body: userData,
    });

    if (response.error || !response.data) {
      throw new Error('Failed to create user');
    }

    revalidateTag('users');
    revalidateTag('user-statistics');
    return response.data;
  } catch (error) {
    console.error('Error creating user:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to create user');
  }
}

/**
 * Update an existing user
 */
export async function updateUser(id: string, userData: UpdateUserDto) {
  await configureAuthenticatedClient();

  try {
    const response = await putApiUsersById({
      path: { id },
      body: userData,
    });

    if (response.error || !response.data) {
      throw new Error('Failed to update user');
    }

    revalidateTag('users');
    revalidateTag(`user-${id}`);
    return response.data;
  } catch (error) {
    console.error('Error updating user:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to update user');
  }
}

/**
 * Delete a user (soft delete)
 */
export async function deleteUser(id: string) {
  await configureAuthenticatedClient();

  try {
    const response = await deleteApiUsersById({
      path: { id },
    });

    if (response.error) {
      throw new Error('Failed to delete user');
    }

    revalidateTag('users');
    revalidateTag(`user-${id}`);
    revalidateTag('user-statistics');
    return { success: true };
  } catch (error) {
    console.error('Error deleting user:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to delete user');
  }
}

/**
 * Restore a deleted user
 */
export async function restoreUser(id: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiUsersByIdRestore({
      path: { id },
    });

    if (response.error || !response.data) {
      throw new Error('Failed to restore user');
    }

    revalidateTag('users');
    revalidateTag(`user-${id}`);
    revalidateTag('user-statistics');
    return response.data;
  } catch (error) {
    console.error('Error restoring user:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to restore user');
  }
}

// =============================================================================
// USER MANAGEMENT & ADMINISTRATION
// =============================================================================

/**
 * Update user balance with transaction tracking
 */
export async function updateUserBalance(id: string, balanceData: UpdateUserBalanceDto) {
  await configureAuthenticatedClient();

  try {
    const response = await putApiUsersByIdBalance({
      path: { id },
      body: balanceData,
    });

    if (response.error || !response.data) {
      throw new Error('Failed to update user balance');
    }

    revalidateTag('users');
    revalidateTag(`user-${id}`);
    revalidateTag('user-statistics');
    return response.data;
  } catch (error) {
    console.error('Error updating user balance:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to update user balance');
  }
}

/**
 * Search users with advanced filters
 */
export async function searchUsers(params: { searchTerm: string; skip?: number; take?: number; isActive?: boolean; minBalance?: number; maxBalance?: number; createdAfter?: string; createdBefore?: string; includeDeleted?: boolean }) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiUsersSearch({
      query: params,
    });

    if (response.error || !response.data) {
      throw new Error('Failed to search users');
    }

    return response.data;
  } catch (error) {
    console.error('Error searching users:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to search users');
  }
}

/**
 * Get comprehensive user statistics
 */
export async function getUserStatistics(params?: { fromDate?: string; toDate?: string; includeDeleted?: boolean }) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiUsersStatistics({
      query: params,
    });

    if (response.error || !response.data) {
      throw new Error('Failed to fetch user statistics');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching user statistics:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user statistics');
  }
}

/**
 * Bulk user operations (create, update, delete)
 */
export async function bulkUserOperations(operations: { create?: CreateUserDto[]; update?: Array<{ id: string; data: UpdateUserDto }>; delete?: string[]; restore?: string[] }) {
  await configureAuthenticatedClient();

  try {
    const results = {
      created: [] as unknown[],
      updated: [] as unknown[],
      deleted: [] as string[],
      restored: [] as unknown[],
      errors: [] as string[],
    };

    // Handle bulk create operations
    if (operations.create && operations.create.length > 0) {
      try {
        const response = await postApiUsersBulk({
          body: operations.create,
        });

        if (response.data) {
          results.created = Array.isArray(response.data) ? response.data : [response.data];
        }
      } catch (error) {
        results.errors.push(`Bulk create failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
      }
    }

    // Handle individual update operations
    if (operations.update && operations.update.length > 0) {
      for (const updateOp of operations.update) {
        try {
          const response = await updateUser(updateOp.id, updateOp.data);
          results.updated.push(response);
        } catch (error) {
          results.errors.push(`Update user ${updateOp.id} failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
        }
      }
    }

    // Handle bulk delete operations
    if (operations.delete && operations.delete.length > 0) {
      for (const userId of operations.delete) {
        try {
          await deleteUser(userId);
          results.deleted.push(userId);
        } catch (error) {
          results.errors.push(`Delete user ${userId} failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
        }
      }
    }

    // Handle bulk restore operations
    if (operations.restore && operations.restore.length > 0) {
      for (const userId of operations.restore) {
        try {
          const response = await restoreUser(userId);
          results.restored.push(response);
        } catch (error) {
          results.errors.push(`Restore user ${userId} failed: ${error instanceof Error ? error.message : 'Unknown error'}`);
        }
      }
    }

    revalidateTag('users');
    revalidateTag('user-statistics');
    return results;
  } catch (error) {
    console.error('Error in bulk user operations:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to execute bulk user operations');
  }
}

// =============================================================================
// USER ACHIEVEMENT MANAGEMENT
// =============================================================================

/**
 * Get user's achievements
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
      query: params,
    });

    if (response.error || !response.data) {
      throw new Error('Failed to fetch user achievements');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching user achievements:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user achievements');
  }
}

/**
 * Get user's achievement progress
 */
export async function getUserAchievementProgress(userId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiUsersByUserIdAchievementsProgress({
      path: { userId },
    });

    if (response.error || !response.data) {
      throw new Error('Failed to fetch user achievement progress');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching user achievement progress:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user achievement progress');
  }
}

/**
 * Get user's achievement summary
 */
export async function getUserAchievementSummary(userId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiUsersByUserIdAchievementsSummary({
      path: { userId },
    });

    if (response.error || !response.data) {
      throw new Error('Failed to fetch user achievement summary');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching user achievement summary:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user achievement summary');
  }
}

/**
 * Get available achievements for user
 */
export async function getUserAvailableAchievements(userId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiUsersByUserIdAchievementsAvailable({
      path: { userId },
    });

    if (response.error || !response.data) {
      throw new Error('Failed to fetch available achievements');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching available achievements:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch available achievements');
  }
}

/**
 * Update user achievement progress
 */
export async function updateUserAchievementProgress(userId: string, achievementId: string, progressData: UpdateAchievementProgressRequest) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiUsersByUserIdAchievementsByAchievementIdProgress({
      path: { userId, achievementId },
      body: progressData,
    });

    if (response.error || !response.data) {
      throw new Error('Failed to update achievement progress');
    }

    revalidateTag(`user-${userId}-achievements`);
    revalidateTag(`user-${userId}`);
    return response.data;
  } catch (error) {
    console.error('Error updating achievement progress:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to update achievement progress');
  }
}

/**
 * Get achievement prerequisites for user
 */
export async function getUserAchievementPrerequisites(userId: string, achievementId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiUsersByUserIdAchievementsByAchievementIdPrerequisites({
      path: { userId, achievementId },
    });

    if (response.error || !response.data) {
      throw new Error('Failed to fetch achievement prerequisites');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching achievement prerequisites:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to fetch achievement prerequisites');
  }
}

/**
 * Mark user achievement as notified
 */
export async function markUserAchievementNotified(userId: string, userAchievementId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiUsersByUserIdAchievementsByUserAchievementIdMarkNotified({
      path: { userId, userAchievementId },
    });

    if (response.error) {
      throw new Error('Failed to mark achievement as notified');
    }

    revalidateTag(`user-${userId}-achievements`);
    return { success: true };
  } catch (error) {
    console.error('Error marking achievement as notified:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to mark achievement as notified');
  }
}

/**
 * Remove user achievement
 */
export async function removeUserAchievement(userId: string, userAchievementId: string) {
  await configureAuthenticatedClient();

  try {
    const response = await deleteApiUsersByUserIdAchievementsByUserAchievementId({
      path: { userId, userAchievementId },
    });

    if (response.error) {
      throw new Error('Failed to remove user achievement');
    }

    revalidateTag(`user-${userId}-achievements`);
    revalidateTag(`user-${userId}`);
    return { success: true };
  } catch (error) {
    console.error('Error removing user achievement:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to remove user achievement');
  }
}

// =============================================================================
// ADMINISTRATIVE FUNCTIONS
// =============================================================================

/**
 * Get comprehensive user dashboard data
 */
export async function getUserDashboardData(userId: string) {
  await configureAuthenticatedClient();

  try {
    const [user, achievements, achievementSummary, achievementProgress] = await Promise.all([getUserById(userId), getUserAchievements(userId, { pageSize: 10 }), getUserAchievementSummary(userId), getUserAchievementProgress(userId)]);

    return {
      user,
      achievements,
      achievementSummary,
      achievementProgress,
      generatedAt: new Date().toISOString(),
    };
  } catch (error) {
    console.error('Error generating user dashboard data:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to generate user dashboard data');
  }
}

/**
 * Activate multiple users
 */
export async function activateUsers(userIds: string[]) {
  return await bulkUserOperations({
    update: userIds.map((id) => ({ id, data: { isActive: true } })),
  });
}

/**
 * Deactivate multiple users
 */
export async function deactivateUsers(userIds: string[]) {
  return await bulkUserOperations({
    update: userIds.map((id) => ({ id, data: { isActive: false } })),
  });
}

/**
 * Generate user activity report
 */
export async function generateUserActivityReport(params?: { fromDate?: string; toDate?: string; includeInactive?: boolean; includeDeleted?: boolean }) {
  await configureAuthenticatedClient();

  try {
    const [users, statistics] = await Promise.all([
      getUsers({
        includeDeleted: params?.includeDeleted,
        isActive: params?.includeInactive ? undefined : true,
      }),
      getUserStatistics({
        fromDate: params?.fromDate,
        toDate: params?.toDate,
        includeDeleted: params?.includeDeleted,
      }),
    ]);

    return {
      users,
      statistics,
      summary: {
        totalUsers: Array.isArray(users) ? users.length : 0,
        reportPeriod: {
          from: params?.fromDate,
          to: params?.toDate,
        },
        generatedAt: new Date().toISOString(),
      },
    };
  } catch (error) {
    console.error('Error generating user activity report:', error);
    throw new Error(error instanceof Error ? error.message : 'Failed to generate user activity report');
  }
}
