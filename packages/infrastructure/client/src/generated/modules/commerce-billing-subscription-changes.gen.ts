/**
 * @game-guild/client - CommerceBillingSubscriptionChanges Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class CommerceBillingSubscriptionChangesModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Run the orchestrated plan change for one subscription
   *
   * Orchestrates the plan change as a saga: proration, invoice, confirmed gateway payment and activation. EffectiveDateUtc is required as an explicit UTC timestamp and must be reused unchanged on retries. Local state and required billing events commit together. Activation requires provider-verified settlement amount and currency; providers without that evidence require reconciliation. Confirmed business failures trigger compensation; uncertain persistence or provider outcomes require reconciliation and retry with the same effective date and payment parameters.
   */
  async postBillingSubscriptionsPlanChangeOrchestrate(
    subscriptionId: string,
    body: Types.CommerceBillingOrchestrateSubscriptionPlanChangeInput,
  ): Promise<Result<Types.CommerceBillingSubscriptionChangeSagaResult, ApiError>> {
    const url = `/api/v1/billing/subscriptions/${subscriptionId}/plan-change:orchestrate`;

    // Validate request body
    const validatedBody = safeParse(Types.CommerceBillingOrchestrateSubscriptionPlanChangeInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceBillingSubscriptionChangeSagaResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createCommerceBillingSubscriptionChangesModule(client: ApiClient): CommerceBillingSubscriptionChangesModule {
  return new CommerceBillingSubscriptionChangesModule(client);
}
