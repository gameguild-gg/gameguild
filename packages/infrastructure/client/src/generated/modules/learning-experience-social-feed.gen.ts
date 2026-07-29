/**
 * @game-guild/client - LearningExperienceSocialFeed Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class LearningExperienceSocialFeedModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getApiSocialFeedMe(query?: {
    skip?: number;
    take?: number;
    filterByType?: Types.LearningExperienceSocialFeedItemType;
  }): Promise<Result<Array<Types.LearningExperienceSocialServicesPersonalizedFeedItem>, ApiError>> {
    const url = '/api/social/feed/me';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.LearningExperienceSocialServicesPersonalizedFeedItem>, ApiError>;
  }

  /**
   */
  async postApiSocialFeedMeGenerate(): Promise<Result<number, ApiError>> {
    const url = '/api/social/feed/me/generate';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<number, ApiError>;
  }

  /**
   */
  async postApiSocialFeedViewed(id: string): Promise<Result<Types.LearningExperienceSocialServicesPersonalizedFeedItem, ApiError>> {
    const url = `/api/social/feed/${id}/viewed`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningExperienceSocialServicesPersonalizedFeedItemSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postApiSocialFeedDismiss(id: string): Promise<Result<Types.LearningExperienceSocialServicesPersonalizedFeedItem, ApiError>> {
    const url = `/api/social/feed/${id}/dismiss`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningExperienceSocialServicesPersonalizedFeedItemSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createLearningExperienceSocialFeedModule(client: ApiClient): LearningExperienceSocialFeedModule {
  return new LearningExperienceSocialFeedModule(client);
}
