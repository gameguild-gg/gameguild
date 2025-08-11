'use server';

import { revalidateTag } from 'next/cache';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import { postApiPosts, getApiPosts } from '@/lib/api/generated/sdk.gen';
import type { CreatePostDto, PostsPageDto } from '@/lib/api/generated/types.gen';
import type { FeedFilters } from './index';

export async function createPostAction(formData: FormData): Promise<{ success: boolean; error?: string }> {
  try {
    await configureAuthenticatedClient();

    const content = (formData.get('content') as string) || '';
    const postType = (formData.get('postType') as string) || 'general';

    const body: CreatePostDto = {
      title: content.slice(0, 120) || null,
      description: content,
      postType,
    };

    await postApiPosts({ body });

    revalidateTag('posts');

    return { success: true };
  } catch (error) {
    console.error('createPostAction error:', error);
    return { success: false, error: error instanceof Error ? error.message : 'Failed to create post' };
  }
}

export async function fetchPostsAction(filters: FeedFilters = {}, page = 1, pageSize = 20): Promise<{ success: boolean; data?: PostsPageDto; error?: string }> {
  try {
    await configureAuthenticatedClient();

    const result = await getApiPosts({
      query: {
        pageNumber: page,
        pageSize,
        postType: filters.postType,
        userId: filters.userId,
        isPinned: filters.isPinned,
        searchTerm: filters.searchTerm,
        orderBy: filters.orderBy,
        descending: filters.descending,
      },
    });

    return { success: true, data: result.data as PostsPageDto };
  } catch (error) {
    console.error('fetchPostsAction error:', error);
    return { success: false, error: error instanceof Error ? error.message : 'Failed to fetch posts' };
  }
}
