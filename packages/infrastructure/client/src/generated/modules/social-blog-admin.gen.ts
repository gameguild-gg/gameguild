/**
 * @game-guild/client - SocialBlogAdmin Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class SocialBlogAdminModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Lists the newest comments platform-wide for moderation.
   */
  async getApiSocialBlogAdminComments(query?: { page?: number }): Promise<Result<Types.SocialBlogQueriesBlogAdminCommentPage, ApiError>> {
    const url = '/api/social/blog/admin/comments';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialBlogQueriesBlogAdminCommentPageSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Soft-deletes any comment; the moderator id is recorded on the row.
   */
  async deleteApiSocialBlogAdminComments(commentId: string): Promise<Result<void, ApiError>> {
    const url = `/api/social/blog/admin/comments/${commentId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Imports a Ghost export JSON into the internal blogging system (issue #96 "port all old
   * content"). Idempotent: re-running skips posts whose (author, slug) already exist. Pass
   * `dryRun: true` to plan without persisting.
   */
  async postApiSocialBlogAdminImportGhost(
    body: Types.SocialBlogControllersGhostImportInput,
  ): Promise<Result<Types.SocialBlogServicesGhostImportReport, ApiError>> {
    const url = '/api/social/blog/admin/import/ghost';

    // Validate request body
    const validatedBody = safeParse(Types.SocialBlogControllersGhostImportInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialBlogServicesGhostImportReportSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Lists every live post (any status, any author), newest edit first.
   */
  async getApiSocialBlogAdminPosts(query?: {
    page?: number;
    status?: Types.SocialBlogBlogPostStatus;
  }): Promise<Result<Types.SocialBlogQueriesBlogAdminPostPage, ApiError>> {
    const url = '/api/social/blog/admin/posts';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialBlogQueriesBlogAdminPostPageSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Soft-deletes a post regardless of authorship.
   */
  async deleteApiSocialBlogAdminPosts(id: string): Promise<Result<void, ApiError>> {
    const url = `/api/social/blog/admin/posts/${id}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Force-unpublishes a post regardless of authorship (takedown).
   */
  async postApiSocialBlogAdminPostsUnpublish(id: string): Promise<Result<Types.SocialBlogBlogPost, ApiError>> {
    const url = `/api/social/blog/admin/posts/${id}/unpublish`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialBlogBlogPostSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Platform-wide blog counters for the admin console header.
   */
  async getApiSocialBlogAdminStats(): Promise<Result<Types.SocialBlogQueriesBlogAdminStatsDto, ApiError>> {
    const url = '/api/social/blog/admin/stats';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialBlogQueriesBlogAdminStatsDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createSocialBlogAdminModule(client: ApiClient): SocialBlogAdminModule {
  return new SocialBlogAdminModule(client);
}
