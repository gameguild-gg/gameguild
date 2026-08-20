/**
 * @game-guild/client - AccessControlTenantPermissions Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from "../../runtime/client.js";
import type { Result } from "../../runtime/result/types.js";
import type { ApiError } from "../../runtime/errors/types.js";
import * as Types from "../types.gen.js";
import { safeParse } from "../../runtime/errors/validation.js";

/* eslint-disable @typescript-eslint/no-explicit-any */

export class AccessControlTenantPermissionsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getAuthorizationTenantsHasPermission(
    tenantId: string,
    query?: { permission?: string; userId?: string },
  ): Promise<Result<boolean, ApiError>> {
    const url = `/api/v1/authorization/tenants/${tenantId}/has-permission`;

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
  async getAuthorizationTenantsPermissions(
    tenantId: string,
    query?: { userId?: string; includeEffective?: boolean },
  ): Promise<
    Result<Types.IdentityAuthorizationGetTenantPermissionsOutput, ApiError>
  > {
    const url = `/api/v1/authorization/tenants/${tenantId}/permissions`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.IdentityAuthorizationGetTenantPermissionsOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAuthorizationTenantsDefaults(
    body: Types.IdentityAuthorizationSetTenantDefaultPermissionsCommand,
  ): Promise<Result<boolean, ApiError>> {
    const url = "/api/v1/authorization/tenants/defaults";

    // Validate request body
    const validatedBody = safeParse(
      Types.IdentityAuthorizationSetTenantDefaultPermissionsCommandSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<boolean, ApiError>;
  }

  /**
   */
  async postAuthorizationTenantsDeny(
    body: Types.IdentityAuthorizationDenyTenantPermissionCommand,
  ): Promise<Result<string, ApiError>> {
    const url = "/api/v1/authorization/tenants/deny";

    // Validate request body
    const validatedBody = safeParse(
      Types.IdentityAuthorizationDenyTenantPermissionCommandSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<string, ApiError>;
  }

  /**
   */
  async postAuthorizationTenantsDenyRemove(
    body: Types.IdentityAuthorizationRemoveDenyPermissionsCommand,
  ): Promise<Result<boolean, ApiError>> {
    const url = "/api/v1/authorization/tenants/deny/remove";

    // Validate request body
    const validatedBody = safeParse(
      Types.IdentityAuthorizationRemoveDenyPermissionsCommandSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<boolean, ApiError>;
  }

  /**
   */
  async postAuthorizationTenantsGlobalDefaults(
    body: Types.IdentityAuthorizationSetGlobalDefaultPermissionsCommand,
  ): Promise<Result<boolean, ApiError>> {
    const url = "/api/v1/authorization/tenants/global/defaults";

    // Validate request body
    const validatedBody = safeParse(
      Types.IdentityAuthorizationSetGlobalDefaultPermissionsCommandSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<boolean, ApiError>;
  }

  /**
   */
  async postAuthorizationTenantsGrant(
    body: Types.IdentityAuthorizationGrantTenantPermissionCommand,
  ): Promise<Result<string, ApiError>> {
    const url = "/api/v1/authorization/tenants/grant";

    // Validate request body
    const validatedBody = safeParse(
      Types.IdentityAuthorizationGrantTenantPermissionCommandSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<string, ApiError>;
  }

  /**
   */
  async postAuthorizationTenantsRevoke(
    body: Types.IdentityAuthorizationRevokeTenantPermissionCommand,
  ): Promise<Result<boolean, ApiError>> {
    const url = "/api/v1/authorization/tenants/revoke";

    // Validate request body
    const validatedBody = safeParse(
      Types.IdentityAuthorizationRevokeTenantPermissionCommandSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<boolean, ApiError>;
  }
}

export function createAccessControlTenantPermissionsModule(
  client: ApiClient,
): AccessControlTenantPermissionsModule {
  return new AccessControlTenantPermissionsModule(client);
}
