/**
 * @game-guild/client - UsersMemberships Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from "../../runtime/client.js";
import type { Result } from "../../runtime/result/types.js";
import type { ApiError } from "../../runtime/errors/types.js";
import * as Types from "../types.gen.js";
import { safeParse } from "../../runtime/errors/validation.js";

/* eslint-disable @typescript-eslint/no-explicit-any */

export class UsersMembershipsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Get all tenant memberships for a user
   *
   * Returns all tenants the user belongs to, with role and membership status. Similar to Discord's 'My Servers' view.
   */
  async getUsersMemberships(
    userId: string,
    query?: { includeInactive?: boolean },
  ): Promise<Result<Types.IdentityTenantsGetUserMembershipsOutput, ApiError>> {
    const url = `/v1/users/${userId}/memberships`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.IdentityTenantsGetUserMembershipsOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Add a tenant membership for a user
   *
   * Adds the specified user to a tenant with the requested role so the user can access that workspace.
   */
  async postUsersMemberships(
    userId: string,
    body: Types.IdentityTenantsAddUserMembershipInput,
  ): Promise<Result<Types.IdentityTenantsAddTenantMemberOutput, ApiError>> {
    const url = `/v1/users/${userId}/memberships`;

    // Validate request body
    const validatedBody = safeParse(
      Types.IdentityTenantsAddUserMembershipInputSchema,
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
        Types.IdentityTenantsAddTenantMemberOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Check if user has any tenant memberships
   */
  async headUsersMemberships(userId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/users/${userId}/memberships`;

    const result = await this.client.request({
      method: "HEAD",
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Accept tenant membership invite
   */
  async postUsersMembershipsInviteAccept(
    userId: string,
    tenantId: string,
    body: Types.IdentityTenantsUpdateUserMembershipInviteInput,
  ): Promise<
    Result<Types.IdentityTenantsUpdateTenantMemberInviteOutput, ApiError>
  > {
    const url = `/v1/users/${userId}/memberships/${tenantId}/invite:accept`;

    // Validate request body
    const validatedBody = safeParse(
      Types.IdentityTenantsUpdateUserMembershipInviteInputSchema,
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
        Types.IdentityTenantsUpdateTenantMemberInviteOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Cancel tenant membership invite
   */
  async postUsersMembershipsInviteCancel(
    userId: string,
    tenantId: string,
    body: Types.IdentityTenantsUpdateUserMembershipInviteInput,
  ): Promise<
    Result<Types.IdentityTenantsUpdateTenantMemberInviteOutput, ApiError>
  > {
    const url = `/v1/users/${userId}/memberships/${tenantId}/invite:cancel`;

    // Validate request body
    const validatedBody = safeParse(
      Types.IdentityTenantsUpdateUserMembershipInviteInputSchema,
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
        Types.IdentityTenantsUpdateTenantMemberInviteOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Resend tenant membership invite
   */
  async postUsersMembershipsInviteResend(
    userId: string,
    tenantId: string,
    body: Types.IdentityTenantsUpdateUserMembershipInviteInput,
  ): Promise<
    Result<Types.IdentityTenantsUpdateTenantMemberInviteOutput, ApiError>
  > {
    const url = `/v1/users/${userId}/memberships/${tenantId}/invite:resend`;

    // Validate request body
    const validatedBody = safeParse(
      Types.IdentityTenantsUpdateUserMembershipInviteInputSchema,
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
        Types.IdentityTenantsUpdateTenantMemberInviteOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Update tenant membership role
   *
   * Updates the user's role in the specified tenant/workspace. Use this for console promotion/demotion flows.
   */
  async patchUsersMembershipsRole(
    userId: string,
    tenantId: string,
    body: Types.IdentityTenantsUpdateUserMembershipRoleInput,
  ): Promise<
    Result<Types.IdentityTenantsUpdateTenantMemberRoleOutput, ApiError>
  > {
    const url = `/v1/users/${userId}/memberships/${tenantId}/role`;

    // Validate request body
    const validatedBody = safeParse(
      Types.IdentityTenantsUpdateUserMembershipRoleInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "PATCH",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.IdentityTenantsUpdateTenantMemberRoleOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Get count of user's active tenant memberships
   */
  async getUsersMembershipsCount(
    userId: string,
  ): Promise<Result<Types.IdentityTenantsMembershipCountOutput, ApiError>> {
    const url = `/v1/users/${userId}/memberships:count`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.IdentityTenantsMembershipCountOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createUsersMembershipsModule(
  client: ApiClient,
): UsersMembershipsModule {
  return new UsersMembershipsModule(client);
}
