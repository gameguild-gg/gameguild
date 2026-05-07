/**
 * @game-guild/client - Resources Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class ResourcesModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Get resource usage by type
   *
   * Retrieves aggregated resource usage across all tenants within the specified date range for the given resource type.
   */
  async getResourcesUsage(query?: {
    type?: Types.ResourcesResourceUsageType;
    startDate?: string;
    endDate?: string;
  }): Promise<Result<Record<string, number>, ApiError>> {
    const url = '/v1/resources/usage';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Record<string, number>, ApiError>;
  }

  /**
   * Get resource usage trends over time
   *
   * Retrieves resource usage trends with time-series data aggregated by the specified granularity.
   */
  async getResourcesUsageTrends(query?: {
    type?: Types.ResourcesResourceUsageType;
    startDate?: string;
    endDate?: string;
    granularity?: Types.ResourcesTrendGranularity;
  }): Promise<Result<Types.ResourcesUsageTrendsResult, ApiError>> {
    const url = '/v1/resources/usage-trends';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ResourcesUsageTrendsResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Archive old resource usage records
   *
   * Archives resource usage records older than the specified date for storage optimization.
   */
  async postResourcesArchive(body: Types.ResourcesArchiveResourceUsageRecordsInput): Promise<Result<void, ApiError>> {
    const url = '/v1/resources:archive';

    // Validate request body
    const validatedBody = safeParse(Types.ResourcesArchiveResourceUsageRecordsInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Cleanup orphaned resources
   *
   * Identifies and removes orphaned resources that are no longer associated with any tenant or user.
   */
  async postResourcesCleanup(body: Types.ResourcesCleanupOrphanedResourcesInput): Promise<Result<void, ApiError>> {
    const url = '/v1/resources:cleanup';

    // Validate request body
    const validatedBody = safeParse(Types.ResourcesCleanupOrphanedResourcesInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createResourcesModule(client: ApiClient): ResourcesModule {
  return new ResourcesModule(client);
}
