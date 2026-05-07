/**
 * @game-guild/client - Serviceaccountoperations Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class ServiceaccountoperationsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async postAuthServiceAccountsRotateSecret(serviceAccountId: string): Promise<Result<Types.IdentityAuthenticationSecretRotationOutput, ApiError>> {
    const url = `/v1/auth/service-accounts/${serviceAccountId}:rotate-secret`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.IdentityAuthenticationSecretRotationOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAuthServiceAccountsUnlock(serviceAccountId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/auth/service-accounts/${serviceAccountId}:unlock`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Lock service account
   *
   * Locks a service account to prevent it from authenticating.
   */
  async postAuthServiceAccountsLock(serviceAccountId: string, body: Types.IdentityAuthenticationLockServiceAccountInput): Promise<Result<void, ApiError>> {
    const url = `/v1/auth/service-accounts/${serviceAccountId}:lock`;

    // Validate request body
    const validatedBody = safeParse(Types.IdentityAuthenticationLockServiceAccountInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Get service account audit log
   *
   * Retrieves the audit log of actions performed on or by a service account.
   */
  async getAuthServiceAccountsAuditLog(
    serviceAccountId: string,
    query?: { page?: number; pageSize?: number },
  ): Promise<Result<Types.IdentityAuthenticationServiceAccountAuditLogOutput, ApiError>> {
    const url = `/v1/auth/service-accounts/${serviceAccountId}/audit-log`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.IdentityAuthenticationServiceAccountAuditLogOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAuthServiceAccountsDeactivate(serviceAccountId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/auth/service-accounts/${serviceAccountId}:deactivate`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postAuthServiceAccountsReactivate(serviceAccountId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/auth/service-accounts/${serviceAccountId}:reactivate`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async patchAuthServiceAccountsScopes(serviceAccountId: string, body: Types.IdentityAuthenticationUpdateScopesInput): Promise<Result<void, ApiError>> {
    const url = `/v1/auth/service-accounts/${serviceAccountId}/scopes`;

    // Validate request body
    const validatedBody = safeParse(Types.IdentityAuthenticationUpdateScopesInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PATCH',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createServiceaccountoperationsModule(client: ApiClient): ServiceaccountoperationsModule {
  return new ServiceaccountoperationsModule(client);
}
