/**
 * @game-guild/client - GamificationAchievements Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class GamificationAchievementsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Get all available achievements.
   */
  async getApiAchievementsForGetApiAchievements(query?: {
    category?: string;
    isActive?: boolean;
  }): Promise<Result<Types.GamificationAchievementsAchievementDto[], ApiError>> {
    const url = '/api/achievements';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<Types.GamificationAchievementsAchievementDto[], ApiError>;
  }

  /**
   * Create a new achievement (admin only).
   */
  async postApiAchievements(
    body: Types.GamificationAchievementsCreateAchievementInput,
  ): Promise<Result<Types.GamificationAchievementsAchievementDto, ApiError>> {
    const url = '/api/achievements';

    // Validate request body
    const validatedBody = safeParse(Types.GamificationAchievementsCreateAchievementInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.GamificationAchievementsAchievementDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Get a specific achievement by ID.
   */
  async getApiAchievementsForGetApiAchievementsByAchievementId(achievementId: string): Promise<Result<Types.GamificationAchievementsAchievementDto, ApiError>> {
    const url = `/api/achievements/${achievementId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: false,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.GamificationAchievementsAchievementDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Update an existing achievement (admin only).
   */
  async putApiAchievements(
    achievementId: string,
    body: Types.GamificationAchievementsUpdateAchievementInput,
  ): Promise<Result<Types.GamificationAchievementsAchievementDto, ApiError>> {
    const url = `/api/achievements/${achievementId}`;

    // Validate request body
    const validatedBody = safeParse(Types.GamificationAchievementsUpdateAchievementInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.GamificationAchievementsAchievementDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Delete an achievement (admin only).
   */
  async deleteApiAchievements(achievementId: string): Promise<Result<void, ApiError>> {
    const url = `/api/achievements/${achievementId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Award an achievement to a user (admin only, for manual awards).
   */
  async postApiAchievementsAward(
    achievementId: string,
    body: Types.GamificationAchievementsAwardAchievementInput,
  ): Promise<Result<Types.GamificationAchievementsUserAchievementDto, ApiError>> {
    const url = `/api/achievements/${achievementId}/award`;

    // Validate request body
    const validatedBody = safeParse(Types.GamificationAchievementsAwardAchievementInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.GamificationAchievementsUserAchievementDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Get eligible achievements the current user can earn.
   */
  async getApiAchievementsEligible(): Promise<Result<Types.GamificationAchievementsAchievementDto[], ApiError>> {
    const url = '/api/achievements/eligible';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Types.GamificationAchievementsAchievementDto[], ApiError>;
  }

  /**
   * Get all achievements for the current user.
   */
  async getApiAchievementsMy(query?: { category?: string }): Promise<Result<Types.GamificationAchievementsUserAchievementDto[], ApiError>> {
    const url = '/api/achievements/my';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.GamificationAchievementsUserAchievementDto[], ApiError>;
  }

  /**
   * Mark an achievement notification as read.
   */
  async postApiAchievementsMyMarkNotified(userAchievementId: string): Promise<Result<void, ApiError>> {
    const url = `/api/achievements/my/${userAchievementId}/mark-notified`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Get total achievement points for the current user.
   */
  async getApiAchievementsMyPoints(): Promise<Result<number, ApiError>> {
    const url = '/api/achievements/my/points';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<number, ApiError>;
  }

  /**
   * Get unnotified achievements for the current user.
   */
  async getApiAchievementsMyUnnotified(): Promise<Result<Types.GamificationAchievementsUserAchievementDto[], ApiError>> {
    const url = '/api/achievements/my/unnotified';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Types.GamificationAchievementsUserAchievementDto[], ApiError>;
  }
}

export function createGamificationAchievementsModule(client: ApiClient): GamificationAchievementsModule {
  return new GamificationAchievementsModule(client);
}
