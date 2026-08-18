/**
 * @game-guild/client - UsersResources Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from "../../runtime/client.js";
import type { Result } from "../../runtime/result/types.js";
import type { ApiError } from "../../runtime/errors/types.js";
import * as Types from "../types.gen.js";
import { safeParse } from "../../runtime/errors/validation.js";

/* eslint-disable @typescript-eslint/no-explicit-any */

export class UsersResourcesModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Get usage records for a user
   *
   * Retrieves resource usage records for a specific user with optional filtering by type and date range.
   */
  async getUsersResourcesUsageRecords(
    userId: string,
    query?: {
      usageType?: Types.ResourcesResourceUsageType;
      startDate?: string;
      endDate?: string;
    },
  ): Promise<Result<Array<Types.ResourcesUsageRecord>, ApiError>> {
    const url = `/v1/users/${userId}/resources/usage-records`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.ResourcesUsageRecord>, ApiError>;
  }

  /**
   * Get current usage summary for a user
   *
   * Retrieves the current aggregated resource usage summary for a specific user.
   */
  async getUsersResourcesUsageSummary(
    userId: string,
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
        Teams?: number;
      },
      ApiError
    >
  > {
    const url = `/v1/users/${userId}/resources/usage-summary`;

    const result = await this.client.request({
      method: "GET",
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
        Teams?: number;
      },
      ApiError
    >;
  }

  /**
   * Check resource limits for a user
   *
   * Checks current resource usage against configured limits for a specific user.
   */
  async getUsersResourcesLimits(
    userId: string,
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
        Teams?: boolean;
      },
      ApiError
    >
  > {
    const url = `/v1/users/${userId}/resources/limits`;

    const result = await this.client.request({
      method: "GET",
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
        Teams?: boolean;
      },
      ApiError
    >;
  }

  /**
   * Record resource usage for a user
   *
   * Records a new resource usage entry for the specified user.
   */
  async postUsersResourcesRecord(
    userId: string,
    body: Types.ResourcesRecordUserResourceUsageInput,
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/users/${userId}/resources:record`;

    // Validate request body
    const validatedBody = safeParse(
      Types.ResourcesRecordUserResourceUsageInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Record resource usage with quota enforcement for a user
   *
   * Records a new resource usage entry after verifying it doesn't exceed configured quotas. Returns 429 if quota would be exceeded.
   */
  async postUsersResourcesRecordWithQuotaCheck(
    userId: string,
    body: Types.ResourcesRecordUserResourceUsageInput,
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/users/${userId}/resources:record-with-quota-check`;

    // Validate request body
    const validatedBody = safeParse(
      Types.ResourcesRecordUserResourceUsageInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Reset resource usage for a user
   *
   * Resets the resource usage counters for a specific user and resource type to zero.
   */
  async postUsersResourcesReset(
    userId: string,
    query?: { usageType?: Types.ResourcesResourceUsageType },
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/users/${userId}/resources:reset`;

    const result = await this.client.request({
      method: "POST",
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createUsersResourcesModule(
  client: ApiClient,
): UsersResourcesModule {
  return new UsersResourcesModule(client);
}
