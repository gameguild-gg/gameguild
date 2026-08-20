/**
 * @game-guild/client - FeaturesFlags Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from "../../runtime/client.js";
import type { Result } from "../../runtime/result/types.js";
import type { ApiError } from "../../runtime/errors/types.js";
import * as Types from "../types.gen.js";
import { safeParse } from "../../runtime/errors/validation.js";

/* eslint-disable @typescript-eslint/no-explicit-any */

export class FeaturesFlagsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async postFeaturesEvaluate(
    body: Types.FeaturesFeatureEvaluationInput,
  ): Promise<Result<void, ApiError>> {
    const url = "/v1/features/:evaluate";

    // Validate request body
    const validatedBody = safeParse(
      Types.FeaturesFeatureEvaluationInputSchema,
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
   */
  async postFeaturesEvaluateBulk(
    body: Types.FeaturesBulkEvaluationInput,
  ): Promise<Result<void, ApiError>> {
    const url = "/v1/features/:evaluate-bulk";

    // Validate request body
    const validatedBody = safeParse(
      Types.FeaturesBulkEvaluationInputSchema,
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
   */
  async getFeaturesValue(
    key: string,
    query?: {
      defaultValue?: boolean;
      userId?: string;
      tenantId?: string;
      environment?: string;
    },
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/features/${key}/value`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getFeaturesEnabled(query?: {
    userId?: string;
    tenantId?: string;
    environment?: string;
  }): Promise<Result<void, ApiError>> {
    const url = "/v1/features/enabled";

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createFeaturesFlagsModule(
  client: ApiClient,
): FeaturesFlagsModule {
  return new FeaturesFlagsModule(client);
}
