/**
 * @game-guild/client - FinanceLedgersFinanceLedgers Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class FinanceLedgersFinanceLedgersModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Gets a ledger by ID.
   */
  async getLedgers(ledgerId: string): Promise<Result<Types.FinanceLedgersModelsLedgerDto, ApiError>> {
    const url = `/api/v1/ledgers/${ledgerId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.FinanceLedgersModelsLedgerDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Gets the ancestor path from root to ledger.
   */
  async getLedgersAncestors(ledgerId: string): Promise<Result<Types.FinanceLedgersModelsLedgerDto[], ApiError>> {
    const url = `/api/v1/ledgers/${ledgerId}/ancestors`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Types.FinanceLedgersModelsLedgerDto[], ApiError>;
  }

  /**
   * Gets balance for a ledger (direct entries only).
   */
  async getLedgersBalance(ledgerId: string, query?: { asOfDate?: string }): Promise<Result<Types.FinanceLedgersModelsLedgerBalance, ApiError>> {
    const url = `/api/v1/ledgers/${ledgerId}/balance`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.FinanceLedgersModelsLedgerBalanceSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Gets consolidated balance including all descendants.
   */
  async getLedgersBalanceConsolidated(ledgerId: string, query?: { asOfDate?: string }): Promise<Result<Types.FinanceLedgersModelsLedgerBalance, ApiError>> {
    const url = `/api/v1/ledgers/${ledgerId}/balance/consolidated`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.FinanceLedgersModelsLedgerBalanceSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Gets direct children of a ledger.
   */
  async getLedgersChildren(ledgerId: string): Promise<Result<Types.FinanceLedgersModelsLedgerDto[], ApiError>> {
    const url = `/api/v1/ledgers/${ledgerId}/children`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Types.FinanceLedgersModelsLedgerDto[], ApiError>;
  }

  /**
   * Gets descendants of a ledger.
   */
  async getLedgersDescendants(ledgerId: string, query?: { maxDepth?: number }): Promise<Result<Types.FinanceLedgersModelsLedgerDto[], ApiError>> {
    const url = `/api/v1/ledgers/${ledgerId}/descendants`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.FinanceLedgersModelsLedgerDto[], ApiError>;
  }

  /**
   * Gets entries for a ledger (direct only).
   */
  async getLedgersEntriesForGetLedgersByLedgerIdEntries(
    ledgerId: string,
    query?: { fromDate?: string; toDate?: string },
  ): Promise<Result<Types.FinanceLedgersModelsLedgerEntryDto[], ApiError>> {
    const url = `/api/v1/ledgers/${ledgerId}/entries`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.FinanceLedgersModelsLedgerEntryDto[], ApiError>;
  }

  /**
   * Posts a new entry to a ledger.
   */
  async postLedgersEntries(
    ledgerId: string,
    body: Types.FinanceLedgersControllersPostEntryInput,
  ): Promise<Result<Types.FinanceLedgersModelsLedgerEntryDto, ApiError>> {
    const url = `/api/v1/ledgers/${ledgerId}/entries`;

    // Validate request body
    const validatedBody = safeParse(Types.FinanceLedgersControllersPostEntryInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.FinanceLedgersModelsLedgerEntryDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Gets entries for a ledger including all descendants (rollup).
   */
  async getLedgersEntriesRollup(
    ledgerId: string,
    query?: { fromDate?: string; toDate?: string },
  ): Promise<Result<Types.FinanceLedgersModelsLedgerEntryDto[], ApiError>> {
    const url = `/api/v1/ledgers/${ledgerId}/entries/rollup`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.FinanceLedgersModelsLedgerEntryDto[], ApiError>;
  }

  /**
   * Gets the hierarchy tree starting from a ledger.
   */
  async getLedgersHierarchy(ledgerId: string, query?: { maxDepth?: number }): Promise<Result<Types.FinanceLedgersModelsLedgerTreeNode, ApiError>> {
    const url = `/api/v1/ledgers/${ledgerId}/hierarchy`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.FinanceLedgersModelsLedgerTreeNodeSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Gets hierarchical rollup for a subtree.
   */
  async getLedgersRollup(
    ledgerId: string,
    query?: { fromDate?: string; toDate?: string },
  ): Promise<Result<Types.FinanceLedgersModelsLedgerRollupNode, ApiError>> {
    const url = `/api/v1/ledgers/${ledgerId}/rollup`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.FinanceLedgersModelsLedgerRollupNodeSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Gets category-based rollup for a ledger.
   */
  async getLedgersRollupCategories(
    ledgerId: string,
    query?: { includeDescendants?: boolean; fromDate?: string; toDate?: string },
  ): Promise<Result<Types.FinanceLedgersModelsCategoryRollup[], ApiError>> {
    const url = `/api/v1/ledgers/${ledgerId}/rollup/categories`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.FinanceLedgersModelsCategoryRollup[], ApiError>;
  }

  /**
   * Gets period-based rollup for a ledger.
   */
  async getLedgersRollupPeriods(
    ledgerId: string,
    query?: { fromDate?: string; toDate?: string; granularity?: Types.FinanceLedgersModelsPeriodGranularity; includeDescendants?: boolean },
  ): Promise<Result<Types.FinanceLedgersModelsPeriodRollup[], ApiError>> {
    const url = `/api/v1/ledgers/${ledgerId}/rollup/periods`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.FinanceLedgersModelsPeriodRollup[], ApiError>;
  }

  /**
   * Creates a new child ledger under a parent.
   */
  async postLedgersChildren(
    parentLedgerId: string,
    body: Types.FinanceLedgersControllersCreateChildLedgerInput,
  ): Promise<Result<Types.FinanceLedgersModelsLedgerDto, ApiError>> {
    const url = `/api/v1/ledgers/${parentLedgerId}/children`;

    // Validate request body
    const validatedBody = safeParse(Types.FinanceLedgersControllersCreateChildLedgerInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.FinanceLedgersModelsLedgerDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Gets a ledger by code within tenant.
   */
  async getLedgersByCode(tenantId: string, code: string): Promise<Result<Types.FinanceLedgersModelsLedgerDto, ApiError>> {
    const url = `/api/v1/ledgers/by-code/${tenantId}/${code}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.FinanceLedgersModelsLedgerDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Gets an entry by ID.
   */
  async getLedgersEntriesForGetLedgersEntriesByEntryId(entryId: string): Promise<Result<Types.FinanceLedgersModelsLedgerEntryDto, ApiError>> {
    const url = `/api/v1/ledgers/entries/${entryId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.FinanceLedgersModelsLedgerEntryDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Deletes an entry that has not been reconciled, voided, transferred, or linked to a reversal.
   */
  async deleteLedgersEntries(entryId: string): Promise<Result<void, ApiError>> {
    const url = `/api/v1/ledgers/entries/${entryId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Updates a mutable entry.
   */
  async patchLedgersEntries(
    entryId: string,
    body: Types.FinanceLedgersControllersUpdateEntryInput,
  ): Promise<Result<Types.FinanceLedgersModelsLedgerEntryDto, ApiError>> {
    const url = `/api/v1/ledgers/entries/${entryId}`;

    // Validate request body
    const validatedBody = safeParse(Types.FinanceLedgersControllersUpdateEntryInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PATCH',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.FinanceLedgersModelsLedgerEntryDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Reverses/voids an entry.
   */
  async postLedgersEntriesReverse(
    entryId: string,
    body: Types.FinanceLedgersControllersReverseEntryInput,
  ): Promise<Result<Types.FinanceLedgersModelsLedgerEntryDto, ApiError>> {
    const url = `/api/v1/ledgers/entries/${entryId}/reverse`;

    // Validate request body
    const validatedBody = safeParse(Types.FinanceLedgersControllersReverseEntryInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.FinanceLedgersModelsLedgerEntryDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Creates a new root ledger.
   */
  async postLedgersRoot(body: Types.FinanceLedgersControllersCreateRootLedgerInput): Promise<Result<Types.FinanceLedgersModelsLedgerDto, ApiError>> {
    const url = '/api/v1/ledgers/root';

    // Validate request body
    const validatedBody = safeParse(Types.FinanceLedgersControllersCreateRootLedgerInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.FinanceLedgersModelsLedgerDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Gets all ledgers for a tenant.
   */
  async getLedgersTenant(
    tenantId: string,
    query?: { status?: Types.FinanceLedgersEnumsLedgerStatus; type?: Types.FinanceLedgersEnumsLedgerType },
  ): Promise<Result<Types.FinanceLedgersModelsLedgerDto[], ApiError>> {
    const url = `/api/v1/ledgers/tenant/${tenantId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.FinanceLedgersModelsLedgerDto[], ApiError>;
  }

  /**
   * Gets root ledgers for a tenant.
   */
  async getLedgersTenantRoots(tenantId: string): Promise<Result<Types.FinanceLedgersModelsLedgerDto[], ApiError>> {
    const url = `/api/v1/ledgers/tenant/${tenantId}/roots`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Types.FinanceLedgersModelsLedgerDto[], ApiError>;
  }

  /**
   * Gets virtual ledgers for a tenant.
   */
  async getLedgersTenantVirtual(tenantId: string): Promise<Result<Types.FinanceLedgersModelsLedgerDto[], ApiError>> {
    const url = `/api/v1/ledgers/tenant/${tenantId}/virtual`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Types.FinanceLedgersModelsLedgerDto[], ApiError>;
  }

  /**
   * Posts a transfer between two ledgers.
   */
  async postLedgersTransfers(body: Types.FinanceLedgersControllersPostTransferInput): Promise<Result<Types.FinanceLedgersModelsTransferResult, ApiError>> {
    const url = '/api/v1/ledgers/transfers';

    // Validate request body
    const validatedBody = safeParse(Types.FinanceLedgersControllersPostTransferInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.FinanceLedgersModelsTransferResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createFinanceLedgersFinanceLedgersModule(client: ApiClient): FinanceLedgersFinanceLedgersModule {
  return new FinanceLedgersFinanceLedgersModule(client);
}
