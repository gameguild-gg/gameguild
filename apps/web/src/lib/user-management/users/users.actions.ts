'use server';

import { revalidateTag } from 'next/cache';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import {
  getApiUsers,
  getApiUsersById,
  postApiUsers,
  putApiUsersById,
  deleteApiUsersById,
  postApiUsersByIdRestore,
  putApiUsersByIdBalance,
  getApiUsersSearch,
  getApiUsersStatistics,
} from '@/lib/api/generated/sdk.gen';

/**
 * Get paginated users with optional filtering
 */
export async function getUsersData(params?: { skip?: number; take?: number; includeDeleted?: boolean; isActive?: boolean }) {
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

    revalidateTag('users');
    return { data: response.data, error: null };
  } catch (error) {
    console.error('Error in getUsersData:', error);
    return { data: null, error: 'Failed to fetch users' };
  }
}

/**
 * Get a specific user by ID
 */
export async function getUserById(id: string) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiUsersById({ path: { id } });
    return { data: response.data, error: null };
  } catch (error) {
    console.error('Error in getUserById:', error);
    return { data: null, error: 'Failed to fetch user' };
  }
}

/**
 * Create a new user
 */
export async function createUser(userData: { name: string; email: string; password?: string; tenantId?: string; roleIds?: string[]; isActive?: boolean }) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiUsers({ body: userData });
    revalidateTag('users');
    return { data: response.data, error: null };
  } catch (error) {
    console.error('Error creating user:', error);
    return { data: null, error: 'Failed to create user' };
  }
}

/**
 * Update an existing user
 */
export async function updateUser(id: string, userData: { name?: string; email?: string; isActive?: boolean }) {
  await configureAuthenticatedClient();

  try {
    const response = await putApiUsersById({ path: { id }, body: userData });
    revalidateTag('users');
    revalidateTag(`user-${id}`);
    return { data: response.data, error: null };
  } catch (error) {
    console.error('Error updating user:', error);
    return { data: null, error: 'Failed to update user' };
  }
}

/**
 * Delete a user (soft delete)
 */
export async function deleteUser(id: string) {
  await configureAuthenticatedClient();

  try {
    const response = await deleteApiUsersById({ path: { id } });
    revalidateTag('users');
    revalidateTag(`user-${id}`);
    return { data: response.data, error: null };
  } catch (error) {
    console.error('Error deleting user:', error);
    return { data: null, error: 'Failed to delete user' };
  }
}

/**
 * Restore a deleted user
 */
export async function restoreUser(id: string) {
  await configureAuthenticatedClient();

  try {
    const response = await postApiUsersByIdRestore({ path: { id } });
    revalidateTag('users');
    revalidateTag(`user-${id}`);
    return { data: response.data, error: null };
  } catch (error) {
    console.error('Error restoring user:', error);
    return { data: null, error: 'Failed to restore user' };
  }
}

/**
 * Update user balance
 */
export async function updateUserBalance(id: string, balanceData: { amount: number; reason?: string }) {
  await configureAuthenticatedClient();

  try {
    const response = await putApiUsersByIdBalance({ path: { id }, body: balanceData });
    revalidateTag('users');
    revalidateTag(`user-${id}`);
    return { data: response.data, error: null };
  } catch (error) {
    console.error('Error updating user balance:', error);
    return { data: null, error: 'Failed to update user balance' };
  }
}

/**
 * Search users
 */
export async function searchUsers(
  searchTerm: string,
  params?: {
    skip?: number;
    take?: number;
    isActive?: boolean;
    minBalance?: number;
    maxBalance?: number;
    createdAfter?: string;
    createdBefore?: string;
    includeDeleted?: boolean;
  },
) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiUsersSearch({
      query: {
        searchTerm: searchTerm,
        skip: params?.skip,
        take: params?.take,
        isActive: params?.isActive,
        minBalance: params?.minBalance,
        maxBalance: params?.maxBalance,
        createdAfter: params?.createdAfter,
        createdBefore: params?.createdBefore,
        includeDeleted: params?.includeDeleted,
      },
    });

    return { data: response.data, error: null };
  } catch (error) {
    console.error('Error searching users:', error);
    return { data: null, error: 'Failed to search users' };
  }
}

/**
 * Get user statistics
 */
export async function getUserStatistics(params?: { fromDate?: string; toDate?: string; includeDeleted?: boolean }) {
  await configureAuthenticatedClient();

  try {
    const response = await getApiUsersStatistics({
      query: {
        fromDate: params?.fromDate,
        toDate: params?.toDate,
        includeDeleted: params?.includeDeleted,
      },
    });

    return { data: response.data, error: null };
  } catch (error) {
    console.error('Error fetching user statistics:', error);
    return { data: null, error: 'Failed to fetch user statistics' };
  }
}
