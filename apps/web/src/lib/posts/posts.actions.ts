'use server';

import { revalidateTag } from 'next/cache';
import { getApiPosts, postApiPosts, getApiPostsByPostId } from '@/lib/api/generated/sdk.gen';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import type { GetApiPostsData, PostApiPostsData, GetApiPostsByPostIdData } from '@/lib/api/generated/types.gen';

/**
 * Get all posts with optional filtering
 */
export async function getPosts(data?: GetApiPostsData) {
  await configureAuthenticatedClient();

  return getApiPosts({
    query: data?.query,
  });
}

/**
 * Create a new post
 */
export async function createPost(data?: PostApiPostsData) {
  await configureAuthenticatedClient();

  const result = await postApiPosts({
    body: data?.body,
  });

  // Revalidate posts cache
  revalidateTag('posts');

  return result;
}

/**
 * Get a specific post by ID
 */
export async function getPostById(data: GetApiPostsByPostIdData) {
  await configureAuthenticatedClient();

  return getApiPostsByPostId({
    path: data.path,
  });
}

/**
 * Get recent posts with limit
 */
export async function getRecentPosts(limit: number = 10) {
  const result = await getPosts({
    query: {
      pageSize: limit,
      orderBy: 'createdAt',
      descending: true,
    },
  });
  
  return {
    success: true,
    data: Array.isArray(result.data) ? result.data.map(post => ({
      id: post.id || '',
      title: post.title || '',
      authorId: post.authorId || '',
      authorName: post.authorName || 'Unknown',
      authorAvatar: post.authorAvatar || null,
      createdAt: post.createdAt || new Date().toISOString(),
    })) : [],
  };
}
