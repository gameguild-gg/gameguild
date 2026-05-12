/**
 * @game-guild/client - RealestateMaintenancereports Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class RealestateMaintenancereportsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getMaintenanceReportsSummary(query?: {
    propertyId?: string;
    fromUtc?: string;
    toUtc?: string;
  }): Promise<Result<Types.RealEstateModelsMaintenanceReportSummary, ApiError>> {
    const url = '/v1/maintenance-reports/summary';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsMaintenanceReportSummarySchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createRealestateMaintenancereportsModule(client: ApiClient): RealestateMaintenancereportsModule {
  return new RealestateMaintenancereportsModule(client);
}
