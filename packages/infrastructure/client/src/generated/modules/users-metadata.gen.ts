/**
 * @game-guild/client - UsersMetadata Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class UsersMetadataModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Get user metadata by user ID
   */
  async getUsersMetadata(userId: string): Promise<Result<Types.IdentityUsersUserMetadataDto, ApiError>> {
    const url = `/v1/users/${userId}/metadata`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.IdentityUsersUserMetadataDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Replace user metadata by user ID
   */
  async putUsersMetadata(userId: string, body: Types.IdentityUsersReplaceUserMetadataInput): Promise<Result<void, ApiError>> {
    const url = `/v1/users/${userId}/metadata`;

    // Validate request body
    const validatedBody = safeParse(Types.IdentityUsersReplaceUserMetadataInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Partially update user metadata by user ID
   */
  async patchUsersMetadata(userId: string, body: Types.IdentityUsersUpdateUserMetadataInput): Promise<Result<void, ApiError>> {
    const url = `/v1/users/${userId}/metadata`;

    // Validate request body
    const validatedBody = safeParse(Types.IdentityUsersUpdateUserMetadataInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PATCH',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createUsersMetadataModule(client: ApiClient): UsersMetadataModule {
  return new UsersMetadataModule(client);
}
