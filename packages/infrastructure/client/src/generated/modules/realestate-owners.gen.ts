/**
 * @game-guild/client - RealestateOwners Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class RealestateOwnersModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getOwners(query?: { search?: string; skip?: number; take?: number }): Promise<Result<Array<Types.RealEstateModelsOwner>, ApiError>> {
    const url = '/v1/owners';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.RealEstateModelsOwner>, ApiError>;
  }

  /**
   */
  async postOwners(body: Types.RealEstateModelsCreateOwnerInput): Promise<Result<Types.RealEstateModelsOwner, ApiError>> {
    const url = '/v1/owners';

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsCreateOwnerInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsOwnerSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getOwnerById(id: string): Promise<Result<Types.RealEstateModelsOwner, ApiError>> {
    const url = `/v1/owners/${id}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsOwnerSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putOwners(id: string, body: Types.RealEstateModelsUpdateOwnerInput): Promise<Result<Types.RealEstateModelsOwner, ApiError>> {
    const url = `/v1/owners/${id}`;

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsUpdateOwnerInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsOwnerSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteOwners(id: string): Promise<Result<void, ApiError>> {
    const url = `/v1/owners/${id}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getOwnersPortal(id: string): Promise<Result<Types.RealEstateModelsOwnerPortal, ApiError>> {
    const url = `/v1/owners/${id}/portal`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsOwnerPortalSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getOwnersMePortal(): Promise<Result<Types.RealEstateModelsOwnerPortal, ApiError>> {
    const url = '/v1/owners/me/portal';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsOwnerPortalSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createRealestateOwnersModule(client: ApiClient): RealestateOwnersModule {
  return new RealestateOwnersModule(client);
}
