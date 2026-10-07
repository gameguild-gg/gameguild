/**
 * @game-guild/client - ComplianceAuditRetentionSimulation Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class ComplianceAuditRetentionSimulationModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getApiAuditRetentionSimulationForGetApiAuditRetentionSimulation(query?: {
    skip?: number;
    take?: number;
  }): Promise<Result<Types.ComplianceAuditAuditRetentionSimulationSummary[], ApiError>> {
    const url = '/api/audit/retention-simulation';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.ComplianceAuditAuditRetentionSimulationSummary[], ApiError>;
  }

  /**
   */
  async postApiAuditRetentionSimulation(
    body: Types.ComplianceAuditRunAuditRetentionSimulationInput,
  ): Promise<Result<Types.ComplianceAuditAuditRetentionSimulationOutput, ApiError>> {
    const url = '/api/audit/retention-simulation';

    // Validate request body
    const validatedBody = safeParse(Types.ComplianceAuditRunAuditRetentionSimulationInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditAuditRetentionSimulationOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getApiAuditRetentionSimulationForGetApiAuditRetentionSimulationById(
    id: string,
  ): Promise<Result<Types.ComplianceAuditAuditRetentionSimulationOutput, ApiError>> {
    const url = `/api/audit/retention-simulation/${id}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditAuditRetentionSimulationOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getApiAuditRetentionSimulationConfiguration(): Promise<Result<Types.ComplianceAuditAuditRetentionConfigurationOutput, ApiError>> {
    const url = '/api/audit/retention-simulation/configuration';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditAuditRetentionConfigurationOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putApiAuditRetentionSimulationConfiguration(
    body: Types.ComplianceAuditConfigureAuditRetentionInput,
  ): Promise<Result<Types.ComplianceAuditAuditRetentionConfigurationOutput, ApiError>> {
    const url = '/api/audit/retention-simulation/configuration';

    // Validate request body
    const validatedBody = safeParse(Types.ComplianceAuditConfigureAuditRetentionInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditAuditRetentionConfigurationOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getAuditRetentionSimulationForGetAuditRetentionSimulation(query?: {
    skip?: number;
    take?: number;
  }): Promise<Result<Types.ComplianceAuditAuditRetentionSimulationSummary[], ApiError>> {
    const url = '/v1/audit/retention-simulation';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.ComplianceAuditAuditRetentionSimulationSummary[], ApiError>;
  }

  /**
   */
  async postAuditRetentionSimulation(
    body: Types.ComplianceAuditRunAuditRetentionSimulationInput,
  ): Promise<Result<Types.ComplianceAuditAuditRetentionSimulationOutput, ApiError>> {
    const url = '/v1/audit/retention-simulation';

    // Validate request body
    const validatedBody = safeParse(Types.ComplianceAuditRunAuditRetentionSimulationInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditAuditRetentionSimulationOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getAuditRetentionSimulationForGetAuditRetentionSimulationById(
    id: string,
  ): Promise<Result<Types.ComplianceAuditAuditRetentionSimulationOutput, ApiError>> {
    const url = `/v1/audit/retention-simulation/${id}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditAuditRetentionSimulationOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getAuditRetentionSimulationConfiguration(): Promise<Result<Types.ComplianceAuditAuditRetentionConfigurationOutput, ApiError>> {
    const url = '/v1/audit/retention-simulation/configuration';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditAuditRetentionConfigurationOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putAuditRetentionSimulationConfiguration(
    body: Types.ComplianceAuditConfigureAuditRetentionInput,
  ): Promise<Result<Types.ComplianceAuditAuditRetentionConfigurationOutput, ApiError>> {
    const url = '/v1/audit/retention-simulation/configuration';

    // Validate request body
    const validatedBody = safeParse(Types.ComplianceAuditConfigureAuditRetentionInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceAuditAuditRetentionConfigurationOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createComplianceAuditRetentionSimulationModule(client: ApiClient): ComplianceAuditRetentionSimulationModule {
  return new ComplianceAuditRetentionSimulationModule(client);
}
