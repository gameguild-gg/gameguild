/**
 * Client-side API utilities for dashboard statistics
 * This module provides client-compatible functions for fetching dashboard data
 */

export interface UserStatistics {
  totalUsers: number;
  activeUsers: number;
  inactiveUsers: number;
  deletedUsers: number;
  totalBalance: number;
  averageBalance: number;
  newUsersToday: number;
  newUsersThisWeek: number;
  newUsersThisMonth: number;
}

export interface StatisticsResponse {
  success: boolean;
  error?: string;
  statistics?: UserStatistics;
}

/**
 * Client-side function to fetch user statistics
 * This is compatible with client components and doesn't use server-side caching
 */
export async function fetchUserStatistics(
  fromDate?: string,
  toDate?: string,
  includeDeleted: boolean = false,
): Promise<StatisticsResponse> {
  try {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';
    const params = new URLSearchParams({
      ...(fromDate && { fromDate }),
      ...(toDate && { toDate }),
      includeDeleted: includeDeleted.toString(),
    });

    const response = await fetch(`${apiUrl}/api/users/statistics?${params}`, {
      headers: {
        'Content-Type': 'application/json',
      },
      // No server-side caching for client components
      cache: 'no-store',
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
 * Client-side function to fetch program statistics
 * TODO: Implement when backend endpoint is available
 */
export async function fetchProgramStatistics(): Promise<{
  success: boolean;
  error?: string;
  statistics?: any;
}> {
  // Mock implementation for now
  return {
    success: true,
    statistics: {
      totalPrograms: 24,
      publishedPrograms: 18,
      draftPrograms: 6,
      totalEnrollments: 1250,
      completionRate: 72.5,
      totalRevenue: 45600,
      averageRating: 4.3,
    },
  };
}
