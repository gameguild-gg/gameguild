/**
 * @game-guild/client - EconomyAdRewards Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from "../../runtime/client.js";
import type { Result } from "../../runtime/result/types.js";
import type { ApiError } from "../../runtime/errors/types.js";
import * as Types from "../types.gen.js";
import { safeParse } from "../../runtime/errors/validation.js";

/* eslint-disable @typescript-eslint/no-explicit-any */

export class EconomyAdRewardsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async postEconomyAdRewardsSessions(
    body: Types.APIControllersStartMyAdRewardSessionInput,
  ): Promise<
    Result<Types.EconomyAdRewardsDurableAdRewardSessionResult, ApiError>
  > {
    const url = "/api/v1/economy/ad-rewards/sessions";

    // Validate request body
    const validatedBody = safeParse(
      Types.APIControllersStartMyAdRewardSessionInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.EconomyAdRewardsDurableAdRewardSessionResultSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getEconomyAdRewardsSessions(
    sessionId: string,
  ): Promise<
    Result<Types.EconomyAdRewardsDurableAdRewardSessionStatus, ApiError>
  > {
    const url = `/api/v1/economy/ad-rewards/sessions/${sessionId}`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.EconomyAdRewardsDurableAdRewardSessionStatusSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postEconomyAdRewardsSessionsComplete(
    sessionId: string,
    body: Types.APIControllersCompleteMyAdRewardSessionInput,
  ): Promise<
    Result<Types.EconomyAdRewardsDurableAdRewardCompletionResult, ApiError>
  > {
    const url = `/api/v1/economy/ad-rewards/sessions/${sessionId}/complete`;

    // Validate request body
    const validatedBody = safeParse(
      Types.APIControllersCompleteMyAdRewardSessionInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.EconomyAdRewardsDurableAdRewardCompletionResultSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postEconomyAdRewardsSessionsConfirmDeferred(
    sessionId: string,
    body: Types.APIControllersConfirmMyDeferredAdRewardInput,
  ): Promise<
    Result<Types.EconomyAdRewardsDurableAdRewardCompletionResult, ApiError>
  > {
    const url = `/api/v1/economy/ad-rewards/sessions/${sessionId}/confirm-deferred`;

    // Validate request body
    const validatedBody = safeParse(
      Types.APIControllersConfirmMyDeferredAdRewardInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.EconomyAdRewardsDurableAdRewardCompletionResultSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createEconomyAdRewardsModule(
  client: ApiClient,
): EconomyAdRewardsModule {
  return new EconomyAdRewardsModule(client);
}
