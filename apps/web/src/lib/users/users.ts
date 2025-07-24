import { revalidateTag, unstable_cache } from 'next/cache';
import { PagedResult, UpdateUserRequest, User } from '@/components/legacy/types/user';

export interface UserData {
  users: User[];
  pagination?: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
  };
}

export interface ActionState {
  success: boolean;
  error?: string;
}

export interface UserActionState extends ActionState {
  user?: User;
}

// Cache configuration
const CACHE_TAGS = {
  USERS: 'users',
  USER_DETAIL: 'user-detail',
} as const;

const REVALIDATION_TIME = 300; // 5 minutes

/**
 * Get users data with caching
 */
const getCachedUsersData = unstable_cache(
  async (page: number = 1, limit: number = 20, search?: string): Promise<UserData> => {
    try {
      const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
      const skip = (page - 1) * limit;

      if (search) {
        // Use search endpoint for text search
        const params = new URLSearchParams({
          searchTerm: search,
          skip: skip.toString(),
          take: limit.toString(),
          includeDeleted: 'false',
        });

        const response = await fetch(`${apiUrl}/api/users/search?${params}`, {
          headers: {
            'Content-Type': 'application/json',
          },
          next: {
            revalidate: REVALIDATION_TIME,
            tags: [CACHE_TAGS.USERS],
          },
        });

        if (!response.ok) {
          throw new Error(`Failed to search users: ${response.status} ${response.statusText}`);
        }

        const data: PagedResult<User> = await response.json();
        return {
          users: data.items,
          pagination: {
            page,
            limit,
            total: data.totalCount,
            totalPages: Math.ceil(data.totalCount / limit),
          },
        };
      }

      // Use regular users endpoint
      const params = new URLSearchParams({
        skip: skip.toString(),
        take: limit.toString(),
        includeDeleted: 'false',
      });

      const response = await fetch(`${apiUrl}/api/users?${params}`, {
        headers: {
          'Content-Type': 'application/json',
        },
        next: {
          revalidate: REVALIDATION_TIME,
          tags: [CACHE_TAGS.USERS],
        },
      });

      if (!response.ok) {
        throw new Error(`Failed to fetch users: ${response.status} ${response.statusText}`);
      }

      const users: User[] = await response.json();

      return {
        users,
        pagination: {
          page,
          limit,
          total: users.length, // Note: This is the current page count, not total
          totalPages: Math.ceil(users.length / limit),
        },
      };
    } catch (error) {
      console.error('Error fetching users:', error);

      // Return empty data for network errors
      if (error instanceof TypeError && error.message.includes('fetch')) {
        return {
          users: [],
          pagination: { page, limit, total: 0, totalPages: 0 },
        };
      }

      throw new Error(error instanceof Error ? error.message : 'Failed to fetch users');
    }
  },
  ['users-data'],
  {
    revalidate: REVALIDATION_TIME,
    tags: [CACHE_TAGS.USERS],
  },
);

/**
 * Get paginated users data
 */
export async function getUsersData(page: number = 1, limit: number = 20, search?: string): Promise<UserData> {
  return getCachedUsersData(page, limit, search);
}

/**
 * Get user by ID with caching
 */
export async function getUserById(id: string): Promise<User | null> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

    const response = await fetch(`${apiUrl}/users/${id}`, {
      headers: {
        'Content-Type': 'application/json',
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.USER_DETAIL, `user-${id}`],
      },
    });

    if (!response.ok) {
      if (response.status === 404) {
        return null;
      }
      throw new Error(`Failed to fetch user: ${response.status} ${response.statusText}`);
    }

    return await response.json();
  } catch (error) {
    console.error('Error fetching user:', error);

    if (error instanceof TypeError && error.message.includes('fetch')) {
      return null;
    }

    throw new Error(error instanceof Error ? error.message : 'Failed to fetch user');
  }
}

/**
 * Create a new user (Server Action)
 */
export async function createUser(prevState: ActionState, formData: FormData): Promise<UserActionState> {
  try {
    const name = formData.get('name') as string;
    const email = formData.get('email') as string;
    const isActive = formData.get('isActive') === 'true';

    // Validation
    if (!name || name.trim().length < 2) {
      return { success: false, error: 'Name must be at least 2 characters long' };
    }

    if (!email || !email.includes('@')) {
      return { success: false, error: 'Please provide a valid email address' };
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

    const response = await fetch(`${apiUrl}/api/users`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        name: name.trim(),
        email: email.trim().toLowerCase(),
        isActive,
        initialBalance: 0, // Default initial balance
      }),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(errorData?.message || `Failed to create user: ${response.status} ${response.statusText}`);
    }

    const user = await response.json();

    // Revalidate caches
    await revalidateUsersData();

    return { success: true, user };
  } catch (error) {
    console.error('Error creating user:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to create user',
    };
  }
}

/**
 * Update user (Server Action)
 */
export async function updateUser(id: string, prevState: ActionState, formData: FormData): Promise<UserActionState> {
  try {
    const name = formData.get('name') as string;
    const email = formData.get('email') as string;
    const isActive = formData.get('isActive') === 'true';

    // Validation
    if (!name || name.trim().length < 2) {
      return { success: false, error: 'Name must be at least 2 characters long' };
    }

    if (!email || !email.includes('@')) {
      return { success: false, error: 'Please provide a valid email address' };
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

    const response = await fetch(`${apiUrl}/api/users/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        name: name.trim(),
        email: email.trim().toLowerCase(),
        isActive,
      }),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(errorData?.message || `Failed to update user: ${response.status} ${response.statusText}`);
    }

    const user = await response.json();

    // Revalidate caches
    await revalidateUsersData();
    revalidateTag(`user-${id}`);

    return { success: true, user };
  } catch (error) {
    console.error('Error updating user:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to update user',
    };
  }
}

/**
 * Delete user (Server Action)
 */
export async function deleteUser(id: string): Promise<{ success: boolean; error?: string }> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

    const response = await fetch(`${apiUrl}/api/users/${id}?softDelete=true`, {
      method: 'DELETE',
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(errorData?.message || `Failed to delete user: ${response.status} ${response.statusText}`);
    }

    // Revalidate caches
    await revalidateUsersData();
    revalidateTag(`user-${id}`);

    return { success: true };
  } catch (error) {
    console.error('Error deleting user:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to delete user',
    };
  }
}

/**
 * Toggle user active status (Server Action)
 */
export async function toggleUserStatus(
  id: string,
  currentStatus: boolean,
): Promise<{
  success: boolean;
  error?: string;
  user?: User;
}> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

    const response = await fetch(`${apiUrl}/api/users/${id}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        isActive: !currentStatus,
      }),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(errorData?.message || `Failed to toggle user status: ${response.status} ${response.statusText}`);
    }

    const user = await response.json();

    // Revalidate caches
    await revalidateUsersData();
    revalidateTag(`user-${id}`);

    return { success: true, user };
  } catch (error) {
    console.error('Error toggling user status:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to toggle user status',
    };
  }
}

/**
 * Revalidate users data cache
 */
export async function revalidateUsersData(): Promise<void> {
  revalidateTag(CACHE_TAGS.USERS);
  revalidateTag(CACHE_TAGS.USER_DETAIL);
}

/**
 * Bulk user operations
 */
export async function bulkUpdateUsers(
  userIds: string[],
  updates: Partial<UpdateUserRequest>,
): Promise<{ success: boolean; error?: string; updatedCount?: number }> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

    const response = await fetch(`${apiUrl}/users/bulk`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        userIds,
        updates,
      }),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(errorData?.message || `Failed to bulk update users: ${response.status} ${response.statusText}`);
    }

    const result = await response.json();

    // Revalidate caches
    await revalidateUsersData();
    userIds.forEach((id) => revalidateTag(`user-${id}`));

    return { success: true, updatedCount: result.updatedCount || userIds.length };
  } catch (error) {
    console.error('Error bulk updating users:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to bulk update users',
    };
  }
}

/**
 * Search users
 */
export async function searchUsers(query: string, limit: number = 10): Promise<User[]> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

    const response = await fetch(`${apiUrl}/users/search?q=${encodeURIComponent(query)}&limit=${limit}`, {
      headers: {
        'Content-Type': 'application/json',
      },
      next: {
        revalidate: 60, // Cache search results for 1 minute
        tags: ['user-search'],
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to search users: ${response.status} ${response.statusText}`);
    }

    const data = await response.json();
    return Array.isArray(data) ? data : data.users || [];
  } catch (error) {
    console.error('Error searching users:', error);
    return [];
  }
}

/**
 * Get user statistics (Server Action)
 */
export async function getUserStatistics(
  fromDate?: string,
  toDate?: string,
  includeDeleted: boolean = false,
): Promise<{ success: boolean; error?: string; statistics?: unknown }> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
    const params = new URLSearchParams({
      ...(fromDate && { fromDate }),
      ...(toDate && { toDate }),
      includeDeleted: includeDeleted.toString(),
    });

    const response = await fetch(`${apiUrl}/api/users/statistics?${params}`, {
      headers: {
        'Content-Type': 'application/json',
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.USERS],
      },
    });

    if (!response.ok) {
      throw new Error(`Failed to fetch user statistics: ${response.status} ${response.statusText}`);
    }

    const statistics = await response.json();
    return { success: true, statistics };
  } catch (error) {
    console.error('Error fetching user statistics:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to fetch user statistics',
    };
  }
}

/**
 * Bulk activate users (Server Action)
 */
export async function bulkActivateUsers(
  userIds: string[],
  reason?: string,
): Promise<{
  success: boolean;
  error?: string;
  result?: unknown;
}> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
    const params = new URLSearchParams();
    if (reason) params.append('reason', reason);

    const response = await fetch(`${apiUrl}/api/users/bulk/activate?${params}`, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(userIds),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(errorData?.message || `Failed to activate users: ${response.status} ${response.statusText}`);
    }

    const result = await response.json();

    // Revalidate caches
    await revalidateUsersData();

    return { success: true, result };
  } catch (error) {
    console.error('Error activating users:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to activate users',
    };
  }
}

/**
 * Bulk deactivate users (Server Action)
 */
export async function bulkDeactivateUsers(
  userIds: string[],
  reason?: string,
): Promise<{
  success: boolean;
  error?: string;
  result?: unknown;
}> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
    const params = new URLSearchParams();
    if (reason) params.append('reason', reason);

    const response = await fetch(`${apiUrl}/api/users/bulk/deactivate?${params}`, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(userIds),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => null);
      throw new Error(errorData?.message || `Failed to deactivate users: ${response.status} ${response.statusText}`);
    }

    const result = await response.json();

    // Revalidate caches
    await revalidateUsersData();

    return { success: true, result };
  } catch (error) {
    console.error('Error deactivating users:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to deactivate users',
    };
  }
}
