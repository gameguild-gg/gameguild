/**
 * @game-guild/client - RealestateRentalapplications Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class RealestateRentalapplicationsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getRentalApplications(query?: {
    propertyId?: string;
    renterId?: string;
    status?: Types.RealEstateEnumsRentalApplicationStatus;
    skip?: number;
    take?: number;
  }): Promise<Result<Array<Types.RealEstateModelsRentalApplication>, ApiError>> {
    const url = '/v1/rental-applications';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.RealEstateModelsRentalApplication>, ApiError>;
  }

  /**
   */
  async postRentalApplications(body: Types.RealEstateModelsCreateRentalApplicationInput): Promise<Result<Types.RealEstateModelsRentalApplication, ApiError>> {
    const url = '/v1/rental-applications';

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsCreateRentalApplicationInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsRentalApplicationSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getRentalApplicationById(id: string): Promise<Result<Types.RealEstateModelsRentalApplication, ApiError>> {
    const url = `/v1/rental-applications/${id}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsRentalApplicationSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postRentalApplicationsPublic(
    body: Types.RealEstateModelsPublicRentalApplicationInput,
  ): Promise<Result<Types.RealEstateModelsRentalApplication, ApiError>> {
    const url = '/v1/rental-applications/public';

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsPublicRentalApplicationInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsRentalApplicationSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postRentalApplicationsApprove(
    id: string,
    body: Types.RealEstateModelsDecisionInput,
  ): Promise<Result<Types.RealEstateModelsRentalApplication, ApiError>> {
    const url = `/v1/rental-applications/${id}/approve`;

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsDecisionInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsRentalApplicationSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postRentalApplicationsReject(
    id: string,
    body: Types.RealEstateModelsDecisionInput,
  ): Promise<Result<Types.RealEstateModelsRentalApplication, ApiError>> {
    const url = `/v1/rental-applications/${id}/reject`;

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsDecisionInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsRentalApplicationSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postRentalApplicationsWithdraw(id: string): Promise<Result<Types.RealEstateModelsRentalApplication, ApiError>> {
    const url = `/v1/rental-applications/${id}/withdraw`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsRentalApplicationSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postRentalApplicationsBackgroundCheck(id: string): Promise<Result<Types.RealEstateModelsRentalApplication, ApiError>> {
    const url = `/v1/rental-applications/${id}/background-check`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsRentalApplicationSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postRentalApplicationsSendLease(
    id: string,
    body: Types.RealEstateControllersSendLeaseInput,
  ): Promise<Result<Types.RealEstateModelsRentalApplication, ApiError>> {
    const url = `/v1/rental-applications/${id}/send-lease`;

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateControllersSendLeaseInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsRentalApplicationSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createRealestateRentalapplicationsModule(client: ApiClient): RealestateRentalapplicationsModule {
  return new RealestateRentalapplicationsModule(client);
}
