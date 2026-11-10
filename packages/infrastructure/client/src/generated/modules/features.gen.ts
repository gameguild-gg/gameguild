/**
 * @game-guild/client - Features Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client';
import type { Result } from '../../runtime/result/types';
import type { ApiError } from '../../runtime/errors/types';
import * as Types from '../types.gen';
import { safeParse } from '../../runtime/errors/validation';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class FeaturesModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getFeatures(query?: { isEnabled?: boolean }): Promise<Result<Types.FeaturesFeatureFlagDto[], ApiError>> {
    const url = '/v1/features';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.FeaturesFeatureFlagDto[], ApiError>;
  }

  /**
   */
  async postFeatures(body?: Types.FeaturesCreateFeatureInput): Promise<Result<Record<string, unknown>, ApiError>> {
    const url = '/v1/features';

    // Validate request body
    const validatedBody = body === undefined ? undefined : safeParse(Types.FeaturesCreateFeatureInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<Record<string, unknown>, ApiError>;
  }

  /**
   */
  async postFeaturesDisable(id: string): Promise<Result<void, ApiError>> {
    const url = `/v1/features/${id}:disable`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postFeaturesEnable(id: string): Promise<Result<void, ApiError>> {
    const url = `/v1/features/${id}:enable`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postFeaturesToggle(id: string, body?: Types.FeaturesToggleFeatureInput): Promise<Result<void, ApiError>> {
    const url = `/v1/features/${id}:toggle`;

    // Validate request body
    const validatedBody = body === undefined ? undefined : safeParse(Types.FeaturesToggleFeatureInputSchema, body, 'request');

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
  async getFeatureByKey(key: string): Promise<Result<void, ApiError>> {
    const url = `/v1/features/${key}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async putFeatures(key: string, body?: Types.FeaturesUpdateFeatureInput): Promise<Result<void, ApiError>> {
    const url = `/v1/features/${key}`;

    // Validate request body
    const validatedBody = body === undefined ? undefined : safeParse(Types.FeaturesUpdateFeatureInputSchema, body, 'request');

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
  async deleteFeatures(key: string): Promise<Result<void, ApiError>> {
    const url = `/v1/features/${key}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getFeaturesExists(key: string, query?: { environment?: string }): Promise<Result<boolean, ApiError>> {
    const url = `/v1/features/${key}/exists`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<boolean, ApiError>;
  }
}

export function createFeaturesModule(client: ApiClient): FeaturesModule {
  return new FeaturesModule(client);
}
