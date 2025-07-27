'use server';

import { revalidateTag } from 'next/cache';
import { getApiPosts, postApiPosts, getApiPostsByPostId } from '@/lib/api/generated/sdk.gen';
import { configureAuthenticatedClient } from '@/lib/api/authenticated-client';
import type { GetApiPostsData, PostApiPostsData, GetApiPostsByPostIdData } from '@/lib/api/generated/types.gen';

/**
 * Get all posts with optional filtering
 */
export async function getPosts(data?: GetApiPostsData) {
  const client = await configureAuthenticatedClient();
  return getApiPosts({
    client,
    ...data,
  });
}

/**
 * Create a new post
 */
export async function createPost(data?: PostApiPostsData) {
  const client = await configureAuthenticatedClient();
  const result = await postApiPosts({
    client,
    ...data,
  });
  
  // Revalidate posts cache
  revalidateTag('posts');
  
  return result;
}

/**
 * Get a specific post by ID
 */
export async function getPostById(data: GetApiPostsByPostIdData) {
  const client = await configureAuthenticatedClient();
  return getApiPostsByPostId({
    client,
    ...data,
  });
}
