/**
 * @game-guild/client - AssetsCdn Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class AssetsCdnModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getAssetsByReferenceIdByToken(referenceId: string, token: string): Promise<Result<void, ApiError>> {
    const url = `/assets/${referenceId}/${token}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: false,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getE(token: string): Promise<Result<void, ApiError>> {
    const url = `/e/${token}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: false,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getT(transformation: string, referenceId: string, token: string): Promise<Result<void, ApiError>> {
    const url = `/t/${transformation}/${referenceId}/${token}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: false,
    });

    return result as Result<void, ApiError>;
  }
}

export function createAssetsCdnModule(client: ApiClient): AssetsCdnModule {
  return new AssetsCdnModule(client);
}
