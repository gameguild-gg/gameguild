/**
 * @game-guild/client - TestinglabAnalytics Module
 *
 * AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from "../../runtime/client.js";
import type { ApiError } from "../../runtime/errors/types.js";
import type { Result } from "../../runtime/result/types.js";
import type * as Types from "../types.gen.js";

export interface TestingLabAnalyticsQuery {
  fromDate?: string;
  toDate?: string;
  includeComparison?: boolean;
}

export interface TestingLabAnalyticsExportQuery {
  fromDate?: string;
  toDate?: string;
}

export class TestinglabAnalyticsModule {
  constructor(private readonly client: ApiClient) {}

  async getTestingAnalytics(
    query?: TestingLabAnalyticsQuery,
  ): Promise<Result<Types.TestingLabAnalyticsReportProjection, ApiError>> {
    const result = await this.client.request({
      method: "GET",
      path: "/v1/testing/analytics",
      params: query
        ? {
            fromDate: query.fromDate,
            toDate: query.toDate,
            includeComparison: query.includeComparison,
          }
        : undefined,
      requiresAuth: true,
    });

    return result as Result<
      Types.TestingLabAnalyticsReportProjection,
      ApiError
    >;
  }

  async getTestingAnalyticsExport(
    query?: TestingLabAnalyticsExportQuery,
  ): Promise<Result<string, ApiError>> {
    const result = await this.client.request({
      method: "GET",
      path: "/v1/testing/analytics/export",
      params: query
        ? {
            fromDate: query.fromDate,
            toDate: query.toDate,
          }
        : undefined,
      requiresAuth: true,
    });

    return result as Result<string, ApiError>;
  }
}

export function createTestinglabAnalyticsModule(
  client: ApiClient,
): TestinglabAnalyticsModule {
  return new TestinglabAnalyticsModule(client);
}
