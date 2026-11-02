/**
 * @game-guild/client - ComplianceAuditCoverage Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class ComplianceAuditCoverageModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Per-family audit trail coverage with evidence; complete when every family reports coverage.
   */
  async getApiAuditComprehensiveCoverage(): Promise<Result<Types.ComplianceAuditComprehensiveAuditTrailCoverageReport, ApiError>> {
    const url = '/api/audit/comprehensive-coverage';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditComprehensiveAuditTrailCoverageReportSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Per-module event coverage manifest; complete when every mutating command carries a contract and a handler.
   */
  async getApiAuditEventsComprehensive(): Promise<Result<Types.ComplianceAuditComprehensiveEventCoverageReport, ApiError>> {
    const url = '/api/audit/events/comprehensive';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditComprehensiveEventCoverageReportSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Per-family audit trail coverage with evidence; complete when every family reports coverage.
   */
  async getAuditComprehensiveCoverage(): Promise<Result<Types.ComplianceAuditComprehensiveAuditTrailCoverageReport, ApiError>> {
    const url = '/v1/audit/comprehensive-coverage';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditComprehensiveAuditTrailCoverageReportSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Per-module event coverage manifest; complete when every mutating command carries a contract and a handler.
   */
  async getAuditEventsComprehensive(): Promise<Result<Types.ComplianceAuditComprehensiveEventCoverageReport, ApiError>> {
    const url = '/v1/audit/events/comprehensive';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditComprehensiveEventCoverageReportSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createComplianceAuditCoverageModule(client: ApiClient): ComplianceAuditCoverageModule {
  return new ComplianceAuditCoverageModule(client);
}
