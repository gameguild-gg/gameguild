/**
 * @game-guild/client - CommercePaymentsWallets Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class CommercePaymentsWalletsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Get my wallet
   */
  async getWallet(): Promise<Result<Types.CommercePaymentsUserWallet, ApiError>> {
    const url = '/api/v1/wallet';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsUserWalletSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Create my wallet
   */
  async postWallet(body: Types.CommercePaymentsCreateWalletInput): Promise<Result<Types.CommercePaymentsUserWallet, ApiError>> {
    const url = '/api/v1/wallet';

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsCreateWalletInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsUserWalletSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Get my wallet balance
   */
  async getWalletBalance(): Promise<Result<void, ApiError>> {
    const url = '/api/v1/wallet/balance';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Lock my wallet
   */
  async postWalletLock(body: Types.CommercePaymentsLockWalletInput): Promise<Result<void, ApiError>> {
    const url = '/api/v1/wallet:lock';

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsLockWalletInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Unlock my wallet
   */
  async postWalletUnlock(): Promise<Result<void, ApiError>> {
    const url = '/api/v1/wallet:unlock';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * List all wallets
   */
  async getWallets(query?: { page?: number; pageSize?: number; currency?: string; isFrozen?: boolean }): Promise<Result<void, ApiError>> {
    const url = '/api/v1/wallets';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Get wallet by ID
   */
  async getWallets1(walletId: string): Promise<Result<void, ApiError>> {
    const url = `/api/v1/wallets/${walletId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Close wallet
   */
  async deleteWallets(walletId: string): Promise<Result<void, ApiError>> {
    const url = `/api/v1/wallets/${walletId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Update wallet settings
   */
  async patchWallets(walletId: string, body: Types.CommercePaymentsModelsPatchWalletInput): Promise<Result<void, ApiError>> {
    const url = `/api/v1/wallets/${walletId}`;

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsModelsPatchWalletInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PATCH',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Check if wallet exists
   */
  async headWallets(walletId: string): Promise<Result<void, ApiError>> {
    const url = `/api/v1/wallets/${walletId}`;

    const result = await this.client.request({
      method: 'HEAD',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Freeze wallet
   */
  async postWalletsFreeze(walletId: string, body: Types.CommercePaymentsModelsFreezeWalletInput): Promise<Result<void, ApiError>> {
    const url = `/api/v1/wallets/${walletId}:freeze`;

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsModelsFreezeWalletInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Unfreeze wallet
   */
  async postWalletsUnfreeze(walletId: string): Promise<Result<void, ApiError>> {
    const url = `/api/v1/wallets/${walletId}:unfreeze`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Get wallet audit log
   */
  async getWalletsAuditLog(walletId: string, query?: { page?: number; pageSize?: number }): Promise<Result<void, ApiError>> {
    const url = `/api/v1/wallets/${walletId}/audit-log`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createCommercePaymentsWalletsModule(client: ApiClient): CommercePaymentsWalletsModule {
  return new CommercePaymentsWalletsModule(client);
}
