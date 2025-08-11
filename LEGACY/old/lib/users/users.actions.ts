import { revalidateTag } from 'next/cache';
import { auth } from '@/auth';
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

export interface UserStatistics {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  deletedUsers: number;
  totalBalance: number;
  averageBalance: number;
  usersCreatedToday: number;
  usersCreatedThisWeek: number;
  usersCreatedThisMonth: number;
}

// Cache configuration
const CACHE_TAGS = {
  USERS: 'users',
  USER_DETAIL: 'user-detail',
  USER_STATISTICS: 'user-statistics',
} as const;

const REVALIDATION_TIME = 300; // 5 minutes

/**
 * Get users data with authentication
 */
export async function getUsersData(page: number = 1, limit: number = 20, search?: string): Promise<UserData> {
  try {
    // Get authenticated session
    const session = await auth();

    if (!session?.api.accessToken) {
      return {
        users: [],
        pagination: { page, limit, total: 0, totalPages: 0 },
      };
    }

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
          Authorization: `Bearer ${session.api.accessToken}`,
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
        Authorization: `Bearer ${session.api.accessToken}`,
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
}

/**
 * Get user by ID with authentication
 */
export async function getUserById(id: string): Promise<User | null> {
  try {
    // Get authenticated session
    const session = await auth();

    if (!session?.api.accessToken) {
      return null;
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

    const response = await fetch(`${apiUrl}/api/users/${id}`, {
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
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
    // Get authenticated session
    const session = await auth();

    if (!session?.api.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

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
        Authorization: `Bearer ${session.api.accessToken}`,
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
    // Get authenticated session
    const session = await auth();

    if (!session?.api.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

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
        Authorization: `Bearer ${session.api.accessToken}`,
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
    // Get authenticated session
    const session = await auth();

    if (!session?.api.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

    const response = await fetch(`${apiUrl}/api/users/${id}?softDelete=true`, {
      method: 'DELETE',
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
        'Content-Type': 'application/json',
      },
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
    // Get authenticated session
    const session = await auth();

    if (!session?.api.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

    const response = await fetch(`${apiUrl}/api/users/${id}`, {
      method: 'PUT',
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
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
    // Get authenticated session
    const session = await auth();

    if (!session?.api.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

    const response = await fetch(`${apiUrl}/users/bulk`, {
      method: 'PUT',
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
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
 * Search users using the backend UserProfiles API
 */
export async function searchUsers(query: string, limit: number = 10): Promise<User[]> {
  try {
    // Get authenticated session
    const session = await auth();

    if (!session?.api.accessToken) {
      console.warn('Authentication required for user search');
      return [];
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

    // Use the UserProfiles endpoint with searchTerm parameter
    const response = await fetch(`${apiUrl}/api/userprofiles?searchTerm=${encodeURIComponent(query)}&take=${limit}&includeDeleted=false`, {
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
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
    console.log('🔍 [SEARCH DEBUG] Raw API response:', data);

    // Transform UserProfile data to User format
    if (Array.isArray(data)) {
      const users = data.map(
        (profile: {
          id: string;
          version: number;
          displayName?: string;
          givenName?: string;
          familyName?: string;
          email?: string;
          isDeleted?: boolean;
          createdAt: string;
          updatedAt: string;
        }) => ({
          id: profile.id,
          version: profile.version,
          name: profile.displayName || profile.givenName + ' ' + profile.familyName,
          email: profile.email || `${profile.displayName}@unknown.com`, // UserProfile doesn't have email
          isActive: !profile.isDeleted,
          balance: 0, // UserProfile doesn't have balance
          availableBalance: 0,
          createdAt: profile.createdAt,
          updatedAt: profile.updatedAt,
          deletedAt: profile.deletedAt,
          isDeleted: profile.isDeleted,
        }),
      );

      console.log('✅ [SEARCH DEBUG] Transformed users:', users.length);
      return users;
    }

    return [];
  } catch (error) {
    console.error('❌ [SEARCH DEBUG] Error searching users:', error);
    return [];
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
    // Get authenticated session
    const session = await auth();

    if (!session?.api.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
    const params = new URLSearchParams();
    if (reason) params.append('reason', reason);

    const response = await fetch(`${apiUrl}/api/users/bulk/activate?${params}`, {
      method: 'PATCH',
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
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
    // Get authenticated session
    const session = await auth();

    if (!session?.api.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
    const params = new URLSearchParams();
    if (reason) params.append('reason', reason);

    const response = await fetch(`${apiUrl}/api/users/bulk/deactivate?${params}`, {
      method: 'PATCH',
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
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

/**
 * Get user by username/displayName search
 */
export async function getUserByUsername(username: string): Promise<User | null> {
  try {
    console.log('🔍 [USER DEBUG] Searching for user:', username);

    // Search for users with matching name or email
    const users = await searchUsers(username, 50);

    if (!users || users.length === 0) {
      console.log('❌ [USER DEBUG] No users found in UserProfiles, creating mock user for demonstration');

      // For demonstration purposes, create a mock user based on the username
      // In a real app, you would either:
      // 1. Automatically create a UserProfile when a user registers
      // 2. Use the authentication user data directly
      const mockUser: User = {
        id: `mock-${username}`,
        version: 1,
        name: username,
        email: `${username}@example.com`,
        isActive: true,
        balance: 100,
        availableBalance: 100,
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
        isDeleted: false,
      };

      console.log('✅ [USER DEBUG] Created mock user for demonstration:', mockUser.name);
      return mockUser;
    }

    console.log('📊 [USER DEBUG] Search found users:', users.length);

    // Look for exact match first (case insensitive)
    const exactMatch = users.find((user: User) => user.name?.toLowerCase() === username.toLowerCase() || user.email?.toLowerCase() === username.toLowerCase());

    if (exactMatch) {
      console.log('✅ [USER DEBUG] Exact match found:', exactMatch.name || exactMatch.email);
      return exactMatch;
    }

    // If no exact match, look for partial matches
    const partialMatch = users.find(
      (user: User) => user.name?.toLowerCase().includes(username.toLowerCase()) || user.email?.toLowerCase().includes(username.toLowerCase()),
    );

    if (partialMatch) {
      console.log('✅ [USER DEBUG] Partial match found:', partialMatch.name || partialMatch.email);
      return partialMatch;
    }

    console.log('❌ [USER DEBUG] No close matches found for:', username);
    return null;
  } catch (error) {
    console.error('❌ [USER DEBUG] Error searching for user:', {
      username,
      error: error instanceof Error ? error.message : error,
      stack: error instanceof Error ? error.stack : 'No stack trace',
    });
    return null;
  }
}

/**
 * Check if a user exists by username
 */
export async function userExists(username: string): Promise<boolean> {
  try {
    const user = await getUserByUsername(username);
    const exists = user !== null;
    console.log('🔍 [USER DEBUG] User exists check:', { username, exists });
    return exists;
  } catch (error) {
    console.error('❌ [USER DEBUG] Error checking if user exists:', error);
    return false;
  }
}

/**
 * Get current authenticated user
 */
export async function getCurrentUser(): Promise<User | null> {
  try {
    const result = await getUsersData();
    if (!result.users || result.users.length === 0) {
      return null;
    }

    // For now, return the first user as current user
    // In a real app, this would get the current session user
    return result.users[0];
  } catch (error) {
    console.error('❌ [USER DEBUG] Error getting current user:', error);
    return null;
  }
}

/**
 * Server action to fetch user statistics with caching
 * Uses Next.js 15+ caching and revalidation features
 */
export async function getUserStatistics(
  fromDate?: string,
  toDate?: string,
  includeDeleted: boolean = false,
): Promise<{ success: boolean; data?: UserStatistics; error?: string }> {
  try {
    // Get authenticated session
    const session = await auth();

    if (!session?.api.accessToken) {
      return {
        success: false,
        error: 'Authentication required',
      };
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
    const params = new URLSearchParams();

    if (fromDate) params.append('fromDate', fromDate);
    if (toDate) params.append('toDate', toDate);
    if (includeDeleted) params.append('includeDeleted', 'true');

    const response = await fetch(`${apiUrl}/api/users/statistics?${params.toString()}`, {
      headers: {
        Authorization: `Bearer ${session.api.accessToken}`,
        'Content-Type': 'application/json',
      },
      next: {
        revalidate: 60, // Cache for 1 minute
        tags: [CACHE_TAGS.USER_STATISTICS],
      },
    });

    if (!response.ok) {
      return {
        success: false,
        error: `Failed to fetch user statistics: ${response.statusText}`,
      };
    }

    const userStatistics = await response.json();

    return {
      success: true,
      data: userStatistics,
    };
  } catch (error) {
    console.error('Error fetching user statistics:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error occurred',
    };
  }
}

/**
 * Note: This function is called from server-side refresh actions only
 * Do not import this directly in client components
 */
export async function refreshUserStatistics(): Promise<{ success: boolean; error?: string }> {
  try {
    revalidateTag(CACHE_TAGS.USER_STATISTICS);

    return {
      success: true,
    };
  } catch (error) {
    console.error('Error refreshing user statistics:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to refresh statistics',
    };
  }
}
