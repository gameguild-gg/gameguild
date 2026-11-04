/**
 * @game-guild/client - SocialAssetsSocialMediaAssets Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client';
import type { Result } from '../../runtime/result/types';
import type { ApiError } from '../../runtime/errors/types';
import * as Types from '../types.gen';
import { safeParse } from '../../runtime/errors/validation';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class SocialAssetsSocialMediaAssetsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async postAssetsSocialMedia(): Promise<Result<Types.SocialAssetsSocialMediaSocialMediaAssetDescriptor, ApiError>> {
    const url = '/v1/assets/social-media';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialAssetsSocialMediaSocialMediaAssetDescriptorSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getAssetsSocialMedia(assetReferenceId: string): Promise<Result<Types.SocialAssetsSocialMediaSocialMediaAssetDescriptor, ApiError>> {
    const url = `/v1/assets/social-media/${assetReferenceId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialAssetsSocialMediaSocialMediaAssetDescriptorSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createSocialAssetsSocialMediaAssetsModule(client: ApiClient): SocialAssetsSocialMediaAssetsModule {
  return new SocialAssetsSocialMediaAssetsModule(client);
}
