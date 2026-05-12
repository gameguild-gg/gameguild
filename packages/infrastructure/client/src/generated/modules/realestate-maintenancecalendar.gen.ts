/**
 * @game-guild/client - RealestateMaintenancecalendar Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class RealestateMaintenancecalendarModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getMaintenanceCalendarConnections(): Promise<Result<Array<Types.RealEstateModelsMaintenanceCalendarConnection>, ApiError>> {
    const url = '/v1/maintenance-calendar/connections';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.RealEstateModelsMaintenanceCalendarConnection>, ApiError>;
  }

  /**
   */
  async postMaintenanceCalendarConnectionsAuthorize(
    provider: string,
    body: Types.RealEstateModelsMaintenanceCalendarAuthorizationInput,
  ): Promise<Result<Types.RealEstateModelsMaintenanceCalendarAuthorization, ApiError>> {
    const url = `/v1/maintenance-calendar/connections/${provider}/authorize`;

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsMaintenanceCalendarAuthorizationInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsMaintenanceCalendarAuthorizationSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getMaintenanceCalendarConnectionsCallback(
    provider: string,
    query?: { code?: string; state?: string; error?: string; error_description?: string },
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/maintenance-calendar/connections/${provider}/callback`;

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
  async deleteMaintenanceCalendarConnections(id: string): Promise<Result<void, ApiError>> {
    const url = `/v1/maintenance-calendar/connections/${id}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postMaintenanceCalendarTicketsSync(
    ticketId: string,
    body: Types.RealEstateModelsSyncMaintenanceCalendarInput,
  ): Promise<Result<Array<Types.RealEstateModelsMaintenanceCalendarSyncResult>, ApiError>> {
    const url = `/v1/maintenance-calendar/tickets/${ticketId}/sync`;

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsSyncMaintenanceCalendarInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<Array<Types.RealEstateModelsMaintenanceCalendarSyncResult>, ApiError>;
  }

  /**
   */
  async getMaintenanceCalendarTicketsIcs(ticketId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/maintenance-calendar/tickets/${ticketId}/ics`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createRealestateMaintenancecalendarModule(client: ApiClient): RealestateMaintenancecalendarModule {
  return new RealestateMaintenancecalendarModule(client);
}
