/**
 * @game-guild/client - AuthRoles Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class AuthRolesModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getRolesForGetRoles(query?: { tenantId?: string; includeInactive?: boolean }): Promise<Result<void, ApiError>> {
    const url = '/v1/roles';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postRoles(body: Types.IdentityAuthenticationCreateRoleInput): Promise<Result<void, ApiError>> {
    const url = '/v1/roles';

    // Validate request body
    const validatedBody = safeParse(Types.IdentityAuthenticationCreateRoleInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postRolesAssign(body: Types.IdentityAuthenticationAssignRoleToUserInput): Promise<Result<void, ApiError>> {
    const url = '/v1/roles/:assign';

    // Validate request body
    const validatedBody = safeParse(Types.IdentityAuthenticationAssignRoleToUserInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postRolesRemove(body: Types.IdentityAuthenticationRemoveRoleFromUserInput): Promise<Result<void, ApiError>> {
    const url = '/v1/roles/:remove';

    // Validate request body
    const validatedBody = safeParse(Types.IdentityAuthenticationRemoveRoleFromUserInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getRolesForGetRolesByRoleId(roleId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/roles/${roleId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async putRoles(roleId: string, body: Types.IdentityAuthenticationUpdateRoleInput): Promise<Result<void, ApiError>> {
    const url = `/v1/roles/${roleId}`;

    // Validate request body
    const validatedBody = safeParse(Types.IdentityAuthenticationUpdateRoleInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async deleteRoles(roleId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/roles/${roleId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getRolesUser(userId: string, query?: { includeExpired?: boolean }): Promise<Result<void, ApiError>> {
    const url = `/v1/roles/user/${userId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createAuthRolesModule(client: ApiClient): AuthRolesModule {
  return new AuthRolesModule(client);
}
