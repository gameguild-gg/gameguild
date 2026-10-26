/**
 * @game-guild/client - CommercePaymentsFraudStatistics Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class CommercePaymentsFraudStatisticsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Get the fraud statistics report for a date window (defaults to the last 30 days).
   *
   * Reports fraud-program statistics over a date window: assessed, flagged and blocked payment counts with rates, the chargeback rate among succeeded payments, the false-positive proxy (held reviews later approved), the manual-review backlog with average time-to-review, and the top fraud signals by frequency. Requires the payments:admin permission.
   */
  async getFraudStatistics(query?: {
    tenantId?: string;
    fromUtc?: string;
    toUtc?: string;
  }): Promise<Result<Types.CommercePaymentsFraudStatisticsReport, ApiError>> {
    const url = '/api/v1/fraud/statistics';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommercePaymentsFraudStatisticsReportSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createCommercePaymentsFraudStatisticsModule(client: ApiClient): CommercePaymentsFraudStatisticsModule {
  return new CommercePaymentsFraudStatisticsModule(client);
}
