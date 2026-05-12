/**
 * @game-guild/client - RealestateMaintenancequotes Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class RealestateMaintenancequotesModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getMaintenanceQuotes(query?: {
    ownerId?: string;
    propertyId?: string;
    status?: Types.RealEstateEnumsMaintenanceQuoteStatus;
    skip?: number;
    take?: number;
  }): Promise<Result<Array<Types.RealEstateModelsMaintenanceQuote>, ApiError>> {
    const url = '/v1/maintenance-quotes';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.RealEstateModelsMaintenanceQuote>, ApiError>;
  }

  /**
   */
  async postMaintenanceQuotes(body: Types.RealEstateModelsCreateMaintenanceQuoteInput): Promise<Result<Types.RealEstateModelsMaintenanceQuote, ApiError>> {
    const url = '/v1/maintenance-quotes';

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsCreateMaintenanceQuoteInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsMaintenanceQuoteSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getMaintenanceQuotesMine(query?: {
    status?: Types.RealEstateEnumsMaintenanceQuoteStatus;
    skip?: number;
    take?: number;
  }): Promise<Result<Array<Types.RealEstateModelsMaintenanceQuote>, ApiError>> {
    const url = '/v1/maintenance-quotes/mine';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.RealEstateModelsMaintenanceQuote>, ApiError>;
  }

  /**
   */
  async getMaintenanceQuoteById(id: string): Promise<Result<Types.RealEstateModelsMaintenanceQuote, ApiError>> {
    const url = `/v1/maintenance-quotes/${id}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsMaintenanceQuoteSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postMaintenanceQuotesApprove(
    id: string,
    body: Types.RealEstateModelsDecideMaintenanceQuoteInput,
  ): Promise<Result<Types.RealEstateModelsMaintenanceQuote, ApiError>> {
    const url = `/v1/maintenance-quotes/${id}/approve`;

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsDecideMaintenanceQuoteInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsMaintenanceQuoteSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postMaintenanceQuotesReject(
    id: string,
    body: Types.RealEstateModelsDecideMaintenanceQuoteInput,
  ): Promise<Result<Types.RealEstateModelsMaintenanceQuote, ApiError>> {
    const url = `/v1/maintenance-quotes/${id}/reject`;

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsDecideMaintenanceQuoteInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsMaintenanceQuoteSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createRealestateMaintenancequotesModule(client: ApiClient): RealestateMaintenancequotesModule {
  return new RealestateMaintenancequotesModule(client);
}
