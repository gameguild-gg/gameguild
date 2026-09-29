/**
 * @game-guild/client - SocialBlogPublic Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class SocialBlogPublicModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getApiSocialBlogPublicAuthorsForGetApiSocialBlogPublicAuthorsByHandle(
    handle: string,
    query?: { beforePublishedAt?: string; beforeId?: string },
  ): Promise<Result<Types.SocialBlogQueriesBlogPostSummaryPage, ApiError>> {
    const url = `/api/social/blog/public/authors/${handle}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialBlogQueriesBlogPostSummaryPageSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getApiSocialBlogPublicAuthorsForGetApiSocialBlogPublicAuthorsByHandleBySlug(
    handle: string,
    slug: string,
  ): Promise<Result<Types.SocialBlogQueriesBlogPostDetailDto, ApiError>> {
    const url = `/api/social/blog/public/authors/${handle}/${slug}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialBlogQueriesBlogPostDetailDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getApiSocialBlogPublicPosts(query?: {
    beforePublishedAt?: string;
    beforeId?: string;
  }): Promise<Result<Types.SocialBlogQueriesBlogPostSummaryPage, ApiError>> {
    const url = '/api/social/blog/public/posts';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialBlogQueriesBlogPostSummaryPageSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getApiSocialBlogPublicPostsComments(
    id: string,
    query?: { afterCreatedAt?: string; afterId?: string },
  ): Promise<Result<Types.SocialBlogQueriesBlogCommentPage, ApiError>> {
    const url = `/api/social/blog/public/posts/${id}/comments`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialBlogQueriesBlogCommentPageSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postApiSocialBlogPublicPostsViews(id: string): Promise<Result<void, ApiError>> {
    const url = `/api/social/blog/public/posts/${id}/views`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getApiSocialBlogPublicResolve(handle: string, slug: string): Promise<Result<Types.SocialBlogQueriesBlogRouteResolutionDto, ApiError>> {
    const url = `/api/social/blog/public/resolve/${handle}/${slug}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialBlogQueriesBlogRouteResolutionDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createSocialBlogPublicModule(client: ApiClient): SocialBlogPublicModule {
  return new SocialBlogPublicModule(client);
}
