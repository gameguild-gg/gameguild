/**
 * @game-guild/client - CommercePaymentsPriceLocalization Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class CommercePaymentsPriceLocalizationModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Effective localized price per (plan, enabled currency), annotated with the live
   * FX-calculated reference price so operators see override vs auto side by side.
   *
   * Effective localization per plan and enabled currency with the current FX-calculated reference price (override vs auto-calculated view).
   */
  async getPriceLocalization(query?: { planId?: string }): Promise<Result<Types.CommercePaymentsPlanLocalizedPriceDto[], ApiError>> {
    const url = '/api/v1/price-localization';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.CommercePaymentsPlanLocalizedPriceDto[], ApiError>;
  }

  /**
   * Sets a manual localized price (psychological pricing) for a plan in an enabled
   * currency. Optional effective date schedules a future change; optional expiry ends a
   * temporary override. Audit-logged.
   *
   * Persists an audited manual price override for the plan/currency pair; recalculation runs never touch manual rows.
   */
  async postPriceLocalizationPlansCurrencies(
    planId: string,
    currencyCode: string,
    body: Types.CommercePaymentsSetLocalizedPriceInput,
  ): Promise<Result<Record<string, unknown>, ApiError>> {
    const url = `/api/v1/price-localization/plans/${planId}/currencies/${currencyCode}`;

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsSetLocalizedPriceInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<Record<string, unknown>, ApiError>;
  }

  /**
   * Clears the manual overrides of a (plan, currency) pair so pricing falls back to the
   * auto-calculated/FX-converted price. Audit-logged.
   *
   * Removes the manual overrides of the plan/currency pair (including future-dated rows); auto-calculated rows are untouched.
   */
  async deletePriceLocalizationPlansCurrencies(
    planId: string,
    currencyCode: string,
    body: Types.CommercePaymentsClearLocalizedPriceInput,
  ): Promise<Result<Record<string, unknown>, ApiError>> {
    const url = `/api/v1/price-localization/plans/${planId}/currencies/${currencyCode}`;

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsClearLocalizedPriceInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<Record<string, unknown>, ApiError>;
  }

  /**
   * Runs a bulk recalculation of auto-calculated prices from the current FX quotations
   * (manual overrides are preserved). Creates a progress-tracked, rollback-capable run.
   *
   * Bulk-recalculates auto-calculated localized prices from current FX rates; preserves manual overrides; tracks progress on a run row that can be rolled back.
   */
  async postPriceLocalizationRecalculate(
    body: Types.CommercePaymentsRecalculateLocalizedPricesInput,
  ): Promise<Result<Types.CommercePaymentsPriceRecalculationOutcome, ApiError>> {
    const url = '/api/v1/price-localization/recalculate';

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsRecalculateLocalizedPricesInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsPriceRecalculationOutcomeSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Previews a recalculation pass: proposed auto prices against currently effective
   * values, with the quotation behind every proposal. Read-only.
   *
   * Computes what a recalculation pass would change (proposed auto prices, rates, deltas) without writing anything.
   */
  async getPriceLocalizationRecalculatePreview(query?: { planId?: string }): Promise<Result<Types.CommercePaymentsLocalizedPricePreviewItem[], ApiError>> {
    const url = '/api/v1/price-localization/recalculate/preview';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.CommercePaymentsLocalizedPricePreviewItem[], ApiError>;
  }

  /**
   * Lists recalculation runs with their progress counters (newest first).
   *
   * Recalculation runs with progress counters (total/completed items, updated prices, preserved overrides) and rollback state.
   */
  async getPriceLocalizationRecalculations(query?: { limit?: number }): Promise<Result<Types.CommercePaymentsPriceRecalculationRunDto[], ApiError>> {
    const url = '/api/v1/price-localization/recalculations';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.CommercePaymentsPriceRecalculationRunDto[], ApiError>;
  }

  /**
   * Rolls back a finished recalculation run, restoring every auto row it rewrote to its
   * snapshotted before-value.
   *
   * Restores the before-values snapshotted by the run; manual overrides were never touched and stay unchanged.
   */
  async postPriceLocalizationRecalculationsRollback(
    runId: string,
    body: Types.CommercePaymentsRollbackRecalculationInput,
  ): Promise<Result<Record<string, unknown>, ApiError>> {
    const url = `/api/v1/price-localization/recalculations/${runId}/rollback`;

    // Validate request body
    const validatedBody = safeParse(Types.CommercePaymentsRollbackRecalculationInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<Record<string, unknown>, ApiError>;
  }
}

export function createCommercePaymentsPriceLocalizationModule(client: ApiClient): CommercePaymentsPriceLocalizationModule {
  return new CommercePaymentsPriceLocalizationModule(client);
}
