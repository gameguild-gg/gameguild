/**
 * @game-guild/client - CommercePaymentsCurrency Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class CommercePaymentsCurrencyModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Converts a batch of amounts (1..50 entries) into the target currency. Entries keep
   * their own source currency; same-currency entries are identity conversions. Per-item
   * failures (unknown code, disabled currency, missing quotation) are reported on the
   * item, never aborting the batch.
   *
   * Converts one or many amounts into the target currency through the platform FX seam (operator overrides and synced market grids, TTL-cached). Supply asOfUtc to convert with the persisted historical snapshot effective at that date. Both currencies must be enabled for the tenant.
   */
  async postCurrencyConvert(body: Types.CommercePaymentsConvertCurrencyInput): Promise<Result<Types.CommercePaymentsCurrencyConversionResult, ApiError>> {
    const url = '/api/v1/currency/convert';

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsConvertCurrencyInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsCurrencyConversionResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createCommercePaymentsCurrencyModule(client: ApiClient): CommercePaymentsCurrencyModule {
  return new CommercePaymentsCurrencyModule(client);
}
