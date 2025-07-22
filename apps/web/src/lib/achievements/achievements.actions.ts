import { revalidateTag, unstable_cache } from 'next/cache';
import { auth } from '@/auth';

// Types for User Achievements
export interface Achievement {
  id: string;
  name: string;
  description?: string;
  category?: string;
  type?: string;
  points: number;
  badgeUrl?: string;
  isSecret: boolean;
  isActive: boolean;
  displayOrder: number;
  prerequisites?: string[];
  createdAt: string;
  updatedAt: string;
  tenantId: string;
}

export interface UserAchievement {
  id: string;
  userId: string;
  achievementId: string;
  achievement?: Achievement;
  earnedAt: string;
  progress: number;
  isCompleted: boolean;
  tenantId: string;
}

export interface AchievementStatistics {
  totalAchievements: number;
  activeAchievements: number;
  secretAchievements: number;
  userAchievements: number;
  completionRate: number;
  averagePoints: number;
  topCategories: Array<{ category: string; count: number }>;
  recentAchievements: number;
}

export interface AchievementsResponse {
  success: boolean;
  data?: Achievement[];
  error?: string;
  pagination?: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
  };
}

export interface UserAchievementsResponse {
  success: boolean;
  data?: UserAchievement[];
  error?: string;
  pagination?: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
  };
}

// Cache configuration
const CACHE_TAGS = {
  ACHIEVEMENTS: 'achievements',
  USER_ACHIEVEMENTS: 'user-achievements',
  ACHIEVEMENT_STATISTICS: 'achievement-statistics',
} as const;

const REVALIDATION_TIME = 300; // 5 minutes

/**
 * Get achievements with authentication
 */
export async function getAchievements(
  page: number = 1,
  limit: number = 20,
  category?: string,
  type?: string,
  isActive?: boolean
): Promise<AchievementsResponse> {
  try {
    const session = await auth();
    if (!session?.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const queryParams = new URLSearchParams({
      pageNumber: page.toString(),
      pageSize: limit.toString(),
      ...(category && { category }),
      ...(type && { type }),
      ...(isActive !== undefined && { isActive: isActive.toString() }),
    });

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/achievements?${queryParams}`, {
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.ACHIEVEMENTS],
      },
    });

    if (!response.ok) {
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    const result = await response.json();
    const achievements = result.achievements || result.data || result;
    
    return {
      success: true,
      data: Array.isArray(achievements) ? achievements : [],
      pagination: {
        page,
        limit,
        total: result.totalCount || achievements.length,
        totalPages: Math.ceil((result.totalCount || achievements.length) / limit),
      },
    };
  } catch (error) {
    console.error('Error fetching achievements:', error);
    return { 
      success: false, 
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}

/**
 * Get user achievements
 */
export async function getUserAchievements(
  userId?: string,
  page: number = 1,
  limit: number = 20
): Promise<UserAchievementsResponse> {
  try {
    const session = await auth();
    if (!session?.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const targetUserId = userId || session.userId;
    if (!targetUserId) {
      return { success: false, error: 'User ID required' };
    }

    const queryParams = new URLSearchParams({
      pageNumber: page.toString(),
      pageSize: limit.toString(),
    });

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/achievements/user/${targetUserId}?${queryParams}`, {
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.USER_ACHIEVEMENTS, `user-achievements-${targetUserId}`],
      },
    });

    if (!response.ok) {
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    const result = await response.json();
    const userAchievements = result.userAchievements || result.data || result;
    
    return {
      success: true,
      data: Array.isArray(userAchievements) ? userAchievements : [],
      pagination: {
        page,
        limit,
        total: result.totalCount || userAchievements.length,
        totalPages: Math.ceil((result.totalCount || userAchievements.length) / limit),
      },
    };
  } catch (error) {
    console.error('Error fetching user achievements:', error);
    return { 
      success: false, 
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}

/**
 * Get achievement statistics
 */
export async function getAchievementStatistics(): Promise<{ success: boolean; data?: AchievementStatistics; error?: string }> {
  try {
    const session = await auth();
    if (!session?.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/achievements/statistics`, {
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.ACHIEVEMENT_STATISTICS],
      },
    });

    if (!response.ok) {
      // If endpoint doesn't exist, return mock data
      if (response.status === 404) {
        return {
          success: true,
          data: {
            totalAchievements: 0,
            activeAchievements: 0,
            secretAchievements: 0,
            userAchievements: 0,
            completionRate: 0,
            averagePoints: 0,
            topCategories: [],
            recentAchievements: 0,
          }
        };
      }
      
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    const statistics: AchievementStatistics = await response.json();
    return { success: true, data: statistics };
  } catch (error) {
    console.error('Error fetching achievement statistics:', error);
    return { 
      success: false, 
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}

/**
 * Award achievement to user
 */
export async function awardAchievement(
  userId: string,
  achievementId: string
): Promise<{ success: boolean; data?: UserAchievement; error?: string }> {
  'use server';
  
  try {
    const session = await auth();
    if (!session?.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/achievements/award`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
      body: JSON.stringify({
        userId,
        achievementId,
      }),
    });

    if (!response.ok) {
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    const userAchievement: UserAchievement = await response.json();
    
    // Revalidate cache
    revalidateTag(CACHE_TAGS.USER_ACHIEVEMENTS);
    revalidateTag(`user-achievements-${userId}`);
    revalidateTag(CACHE_TAGS.ACHIEVEMENT_STATISTICS);
    
    return { success: true, data: userAchievement };
  } catch (error) {
    console.error('Error awarding achievement:', error);
    return { 
      success: false, 
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}

/**
 * Create a new achievement (admin only)
 */
export async function createAchievement(achievementData: Omit<Achievement, 'id' | 'createdAt' | 'updatedAt' | 'tenantId'>): Promise<{ success: boolean; data?: Achievement; error?: string }> {
  'use server';
  
  try {
    const session = await auth();
    if (!session?.accessToken) {
      return { success: false, error: 'Authentication required' };
    }

    const response = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/api/achievements`, {
      method: 'POST',
      headers: {
        Authorization: `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
        ...(session.tenantId && { 'X-Tenant-ID': session.tenantId }),
      },
      body: JSON.stringify(achievementData),
    });

    if (!response.ok) {
      const errorText = await response.text();
      return { success: false, error: `API error: ${response.status} - ${errorText}` };
    }

    const achievement: Achievement = await response.json();
    
    // Revalidate cache
    revalidateTag(CACHE_TAGS.ACHIEVEMENTS);
    revalidateTag(CACHE_TAGS.ACHIEVEMENT_STATISTICS);
    
    return { success: true, data: achievement };
  } catch (error) {
    console.error('Error creating achievement:', error);
    return { 
      success: false, 
      error: error instanceof Error ? error.message : 'Unknown error',
    };
  }
}

/**
 * Server-only function to refresh achievements cache
 */
export async function refreshAchievements(): Promise<void> {
  'use server';
  revalidateTag(CACHE_TAGS.ACHIEVEMENTS);
  revalidateTag(CACHE_TAGS.USER_ACHIEVEMENTS);
  revalidateTag(CACHE_TAGS.ACHIEVEMENT_STATISTICS);
}

/**
 * Cached version of getAchievements for better performance
 */
export const getCachedAchievements = unstable_cache(
  async (page: number, limit: number, category?: string, type?: string, isActive?: boolean) => {
    return getAchievements(page, limit, category, type, isActive);
  },
  ['achievements'],
  {
    tags: [CACHE_TAGS.ACHIEVEMENTS],
    revalidate: REVALIDATION_TIME,
  }
);
