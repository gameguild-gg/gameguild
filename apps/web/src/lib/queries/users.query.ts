import type {
    CreateUserDto,
    UpdateUserDto,
    UserResponseDto,
    UserResponseDtoPagedResult
} from '@/lib/api/generated/types.gen';
import { queryOptions, useMutation, useQueryClient } from '@tanstack/react-query';

export interface UserData {
    users: UserResponseDto[];
    pagination?: {
        page: number;
        limit: number;
        total: number;
        totalPages: number;
    };
}

export interface UserSearchParams {
    page?: number;
    limit?: number;
    search?: string;
    includeDeleted?: boolean;
}

export interface UserStatsData {
    totalUsers: number;
    activeUsers: number;
    newUsersThisMonth: number;
    deletedUsers: number;
}

// API fetching functions
async function fetchUsers(params: UserSearchParams = {}): Promise<UserData> {
    const { page = 1, limit = 20, search, includeDeleted = false } = params;
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';
    const skip = (page - 1) * limit;

    try {
        if (search) {
            // Use search endpoint for text search
            const searchParams = new URLSearchParams({
                searchTerm: search,
                skip: skip.toString(),
                take: limit.toString(),
                includeDeleted: includeDeleted.toString(),
            });

            const response = await fetch(`${apiUrl}/api/users/search?${searchParams}`, {
                headers: {
                    'Content-Type': 'application/json',
                },
            });

            if (!response.ok) {
                throw new Error(`Search failed: ${response.statusText}`);
            }

            const users: UserResponseDto[] = await response.json();

            return {
                users,
                pagination: {
                    page,
                    limit,
                    total: users.length, // Note: This is the current page count, not total
                    totalPages: Math.ceil(users.length / limit),
                },
            };
        } else {
            // Use regular users endpoint for paginated list
            const listParams = new URLSearchParams({
                skip: skip.toString(),
                take: limit.toString(),
                includeDeleted: includeDeleted.toString(),
            });

            const response = await fetch(`${apiUrl}/api/users?${listParams}`, {
                headers: {
                    'Content-Type': 'application/json',
                },
            });

            if (!response.ok) {
                throw new Error(`Fetch failed: ${response.statusText}`);
            }

            const result: UserResponseDtoPagedResult = await response.json();

            return {
                users: result.items || [],
                pagination: {
                    page,
                    limit,
                    total: result.totalCount || 0,
                    totalPages: Math.ceil((result.totalCount || 0) / limit),
                },
            };
        }
    } catch (error) {
        console.error('Error fetching users:', error);

        // Return empty data for network errors to prevent crashes
        if (error instanceof TypeError && error.message.includes('fetch')) {
            return {
                users: [],
                pagination: { page, limit, total: 0, totalPages: 0 },
            };
        }

        throw new Error(error instanceof Error ? error.message : 'Failed to fetch users');
    }
}

async function fetchUserById(id: string): Promise<UserResponseDto> {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';

    const response = await fetch(`${apiUrl}/users/${id}`, {
        headers: {
            'Content-Type': 'application/json',
        },
    });

    if (!response.ok) {
        throw new Error(`Failed to fetch user: ${response.statusText}`);
    }

    return response.json();
}

async function fetchUserStats(): Promise<UserStatsData> {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';

    const response = await fetch(`${apiUrl}/api/users/statistics`, {
        headers: {
            'Content-Type': 'application/json',
        },
    });

    if (!response.ok) {
        throw new Error(`Failed to fetch user statistics: ${response.statusText}`);
    }

    return response.json();
}

async function createUser(userData: CreateUserDto): Promise<UserResponseDto> {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';

    const response = await fetch(`${apiUrl}/api/users`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(userData),
    });

    if (!response.ok) {
        const errorData = await response.text();
        throw new Error(`Failed to create user: ${errorData}`);
    }

    return response.json();
}

async function updateUser(id: string, userData: UpdateUserDto): Promise<UserResponseDto> {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';

    const response = await fetch(`${apiUrl}/api/users/${id}`, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(userData),
    });

    if (!response.ok) {
        const errorData = await response.text();
        throw new Error(`Failed to update user: ${errorData}`);
    }

    return response.json();
}

async function deleteUser(id: string, softDelete: boolean = true): Promise<void> {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5295';

    const response = await fetch(`${apiUrl}/api/users/${id}?softDelete=${softDelete}`, {
        method: 'DELETE',
        headers: {
            'Content-Type': 'application/json',
        },
    });

    if (!response.ok) {
        const errorData = await response.text();
        throw new Error(`Failed to delete user: ${errorData}`);
    }
}

// Query options for React Query
export const userQueries = {
    all: () => ['users'] as const,

    lists: () => [...userQueries.all(), 'list'] as const,
    list: (params: UserSearchParams) =>
        queryOptions({
            queryKey: [...userQueries.lists(), params] as const,
            queryFn: () => fetchUsers(params),
            staleTime: 2 * 60 * 1000, // 2 minutes
            gcTime: 5 * 60 * 1000, // 5 minutes
        }),

    details: () => [...userQueries.all(), 'detail'] as const,
    detail: (id: string) =>
        queryOptions({
            queryKey: [...userQueries.details(), id] as const,
            queryFn: () => fetchUserById(id),
            staleTime: 5 * 60 * 1000, // 5 minutes
            gcTime: 10 * 60 * 1000, // 10 minutes
        }),

    stats: () =>
        queryOptions({
            queryKey: [...userQueries.all(), 'stats'] as const,
            queryFn: fetchUserStats,
            staleTime: 5 * 60 * 1000, // 5 minutes
            gcTime: 10 * 60 * 1000, // 10 minutes
        }),
} as const;

// Mutation hooks for user operations
export function useCreateUser() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: createUser,
        onSuccess: () => {
            // Invalidate and refetch users list
            queryClient.invalidateQueries({ queryKey: userQueries.lists() });
            queryClient.invalidateQueries({ queryKey: userQueries.stats().queryKey });
        },
    });
}

export function useUpdateUser() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({ id, data }: { id: string; data: UpdateUserDto }) => updateUser(id, data),
        onSuccess: (updatedUser) => {
            // Update the specific user in cache
            queryClient.setQueryData(
                userQueries.detail(updatedUser.id!).queryKey,
                updatedUser
            );

            // Invalidate lists to reflect changes
            queryClient.invalidateQueries({ queryKey: userQueries.lists() });
            queryClient.invalidateQueries({ queryKey: userQueries.stats().queryKey });
        },
    });
}

export function useDeleteUser() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({ id, softDelete = true }: { id: string; softDelete?: boolean }) =>
            deleteUser(id, softDelete),
        onSuccess: (_, { id }) => {
            // Remove from cache or mark as deleted
            queryClient.removeQueries({ queryKey: userQueries.detail(id).queryKey });

            // Invalidate lists to reflect changes
            queryClient.invalidateQueries({ queryKey: userQueries.lists() });
            queryClient.invalidateQueries({ queryKey: userQueries.stats().queryKey });
        },
    });
}