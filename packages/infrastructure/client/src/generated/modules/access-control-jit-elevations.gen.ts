/**
 * @game-guild/client - AccessControlJitElevations Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from "../../runtime/client.js";
import type { Result } from "../../runtime/result/types.js";
import type { ApiError } from "../../runtime/errors/types.js";
import * as Types from "../types.gen.js";
import { safeParse } from "../../runtime/errors/validation.js";

/* eslint-disable @typescript-eslint/no-explicit-any */

export class AccessControlJitElevationsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async postJitElevations(
    body: Types.IdentityAuthorizationCommandsRequestJitElevationCommand,
  ): Promise<Result<Types.IdentityAuthorizationJitElevationInput, ApiError>> {
    const url = "/v1/jit-elevations";

    // Validate request body
    const validatedBody = safeParse(
      Types.IdentityAuthorizationCommandsRequestJitElevationCommandSchema,
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
        Types.IdentityAuthorizationJitElevationInputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postJitElevationsApprove(
    id: string,
    body: Types.IdentityAuthorizationControllersApproveElevationInput,
  ): Promise<Result<Types.IdentityAuthorizationJitElevationInput, ApiError>> {
    const url = `/v1/jit-elevations/${id}:approve`;

    // Validate request body
    const validatedBody = safeParse(
      Types.IdentityAuthorizationControllersApproveElevationInputSchema,
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
        Types.IdentityAuthorizationJitElevationInputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postJitElevationsDeny(
    id: string,
    body: Types.IdentityAuthorizationControllersDenyElevationInput,
  ): Promise<Result<Types.IdentityAuthorizationJitElevationInput, ApiError>> {
    const url = `/v1/jit-elevations/${id}:deny`;

    // Validate request body
    const validatedBody = safeParse(
      Types.IdentityAuthorizationControllersDenyElevationInputSchema,
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
        Types.IdentityAuthorizationJitElevationInputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postJitElevationsRevoke(
    id: string,
    body: Types.IdentityAuthorizationControllersRevokeElevationInput,
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/jit-elevations/${id}:revoke`;

    // Validate request body
    const validatedBody = safeParse(
      Types.IdentityAuthorizationControllersRevokeElevationInputSchema,
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
  async getJitElevations(
    id: string,
  ): Promise<Result<Types.IdentityAuthorizationJitElevationInput, ApiError>> {
    const url = `/v1/jit-elevations/${id}`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.IdentityAuthorizationJitElevationInputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getJitElevationsPending(query?: {
    tenantId?: string;
  }): Promise<
    Result<Array<Types.IdentityAuthorizationJitElevationInput>, ApiError>
  > {
    const url = "/v1/jit-elevations/pending";

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<
      Array<Types.IdentityAuthorizationJitElevationInput>,
      ApiError
    >;
  }

  /**
   */
  async getJitElevationsUser(
    userId: string,
    query?: { tenantId?: string },
  ): Promise<
    Result<Array<Types.IdentityAuthorizationJitElevationInput>, ApiError>
  > {
    const url = `/v1/jit-elevations/user/${userId}`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<
      Array<Types.IdentityAuthorizationJitElevationInput>,
      ApiError
    >;
  }

  /**
   */
  async getJitElevationsUserActive(
    userId: string,
    query?: { tenantId?: string },
  ): Promise<
    Result<Array<Types.IdentityAuthorizationJitElevationInput>, ApiError>
  > {
    const url = `/v1/jit-elevations/user/${userId}/active`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<
      Array<Types.IdentityAuthorizationJitElevationInput>,
      ApiError
    >;
  }

  /**
   */
  async getJitElevationsUserCheck(
    userId: string,
    query?: { permission?: string; tenantId?: string; resourceId?: string },
  ): Promise<Result<boolean, ApiError>> {
    const url = `/v1/jit-elevations/user/${userId}/check`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<boolean, ApiError>;
  }

  /**
   */
  async postJitElevationsCleanup(): Promise<Result<number, ApiError>> {
    const url = "/v1/jit-elevations/:cleanup";

    const result = await this.client.request({
      method: "POST",
      path: url,
      requiresAuth: true,
    });

    return result as Result<number, ApiError>;
  }
}

export function createAccessControlJitElevationsModule(
  client: ApiClient,
): AccessControlJitElevationsModule {
  return new AccessControlJitElevationsModule(client);
}
