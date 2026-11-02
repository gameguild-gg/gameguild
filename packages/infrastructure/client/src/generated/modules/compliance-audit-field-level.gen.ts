/**
 * @game-guild/client - ComplianceAuditFieldLevel Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class ComplianceAuditFieldLevelModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Records a field-level access observation (read or write) with automatic classification and masking.
   */
  async postApiAuditFieldLevelAccess(
    body: Types.ComplianceAuditRecordFieldAccessInput,
  ): Promise<Result<Types.ComplianceAuditFieldAccessAuditOutput, ApiError>> {
    const url = '/api/audit/field-level/access';

    // Validate request body
    const validatedBody = safeParse(Types.ComplianceAuditRecordFieldAccessInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditFieldAccessAuditOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Field access history for one entity (or one field of it), newest first.
   */
  async getApiAuditFieldLevelAccessHistory(
    entityId: string,
    query?: { fieldName?: string },
  ): Promise<Result<Types.ComplianceAuditFieldAccessAuditOutput[], ApiError>> {
    const url = `/api/audit/field-level/access-history/${entityId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.ComplianceAuditFieldAccessAuditOutput[], ApiError>;
  }

  /**
   * Anomaly findings from access-pattern analysis, optionally filtered by status.
   */
  async getApiAuditFieldLevelAnomalies(query?: {
    status?: string;
    skip?: number;
    take?: number;
  }): Promise<Result<Types.ComplianceAuditFieldAccessAnomalyOutput[], ApiError>> {
    const url = '/api/audit/field-level/anomalies';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.ComplianceAuditFieldAccessAnomalyOutput[], ApiError>;
  }

  /**
   * Acknowledges an anomaly finding (investigation started).
   */
  async postApiAuditFieldLevelAnomaliesAcknowledge(id: string): Promise<Result<Types.ComplianceAuditFieldAccessAnomalyOutput, ApiError>> {
    const url = `/api/audit/field-level/anomalies/${id}/acknowledge`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditFieldAccessAnomalyOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Resolves an anomaly finding with notes.
   */
  async postApiAuditFieldLevelAnomaliesResolve(
    id: string,
    body: Types.ComplianceAuditResolveFieldAccessAnomalyInput,
  ): Promise<Result<Types.ComplianceAuditFieldAccessAnomalyOutput, ApiError>> {
    const url = `/api/audit/field-level/anomalies/${id}/resolve`;

    // Validate request body
    const validatedBody = safeParse(Types.ComplianceAuditResolveFieldAccessAnomalyInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditFieldAccessAnomalyOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Runs access-pattern analysis over the recent window and persists detected anomalies.
   */
  async postApiAuditFieldLevelAnomaliesAnalyze(
    body: Types.ComplianceAuditAnalyzeFieldAccessPatternsInput,
  ): Promise<Result<Types.ComplianceAuditFieldAccessPatternAnalysisResult[], ApiError>> {
    const url = '/api/audit/field-level/anomalies/analyze';

    // Validate request body
    const validatedBody = safeParse(Types.ComplianceAuditAnalyzeFieldAccessPatternsInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<Types.ComplianceAuditFieldAccessPatternAnalysisResult[], ApiError>;
  }

  /**
   * Observed bulk exports, optionally only threshold-exceeding ones.
   */
  async getApiAuditFieldLevelBulkExports(query?: {
    thresholdExceeded?: boolean;
    skip?: number;
    take?: number;
  }): Promise<Result<Types.ComplianceAuditBulkExportEvent[], ApiError>> {
    const url = '/api/audit/field-level/bulk-exports';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.ComplianceAuditBulkExportEvent[], ApiError>;
  }

  /**
   * Evaluates an export attempt against the bulk-export thresholds, records it and alerts when exceeded.
   */
  async postApiAuditFieldLevelBulkExportsEvaluate(
    body: Types.ComplianceAuditEvaluateBulkExportInput,
  ): Promise<Result<Types.ComplianceAuditBulkExportEvaluationOutput, ApiError>> {
    const url = '/api/audit/field-level/bulk-exports/evaluate';

    // Validate request body
    const validatedBody = safeParse(Types.ComplianceAuditEvaluateBulkExportInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditBulkExportEvaluationOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * The sensitive-field classification registry.
   */
  async getApiAuditFieldLevelClassifications(): Promise<Result<Types.ComplianceAuditSensitiveFieldClassificationOutput[], ApiError>> {
    const url = '/api/audit/field-level/classifications';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Types.ComplianceAuditSensitiveFieldClassificationOutput[], ApiError>;
  }

  /**
   * Re-scans recent field access observations and upserts automatic classifications.
   */
  async postApiAuditFieldLevelClassificationsScan(): Promise<Result<Types.ComplianceAuditSensitiveFieldScanOutput, ApiError>> {
    const url = '/api/audit/field-level/classifications/scan';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditSensitiveFieldScanOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Cross-border transfer log, optionally filtered by validation status.
   */
  async getApiAuditFieldLevelCrossBorderTransfers(query?: {
    status?: string;
    skip?: number;
    take?: number;
  }): Promise<Result<Types.ComplianceAuditCrossBorderTransferLog[], ApiError>> {
    const url = '/api/audit/field-level/cross-border-transfers';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.ComplianceAuditCrossBorderTransferLog[], ApiError>;
  }

  /**
   * Validates a prospective cross-border transfer against the jurisdiction policy and logs the outcome.
   */
  async postApiAuditFieldLevelCrossBorderTransfersValidate(
    body: Types.ComplianceAuditValidateCrossBorderTransferInput,
  ): Promise<Result<Types.ComplianceAuditCrossBorderTransferOutput, ApiError>> {
    const url = '/api/audit/field-level/cross-border-transfers/validate';

    // Validate request body
    const validatedBody = safeParse(Types.ComplianceAuditValidateCrossBorderTransferInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditCrossBorderTransferOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Data subject rights requests with statutory deadline tracking, optionally filtered by status.
   */
  async getApiAuditFieldLevelDataSubjectRequests(query?: {
    status?: string;
    skip?: number;
    take?: number;
  }): Promise<Result<Types.ComplianceAuditDataSubjectRequestOutput[], ApiError>> {
    const url = '/api/audit/field-level/data-subject-requests';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.ComplianceAuditDataSubjectRequestOutput[], ApiError>;
  }

  /**
   * Consent monitoring for one data subject: consent states plus their rights requests.
   */
  async getApiAuditFieldLevelDataSubjectRequestsSubjects(userId: string): Promise<Result<Types.ComplianceAuditDataSubjectConsentMonitoringOutput, ApiError>> {
    const url = `/api/audit/field-level/data-subject-requests/subjects/${userId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditDataSubjectConsentMonitoringOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Aggregate monitoring summary: open, fulfilled and overdue requests plus active consents.
   */
  async getApiAuditFieldLevelDataSubjectRequestsSummary(): Promise<Result<Types.ComplianceAuditDataSubjectAccessMonitoringSummary, ApiError>> {
    const url = '/api/audit/field-level/data-subject-requests/summary';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditDataSubjectAccessMonitoringSummarySchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * The GDPR Article 30 processing activity register, with completeness gaps listed.
   */
  async getApiAuditFieldLevelGdprArticle30(): Promise<Result<Types.ComplianceAuditGdprArticle30Output, ApiError>> {
    const url = '/api/audit/field-level/gdpr/article-30';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditGdprArticle30OutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Creates or updates a processing activity record.
   */
  async putApiAuditFieldLevelGdprArticle30(
    body: Types.ComplianceAuditSaveGdprActivityInput,
  ): Promise<Result<Types.ComplianceAuditGdprProcessingActivityOutput, ApiError>> {
    const url = '/api/audit/field-level/gdpr/article-30';

    // Validate request body
    const validatedBody = safeParse(Types.ComplianceAuditSaveGdprActivityInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditGdprProcessingActivityOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Deletes a processing activity record.
   */
  async deleteApiAuditFieldLevelGdprArticle30(id: string): Promise<Result<void, ApiError>> {
    const url = `/api/audit/field-level/gdpr/article-30/${id}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Marks a processing activity reviewed by the current actor.
   */
  async postApiAuditFieldLevelGdprArticle30Review(id: string): Promise<Result<Types.ComplianceAuditGdprProcessingActivityOutput, ApiError>> {
    const url = `/api/audit/field-level/gdpr/article-30/${id}/review`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditGdprProcessingActivityOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * (Re)derives the platform processing activities for the tenant; manual entries are preserved.
   */
  async postApiAuditFieldLevelGdprArticle30Seed(): Promise<Result<number, ApiError>> {
    const url = '/api/audit/field-level/gdpr/article-30/seed';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<number, ApiError>;
  }

  /**
   * Per-category sensitive-data retention rules.
   */
  async getApiAuditFieldLevelRetention(): Promise<Result<Types.ComplianceAuditSensitiveDataRetentionPolicyOutput[], ApiError>> {
    const url = '/api/audit/field-level/retention';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Types.ComplianceAuditSensitiveDataRetentionPolicyOutput[], ApiError>;
  }

  /**
   * Configures the retention rule for one sensitive data category.
   */
  async putApiAuditFieldLevelRetention(
    body: Types.ComplianceAuditConfigureSensitiveDataRetentionInput,
  ): Promise<Result<Types.ComplianceAuditSensitiveDataRetentionPolicyOutput, ApiError>> {
    const url = '/api/audit/field-level/retention';

    // Validate request body
    const validatedBody = safeParse(Types.ComplianceAuditConfigureSensitiveDataRetentionInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditSensitiveDataRetentionPolicyOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Enforces the sensitive-data retention policies (purge or mask expired records); dry-run reports without changing data.
   */
  async postApiAuditFieldLevelRetentionEnforce(query?: {
    dryRun?: boolean;
  }): Promise<Result<Types.ComplianceAuditSensitiveDataRetentionEnforcementResult, ApiError>> {
    const url = '/api/audit/field-level/retention/enforce';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditSensitiveDataRetentionEnforcementResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Records a field-level access observation (read or write) with automatic classification and masking.
   */
  async postAuditFieldLevelAccess(body: Types.ComplianceAuditRecordFieldAccessInput): Promise<Result<Types.ComplianceAuditFieldAccessAuditOutput, ApiError>> {
    const url = '/v1/audit/field-level/access';

    // Validate request body
    const validatedBody = safeParse(Types.ComplianceAuditRecordFieldAccessInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditFieldAccessAuditOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Field access history for one entity (or one field of it), newest first.
   */
  async getAuditFieldLevelAccessHistory(
    entityId: string,
    query?: { fieldName?: string },
  ): Promise<Result<Types.ComplianceAuditFieldAccessAuditOutput[], ApiError>> {
    const url = `/v1/audit/field-level/access-history/${entityId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.ComplianceAuditFieldAccessAuditOutput[], ApiError>;
  }

  /**
   * Anomaly findings from access-pattern analysis, optionally filtered by status.
   */
  async getAuditFieldLevelAnomalies(query?: {
    status?: string;
    skip?: number;
    take?: number;
  }): Promise<Result<Types.ComplianceAuditFieldAccessAnomalyOutput[], ApiError>> {
    const url = '/v1/audit/field-level/anomalies';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.ComplianceAuditFieldAccessAnomalyOutput[], ApiError>;
  }

  /**
   * Acknowledges an anomaly finding (investigation started).
   */
  async postAuditFieldLevelAnomaliesAcknowledge(id: string): Promise<Result<Types.ComplianceAuditFieldAccessAnomalyOutput, ApiError>> {
    const url = `/v1/audit/field-level/anomalies/${id}/acknowledge`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditFieldAccessAnomalyOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Resolves an anomaly finding with notes.
   */
  async postAuditFieldLevelAnomaliesResolve(
    id: string,
    body: Types.ComplianceAuditResolveFieldAccessAnomalyInput,
  ): Promise<Result<Types.ComplianceAuditFieldAccessAnomalyOutput, ApiError>> {
    const url = `/v1/audit/field-level/anomalies/${id}/resolve`;

    // Validate request body
    const validatedBody = safeParse(Types.ComplianceAuditResolveFieldAccessAnomalyInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditFieldAccessAnomalyOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Runs access-pattern analysis over the recent window and persists detected anomalies.
   */
  async postAuditFieldLevelAnomaliesAnalyze(
    body: Types.ComplianceAuditAnalyzeFieldAccessPatternsInput,
  ): Promise<Result<Types.ComplianceAuditFieldAccessPatternAnalysisResult[], ApiError>> {
    const url = '/v1/audit/field-level/anomalies/analyze';

    // Validate request body
    const validatedBody = safeParse(Types.ComplianceAuditAnalyzeFieldAccessPatternsInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<Types.ComplianceAuditFieldAccessPatternAnalysisResult[], ApiError>;
  }

  /**
   * Observed bulk exports, optionally only threshold-exceeding ones.
   */
  async getAuditFieldLevelBulkExports(query?: {
    thresholdExceeded?: boolean;
    skip?: number;
    take?: number;
  }): Promise<Result<Types.ComplianceAuditBulkExportEvent[], ApiError>> {
    const url = '/v1/audit/field-level/bulk-exports';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.ComplianceAuditBulkExportEvent[], ApiError>;
  }

  /**
   * Evaluates an export attempt against the bulk-export thresholds, records it and alerts when exceeded.
   */
  async postAuditFieldLevelBulkExportsEvaluate(
    body: Types.ComplianceAuditEvaluateBulkExportInput,
  ): Promise<Result<Types.ComplianceAuditBulkExportEvaluationOutput, ApiError>> {
    const url = '/v1/audit/field-level/bulk-exports/evaluate';

    // Validate request body
    const validatedBody = safeParse(Types.ComplianceAuditEvaluateBulkExportInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditBulkExportEvaluationOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * The sensitive-field classification registry.
   */
  async getAuditFieldLevelClassifications(): Promise<Result<Types.ComplianceAuditSensitiveFieldClassificationOutput[], ApiError>> {
    const url = '/v1/audit/field-level/classifications';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Types.ComplianceAuditSensitiveFieldClassificationOutput[], ApiError>;
  }

  /**
   * Re-scans recent field access observations and upserts automatic classifications.
   */
  async postAuditFieldLevelClassificationsScan(): Promise<Result<Types.ComplianceAuditSensitiveFieldScanOutput, ApiError>> {
    const url = '/v1/audit/field-level/classifications/scan';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditSensitiveFieldScanOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Cross-border transfer log, optionally filtered by validation status.
   */
  async getAuditFieldLevelCrossBorderTransfers(query?: {
    status?: string;
    skip?: number;
    take?: number;
  }): Promise<Result<Types.ComplianceAuditCrossBorderTransferLog[], ApiError>> {
    const url = '/v1/audit/field-level/cross-border-transfers';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.ComplianceAuditCrossBorderTransferLog[], ApiError>;
  }

  /**
   * Validates a prospective cross-border transfer against the jurisdiction policy and logs the outcome.
   */
  async postAuditFieldLevelCrossBorderTransfersValidate(
    body: Types.ComplianceAuditValidateCrossBorderTransferInput,
  ): Promise<Result<Types.ComplianceAuditCrossBorderTransferOutput, ApiError>> {
    const url = '/v1/audit/field-level/cross-border-transfers/validate';

    // Validate request body
    const validatedBody = safeParse(Types.ComplianceAuditValidateCrossBorderTransferInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditCrossBorderTransferOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Data subject rights requests with statutory deadline tracking, optionally filtered by status.
   */
  async getAuditFieldLevelDataSubjectRequests(query?: {
    status?: string;
    skip?: number;
    take?: number;
  }): Promise<Result<Types.ComplianceAuditDataSubjectRequestOutput[], ApiError>> {
    const url = '/v1/audit/field-level/data-subject-requests';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.ComplianceAuditDataSubjectRequestOutput[], ApiError>;
  }

  /**
   * Consent monitoring for one data subject: consent states plus their rights requests.
   */
  async getAuditFieldLevelDataSubjectRequestsSubjects(userId: string): Promise<Result<Types.ComplianceAuditDataSubjectConsentMonitoringOutput, ApiError>> {
    const url = `/v1/audit/field-level/data-subject-requests/subjects/${userId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditDataSubjectConsentMonitoringOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Aggregate monitoring summary: open, fulfilled and overdue requests plus active consents.
   */
  async getAuditFieldLevelDataSubjectRequestsSummary(): Promise<Result<Types.ComplianceAuditDataSubjectAccessMonitoringSummary, ApiError>> {
    const url = '/v1/audit/field-level/data-subject-requests/summary';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditDataSubjectAccessMonitoringSummarySchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * The GDPR Article 30 processing activity register, with completeness gaps listed.
   */
  async getAuditFieldLevelGdprArticle30(): Promise<Result<Types.ComplianceAuditGdprArticle30Output, ApiError>> {
    const url = '/v1/audit/field-level/gdpr/article-30';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditGdprArticle30OutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Creates or updates a processing activity record.
   */
  async putAuditFieldLevelGdprArticle30(
    body: Types.ComplianceAuditSaveGdprActivityInput,
  ): Promise<Result<Types.ComplianceAuditGdprProcessingActivityOutput, ApiError>> {
    const url = '/v1/audit/field-level/gdpr/article-30';

    // Validate request body
    const validatedBody = safeParse(Types.ComplianceAuditSaveGdprActivityInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditGdprProcessingActivityOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Deletes a processing activity record.
   */
  async deleteAuditFieldLevelGdprArticle30(id: string): Promise<Result<void, ApiError>> {
    const url = `/v1/audit/field-level/gdpr/article-30/${id}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Marks a processing activity reviewed by the current actor.
   */
  async postAuditFieldLevelGdprArticle30Review(id: string): Promise<Result<Types.ComplianceAuditGdprProcessingActivityOutput, ApiError>> {
    const url = `/v1/audit/field-level/gdpr/article-30/${id}/review`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditGdprProcessingActivityOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * (Re)derives the platform processing activities for the tenant; manual entries are preserved.
   */
  async postAuditFieldLevelGdprArticle30Seed(): Promise<Result<number, ApiError>> {
    const url = '/v1/audit/field-level/gdpr/article-30/seed';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<number, ApiError>;
  }

  /**
   * Per-category sensitive-data retention rules.
   */
  async getAuditFieldLevelRetention(): Promise<Result<Types.ComplianceAuditSensitiveDataRetentionPolicyOutput[], ApiError>> {
    const url = '/v1/audit/field-level/retention';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Types.ComplianceAuditSensitiveDataRetentionPolicyOutput[], ApiError>;
  }

  /**
   * Configures the retention rule for one sensitive data category.
   */
  async putAuditFieldLevelRetention(
    body: Types.ComplianceAuditConfigureSensitiveDataRetentionInput,
  ): Promise<Result<Types.ComplianceAuditSensitiveDataRetentionPolicyOutput, ApiError>> {
    const url = '/v1/audit/field-level/retention';

    // Validate request body
    const validatedBody = safeParse(Types.ComplianceAuditConfigureSensitiveDataRetentionInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditSensitiveDataRetentionPolicyOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Enforces the sensitive-data retention policies (purge or mask expired records); dry-run reports without changing data.
   */
  async postAuditFieldLevelRetentionEnforce(query?: {
    dryRun?: boolean;
  }): Promise<Result<Types.ComplianceAuditSensitiveDataRetentionEnforcementResult, ApiError>> {
    const url = '/v1/audit/field-level/retention/enforce';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditSensitiveDataRetentionEnforcementResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createComplianceAuditFieldLevelModule(client: ApiClient): ComplianceAuditFieldLevelModule {
  return new ComplianceAuditFieldLevelModule(client);
}
