/**
 * @game-guild/client - AccessControlPermissionAnalytics Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class AccessControlPermissionAnalyticsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getPermissionAnalyticsAnomalies(query?: {
    tenantId?: string;
    fromDate?: string;
  }): Promise<Result<Array<Types.IdentityAuthorizationPermissionAnomaly>, ApiError>> {
    const url = '/v1/permission-analytics/anomalies';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.IdentityAuthorizationPermissionAnomaly>, ApiError>;
  }

  /**
   */
  async getPermissionAnalyticsReport(query?: {
    tenantId?: string;
    periodStart?: string;
    periodEnd?: string;
  }): Promise<Result<Types.IdentityAuthorizationPermissionAnalyticsReport, ApiError>> {
    const url = '/v1/permission-analytics/report';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.IdentityAuthorizationPermissionAnalyticsReportSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getPermissionAnalyticsResourcePatterns(query?: {
    tenantId?: string;
    top?: number;
    fromDate?: string;
    toDate?: string;
  }): Promise<Result<Array<Types.IdentityAuthorizationResourceAccessPattern>, ApiError>> {
    const url = '/v1/permission-analytics/resource-patterns';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.IdentityAuthorizationResourceAccessPattern>, ApiError>;
  }

  /**
   */
  async getPermissionAnalyticsTrends(query?: {
    tenantId?: string;
    fromDate?: string;
    toDate?: string;
  }): Promise<Result<Array<Types.IdentityAuthorizationPermissionTrend>, ApiError>> {
    const url = '/v1/permission-analytics/trends';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.IdentityAuthorizationPermissionTrend>, ApiError>;
  }

  /**
   */
  async getPermissionAnalyticsUsage(query?: {
    tenantId?: string;
    fromDate?: string;
    toDate?: string;
  }): Promise<Result<Array<Types.IdentityAuthorizationPermissionUsageMetrics>, ApiError>> {
    const url = '/v1/permission-analytics/usage';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.IdentityAuthorizationPermissionUsageMetrics>, ApiError>;
  }

  /**
   */
  async getPermissionAnalyticsUserActivity(query?: {
    tenantId?: string;
    top?: number;
    fromDate?: string;
    toDate?: string;
  }): Promise<Result<Array<Types.IdentityAuthorizationUserActivitySummary>, ApiError>> {
    const url = '/v1/permission-analytics/user-activity';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.IdentityAuthorizationUserActivitySummary>, ApiError>;
  }
}

export function createAccessControlPermissionAnalyticsModule(client: ApiClient): AccessControlPermissionAnalyticsModule {
  return new AccessControlPermissionAnalyticsModule(client);
}
