/**
 * @game-guild/client - RealestateRenters Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class RealestateRentersModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getRenters(query?: { search?: string; skip?: number; take?: number }): Promise<Result<Array<Types.RealEstateModelsRenter>, ApiError>> {
    const url = '/v1/renters';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.RealEstateModelsRenter>, ApiError>;
  }

  /**
   */
  async postRenters(body: Types.RealEstateModelsCreateRenterInput): Promise<Result<Types.RealEstateModelsRenter, ApiError>> {
    const url = '/v1/renters';

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsCreateRenterInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsRenterSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getRenterById(id: string): Promise<Result<Types.RealEstateModelsRenter, ApiError>> {
    const url = `/v1/renters/${id}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsRenterSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putRenters(id: string, body: Types.RealEstateModelsUpdateRenterInput): Promise<Result<Types.RealEstateModelsRenter, ApiError>> {
    const url = `/v1/renters/${id}`;

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsUpdateRenterInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsRenterSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteRenters(id: string): Promise<Result<void, ApiError>> {
    const url = `/v1/renters/${id}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getRentersMePortal(): Promise<Result<Types.RealEstateModelsRenterPortal, ApiError>> {
    const url = '/v1/renters/me/portal';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsRenterPortalSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getRentersMeMaintenancePortal(): Promise<Result<Types.RealEstateModelsRenterMaintenancePortal, ApiError>> {
    const url = '/v1/renters/me/maintenance-portal';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsRenterMaintenancePortalSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createRealestateRentersModule(client: ApiClient): RealestateRentersModule {
  return new RealestateRentersModule(client);
}
