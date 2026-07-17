/**
 * @game-guild/client - CommerceOrders Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class CommerceOrdersModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getOrders(query?: { owner?: string; status?: Types.CommerceOrdersOrderStatus }): Promise<Result<Array<Types.CommerceOrdersOrder>, ApiError>> {
    const url = '/v1/orders';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.CommerceOrdersOrder>, ApiError>;
  }

  /**
   */
  async postOrders(body: Types.CommerceOrdersCreateOrderInput): Promise<Result<Types.CommerceOrdersOrder, ApiError>> {
    const url = '/v1/orders';

    // Validate request body
    const validatedBody = safeParse(Types.CommerceOrdersCreateOrderInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceOrdersOrderSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getOrders1(orderId: string): Promise<Result<Types.CommerceOrdersOrder, ApiError>> {
    const url = `/v1/orders/${orderId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceOrdersOrderSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteOrders(orderId: string, query?: { reason?: string }): Promise<Result<void, ApiError>> {
    const url = `/v1/orders/${orderId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async patchOrders(orderId: string, body: Types.CommerceOrdersPatchOrderInput): Promise<Result<Types.CommerceOrdersOrder, ApiError>> {
    const url = `/v1/orders/${orderId}`;

    // Validate request body
    const validatedBody = safeParse(Types.CommerceOrdersPatchOrderInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PATCH',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceOrdersOrderSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async headOrders(orderId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/orders/${orderId}`;

    const result = await this.client.request({
      method: 'HEAD',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postOrdersItems(orderId: string, body: Types.CommerceOrdersAddOrderItemInput): Promise<Result<Types.CommerceOrdersOrder, ApiError>> {
    const url = `/v1/orders/${orderId}/items`;

    // Validate request body
    const validatedBody = safeParse(Types.CommerceOrdersAddOrderItemInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceOrdersOrderSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postOrdersCancel(orderId: string, body: Types.CommerceOrdersCancelOrderInput): Promise<Result<void, ApiError>> {
    const url = `/v1/orders/${orderId}:cancel`;

    // Validate request body
    const validatedBody = safeParse(Types.CommerceOrdersCancelOrderInputSchema, body, 'request');

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
  async postOrdersCapture(orderId: string, body: Types.CommerceOrdersCaptureOrderInput): Promise<Result<Types.CommerceOrdersOrder, ApiError>> {
    const url = `/v1/orders/${orderId}:capture`;

    // Validate request body
    const validatedBody = safeParse(Types.CommerceOrdersCaptureOrderInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceOrdersOrderSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postOrdersComplete(orderId: string, body: Types.CommerceOrdersCompleteOrderInput): Promise<Result<Types.CommerceOrdersOrder, ApiError>> {
    const url = `/v1/orders/${orderId}:complete`;

    // Validate request body
    const validatedBody = safeParse(Types.CommerceOrdersCompleteOrderInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceOrdersOrderSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postOrdersHold(orderId: string, body: Types.CommerceOrdersHoldOrderInput): Promise<Result<Types.CommerceOrdersOrder, ApiError>> {
    const url = `/v1/orders/${orderId}:hold`;

    // Validate request body
    const validatedBody = safeParse(Types.CommerceOrdersHoldOrderInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceOrdersOrderSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postOrdersRefund(orderId: string, body: Types.CommerceOrdersRefundOrderInput): Promise<Result<Types.CommerceOrdersOrder, ApiError>> {
    const url = `/v1/orders/${orderId}:refund`;

    // Validate request body
    const validatedBody = safeParse(Types.CommerceOrdersRefundOrderInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceOrdersOrderSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postOrdersRelease(orderId: string): Promise<Result<Types.CommerceOrdersOrder, ApiError>> {
    const url = `/v1/orders/${orderId}:release`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.CommerceOrdersOrderSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createCommerceOrdersModule(client: ApiClient): CommerceOrdersModule {
  return new CommerceOrdersModule(client);
}
