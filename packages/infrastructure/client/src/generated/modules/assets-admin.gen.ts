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
   * List admin assets with optional status filter.
   * Use status=pending-virus-scan or status=pending-moderation to filter.
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
   * Trigger manual garbage collection.
   *
   * Runs the garbage collection process manually instead of waiting for the scheduled background job.
   * Only deletes content that has been marked for deletion and past the grace period.
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
   * Mark an asset content as non-deletable (legal hold).
   *
   * Prevents the asset from being garbage collected, even if all references are deleted.
   * Use for legal holds, compliance requirements, or audit preservation.
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
   * Review and moderate content directly.
   *
   * Unlike report review which handles user reports, this endpoint allows
   * direct moderation of content by admins for proactive moderation workflows.
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
   * Run virus scan on an asset.
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
   * Remove the non-deletable flag from an asset.
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
   * Force delete an asset (admin override).
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
   * Get reports for an asset.
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
   * Get garbage collection candidates.
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
   * Get moderation queue.
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
   * Review a moderation report.
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
   * Get the current retention candidate report.
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
   * Trigger manual garbage collection.
   *
   * Runs the garbage collection process manually instead of waiting for the scheduled background job.
   * Only deletes content that has been marked for deletion and past the grace period.
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
   * Get asset/document statistics for document center dashboards.
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
   * Export asset/document statistics as CSV or PDF.
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
