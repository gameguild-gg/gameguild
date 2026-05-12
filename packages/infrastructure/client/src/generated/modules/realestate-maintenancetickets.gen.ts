/**
 * @game-guild/client - RealestateMaintenancetickets Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class RealestateMaintenanceticketsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getMaintenanceTickets(query?: {
    propertyId?: string;
    status?: Types.RealEstateEnumsMaintenanceTicketStatus;
    workflowType?: Types.RealEstateEnumsMaintenanceWorkflowType;
    priority?: Types.RealEstateEnumsMaintenanceTicketPriority;
    skip?: number;
    take?: number;
  }): Promise<Result<Array<Types.RealEstateModelsMaintenanceTicket>, ApiError>> {
    const url = '/v1/maintenance-tickets';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.RealEstateModelsMaintenanceTicket>, ApiError>;
  }

  /**
   */
  async postMaintenanceTickets(body: Types.RealEstateModelsCreateMaintenanceTicketInput): Promise<Result<Types.RealEstateModelsMaintenanceTicket, ApiError>> {
    const url = '/v1/maintenance-tickets';

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsCreateMaintenanceTicketInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsMaintenanceTicketSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getMaintenanceTicketsMineOwner(): Promise<Result<Array<Types.RealEstateModelsMaintenanceTicket>, ApiError>> {
    const url = '/v1/maintenance-tickets/mine/owner';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.RealEstateModelsMaintenanceTicket>, ApiError>;
  }

  /**
   */
  async postMaintenanceTicketsMineOwner(
    body: Types.RealEstateModelsCreateOwnerMaintenanceTicketInput,
  ): Promise<Result<Types.RealEstateModelsMaintenanceTicket, ApiError>> {
    const url = '/v1/maintenance-tickets/mine/owner';

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsCreateOwnerMaintenanceTicketInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsMaintenanceTicketSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getMaintenanceTicketsMineRenter(): Promise<Result<Array<Types.RealEstateModelsMaintenanceTicket>, ApiError>> {
    const url = '/v1/maintenance-tickets/mine/renter';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.RealEstateModelsMaintenanceTicket>, ApiError>;
  }

  /**
   */
  async postMaintenanceTicketsMineRenter(
    body: Types.RealEstateModelsCreateRenterMaintenanceTicketInput,
  ): Promise<Result<Types.RealEstateModelsMaintenanceTicket, ApiError>> {
    const url = '/v1/maintenance-tickets/mine/renter';

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsCreateRenterMaintenanceTicketInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsMaintenanceTicketSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getMaintenanceTicketById(id: string): Promise<Result<Types.RealEstateModelsMaintenanceTicket, ApiError>> {
    const url = `/v1/maintenance-tickets/${id}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsMaintenanceTicketSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postMaintenanceTicketsAssign(
    id: string,
    body: Types.RealEstateModelsAssignMaintenanceTicketInput,
  ): Promise<Result<Types.RealEstateModelsMaintenanceTicket, ApiError>> {
    const url = `/v1/maintenance-tickets/${id}/assign`;

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsAssignMaintenanceTicketInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsMaintenanceTicketSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postMaintenanceTicketsStart(id: string): Promise<Result<Types.RealEstateModelsMaintenanceTicket, ApiError>> {
    const url = `/v1/maintenance-tickets/${id}/start`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsMaintenanceTicketSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postMaintenanceTicketsResolve(
    id: string,
    body: Types.RealEstateModelsResolveMaintenanceTicketInput,
  ): Promise<Result<Types.RealEstateModelsMaintenanceTicket, ApiError>> {
    const url = `/v1/maintenance-tickets/${id}/resolve`;

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsResolveMaintenanceTicketInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsMaintenanceTicketSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postMaintenanceTicketsClose(
    id: string,
    body: Types.RealEstateModelsCloseMaintenanceTicketInput,
  ): Promise<Result<Types.RealEstateModelsMaintenanceTicket, ApiError>> {
    const url = `/v1/maintenance-tickets/${id}/close`;

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsCloseMaintenanceTicketInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsMaintenanceTicketSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createRealestateMaintenanceticketsModule(client: ApiClient): RealestateMaintenanceticketsModule {
  return new RealestateMaintenanceticketsModule(client);
}
