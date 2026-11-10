/**
 * @game-guild/client - AuthServiceAccounts Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client';
import type { Result } from '../../runtime/result/types';
import type { ApiError } from '../../runtime/errors/types';
import * as Types from '../types.gen';
import { safeParse } from '../../runtime/errors/validation';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class AuthServiceAccountsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Gets all service accounts with optional tenant filtering.
   */
  async getAuthServiceAccountsForGetAuthServiceAccounts(query?: {
    tenantId?: string;
  }): Promise<Result<Types.IdentityAuthenticationServiceAccountOutput[], ApiError>> {
    const url = '/v1/auth/service-accounts';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.IdentityAuthenticationServiceAccountOutput[], ApiError>;
  }

  /**
   * Creates a new service account.
   *
   * The client secret is only returned once during creation. Store it securely.
   */
  async postAuthServiceAccounts(
    body?: Types.IdentityAuthenticationCreateServiceAccountInput,
  ): Promise<Result<Types.IdentityAuthenticationServiceAccountCreatedOutput, ApiError>> {
    const url = '/v1/auth/service-accounts';

    // Validate request body
    const validatedBody = body === undefined ? undefined : safeParse(Types.IdentityAuthenticationCreateServiceAccountInputSchema, body, 'request');

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
   * Gets a service account by ID.
   */
  async getAuthServiceAccountsForGetAuthServiceAccountsByServiceAccountId(
    serviceAccountId: string,
  ): Promise<Result<Types.IdentityAuthenticationServiceAccountOutput, ApiError>> {
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
   * Deletes a service account.
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
   * Partially updates a service account.
   *
   * Updates specific fields of a service account. Only provided fields are updated.
   */
  async patchAuthServiceAccounts(serviceAccountId: string, body?: Types.IdentityAuthenticationPatchServiceAccountInput): Promise<Result<void, ApiError>> {
    const url = `/v1/auth/service-accounts/${serviceAccountId}`;

    // Validate request body
    const validatedBody = body === undefined ? undefined : safeParse(Types.IdentityAuthenticationPatchServiceAccountInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PATCH',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Checks if a service account exists by ID.
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
   * Deactivates a service account.
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
   * Locks a service account to prevent authentication.
   *
   * Locks a service account to prevent it from authenticating.
   */
  async postAuthServiceAccountsLock(serviceAccountId: string, body?: Types.IdentityAuthenticationLockServiceAccountInput): Promise<Result<void, ApiError>> {
    const url = `/v1/auth/service-accounts/${serviceAccountId}:lock`;

    // Validate request body
    const validatedBody = body === undefined ? undefined : safeParse(Types.IdentityAuthenticationLockServiceAccountInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Reactivates a deactivated service account.
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
   * Rotates the client secret for a service account.
   *
   * The new client secret is only returned once. Store it securely.
   * The old secret is immediately invalidated.
   */
  async postAuthServiceAccountsRotateSecret(
    serviceAccountId: string,
    body?: Types.IdentityAuthenticationRotateServiceAccountSecretInput,
  ): Promise<Result<Types.IdentityAuthenticationSecretRotationOutput, ApiError>> {
    const url = `/v1/auth/service-accounts/${serviceAccountId}:rotate-secret`;

    // Validate request body
    const validatedBody = body === undefined ? undefined : safeParse(Types.IdentityAuthenticationRotateServiceAccountSecretInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
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
   * Unlocks a locked service account.
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
   * Gets the audit log for a service account.
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
   * Removes both certificate pins; client-secret authentication remains available.
   *
   * Removes the certificate thumbprint and SPKI pin. Requires an administrator in the account's tenant, or a system administrator for a global or cross-tenant account.
   */
  async deleteAuthServiceAccountsClientCertificate(serviceAccountId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/auth/service-accounts/${serviceAccountId}/client-certificate`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Binds an X.509 client certificate to a service account so the certificate can
   * authenticate in lieu of the client secret (issue #1146).
   *
   * Binds an X.509 client certificate (thumbprint plus optional SPKI key pin) to a service account, allowing certificate authentication in lieu of the client secret. Pins are normalized to the authentication-time form; a later certificate presentation matches on either the thumbprint or the SPKI pin.
   */
  async patchAuthServiceAccountsClientCertificate(
    serviceAccountId: string,
    body?: Types.IdentityAuthenticationBindServiceAccountCertificateInput,
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/auth/service-accounts/${serviceAccountId}/client-certificate`;

    // Validate request body
    const validatedBody = body === undefined ? undefined : safeParse(Types.IdentityAuthenticationBindServiceAccountCertificateInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PATCH',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Updates the scopes for a service account.
   */
  async patchAuthServiceAccountsScopes(serviceAccountId: string, body?: Types.IdentityAuthenticationUpdateScopesInput): Promise<Result<void, ApiError>> {
    const url = `/v1/auth/service-accounts/${serviceAccountId}/scopes`;

    // Validate request body
    const validatedBody = body === undefined ? undefined : safeParse(Types.IdentityAuthenticationUpdateScopesInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PATCH',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createAuthServiceAccountsModule(client: ApiClient): AuthServiceAccountsModule {
  return new AuthServiceAccountsModule(client);
}
