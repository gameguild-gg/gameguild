'use server';

import { 
  getApiUsers,
  getApiUsersById,
  getApiUsersSearch,
  getApiUsersStatistics,
  postApiUsers,
  putApiUsersById,
  deleteApiUsersById,
  postApiUsersByIdRestore,
  putApiUsersByIdBalance,
  patchApiUsersBulkActivate,
  patchApiUsersBulkDeactivate
} from '@/lib/api/generated/sdk.gen';
import { UserSortField, SortDirection } from '@/lib/api/generated/types.gen';

export interface User {
  id: string;
  version?: number;
  name: string;
  username: string;
  email: string;
  isActive: boolean;
  balance: number;
  availableBalance: number;
  createdAt: string;
  updatedAt: string;
  deletedAt?: string;
  isDeleted: boolean;
}

export interface CreateUserRequest {
  name: string;
  email: string;
  isActive?: boolean;
  initialBalance?: number;
}

export interface UpdateUserRequest {
  name?: string;
  email?: string;
  isActive?: boolean;
  expectedVersion?: number;
}

export interface UpdateUserBalanceRequest {
  balance: number;
  availableBalance: number;
  reason?: string;
  expectedVersion?: number;
}

export interface UserSearchOptions {
  searchTerm?: string;
  isActive?: boolean;
  minBalance?: number;
  maxBalance?: number;
  createdAfter?: string;
  createdBefore?: string;
  includeDeleted?: boolean;
  skip?: number;
  take?: number;
  sortBy?: UserSortField;
  sortDirection?: SortDirection;
  [key: string]: unknown;
}

export interface UserStatistics {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  deletedUsers: number;
  totalBalance: number;
  averageBalance: number;
  newUsersThisMonth: number;
  newUsersThisWeek: number;
}

export interface BulkOperationResult {
  totalProcessed: number;
  successful: number;
  failed: number;
  errors: string[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  take: number;
}

/**
 * Get all users with optional filtering and pagination
 */
export async function getUsers(includeDeleted = false, skip = 0, take = 50, isActive?: boolean): Promise<User[]> {
  const response = await getApiUsers({
    query: { includeDeleted, skip, take, isActive },
  });

  if (response.error) {
    throw new Error(`Failed to fetch users: ${response.error}`);
  }

  return response.data as User[];
}

/**
 * Get a specific user by ID
 */
export async function getUser(id: string, includeDeleted = false): Promise<User | null> {
  const response = await getApiUsersById({
    path: { id },
    query: { includeDeleted },
  });

  if (response.error) {
    if (response.error.toString().includes('404')) {
      return null;
    }
    throw new Error(`Failed to fetch user: ${response.error}`);
  }

  return response.data as User;
}

/**
 * Create a new user
 */
export async function createUser(userData: CreateUserRequest): Promise<User> {
  const response = await postApiUsers({
    body: userData,
  });

  if (response.error) {
    throw new Error(`Failed to create user: ${response.error}`);
  }

  return response.data as User;
}

/**
 * Update an existing user
 */
export async function updateUser(id: string, userData: UpdateUserRequest): Promise<User> {
  const response = await putApiUsersById({
    path: { id },
    body: userData,
  });

  if (response.error) {
    throw new Error(`Failed to update user: ${response.error}`);
  }

  return response.data as User;
}

/**
 * Delete a user (soft delete by default)
 */
export async function deleteUser(id: string, softDelete = true, reason?: string): Promise<void> {
  const response = await deleteApiUsersById({
    path: { id },
    query: { softDelete, reason },
  });

  if (response.error) {
    throw new Error(`Failed to delete user: ${response.error}`);
  }
}

/**
 * Restore a soft-deleted user
 */
export async function restoreUser(id: string, reason?: string): Promise<void> {
  const response = await postApiUsersByIdRestore({
    path: { id },
    query: reason ? { reason } : undefined,
  });

  if (response.error) {
    throw new Error(`Failed to restore user: ${response.error}`);
  }
}

/**
 * Update user balance
 */
export async function updateUserBalance(id: string, balanceData: UpdateUserBalanceRequest): Promise<User> {
  const response = await putApiUsersByIdBalance({
    path: { id },
    body: balanceData,
  });

  if (response.error) {
    throw new Error(`Failed to update user balance: ${response.error}`);
  }

  return response.data as User;
}

/**
 * Search users with advanced filtering
 */
export async function searchUsers(options: UserSearchOptions = {}): Promise<PagedResult<User>> {
  const response = await getApiUsersSearch({
    query: options,
  });

  if (response.error) {
    throw new Error(`Failed to search users: ${response.error}`);
  }

  return response.data as PagedResult<User>;
}

/**
 * Get user statistics
 */
export async function getUserStatistics(fromDate?: string, toDate?: string, includeDeleted = false): Promise<UserStatistics> {
  const response = await getApiUsersStatistics({
    query: { fromDate, toDate, includeDeleted },
  });

  if (response.error) {
    throw new Error(`Failed to fetch user statistics: ${response.error}`);
  }

  return response.data as UserStatistics;
}

/**
 * Bulk activate users
 */
export async function bulkActivateUsers(userIds: string[], reason?: string): Promise<BulkOperationResult> {
  const response = await patchApiUsersBulkActivate({
    body: userIds,
    query: reason ? { reason } : undefined,
  });

  if (response.error) {
    throw new Error(`Failed to bulk activate users: ${response.error}`);
  }

  return response.data as BulkOperationResult;
}

/**
 * Bulk deactivate users
 */
export async function bulkDeactivateUsers(userIds: string[], reason?: string): Promise<BulkOperationResult> {
  const response = await patchApiUsersBulkDeactivate({
    body: userIds,
    query: reason ? { reason } : undefined,
  });

  if (response.error) {
    throw new Error(`Failed to bulk deactivate users: ${response.error}`);
  }

  return response.data as BulkOperationResult;
}

/**
 * Get a user by username (public version, no authentication required)
 */
export async function getUserByUsername(username: string, includeDeleted = false): Promise<User | null> {
  try {
    // Try using the public client first (for public profiles)
    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'}/api/users/search?searchTerm=${encodeURIComponent(username)}&includeDeleted=${includeDeleted}&take=1`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      if (response.status === 401) {
        console.warn('Public user search requires authentication, falling back to authenticated client');
        // Fallback to authenticated client if needed
        const authResponse = await getApiUsersSearch({
          query: { 
            searchTerm: username,
            includeDeleted,
            take: 1
          },
        });

        if (authResponse.error) {
          throw new Error(`Failed to search for user: ${authResponse.error}`);
        }

        const paged = authResponse.data as PagedResult<User> | { items?: User[]; totalCount?: number } | undefined;
        const items = (paged && (paged as any).items) as User[] | undefined;

        if (!items || items.length === 0) return null;

        const exact = items.find(u => u.username?.toLowerCase() === username.toLowerCase());
        return exact || items[0] || null;
      }
      throw new Error(`Failed to search for user: ${response.status} ${response.statusText}`);
    }

    const data = await response.json() as PagedResult<User> | { items?: User[]; totalCount?: number } | undefined;
    const items = (data && (data as any).items) as User[] | undefined;

    if (!items || items.length === 0) return null;

    // Prefer exact username match when present
    const exact = items.find(u => u.username?.toLowerCase() === username.toLowerCase());
    return exact || items[0] || null;
  } catch (error) {
    console.error('Error in getUserByUsername:', error);
    throw error;
  }
}
