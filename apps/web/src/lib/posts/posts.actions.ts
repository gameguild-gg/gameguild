'use server';

import { revalidateTag, unstable_cache } from 'next/cache';
import { auth } from '@/auth';

export interface Post {
  id: string;
  title: string;
  content: string;
  authorId: string;
  authorName: string;
  authorAvatar?: string;
  createdAt: string;
  updatedAt: string;
  isPublished: boolean;
  tags?: string[];
  viewCount: number;
  likeCount: number;
}

export interface PostsData {
  posts: Post[];
  pagination?: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
  };
}

// Cache configuration
const CACHE_TAGS = {
  POSTS: 'posts',
  POST_DETAIL: 'post-detail',
} as const;

const REVALIDATION_TIME = 120; // 2 minutes for posts

/**
 * Server action to fetch recent posts with caching
 */
export async function getRecentPosts(
  limit: number = 10,
  includeUnpublished: boolean = false
): Promise<{ success: boolean; data?: Post[]; error?: string }> {
  try {
    const session = await auth();

    if (!session?.accessToken) {
      return {
        success: false,
        error: 'Authentication required'
      };
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';
    const params = new URLSearchParams({
      skip: '0',
      take: limit.toString(),
      sortBy: 'createdAt',
      sortOrder: 'desc'
    });

    if (includeUnpublished) {
      params.append('includeUnpublished', 'true');
    }

    const response = await fetch(`${apiUrl}/api/posts?${params.toString()}`, {
      headers: {
        'Authorization': `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.POSTS],
      },
    });

    if (!response.ok) {
      return {
        success: false,
        error: `Failed to fetch posts: ${response.statusText}`
      };
    }

    const postsData = await response.json();
    
    return {
      success: true,
      data: Array.isArray(postsData) ? postsData : postsData.items || []
    };
  } catch (error) {
    console.error('Error fetching recent posts:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error occurred'
    };
  }
}

/**
 * Server action to fetch all posts with pagination
 */
export async function getPosts(
  page: number = 1,
  limit: number = 20,
  search?: string
): Promise<{ success: boolean; data?: PostsData; error?: string }> {
  try {
    const session = await auth();

    if (!session?.accessToken) {
      return {
        success: false,
        error: 'Authentication required'
      };
    }

    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5001';
    const skip = (page - 1) * limit;
    const params = new URLSearchParams({
      skip: skip.toString(),
      take: limit.toString(),
      sortBy: 'createdAt',
      sortOrder: 'desc'
    });

    if (search) {
      params.append('search', search);
    }

    const response = await fetch(`${apiUrl}/api/posts?${params.toString()}`, {
      headers: {
        'Authorization': `Bearer ${session.accessToken}`,
        'Content-Type': 'application/json',
      },
      next: {
        revalidate: REVALIDATION_TIME,
        tags: [CACHE_TAGS.POSTS],
      },
    });

    if (!response.ok) {
      return {
        success: false,
        error: `Failed to fetch posts: ${response.statusText}`
      };
    }

    const result = await response.json();
    
    return {
      success: true,
      data: {
        posts: Array.isArray(result) ? result : result.items || [],
        pagination: result.pagination || {
          page,
          limit,
          total: result.length || 0,
          totalPages: Math.ceil((result.length || 0) / limit)
        }
      }
    };
  } catch (error) {
    console.error('Error fetching posts:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error occurred'
    };
  }
}

/**
 * Revalidate posts cache
 */
export async function refreshPosts(): Promise<{ success: boolean; error?: string }> {
  try {
    revalidateTag(CACHE_TAGS.POSTS);
    
    return {
      success: true
    };
  } catch (error) {
    console.error('Error refreshing posts:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Failed to refresh posts'
    };
  }
}
