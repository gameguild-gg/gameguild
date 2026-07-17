/**
 * @game-guild/client - AuthServiceaccounts Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class AuthServiceaccountsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getAuthServiceAccounts(query?: { tenantId?: string }): Promise<Result<Array<Types.IdentityAuthenticationServiceAccountOutput>, ApiError>> {
    const url = '/v1/auth/service-accounts';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.IdentityAuthenticationServiceAccountOutput>, ApiError>;
  }

  /**
   */
  async postAuthServiceAccounts(
    body: Types.IdentityAuthenticationCreateServiceAccountInput,
  ): Promise<Result<Types.IdentityAuthenticationServiceAccountCreatedOutput, ApiError>> {
    const url = '/v1/auth/service-accounts';

    // Validate request body
    const validatedBody = safeParse(Types.IdentityAuthenticationCreateServiceAccountInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.IdentityAuthenticationServiceAccountCreatedOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getAuthServiceAccounts1(serviceAccountId: string): Promise<Result<Types.IdentityAuthenticationServiceAccountOutput, ApiError>> {
    const url = `/v1/auth/service-accounts/${serviceAccountId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.IdentityAuthenticationServiceAccountOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteAuthServiceAccounts(serviceAccountId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/auth/service-accounts/${serviceAccountId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Partially update service account
   *
   * Updates specific fields of a service account. Only provided fields are updated.
   */
  async patchAuthServiceAccounts(serviceAccountId: string, body: Types.IdentityAuthenticationPatchServiceAccountInput): Promise<Result<void, ApiError>> {
    const url = `/v1/auth/service-accounts/${serviceAccountId}`;

    // Validate request body
    const validatedBody = safeParse(Types.IdentityAuthenticationPatchServiceAccountInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PATCH',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Check if service account exists
   *
   * Checks if a service account exists without returning the body.
   */
  async headAuthServiceAccounts(serviceAccountId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/auth/service-accounts/${serviceAccountId}`;

    const result = await this.client.request({
      method: 'HEAD',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
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

export function createAuthServiceaccountsModule(client: ApiClient): AuthServiceaccountsModule {
  return new AuthServiceaccountsModule(client);
}
