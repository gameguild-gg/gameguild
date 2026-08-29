/**
 * @game-guild/client - SocialPosts Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class SocialPostsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getPostsForGetPosts(query?: { skip?: number; take?: number }): Promise<Result<void, ApiError>> {
    const url = '/api/v1/posts';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postPosts(body: Types.SocialPostsControllersCreatePostInput): Promise<Result<void, ApiError>> {
    const url = '/api/v1/posts';

    // Validate request body
    const validatedBody = safeParse(Types.SocialPostsControllersCreatePostInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getPostsForGetPostsByPostId(postId: string): Promise<Result<void, ApiError>> {
    const url = `/api/v1/posts/${postId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async putPosts(postId: string, body: Types.SocialPostsControllersUpdatePostInput): Promise<Result<void, ApiError>> {
    const url = `/api/v1/posts/${postId}`;

    // Validate request body
    const validatedBody = safeParse(Types.SocialPostsControllersUpdatePostInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
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
   */
  async getPostsAuthor(authorId: string, query?: { skip?: number; take?: number }): Promise<Result<void, ApiError>> {
    const url = `/api/v1/posts/author/${authorId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
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
   */
  async getPostsSearch(query?: { q?: string; skip?: number; take?: number }): Promise<Result<void, ApiError>> {
    const url = '/api/v1/posts/search';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getPostsTrending(query?: { skip?: number; take?: number }): Promise<Result<void, ApiError>> {
    const url = '/api/v1/posts/trending';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createSocialPostsModule(client: ApiClient): SocialPostsModule {
  return new SocialPostsModule(client);
}
