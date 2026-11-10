/**
 * @game-guild/client - CommerceProductsEntitlements Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client';
import type { Result } from '../../runtime/result/types';
import type { ApiError } from '../../runtime/errors/types';
import * as Types from '../types.gen';
import { safeParse } from '../../runtime/errors/validation';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class CommerceProductsEntitlementsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * List entitlements with optional status filter
   */
  async getEntitlements(query?: { status?: string; days?: number }): Promise<Result<Types.CommerceProductsEntitlementInfoDto[], ApiError>> {
    const url = '/v1/entitlements';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.CommerceProductsEntitlementInfoDto[], ApiError>;
  }

  /**
   * Grant entitlement to a user (create)
   */
  async postEntitlements(body?: Types.CommerceProductsGrantEntitlementInput): Promise<Result<Types.CommerceProductsEntitlementInfoDto, ApiError>> {
    const url = '/v1/entitlements';

    // Validate request body
    const validatedBody = body === undefined ? undefined : safeParse(Types.CommerceProductsGrantEntitlementInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceProductsEntitlementInfoDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Check if current user has access to a product
   */
  async getEntitlementsCheck(query?: { productId?: string }): Promise<Result<Types.CommerceProductsEntitlementCheckResult, ApiError>> {
    const url = '/v1/entitlements/:check';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceProductsEntitlementCheckResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Check if current user has access to multiple products
   */
  async postEntitlementsCheckBatch(body?: Types.CommerceProductsCheckMultipleAccessInput): Promise<Result<Record<string, boolean>, ApiError>> {
    const url = '/v1/entitlements/:check-batch';

    // Validate request body
    const validatedBody = body === undefined ? undefined : safeParse(Types.CommerceProductsCheckMultipleAccessInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<Record<string, boolean>, ApiError>;
  }

  /**
   * Revoke an entitlement (admin only)
   */
  async postEntitlementsRevoke(entitlementId: string, body?: Types.CommerceProductsRevokeEntitlementInput): Promise<Result<void, ApiError>> {
    const url = `/v1/entitlements/${entitlementId}:revoke`;

    // Validate request body
    const validatedBody = body === undefined ? undefined : safeParse(Types.CommerceProductsRevokeEntitlementInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createCommerceProductsEntitlementsModule(client: ApiClient): CommerceProductsEntitlementsModule {
  return new CommerceProductsEntitlementsModule(client);
}
