/**
 * @game-guild/client - AuthIpBlocklist Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class AuthIpBlocklistModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Lists every active blocklist entry, expired entries filtered out.
   *
   * Lists the active known-attacker IP blocklist entries stored in the distributed cache.
   */
  async getAuthIpBlocklist(): Promise<Result<Types.IdentityAuthenticationIpBlocklistEntry[], ApiError>> {
    const url = '/v1/auth/ip-blocklist';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Types.IdentityAuthenticationIpBlocklistEntry[], ApiError>;
  }

  /**
   * Adds or replaces a blocklist entry.
   *
   * Stores a known-attacker IP address in the shared blocklist. Omit expiresAt to apply the configured default entry lifetime.
   */
  async putAuthIpBlocklist(body: Types.IdentityAuthenticationIpBlocklistEntryInput): Promise<Result<void, ApiError>> {
    const url = '/v1/auth/ip-blocklist';

    // Validate request body
    const validatedBody = safeParse(Types.IdentityAuthenticationIpBlocklistEntryInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Removes a blocklist entry.
   *
   * Removes the blocklist entry for the given IP address. Returns 404 when no entry exists.
   */
  async deleteAuthIpBlocklist(query?: { ipAddress?: string }): Promise<Result<void, ApiError>> {
    const url = '/v1/auth/ip-blocklist';

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createAuthIpBlocklistModule(client: ApiClient): AuthIpBlocklistModule {
  return new AuthIpBlocklistModule(client);
}
