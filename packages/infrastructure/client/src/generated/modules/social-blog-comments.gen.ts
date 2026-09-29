/**
 * @game-guild/client - SocialBlogComments Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class SocialBlogCommentsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async deleteApiSocialBlogComments(commentId: string): Promise<Result<void, ApiError>> {
    const url = `/api/social/blog/comments/${commentId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postApiSocialBlogPostsComments(
    id: string,
    body: Types.SocialBlogControllersAddBlogCommentInput,
  ): Promise<Result<Types.SocialBlogBlogComment, ApiError>> {
    const url = `/api/social/blog/posts/${id}/comments`;

    // Validate request body
    const validatedBody = safeParse(Types.SocialBlogControllersAddBlogCommentInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialBlogBlogCommentSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createSocialBlogCommentsModule(client: ApiClient): SocialBlogCommentsModule {
  return new SocialBlogCommentsModule(client);
}
