/**
 * @game-guild/client - Economy Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class EconomyModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Get my Economy capability readiness
   */
  async getEconomyCapabilities(): Promise<Result<Array<Types.APIControllersEconomySelfServiceCapability>, ApiError>> {
    const url = '/api/v1/economy/capabilities';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.APIControllersEconomySelfServiceCapability>, ApiError>;
  }

  /**
   * Convert my confirmed HardCoin balance into SoftCoin
   */
  async postEconomyConversionsHardToSoft(
    body: Types.EconomyCommandsConvertMyHardToSoftInput,
  ): Promise<Result<Types.EconomyFundingSelfServiceHardToSoftConversionReceipt, ApiError>> {
    const url = '/api/v1/economy/conversions/hard-to-soft';

    // Validate request body
    const validatedBody = safeParse(Types.EconomyCommandsConvertMyHardToSoftInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyFundingSelfServiceHardToSoftConversionReceiptSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * List my payout operations
   */
  async getEconomyPayouts(query?: { take?: number }): Promise<Result<Array<Types.EconomyPayoutsQueriesEconomyPayoutOperation>, ApiError>> {
    const url = '/api/v1/economy/payouts';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.EconomyPayoutsQueriesEconomyPayoutOperation>, ApiError>;
  }

  /**
   * Get my payout operation
   */
  async getEconomyPayouts1(operationId: string): Promise<Result<Types.EconomyPayoutsQueriesEconomyPayoutOperation, ApiError>> {
    const url = `/api/v1/economy/payouts/${operationId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyPayoutsQueriesEconomyPayoutOperationSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Get my Economy wallet
   */
  async getEconomyWallet(): Promise<Result<Types.EconomyContractsEconomyWalletSummary, ApiError>> {
    const url = '/api/v1/economy/wallet';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.EconomyContractsEconomyWalletSummarySchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * List my Economy wallet transactions
   */
  async getEconomyWalletTransactions(query?: { take?: number }): Promise<Result<Array<Types.EconomyContractsEconomyWalletTransaction>, ApiError>> {
    const url = '/api/v1/economy/wallet/transactions';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.EconomyContractsEconomyWalletTransaction>, ApiError>;
  }
}

export function createEconomyModule(client: ApiClient): EconomyModule {
  return new EconomyModule(client);
}
