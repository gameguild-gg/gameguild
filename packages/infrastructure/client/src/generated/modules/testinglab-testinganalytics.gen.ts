/**
 * @game-guild/client - TestinglabTestinganalytics Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class TestinglabTestinganalyticsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getTestingAnalytics(query?: {
    fromDate?: string;
    toDate?: string;
    includeComparison?: boolean;
  }): Promise<Result<Types.TestingLabTestingLabAnalyticsReportProjection, ApiError>> {
    const url = '/v1/testing/analytics';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.TestingLabTestingLabAnalyticsReportProjectionSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getTestingAnalyticsExport(query?: { fromDate?: string; toDate?: string }): Promise<Result<Blob, ApiError>> {
    const url = '/v1/testing/analytics/export';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Blob, ApiError>;
  }
}

export function createTestinglabTestinganalyticsModule(client: ApiClient): TestinglabTestinganalyticsModule {
  return new TestinglabTestinganalyticsModule(client);
}
