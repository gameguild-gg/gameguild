/**
 * @game-guild/client - SocialRatings Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class SocialRatingsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Rate an entity (create or update)
   */
  async postApiRatings(body: Types.SocialRatingsCreateRatingInput): Promise<Result<Types.SocialRatingsRatingDto, ApiError>> {
    const url = '/api/ratings';

    // Validate request body
    const validatedBody = safeParse(Types.SocialRatingsCreateRatingInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialRatingsRatingDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Get a specific rating by ID
   */
  async getApiRatings(ratingId: string): Promise<Result<Types.SocialRatingsRatingDto, ApiError>> {
    const url = `/api/ratings/${ratingId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: false,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialRatingsRatingDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Delete the current user's rating
   */
  async deleteApiRatings(ratingId: string): Promise<Result<void, ApiError>> {
    const url = `/api/ratings/${ratingId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Mark a review as helpful or not helpful
   */
  async postApiRatingsHelpful(ratingId: string, body: Types.SocialRatingsVoteHelpfulInput): Promise<Result<void, ApiError>> {
    const url = `/api/ratings/${ratingId}/helpful`;

    // Validate request body
    const validatedBody = safeParse(Types.SocialRatingsVoteHelpfulInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Remove helpful vote
   */
  async deleteApiRatingsHelpful(ratingId: string): Promise<Result<void, ApiError>> {
    const url = `/api/ratings/${ratingId}/helpful`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Report a review
   */
  async postApiRatingsReport(ratingId: string, body: Types.SocialRatingsReportInput): Promise<Result<void, ApiError>> {
    const url = `/api/ratings/${ratingId}/report`;

    // Validate request body
    const validatedBody = safeParse(Types.SocialRatingsReportInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Admin delete a rating
   */
  async deleteApiRatingsAdmin(ratingId: string): Promise<Result<void, ApiError>> {
    const url = `/api/ratings/admin/${ratingId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Approve a rating (Admin)
   */
  async postApiRatingsAdminApprove(ratingId: string): Promise<Result<void, ApiError>> {
    const url = `/api/ratings/admin/${ratingId}/approve`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Reject a rating (Admin)
   */
  async postApiRatingsAdminReject(ratingId: string): Promise<Result<void, ApiError>> {
    const url = `/api/ratings/admin/${ratingId}/reject`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Get ratings pending moderation (Admin)
   */
  async getApiRatingsAdminModeration(query?: { skip?: number; take?: number }): Promise<Result<Types.SocialRatingsRatingDto[], ApiError>> {
    const url = '/api/ratings/admin/moderation';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.SocialRatingsRatingDto[], ApiError>;
  }

  /**
   * Force recalculate rating summary (Admin)
   */
  async postApiRatingsAdminRecalculate(entityType: string, entityId: string): Promise<Result<void, ApiError>> {
    const url = `/api/ratings/admin/recalculate/${entityType}/${entityId}`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Get ratings count for an entity
   */
  async getApiRatingsCount(entityType: string, entityId: string): Promise<Result<number, ApiError>> {
    const url = `/api/ratings/count/${entityType}/${entityId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: false,
    });

    return result as Result<number, ApiError>;
  }

  /**
   * Get ratings for an entity
   */
  async getApiRatingsEntity(
    entityType: string,
    entityId: string,
    query?: {
      minValue?: number;
      maxValue?: number;
      withReviewOnly?: boolean;
      verifiedOnly?: boolean;
      sortOrder?: Types.SocialRatingsRatingSortOrder;
      skip?: number;
      take?: number;
    },
  ): Promise<Result<Types.SocialRatingsRatingDto[], ApiError>> {
    const url = `/api/ratings/entity/${entityType}/${entityId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<Types.SocialRatingsRatingDto[], ApiError>;
  }

  /**
   * Check if current user has rated an entity
   */
  async getApiRatingsHasRated(entityType: string, entityId: string): Promise<Result<boolean, ApiError>> {
    const url = `/api/ratings/has-rated/${entityType}/${entityId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<boolean, ApiError>;
  }

  /**
   * Get the current user's rating for an entity
   */
  async getApiRatingsMy(entityType: string, entityId: string): Promise<Result<Types.SocialRatingsRatingDto, ApiError>> {
    const url = `/api/ratings/my/${entityType}/${entityId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialRatingsRatingDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Get current user's ratings for multiple entities (batch)
   */
  async postApiRatingsMyBatch(body: Types.SocialRatingsBatchSummaryInput): Promise<Result<Record<string, SocialRatingsRatingDto>, ApiError>> {
    const url = '/api/ratings/my/batch';

    // Validate request body
    const validatedBody = safeParse(Types.SocialRatingsBatchSummaryInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<Record<string, SocialRatingsRatingDto>, ApiError>;
  }

  /**
   * Get recent reviews
   */
  async getApiRatingsRecentReviews(query?: { entityType?: string; take?: number }): Promise<Result<Types.SocialRatingsRatingDto[], ApiError>> {
    const url = '/api/ratings/recent-reviews';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<Types.SocialRatingsRatingDto[], ApiError>;
  }

  /**
   * Get summaries for multiple entities (batch)
   */
  async postApiRatingsSummariesBatch(body: Types.SocialRatingsBatchSummaryInput): Promise<Result<Record<string, SocialRatingsRatingSummaryDto>, ApiError>> {
    const url = '/api/ratings/summaries/batch';

    // Validate request body
    const validatedBody = safeParse(Types.SocialRatingsBatchSummaryInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: false,
    });

    return result as Result<Record<string, SocialRatingsRatingSummaryDto>, ApiError>;
  }

  /**
   * Get rating summary for an entity
   */
  async getApiRatingsSummary(entityType: string, entityId: string): Promise<Result<Types.SocialRatingsRatingSummaryDto, ApiError>> {
    const url = `/api/ratings/summary/${entityType}/${entityId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: false,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialRatingsRatingSummaryDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Get top-rated entities of a type
   */
  async getApiRatingsTop(entityType: string, query?: { minRatings?: number; take?: number }): Promise<Result<Types.SocialRatingsRatingSummaryDto[], ApiError>> {
    const url = `/api/ratings/top/${entityType}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<Types.SocialRatingsRatingSummaryDto[], ApiError>;
  }

  /**
   * Get all ratings by a specific user
   */
  async getApiRatingsUser(
    userId: string,
    query?: { entityType?: string; skip?: number; take?: number },
  ): Promise<Result<Types.SocialRatingsRatingDto[], ApiError>> {
    const url = `/api/ratings/user/${userId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<Types.SocialRatingsRatingDto[], ApiError>;
  }
}

export function createSocialRatingsModule(client: ApiClient): SocialRatingsModule {
  return new SocialRatingsModule(client);
}
