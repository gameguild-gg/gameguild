/**
 * @game-guild/client - RealestateMaintenancevendors Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class RealestateMaintenancevendorsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getMaintenanceVendors(query?: {
    search?: string;
    approved?: boolean;
    skip?: number;
    take?: number;
  }): Promise<Result<Array<Types.RealEstateModelsMaintenanceVendor>, ApiError>> {
    const url = '/v1/maintenance-vendors';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.RealEstateModelsMaintenanceVendor>, ApiError>;
  }

  /**
   */
  async postMaintenanceVendors(body: Types.RealEstateModelsCreateMaintenanceVendorInput): Promise<Result<Types.RealEstateModelsMaintenanceVendor, ApiError>> {
    const url = '/v1/maintenance-vendors';

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsCreateMaintenanceVendorInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsMaintenanceVendorSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getMaintenanceVendorById(id: string): Promise<Result<Types.RealEstateModelsMaintenanceVendor, ApiError>> {
    const url = `/v1/maintenance-vendors/${id}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsMaintenanceVendorSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putMaintenanceVendors(
    id: string,
    body: Types.RealEstateModelsUpdateMaintenanceVendorInput,
  ): Promise<Result<Types.RealEstateModelsMaintenanceVendor, ApiError>> {
    const url = `/v1/maintenance-vendors/${id}`;

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsUpdateMaintenanceVendorInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsMaintenanceVendorSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteMaintenanceVendors(id: string): Promise<Result<void, ApiError>> {
    const url = `/v1/maintenance-vendors/${id}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createRealestateMaintenancevendorsModule(client: ApiClient): RealestateMaintenancevendorsModule {
  return new RealestateMaintenancevendorsModule(client);
}
