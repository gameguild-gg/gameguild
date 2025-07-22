'use server';

import { revalidateTag, revalidatePath } from 'next/cache';
import { auth } from '@/auth';
import { getApiPosts, postApiPosts, getApiPostsByPostId } from '@/lib/api/generated/sdk.gen';
import type { PostDto, PostsPageDto, CreatePostDto, FeedFilters } from '@/lib/feed/types';
import type { AccessLevel } from '@/lib/api/generated/types.gen';

// Cache tags for revalidation
const CACHE_TAGS = {
  POSTS: 'posts',
  POST_DETAIL: (id: string) => `post-${id}`,
  USER_POSTS: (userId: string) => `user-posts-${userId}`,
} as const;

/**
 * Server action to fetch posts with SSR support
 */
export async function fetchPostsAction(
  filters: FeedFilters = {},
  page: number = 1,
  pageSize: number = 20,
): Promise<{ success: boolean; data?: PostsPageDto; error?: string }> {
  try {
    // const session = await auth(); // TODO: Use for tenantId when available
    
    const response = await getApiPosts({
      query: {
        pageNumber: page,
        pageSize,
        postType: filters.postType,
        userId: filters.userId,
        isPinned: filters.isPinned,
        searchTerm: filters.searchTerm,
        orderBy: filters.orderBy ?? 'createdAt',
        descending: filters.descending ?? true,
        // tenantId: session?.user?.tenantId, // TODO: Add tenantId to user session
      },
      // Add caching configuration
      cache: 'force-cache',
      next: {
        tags: [CACHE_TAGS.POSTS, ...(filters.userId ? [CACHE_TAGS.USER_POSTS(filters.userId)] : [])],
        revalidate: 300, // 5 minutes
      },
    });

    if (response.error) {
      console.error('Failed to fetch posts:', response.error);
      return { success: false, error: 'Failed to fetch posts' };
    }

    if (!response.data) {
      return { success: false, error: 'No data received' };
    }

    return { success: true, data: response.data };
  } catch (error) {
    console.error('Error in fetchPostsAction:', error);
    return { success: false, error: 'An unexpected error occurred' };
  }
}

/**
 * Server action to create a new post
 */
export async function createPostAction(formData: FormData): Promise<{ success: boolean; data?: PostDto; error?: string }> {
  try {
    const session = await auth();
    
    if (!session?.user) {
      return { success: false, error: 'Authentication required' };
    }

    // Extract form data
    const title = formData.get('title') as string;
    const description = formData.get('description') as string;
    const richContent = formData.get('richContent') as string;
    const postType = formData.get('postType') as string;
    const visibility = (Number(formData.get('visibility')) as AccessLevel) || 0; // Public by default
    const contentReferences = formData.getAll('contentReferences') as string[];

    // Validate required fields
    if (!title && !description && !richContent) {
      return { success: false, error: 'Post content is required' };
    }

    const createPostDto: CreatePostDto = {
      title: title || null,
      description: description || null,
      richContent: richContent || null,
      postType: postType || 'general',
      visibility: visibility, // AccessLevel
      contentReferences: contentReferences.length > 0 ? contentReferences : null,
    };

    const response = await postApiPosts({
      body: createPostDto,
    });

    if (response.error) {
      console.error('Failed to create post:', response.error);
      return { success: false, error: 'Failed to create post' };
    }

    if (!response.data) {
      return { success: false, error: 'No data received' };
    }

    // Revalidate relevant caches
    revalidateTag(CACHE_TAGS.POSTS);
    revalidateTag(CACHE_TAGS.USER_POSTS(session.user.id));
    revalidatePath('/feed');

    return { success: true, data: response.data };
  } catch (error) {
    console.error('Error in createPostAction:', error);
    return { success: false, error: 'An unexpected error occurred' };
  }
}

/**
 * Server action to fetch a single post with comments
 */
export async function fetchPostAction(postId: string): Promise<{ success: boolean; data?: PostDto; error?: string }> {
  try {
    const response = await getApiPostsByPostId({
      path: {
        postId,
      },
      cache: 'force-cache',
      next: {
        tags: [CACHE_TAGS.POST_DETAIL(postId)],
        revalidate: 60, // 1 minute for post details
      },
    });

    if (response.error) {
      console.error('Failed to fetch post:', response.error);
      return { success: false, error: 'Failed to fetch post' };
    }

    if (!response.data) {
      return { success: false, error: 'Post not found' };
    }

    return { success: true, data: response.data };
  } catch (error) {
    console.error('Error in fetchPostAction:', error);
    return { success: false, error: 'An unexpected error occurred' };
  }
}

/**
 * Server action to like/unlike a post
 */
export async function togglePostLikeAction(postId: string, _isLiked: boolean): Promise<{ success: boolean; error?: string }> {
  try {
    const session = await auth();
    
    if (!session?.user) {
      return { success: false, error: 'Authentication required' };
    }

    // TODO: Implement like/unlike API endpoint when available
    // For now, we'll just revalidate the cache
    
    // Revalidate the specific post
    revalidateTag(CACHE_TAGS.POST_DETAIL(postId));
    revalidateTag(CACHE_TAGS.POSTS);

    return { success: true };
  } catch (error) {
    console.error('Error in togglePostLikeAction:', error);
    return { success: false, error: 'An unexpected error occurred' };
  }
}

/**
 * Server action to share a post
 */
export async function sharePostAction(postId: string): Promise<{ success: boolean; error?: string }> {
  try {
    const session = await auth();
    
    if (!session?.user) {
      return { success: false, error: 'Authentication required' };
    }

    // TODO: Implement share API endpoint when available
    
    // Revalidate the specific post
    revalidateTag(CACHE_TAGS.POST_DETAIL(postId));
    revalidateTag(CACHE_TAGS.POSTS);

    return { success: true };
  } catch (error) {
    console.error('Error in sharePostAction:', error);
    return { success: false, error: 'An unexpected error occurred' };
  }
}

/**
 * Server action to pin/unpin a post (admin only)
 */
export async function togglePostPinAction(_postId: string, _isPinned: boolean): Promise<{ success: boolean; error?: string }> {
  try {
    const session = await auth();
    
    if (!session?.user) {
      return { success: false, error: 'Authentication required' };
    }

    // TODO: Add admin role check when available
    // if (!session.user.isAdmin) {
    //   return { success: false, error: 'Admin access required' };
    // }

    // TODO: Implement pin/unpin API endpoint when available
    
    // Revalidate all posts since pinned posts might affect order
    revalidateTag(CACHE_TAGS.POSTS);

    return { success: true };
  } catch (error) {
    console.error('Error in togglePostPinAction:', error);
    return { success: false, error: 'An unexpected error occurred' };
  }
}
