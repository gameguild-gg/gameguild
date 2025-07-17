import { getApiPosts, getApiPostsByPostId } from '@/lib/api/generated/sdk.gen';
import type { PostDto, PostsPageDto } from './types';

export interface GetPostsParams {
  pageNumber?: number;
  pageSize?: number;
  postType?: string;
  userId?: string;
  isPinned?: boolean;
  searchTerm?: string;
  orderBy?: string;
  descending?: boolean;
  tenantId?: string;
}

/**
 * Fetch posts from the API with pagination and filtering
 */
export async function fetchPosts(params: GetPostsParams = {}): Promise<PostsPageDto> {
  try {
    const response = await getApiPosts({
      query: {
        pageNumber: params.pageNumber ?? 1,
        pageSize: params.pageSize ?? 20,
        postType: params.postType,
        userId: params.userId,
        isPinned: params.isPinned,
        searchTerm: params.searchTerm,
        orderBy: params.orderBy ?? 'createdAt',
        descending: params.descending ?? true,
        tenantId: params.tenantId,
      },
    });

    if (response.error) {
      throw new Error(`Failed to fetch posts: ${response.error}`);
    }

    if (!response.data) {
      throw new Error('No data received from API');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching posts:', error);
    throw error;
  }
}

/**
 * Fetch a single post by ID
 */
export async function fetchPostById(postId: string): Promise<PostDto> {
  try {
    const response = await getApiPostsByPostId({
      path: { postId },
    });

    if (response.error) {
      throw new Error(`Failed to fetch post: ${response.error}`);
    }

    if (!response.data) {
      throw new Error('No data received from API');
    }

    return response.data;
  } catch (error) {
    console.error('Error fetching post:', error);
    throw error;
  }
}

/**
 * Fetch posts by type (e.g., announcements, achievements, etc.)
 */
export async function fetchPostsByType(postType: string, pageSize = 10): Promise<PostsPageDto> {
  return fetchPosts({
    postType,
    pageSize,
    orderBy: 'createdAt',
    descending: true,
  });
}

/**
 * Fetch pinned posts
 */
export async function fetchPinnedPosts(): Promise<PostsPageDto> {
  return fetchPosts({
    isPinned: true,
    pageSize: 5,
    orderBy: 'createdAt',
    descending: true,
  });
}

/**
 * Fetch recent posts for the community feed
 */
export async function fetchRecentPosts(pageSize = 20): Promise<PostsPageDto> {
  return fetchPosts({
    pageSize,
    orderBy: 'createdAt',
    descending: true,
  });
}

/**
 * Search posts by term
 */
export async function searchPosts(searchTerm: string, pageSize = 20): Promise<PostsPageDto> {
  return fetchPosts({
    searchTerm,
    pageSize,
    orderBy: 'createdAt',
    descending: true,
  });
}
