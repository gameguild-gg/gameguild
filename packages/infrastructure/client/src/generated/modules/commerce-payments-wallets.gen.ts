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
   * Get user's wallet
   *
   * Retrieves the wallet for a specific user.
   */
  async getUsersWallet(userId: string): Promise<Result<Types.CommercePaymentsUserWallet, ApiError>> {
    const url = `/api/v1/users/${userId}/wallet`;

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
   * Get user's wallet balance
   *
   * Retrieves the wallet balance for a specific user.
   */
  async getUsersWalletBalance(userId: string): Promise<Result<number, ApiError>> {
    const url = `/api/v1/users/${userId}/wallet/balance`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<number, ApiError>;
  }

  /**
   * Add funds to user's wallet
   *
   * Adds funds to the wallet for the specified user.
   */
  async postUsersWalletAddFunds(userId: string, body: Types.CommercePaymentsAddFundsInput): Promise<Result<Types.CommercePaymentsWalletTransaction, ApiError>> {
    const url = `/api/v1/users/${userId}/wallet:add-funds`;

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsAddFundsInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsWalletTransactionSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Deduct funds from user's wallet
   *
   * Deducts funds from the wallet for the specified user.
   */
  async postUsersWalletDeductFunds(
    userId: string,
    body: Types.CommercePaymentsDeductFundsInput,
  ): Promise<Result<Types.CommercePaymentsWalletTransaction, ApiError>> {
    const url = `/api/v1/users/${userId}/wallet:deduct-funds`;

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsDeductFundsInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsWalletTransactionSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Lock user's wallet
   *
   * Locks a user's wallet to prevent transactions.
   */
  async postUsersWalletLock(userId: string, body: Types.CommercePaymentsLockWalletInput): Promise<Result<void, ApiError>> {
    const url = `/api/v1/users/${userId}/wallet:lock`;

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
   * Transfer funds to another user's wallet
   *
   * Transfers funds from this user's wallet to another user's wallet.
   */
  async postUsersWalletTransfer(
    userId: string,
    body: Types.CommercePaymentsTransferFundsInput,
  ): Promise<Result<Types.CommercePaymentsTransferResult, ApiError>> {
    const url = `/api/v1/users/${userId}/wallet:transfer`;

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsTransferFundsInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsTransferResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Unlock user's wallet
   *
   * Unlocks a user's wallet to allow transactions.
   */
  async postUsersWalletUnlock(userId: string): Promise<Result<void, ApiError>> {
    const url = `/api/v1/users/${userId}/wallet:unlock`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * List all wallets
   *
   * Retrieves a paginated list of all wallets. Admin only.
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
   * Create a new wallet
   *
   * Creates a new wallet for the specified user.
   */
  async postWallets(body: Types.CommercePaymentsCreateWalletInput): Promise<Result<Types.CommercePaymentsUserWallet, ApiError>> {
    const url = '/api/v1/wallets';

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
   * Get wallet by ID
   *
   * Retrieves detailed information for a specific wallet.
   */
  async getWallets1(walletId: string): Promise<Result<Types.CommercePaymentsUserWallet, ApiError>> {
    const url = `/api/v1/wallets/${walletId}`;

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
   * Close wallet
   *
   * Closes a wallet. Requires zero balance and admin permissions.
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
   *
   * Updates specific settings of a wallet.
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
   *
   * Checks if a wallet exists without returning the body.
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
   * Get wallet audit log
   *
   * Retrieves the audit log of all transactions and actions on a wallet.
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

  /**
   * Freeze wallet
   *
   * Freezes a wallet to prevent all transactions.
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
   *
   * Unfreezes a wallet to allow transactions.
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
}

export function createCommercePaymentsWalletsModule(client: ApiClient): CommercePaymentsWalletsModule {
  return new CommercePaymentsWalletsModule(client);
}
