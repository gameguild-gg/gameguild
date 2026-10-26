/**
 * @game-guild/client - ComplianceAuditEntityChanges Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class ComplianceAuditEntityChangesModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Returns the universal entity-change audit history for the current tenant. Tenant
   * administrators only; supports filtering by entity name, change kind, and actor.
   */
  async getApiAuditEntityChanges(query?: {
    EntityName?: string;
    ChangeKind?: string;
    ActorId?: string;
    Skip?: number;
    Take?: number;
  }): Promise<Result<Types.ComplianceAuditEntityChangeAuditListOutput, ApiError>> {
    const url = '/api/audit/entity-changes';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditEntityChangeAuditListOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Returns the universal entity-change audit history for the current tenant. Tenant
   * administrators only; supports filtering by entity name, change kind, and actor.
   */
  async getAuditEntityChanges(query?: {
    EntityName?: string;
    ChangeKind?: string;
    ActorId?: string;
    Skip?: number;
    Take?: number;
  }): Promise<Result<Types.ComplianceAuditEntityChangeAuditListOutput, ApiError>> {
    const url = '/v1/audit/entity-changes';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditEntityChangeAuditListOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createComplianceAuditEntityChangesModule(client: ApiClient): ComplianceAuditEntityChangesModule {
  return new ComplianceAuditEntityChangesModule(client);
}
