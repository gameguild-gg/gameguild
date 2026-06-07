/**
 * @game-guild/client - TenantsResources Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class TenantsResourcesModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Get usage records for a tenant
   *
   * Retrieves paginated resource usage records for a specific tenant with optional filtering by type and date range.
   */
  async getTenantsResourcesUsageRecords(
    tenantId: string,
    query?: { usageType?: Types.ResourcesResourceUsageType; startDate?: string; endDate?: string; pageNumber?: number; pageSize?: number },
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/tenants/${tenantId}/resources/usage-records`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Get current usage summary for a tenant
   *
   * Retrieves the current aggregated resource usage summary for a specific tenant.
   */
  async getTenantsResourcesUsageSummary(
    tenantId: string,
  ): Promise<
    Result<
      {
        Users?: number;
        Projects?: number;
        Storage?: number;
        ApiCalls?: number;
        Programs?: number;
        Courses?: number;
        FeatureFlags?: number;
        SubscriptionPlans?: number;
        Products?: number;
        TestingSessions?: number;
        Roles?: number;
        Tenants?: number;
        Subscriptions?: number;
        SLOs?: number;
        AccessReviewCampaigns?: number;
        SoDRules?: number;
        AbacPolicies?: number;
        ConditionalPolicies?: number;
        Wallets?: number;
        Disputes?: number;
        PromoCodes?: number;
        Orders?: number;
        AuditEntries?: number;
        Assets?: number;
        AssetStorage?: number;
        AssetDownloads?: number;
        AssetTransformations?: number;
        AiRequests?: number;
        AiTokens?: number;
      },
      ApiError
    >
  > {
    const url = `/v1/tenants/${tenantId}/resources/usage-summary`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<
      {
        Users?: number;
        Projects?: number;
        Storage?: number;
        ApiCalls?: number;
        Programs?: number;
        Courses?: number;
        FeatureFlags?: number;
        SubscriptionPlans?: number;
        Products?: number;
        TestingSessions?: number;
        Roles?: number;
        Tenants?: number;
        Subscriptions?: number;
        SLOs?: number;
        AccessReviewCampaigns?: number;
        SoDRules?: number;
        AbacPolicies?: number;
        ConditionalPolicies?: number;
        Wallets?: number;
        Disputes?: number;
        PromoCodes?: number;
        Orders?: number;
        AuditEntries?: number;
        Assets?: number;
        AssetStorage?: number;
        AssetDownloads?: number;
        AssetTransformations?: number;
        AiRequests?: number;
        AiTokens?: number;
      },
      ApiError
    >;
  }

  /**
   * Check resource limits for a tenant
   *
   * Checks current resource usage against configured limits for a specific tenant.
   */
  async getTenantsResourcesLimits(
    tenantId: string,
    query?: { usageType?: Types.ResourcesResourceUsageType },
  ): Promise<
    Result<
      {
        Users?: boolean;
        Projects?: boolean;
        Storage?: boolean;
        ApiCalls?: boolean;
        Programs?: boolean;
        Courses?: boolean;
        FeatureFlags?: boolean;
        SubscriptionPlans?: boolean;
        Products?: boolean;
        TestingSessions?: boolean;
        Roles?: boolean;
        Tenants?: boolean;
        Subscriptions?: boolean;
        SLOs?: boolean;
        AccessReviewCampaigns?: boolean;
        SoDRules?: boolean;
        AbacPolicies?: boolean;
        ConditionalPolicies?: boolean;
        Wallets?: boolean;
        Disputes?: boolean;
        PromoCodes?: boolean;
        Orders?: boolean;
        AuditEntries?: boolean;
        Assets?: boolean;
        AssetStorage?: boolean;
        AssetDownloads?: boolean;
        AssetTransformations?: boolean;
        AiRequests?: boolean;
        AiTokens?: boolean;
      },
      ApiError
    >
  > {
    const url = `/v1/tenants/${tenantId}/resources/limits`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<
      {
        Users?: boolean;
        Projects?: boolean;
        Storage?: boolean;
        ApiCalls?: boolean;
        Programs?: boolean;
        Courses?: boolean;
        FeatureFlags?: boolean;
        SubscriptionPlans?: boolean;
        Products?: boolean;
        TestingSessions?: boolean;
        Roles?: boolean;
        Tenants?: boolean;
        Subscriptions?: boolean;
        SLOs?: boolean;
        AccessReviewCampaigns?: boolean;
        SoDRules?: boolean;
        AbacPolicies?: boolean;
        ConditionalPolicies?: boolean;
        Wallets?: boolean;
        Disputes?: boolean;
        PromoCodes?: boolean;
        Orders?: boolean;
        AuditEntries?: boolean;
        Assets?: boolean;
        AssetStorage?: boolean;
        AssetDownloads?: boolean;
        AssetTransformations?: boolean;
        AiRequests?: boolean;
        AiTokens?: boolean;
      },
      ApiError
    >;
  }

  /**
   * Record resource usage for a tenant
   *
   * Records a new resource usage entry for the specified tenant.
   */
  async postTenantsResourcesRecord(tenantId: string, body: Types.ResourcesRecordTenantResourceUsageInput): Promise<Result<void, ApiError>> {
    const url = `/v1/tenants/${tenantId}/resources:record`;

    // Validate request body
    const validatedBody = safeParse(Types.ResourcesRecordTenantResourceUsageInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Record resource usage with quota enforcement for a tenant
   *
   * Records a new resource usage entry after verifying it doesn't exceed configured quotas. Returns 429 if quota would be exceeded.
   */
  async postTenantsResourcesRecordWithQuotaCheck(tenantId: string, body: Types.ResourcesRecordTenantResourceUsageInput): Promise<Result<void, ApiError>> {
    const url = `/v1/tenants/${tenantId}/resources:record-with-quota-check`;

    // Validate request body
    const validatedBody = safeParse(Types.ResourcesRecordTenantResourceUsageInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Reset resource usage for a tenant
   *
   * Resets the resource usage counters for a specific tenant and resource type to zero.
   */
  async postTenantsResourcesReset(tenantId: string, query?: { usageType?: Types.ResourcesResourceUsageType }): Promise<Result<void, ApiError>> {
    const url = `/v1/tenants/${tenantId}/resources:reset`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createTenantsResourcesModule(client: ApiClient): TenantsResourcesModule {
  return new TenantsResourcesModule(client);
}
