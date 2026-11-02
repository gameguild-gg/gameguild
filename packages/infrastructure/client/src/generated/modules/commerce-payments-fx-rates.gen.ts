/**
 * @game-guild/client - CommercePaymentsFxRates Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class CommercePaymentsFxRatesModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Lists persisted FX quotations (market snapshots and overrides) with optional pair and
   * effective-date-window filters for historical lookup.
   *
   * Persisted FX quotations with pair and effective-date filters, newest effective date first.
   */
  async getFxRates(query?: {
    baseCurrency?: string;
    quoteCurrency?: string;
    fromEffectiveDateUtc?: string;
    toEffectiveDateUtc?: string;
    limit?: number;
  }): Promise<Result<Types.CommercePaymentsFxRateDto[], ApiError>> {
    const url = '/api/v1/fx-rates';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.CommercePaymentsFxRateDto[], ApiError>;
  }

  /**
   *     Sets or replaces an operator manual FX rate override for an exact direction
   * (audit-logged with reason; effective immediately).
   *
   * Persists an audited manual override for one exact currency pair direction; overrides win over synced market rates until replaced or expired.
   */
  async postFxRatesOverrides(body: Types.CommercePaymentsSetManualFxRateOverrideInput): Promise<Result<void, ApiError>> {
    const url = '/api/v1/fx-rates/overrides';

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsSetManualFxRateOverrideInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   *     Provisions the API key of an FX rate provider into the encrypted-at-rest store
   * (issue #412 spec H): the plaintext key is encrypted with ASP.NET DataProtection
   * (AES-256 under the application key ring) before it touches the database. Only the
   * key fingerprint is ever returned or audited.
   *
   * Encrypts the provider API key at rest (DataProtection AES-256) and makes it the active credential; the plaintext is never stored, returned or logged.
   */
  async putFxRatesProviderKeys(
    provider: string,
    body: Types.CommercePaymentsProvisionFxProviderApiKeyInput,
  ): Promise<Result<Types.CommercePaymentsGatewayCredentialProvisionResult, ApiError>> {
    const url = `/api/v1/fx-rates/provider-keys/${provider}`;

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsProvisionFxProviderApiKeyInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsGatewayCredentialProvisionResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Deactivates the active encrypted FX provider API key. The provider falls back to
   * configuration and fails closed when no key is available from either source.
   *
   * Deactivates the active encrypted credential; the encrypted row stays for audit and the provider falls back to configuration.
   */
  async deleteFxRatesProviderKeys(provider: string): Promise<Result<Record<string, unknown>, ApiError>> {
    const url = `/api/v1/fx-rates/provider-keys/${provider}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Record<string, unknown>, ApiError>;
  }

  /**
   * Triggers one FX rate sync pass immediately (same code path as the scheduled worker).
   * Rejected while no provider is configured.
   *
   * Runs one FX sync pass now through the primary (or fallback) provider and persists the rate snapshot.
   */
  async postFxRatesSync(): Promise<Result<Types.CommercePaymentsFxRateSyncResult, ApiError>> {
    const url = '/api/v1/fx-rates/sync';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsFxRateSyncResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * FX sync status: recent runs, consecutive failures, staleness assessment and active
   * grid/override counts.
   *
   * Recent FX sync runs with the consecutive-failure counter and the staleness assessment of the newest persisted rates.
   */
  async getFxRatesSyncStatus(query?: { runLimit?: number }): Promise<Result<Types.CommercePaymentsFxSyncStatusDto, ApiError>> {
    const url = '/api/v1/fx-rates/sync-status';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsFxSyncStatusDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Exchange rate variance report (issue #412 spec G): every active operator override
   * against the newest synced market quotation of the same pair (actual vs expected),
   * with variances beyond the anomaly threshold flagged.
   *
   * Compares every active operator rate override (expected) against the newest synced market quotation (actual) of the same pair; variances beyond the configured threshold are flagged.
   */
  async getFxRatesVariance(): Promise<Result<Types.CommercePaymentsFxRateVarianceReport, ApiError>> {
    const url = '/api/v1/fx-rates/variance';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsFxRateVarianceReportSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createCommercePaymentsFxRatesModule(client: ApiClient): CommercePaymentsFxRatesModule {
  return new CommercePaymentsFxRatesModule(client);
}
