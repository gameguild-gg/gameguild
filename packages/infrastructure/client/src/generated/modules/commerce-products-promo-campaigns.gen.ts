/**
 * @game-guild/client - CommerceProductsPromoCampaigns Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class CommerceProductsPromoCampaignsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * List campaigns (paged) with optional status filter and search
   */
  async getPromoCampaignsForGetPromoCampaigns(query?: {
    status?: Types.CommerceProductsPromoCampaignStatus;
    searchTerm?: string;
    skip?: number;
    take?: number;
  }): Promise<Result<Types.PagedResultPromoCampaignDto, ApiError>> {
    const url = '/v1/promo-campaigns';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.PagedResultPromoCampaignDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Create a campaign (starts in Draft)
   */
  async postPromoCampaigns(body: Types.CommerceProductsCreatePromoCampaignInput): Promise<Result<Types.CommerceProductsPromoCampaignDto, ApiError>> {
    const url = '/v1/promo-campaigns';

    // Validate request body
    const validatedBody = safeParse(Types.CommerceProductsCreatePromoCampaignInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceProductsPromoCampaignDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Get a campaign by ID
   */
  async getPromoCampaignsForGetPromoCampaignsByCampaignId(campaignId: string): Promise<Result<Types.CommerceProductsPromoCampaignDto, ApiError>> {
    const url = `/v1/promo-campaigns/${campaignId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceProductsPromoCampaignDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Update a campaign (key and lifecycle status are immutable here)
   */
  async putPromoCampaigns(
    campaignId: string,
    body: Types.CommerceProductsUpdatePromoCampaignInput,
  ): Promise<Result<Types.CommerceProductsPromoCampaignDto, ApiError>> {
    const url = `/v1/promo-campaigns/${campaignId}`;

    // Validate request body
    const validatedBody = safeParse(Types.CommerceProductsUpdatePromoCampaignInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceProductsPromoCampaignDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Delete a campaign. Fails closed while promo codes still reference its key.
   */
  async deletePromoCampaigns(campaignId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/promo-campaigns/${campaignId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   * Activate a draft or paused campaign
   */
  async postPromoCampaignsActivate(campaignId: string): Promise<Result<Types.CommerceProductsPromoCampaignDto, ApiError>> {
    const url = `/v1/promo-campaigns/${campaignId}:activate`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceProductsPromoCampaignDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Archive a completed or cancelled campaign
   */
  async postPromoCampaignsArchive(campaignId: string): Promise<Result<Types.CommerceProductsPromoCampaignDto, ApiError>> {
    const url = `/v1/promo-campaigns/${campaignId}:archive`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceProductsPromoCampaignDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Cancel a draft, active, or paused campaign
   */
  async postPromoCampaignsCancel(campaignId: string): Promise<Result<Types.CommerceProductsPromoCampaignDto, ApiError>> {
    const url = `/v1/promo-campaigns/${campaignId}:cancel`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceProductsPromoCampaignDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Complete an active or paused campaign
   */
  async postPromoCampaignsComplete(campaignId: string): Promise<Result<Types.CommerceProductsPromoCampaignDto, ApiError>> {
    const url = `/v1/promo-campaigns/${campaignId}:complete`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceProductsPromoCampaignDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Pause an active campaign
   */
  async postPromoCampaignsPause(campaignId: string): Promise<Result<Types.CommerceProductsPromoCampaignDto, ApiError>> {
    const url = `/v1/promo-campaigns/${campaignId}:pause`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceProductsPromoCampaignDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Campaign performance report (uses, discount, conversion, CAC attribution) within an optional window
   */
  async getPromoCampaignsPerformance(
    campaignId: string,
    query?: { from?: string; to?: string },
  ): Promise<Result<Types.CommerceProductsCampaignPerformanceDto, ApiError>> {
    const url = `/v1/promo-campaigns/${campaignId}/performance`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceProductsCampaignPerformanceDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createCommerceProductsPromoCampaignsModule(client: ApiClient): CommerceProductsPromoCampaignsModule {
  return new CommerceProductsPromoCampaignsModule(client);
}
