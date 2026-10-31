/**
 * @game-guild/client - CommercePaymentsFraudBlacklist Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class CommercePaymentsFraudBlacklistModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * List the tenant's fraud blacklist entries, newest first.
   *
   * Lists the caller's tenant fraud blacklist and whitelist-override entries, newest first, with optional entry-type and expiry filters. Requires the payments:admin permission.
   */
  async getFraudBlacklist(query?: {
    entryType?: Types.CommercePaymentsFraudBlacklistEntryType;
    includeExpired?: boolean;
    page?: number;
    pageSize?: number;
  }): Promise<Result<Types.CommercePaymentsFraudBlacklistPage, ApiError>> {
    const url = '/api/v1/fraud/blacklist';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsFraudBlacklistPageSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Add a blacklist entry or a manual whitelist override for the caller's tenant.
   *
   * Adds a fraud blacklist entry (or a manual whitelist override) for the caller's tenant. Values support * wildcard patterns (e.g. *.malicious.com, 192.168.*) and optional expiry dates. Idempotent per (type, value, kind). Requires the payments:admin permission.
   */
  async postFraudBlacklist(
    body: Types.CommercePaymentsFraudBlacklistControllerAddFraudBlacklistEntryInput,
  ): Promise<Result<Types.CommercePaymentsFraudBlacklistEntryResult, ApiError>> {
    const url = '/api/v1/fraud/blacklist';

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsFraudBlacklistControllerAddFraudBlacklistEntryInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsFraudBlacklistEntryResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Remove a blacklist entry owned by the caller's tenant.
   *
   * Removes (soft-deletes) a fraud blacklist entry owned by the caller's tenant. Requires the payments:admin permission.
   */
  async deleteFraudBlacklist(entryId: string): Promise<Result<Types.CommercePaymentsRemoveFraudBlacklistEntryResult, ApiError>> {
    const url = `/api/v1/fraud/blacklist/${entryId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsRemoveFraudBlacklistEntryResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Check whether a concrete value matches the tenant blacklist (same evaluation the
   * payment fraud gate performs).
   *
   * Evaluates a concrete value (IP address, email domain, card BIN, device fingerprint, payment-method token, card fingerprint or provider customer id) against the caller's tenant blacklist, honoring wildcard patterns, expiry dates and manual whitelist overrides. Requires the payments:admin permission.
   */
  async getFraudBlacklistCheck(query?: {
    entryType?: Types.CommercePaymentsFraudBlacklistEntryType;
    value?: string;
  }): Promise<Result<Types.CommercePaymentsFraudBlacklistCheckResult, ApiError>> {
    const url = '/api/v1/fraud/blacklist/check';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsFraudBlacklistCheckResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Export the tenant's fraud blacklist entries as CSV (issue #409: import/export blacklists).
   *
   * Exports the caller's tenant fraud blacklist and whitelist-override entries as CSV (columns: entry_type,value,kind,reason,expires_at,created_at). Wildcard patterns are exported verbatim so a re-import reproduces them. Requires the payments:admin permission.
   */
  async getFraudBlacklistExport(query?: {
    entryType?: Types.CommercePaymentsFraudBlacklistEntryType;
    includeExpired?: boolean;
  }): Promise<Result<void, ApiError>> {
    const url = '/api/v1/fraud/blacklist/export';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Import fraud blacklist entries from a CSV request body (issue #409: import/export
   * blacklists). Accepts the file shape produced by `GET /fraud/blacklist/export`
   * (header row detected and skipped); every row is added through the same idempotent
   * command as the add endpoint, so re-importing is safe. Rows that fail validation
   * are reported individually and never abort the rest of the batch.
   *
   * Imports fraud blacklist entries from a CSV body (columns: entry_type,value,kind,reason,expires_at; header row optional, * wildcards supported). At most 1000 rows and 1 MB per request; idempotent per (type, value, kind). Requires the payments:admin permission.
   */
  async postFraudBlacklistImport(): Promise<Result<Types.CommercePaymentsFraudBlacklistControllerFraudBlacklistImportResult, ApiError>> {
    const url = '/api/v1/fraud/blacklist/import';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsFraudBlacklistControllerFraudBlacklistImportResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createCommercePaymentsFraudBlacklistModule(client: ApiClient): CommercePaymentsFraudBlacklistModule {
  return new CommercePaymentsFraudBlacklistModule(client);
}
