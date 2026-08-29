/**
 * @game-guild/client - ComplianceAudit Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class ComplianceAuditModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getAdminAuditLogs(query?: {
    UserId?: string;
    TenantId?: string;
    ActionType?: string;
    ResourceType?: string;
    Category?: Types.ComplianceAuditAuditCategory;
    RiskLevel?: Types.ComplianceAuditAuditRiskLevel;
    Success?: boolean;
    StartDate?: string;
    EndDate?: string;
    IpAddress?: string;
    Skip?: number;
    Take?: number;
  }): Promise<Result<Types.ComplianceAuditAuditLogOutput, ApiError>> {
    const url = '/v1/admin/audit-logs';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditAuditLogOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAdminAuditLogsExport(body: Types.ComplianceAuditAuditExportInput): Promise<Result<void, ApiError>> {
    const url = '/v1/admin/audit-logs/:export';

    // Validate request body
    const validatedBody = safeParse(Types.ComplianceAuditAuditExportInputSchema, body, 'request');

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
  async getAdminAuditLogsStatistics(query?: { StartDate?: string; EndDate?: string }): Promise<Result<Types.ComplianceAuditAuditStatisticsOutput, ApiError>> {
    const url = '/v1/admin/audit-logs/statistics';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditAuditStatisticsOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createComplianceAuditModule(client: ApiClient): ComplianceAuditModule {
  return new ComplianceAuditModule(client);
}
