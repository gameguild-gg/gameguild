/**
 * @game-guild/client - SocialPosts Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client';
import type { Result } from '../../runtime/result/types';
import type { ApiError } from '../../runtime/errors/types';
import * as Types from '../types.gen';
import { safeParse } from '../../runtime/errors/validation';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class SocialPostsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Get paginated list of public posts
   */
  async getPostsForGetPosts(query?: { skip?: number; take?: number }): Promise<Result<void, ApiError>> {
    const url = '/api/v1/posts';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Create a new post
   */
  async postPosts(body?: Types.SocialPostsControllersCreatePostInput): Promise<Result<void, ApiError>> {
    const url = '/api/v1/posts';

    // Validate request body
    const validatedBody = body === undefined ? undefined : safeParse(Types.SocialPostsControllersCreatePostInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Get a single post by ID
   */
  async getPostsForGetPostsByPostId(postId: string): Promise<Result<void, ApiError>> {
    const url = `/api/v1/posts/${postId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: false,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Update a post
   */
  async putPosts(postId: string, body?: Types.SocialPostsControllersUpdatePostInput): Promise<Result<void, ApiError>> {
    const url = `/api/v1/posts/${postId}`;

    // Validate request body
    const validatedBody = body === undefined ? undefined : safeParse(Types.SocialPostsControllersUpdatePostInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Delete a post
   */
  async deletePosts(postId: string): Promise<Result<void, ApiError>> {
    const url = `/api/v1/posts/${postId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Get posts by a specific author
   */
  async getPostsAuthor(authorId: string, query?: { skip?: number; take?: number }): Promise<Result<void, ApiError>> {
    const url = `/api/v1/posts/author/${authorId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Get posts for the current user's feed
   */
  async getPostsFeed(query?: { skip?: number; take?: number }): Promise<Result<void, ApiError>> {
    const url = '/api/v1/posts/feed';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Get current user's posts
   */
  async getPostsMy(query?: { skip?: number; take?: number }): Promise<Result<void, ApiError>> {
    const url = '/api/v1/posts/my';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Search posts by content
   */
  async getPostsSearch(query?: { q?: string; skip?: number; take?: number }): Promise<Result<void, ApiError>> {
    const url = '/api/v1/posts/search';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Get trending posts
   */
  async getPostsTrending(query?: { skip?: number; take?: number }): Promise<Result<void, ApiError>> {
    const url = '/api/v1/posts/trending';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<void, ApiError>;
  }
}

export function createSocialPostsModule(client: ApiClient): SocialPostsModule {
  return new SocialPostsModule(client);
}
