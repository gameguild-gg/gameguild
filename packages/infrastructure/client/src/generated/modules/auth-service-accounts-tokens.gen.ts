/**
 * @game-guild/client - AuthServiceAccountsTokens Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class AuthServiceAccountsTokensModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async postOauthToken(): Promise<Result<Types.IdentityAuthenticationClientCredentialsTokenOutput, ApiError>> {
    const url = '/v1/oauth/token';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.IdentityAuthenticationClientCredentialsTokenOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createAuthServiceAccountsTokensModule(client: ApiClient): AuthServiceAccountsTokensModule {
  return new AuthServiceAccountsTokensModule(client);
}
