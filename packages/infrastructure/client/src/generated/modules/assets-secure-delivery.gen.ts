/**
 * @game-guild/client - AssetsSecureDelivery Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class AssetsSecureDeliveryModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async postApiAssetsAccessUrl(assetId: string, body: Types.AssetsSecurityAccessUrlInput): Promise<Result<Types.AssetsAssetAccessUrl, ApiError>> {
    const url = `/api/assets/${assetId}/access-url`;

    // Validate request body
    const validatedBody = safeParse(Types.AssetsSecurityAccessUrlInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.AssetsAssetAccessUrlSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getApiAssetsContent(assetId: string, query?: { token?: string; transform?: string }): Promise<Result<void, ApiError>> {
    const url = `/api/assets/${assetId}/content`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createAssetsSecureDeliveryModule(client: ApiClient): AssetsSecureDeliveryModule {
  return new AssetsSecureDeliveryModule(client);
}
