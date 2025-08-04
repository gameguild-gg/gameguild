'use server';

import { revalidateTag } from 'next/cache';
import { getUsersData } from '@/lib/users/users.actions';
import { getRecentPosts } from '@/lib/posts/posts.actions';

export interface ActivityItem {
  id: string;
  type: 'user_joined' | 'post_created' | 'course_completed' | 'achievement_earned';
  title: string;
  description: string;
  user: {
    id: string;
    name: string;
    avatar?: string;
  };
  metadata?: {
    postTitle?: string;
    courseName?: string;
    achievementName?: string;
    timestamp?: string;
  };
  timestamp: string;
}

export interface RecentActivityData {
  activities: ActivityItem[];
  total: number;
}

// Cache configuration
const CACHE_TAGS = {
  RECENT_ACTIVITY: 'recent-activity',
} as const;

/**
 * Server action to fetch recent activity combining users and posts
 */
export async function getRecentActivity(limit: number = 10): Promise<{ success: boolean; data?: RecentActivityData; error?: string }> {
  try {
    // Fetch recent users and posts in parallel
    const [usersResult, postsResult] = await Promise.all([getUsersData(1, Math.ceil(limit / 2)), getRecentPosts(Math.ceil(limit / 2))]);

    const activities: ActivityItem[] = [];

    // Add user activities
    if (usersResult.users && usersResult.users.length > 0) {
      const userActivities = usersResult.users.map((user) => ({
        id: `user-${user.id}`,
        type: 'user_joined' as const,
        title: 'New User Joined',
        description: `${user.name} joined the platform`,
        user: {
          id: user.id,
          name: user.name,
          avatar: undefined, // User interface doesn't have avatar
        },
        metadata: {
          timestamp: user.createdAt,
        },
        timestamp: user.createdAt,
      }));

      activities.push(...userActivities);
    }

    // Add post activities
    if (postsResult.success && postsResult.data) {
      const postActivities = postsResult.data.map((post) => ({
        id: `post-${post.id}`,
        type: 'post_created' as const,
        title: 'New Post Published',
        description: post.title,
        user: {
          id: post.authorId,
          name: post.authorName,
          avatar: post.authorAvatar || undefined,
        },
        metadata: {
          postTitle: post.title,
          timestamp: post.createdAt,
        },
        timestamp: post.createdAt,
      }));

      activities.push(...postActivities);
    }

    // Sort by timestamp (most recent first) and limit
    const sortedActivities = activities.sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime()).slice(0, limit);

    return {
      success: true,
      data: {
        activities: sortedActivities,
        total: sortedActivities.length,
      },
    };
  } catch (error) {
    console.error('Error fetching recent activity:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error occurred',
    };
  }
}

/**
 * Revalidate recent activity cache
 /**
 * Note: This function is called from server-side refresh actions only
 * Do not import this directly in client components
 */
export async function refreshRecentActivity(): Promise<{ success: boolean; error?: string }> {
  try {
    revalidateTag(CACHE_TAGS.RECENT_ACTIVITY);

    return {
      success: true,
    };
  } catch (error) {
    console.error('Error refreshing recent activity:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to refresh activity',
    };
  }
}
