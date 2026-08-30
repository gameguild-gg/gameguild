/**
 * @game-guild/client - CommerceBillingInvoices Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class CommerceBillingInvoicesModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Retry invoice payment
   *
   * Accepts a local retry scheduling request for open or past-due invoices. External gateway capture requires configured payment-provider credentials.
   */
  async postBillingInvoicesRetry(invoiceId: string): Promise<Result<Types.CommerceBillingInvoicePaymentRetryResult, ApiError>> {
    const url = `/api/v1/billing/invoices/${invoiceId}/retry`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceBillingInvoicePaymentRetryResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createCommerceBillingInvoicesModule(client: ApiClient): CommerceBillingInvoicesModule {
  return new CommerceBillingInvoicesModule(client);
}
