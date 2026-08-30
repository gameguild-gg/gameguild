/**
 * @game-guild/client - AssetsAdmin Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class AssetsAdminModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getAdminAssets(query?: { status?: string; limit?: number }): Promise<Result<void, ApiError>> {
    const url = '/v1/admin/assets';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postAdminAssetsRunGc(query?: { gracePeriodHours?: number; limit?: number; dryRun?: boolean }): Promise<Result<void, ApiError>> {
    const url = '/v1/admin/assets/:run-gc';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postAdminAssetsMarkUndeletable(contentId: string, body: Types.AssetsControllersMarkNonDeletableInput): Promise<Result<void, ApiError>> {
    const url = `/v1/admin/assets/${contentId}:mark-undeletable`;

    // Validate request body
    const validatedBody = safeParse(Types.AssetsControllersMarkNonDeletableInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postAdminAssetsReviewModeration(contentId: string, body: Types.AssetsControllersContentModerationInput): Promise<Result<void, ApiError>> {
    const url = `/v1/admin/assets/${contentId}:review-moderation`;

    // Validate request body
    const validatedBody = safeParse(Types.AssetsControllersContentModerationInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postAdminAssetsRunVirusScan(contentId: string, body: Types.AssetsControllersUpdateVirusScanInput): Promise<Result<void, ApiError>> {
    const url = `/v1/admin/assets/${contentId}:run-virus-scan`;

    // Validate request body
    const validatedBody = safeParse(Types.AssetsControllersUpdateVirusScanInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postAdminAssetsUnmarkUndeletable(contentId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/admin/assets/${contentId}:unmark-undeletable`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postAdminAssetsForceDelete(id: string): Promise<Result<void, ApiError>> {
    const url = `/v1/admin/assets/${id}:force-delete`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getAdminAssetsReports(id: string): Promise<Result<void, ApiError>> {
    const url = `/v1/admin/assets/${id}/reports`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getAdminAssetsGcCandidates(query?: { gracePeriodHours?: number; limit?: number }): Promise<Result<void, ApiError>> {
    const url = '/v1/admin/assets/gc-candidates';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getAdminAssetsModerationQueue(query?: { limit?: number }): Promise<Result<void, ApiError>> {
    const url = '/v1/admin/assets/moderation-queue';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postAdminAssetsReportsReview(reportId: string, body: Types.AssetsControllersReviewReportInput): Promise<Result<void, ApiError>> {
    const url = `/v1/admin/assets/reports/${reportId}:review`;

    // Validate request body
    const validatedBody = safeParse(Types.AssetsControllersReviewReportInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getAdminAssetsRetention(query?: {
    gracePeriodHours?: number;
    limit?: number;
  }): Promise<Result<Types.AssetsQueriesAssetRetentionReportOutput, ApiError>> {
    const url = '/v1/admin/assets/retention';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.AssetsQueriesAssetRetentionReportOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAdminAssetsRetentionRun(query?: { gracePeriodHours?: number; limit?: number; dryRun?: boolean }): Promise<Result<void, ApiError>> {
    const url = '/v1/admin/assets/retention:run';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getAdminAssetsStatistics(): Promise<Result<Types.AssetsQueriesAssetStatisticsOutput, ApiError>> {
    const url = '/v1/admin/assets/statistics';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.AssetsQueriesAssetStatisticsOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getAdminAssetsStatisticsExport(query?: { format?: string }): Promise<Result<Blob, ApiError>> {
    const url = '/v1/admin/assets/statistics:export';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Blob, ApiError>;
  }
}

export function createAssetsAdminModule(client: ApiClient): AssetsAdminModule {
  return new AssetsAdminModule(client);
}
