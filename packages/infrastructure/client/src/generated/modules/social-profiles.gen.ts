/**
 * @game-guild/client - SocialProfiles Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class SocialProfilesModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getApiSocialProfilesUsers(userId: string): Promise<Result<Types.SocialProfilesSocialProfile, ApiError>> {
    const url = `/api/social/profiles/users/${userId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialProfilesSocialProfileSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putApiSocialProfilesUsers(
    userId: string,
    body: Types.SocialProfilesUpdateSocialProfileBody,
  ): Promise<Result<Types.SocialProfilesSocialProfile, ApiError>> {
    const url = `/api/social/profiles/users/${userId}`;

    // Validate request body
    const validatedBody = safeParse(Types.SocialProfilesUpdateSocialProfileBodySchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialProfilesSocialProfileSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getApiSocialProfiles(handle: string): Promise<Result<Types.SocialProfilesSocialProfile, ApiError>> {
    const url = `/api/social/profiles/@${handle}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: false,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialProfilesSocialProfileSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getApiSocialProfilesSearch(query?: { query?: string; take?: number }): Promise<Result<Array<Types.SocialProfilesSocialProfile>, ApiError>> {
    const url = '/api/social/profiles/search';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<Array<Types.SocialProfilesSocialProfile>, ApiError>;
  }

  /**
   */
  async putApiSocialProfilesUsersPrivacy(
    userId: string,
    body: Types.SocialProfilesUpdateProfilePrivacyBody,
  ): Promise<Result<Types.SocialProfilesSocialProfile, ApiError>> {
    const url = `/api/social/profiles/users/${userId}/privacy`;

    // Validate request body
    const validatedBody = safeParse(Types.SocialProfilesUpdateProfilePrivacyBodySchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialProfilesSocialProfileSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putApiSocialProfilesUsersStats(
    userId: string,
    body: Types.SocialProfilesUpdateProfileStatsBody,
  ): Promise<Result<Types.SocialProfilesSocialProfile, ApiError>> {
    const url = `/api/social/profiles/users/${userId}/stats`;

    // Validate request body
    const validatedBody = safeParse(Types.SocialProfilesUpdateProfileStatsBodySchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialProfilesSocialProfileSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postApiSocialProfilesSkills(
    profileId: string,
    body: Types.SocialProfilesAddProfileSkillBody,
  ): Promise<Result<Types.SocialProfilesProfileSkill, ApiError>> {
    const url = `/api/social/profiles/${profileId}/skills`;

    // Validate request body
    const validatedBody = safeParse(Types.SocialProfilesAddProfileSkillBodySchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialProfilesProfileSkillSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteApiSocialProfilesSkills(skillId: string): Promise<Result<void, ApiError>> {
    const url = `/api/social/profiles/skills/${skillId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postApiSocialProfilesPortfolio(
    profileId: string,
    body: Types.SocialProfilesAddProfilePortfolioItemBody,
  ): Promise<Result<Types.SocialProfilesProfilePortfolioItem, ApiError>> {
    const url = `/api/social/profiles/${profileId}/portfolio`;

    // Validate request body
    const validatedBody = safeParse(Types.SocialProfilesAddProfilePortfolioItemBodySchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialProfilesProfilePortfolioItemSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putApiSocialProfilesPortfolio(
    itemId: string,
    body: Types.SocialProfilesUpdateProfilePortfolioItemBody,
  ): Promise<Result<Types.SocialProfilesProfilePortfolioItem, ApiError>> {
    const url = `/api/social/profiles/portfolio/${itemId}`;

    // Validate request body
    const validatedBody = safeParse(Types.SocialProfilesUpdateProfilePortfolioItemBodySchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialProfilesProfilePortfolioItemSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteApiSocialProfilesPortfolio(itemId: string): Promise<Result<void, ApiError>> {
    const url = `/api/social/profiles/portfolio/${itemId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createSocialProfilesModule(client: ApiClient): SocialProfilesModule {
  return new SocialProfilesModule(client);
}
