/**
 * @game-guild/client - AccessControlResourcePermissions Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from "../../runtime/client.js";
import type { Result } from "../../runtime/result/types.js";
import type { ApiError } from "../../runtime/errors/types.js";
import * as Types from "../types.gen.js";
import { safeParse } from "../../runtime/errors/validation.js";

/* eslint-disable @typescript-eslint/no-explicit-any */

export class AccessControlResourcePermissionsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getAuthorizationResourcesInvitations(
    invitationId: string,
  ): Promise<
    Result<Types.IdentityAuthorizationGetResourceInvitationOutput, ApiError>
  > {
    const url = `/api/v1/authorization/resources/invitations/${invitationId}`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.IdentityAuthorizationGetResourceInvitationOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteAuthorizationResourcesInvitations(
    invitationId: string,
  ): Promise<
    Result<Types.IdentityAuthorizationInvitationActionResult, ApiError>
  > {
    const url = `/api/v1/authorization/resources/invitations/${invitationId}`;

    const result = await this.client.request({
      method: "DELETE",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.IdentityAuthorizationInvitationActionResultSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getAuthorizationResourcesInvitationsPending(): Promise<
    Result<
      Types.IdentityAuthorizationGetPendingResourceInvitationsOutput,
      ApiError
    >
  > {
    const url = "/api/v1/authorization/resources/invitations/pending";

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.IdentityAuthorizationGetPendingResourceInvitationsOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAuthorizationResourcesInvitationsAccept(
    invitationId: string,
  ): Promise<
    Result<Types.IdentityAuthorizationInvitationActionResult, ApiError>
  > {
    const url = `/api/v1/authorization/resources/invitations/${invitationId}/accept`;

    const result = await this.client.request({
      method: "POST",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.IdentityAuthorizationInvitationActionResultSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAuthorizationResourcesInvitationsDecline(
    invitationId: string,
    body: Types.IdentityAuthorizationDeclineInvitationInput,
  ): Promise<
    Result<Types.IdentityAuthorizationInvitationActionResult, ApiError>
  > {
    const url = `/api/v1/authorization/resources/invitations/${invitationId}/decline`;

    // Validate request body
    const validatedBody = safeParse(
      Types.IdentityAuthorizationDeclineInvitationInputSchema,
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
        Types.IdentityAuthorizationInvitationActionResultSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAuthorizationResourcesShare(
    body: Types.IdentityAuthorizationShareResourceCommand,
  ): Promise<Result<Types.IdentityAuthorizationShareResult, ApiError>> {
    const url = "/api/v1/authorization/resources/share";

    // Validate request body
    const validatedBody = safeParse(
      Types.IdentityAuthorizationShareResourceCommandSchema,
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
        Types.IdentityAuthorizationShareResultSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putAuthorizationResourcesUsersPermissions(
    body: Types.IdentityAuthorizationUpdateUserPermissionsCommand,
  ): Promise<
    Result<Types.IdentityAuthorizationPermissionUpdateResult, ApiError>
  > {
    const url = "/api/v1/authorization/resources/users/permissions";

    // Validate request body
    const validatedBody = safeParse(
      Types.IdentityAuthorizationUpdateUserPermissionsCommandSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "PUT",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.IdentityAuthorizationPermissionUpdateResultSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteAuthorizationResourcesUsersAccess(
    body: Types.IdentityAuthorizationRemoveUserAccessCommand,
  ): Promise<
    Result<Types.IdentityAuthorizationPermissionUpdateResult, ApiError>
  > {
    const url = "/api/v1/authorization/resources/users/access";

    // Validate request body
    const validatedBody = safeParse(
      Types.IdentityAuthorizationRemoveUserAccessCommandSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "DELETE",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.IdentityAuthorizationPermissionUpdateResultSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getAuthorizationResourcesPermissions(
    resourceType: string,
    resourceId: string,
    query?: { tenantId?: string; userId?: string },
  ): Promise<
    Result<Types.IdentityAuthorizationEffectivePermissionsOutput, ApiError>
  > {
    const url = `/api/v1/authorization/resources/${resourceType}/${resourceId}/permissions`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.IdentityAuthorizationEffectivePermissionsOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getAuthorizationResourcesHasPermission(
    resourceType: string,
    resourceId: string,
    query?: { tenantId?: string; permission?: string; userId?: string },
  ): Promise<Result<Types.IdentityAuthorizationHasPermissionOutput, ApiError>> {
    const url = `/api/v1/authorization/resources/${resourceType}/${resourceId}/has-permission`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.IdentityAuthorizationHasPermissionOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getAuthorizationResourcesUsers(
    resourceType: string,
    resourceId: string,
    query?: {
      tenantId?: string;
      includeInherited?: boolean;
      includeExpired?: boolean;
    },
  ): Promise<
    Result<Types.IdentityAuthorizationGetResourceUsersOutput, ApiError>
  > {
    const url = `/api/v1/authorization/resources/${resourceType}/${resourceId}/users`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.IdentityAuthorizationGetResourceUsersOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createAccessControlResourcePermissionsModule(
  client: ApiClient,
): AccessControlResourcePermissionsModule {
  return new AccessControlResourcePermissionsModule(client);
}
