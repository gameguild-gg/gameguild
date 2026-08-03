/**
 * @game-guild/client - TestinglabTestingrequests Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class TestinglabTestingrequestsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getTestingAvailableForTesting(): Promise<Result<Array<Types.TestingLabTestingInput>, ApiError>> {
    const url = '/v1/testing/available-for-testing';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.TestingLabTestingInput>, ApiError>;
  }

  /**
   */
  async getTestingMyRequests(): Promise<Result<Array<Types.TestingLabTestingInput>, ApiError>> {
    const url = '/v1/testing/my-requests';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.TestingLabTestingInput>, ApiError>;
  }

  /**
   */
  async getTestingRequests(query?: { skip?: number; take?: number }): Promise<Result<Array<Types.TestingLabTestingInput>, ApiError>> {
    const url = '/v1/testing/requests';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.TestingLabTestingInput>, ApiError>;
  }

  /**
   */
  async postTestingRequests(body: Types.TestingLabCreateTestingInput): Promise<Result<Types.TestingLabTestingInput, ApiError>> {
    const url = '/v1/testing/requests';

    // Validate request body
    const validatedBody = safeParse(Types.TestingLabCreateTestingInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.TestingLabTestingInputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getTestingRequestsByCreator(creatorId: string): Promise<Result<Array<Types.TestingLabTestingInput>, ApiError>> {
    const url = `/v1/testing/requests/by-creator/${creatorId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.TestingLabTestingInput>, ApiError>;
  }

  /**
   */
  async getTestingRequestsByProjectVersion(projectVersionId: string): Promise<Result<Array<Types.TestingLabTestingInput>, ApiError>> {
    const url = `/v1/testing/requests/by-project-version/${projectVersionId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.TestingLabTestingInput>, ApiError>;
  }

  /**
   */
  async getTestingRequestsByStatus(status: Types.TestingLabTestingRequestStatus): Promise<Result<Array<Types.TestingLabTestingInput>, ApiError>> {
    const url = `/v1/testing/requests/by-status/${status}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.TestingLabTestingInput>, ApiError>;
  }

  /**
   */
  async getTestingRequestsSearch(query?: { searchTerm?: string }): Promise<Result<Array<Types.TestingLabTestingInput>, ApiError>> {
    const url = '/v1/testing/requests/search';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.TestingLabTestingInput>, ApiError>;
  }

  /**
   */
  async getTestingRequests1(id: string): Promise<Result<Types.TestingLabTestingInput, ApiError>> {
    const url = `/v1/testing/requests/${id}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.TestingLabTestingInputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putTestingRequests(id: string, body: Types.TestingLabTestingInput): Promise<Result<Types.TestingLabTestingInput, ApiError>> {
    const url = `/v1/testing/requests/${id}`;

    // Validate request body
    const validatedBody = safeParse(Types.TestingLabTestingInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.TestingLabTestingInputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteTestingRequests(id: string): Promise<Result<void, ApiError>> {
    const url = `/v1/testing/requests/${id}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getTestingRequestsDetails(id: string): Promise<Result<Types.TestingLabTestingInput, ApiError>> {
    const url = `/v1/testing/requests/${id}/details`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.TestingLabTestingInputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postTestingRequestsRestore(id: string): Promise<Result<void, ApiError>> {
    const url = `/v1/testing/requests/${id}:restore`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getTestingRequestsStatistics(requestId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/testing/requests/${requestId}/statistics`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postTestingSubmitSimple(body: Types.TestingLabCreateSimpleTestingInput): Promise<Result<Types.TestingLabTestingInput, ApiError>> {
    const url = '/v1/testing/submit-simple';

    // Validate request body
    const validatedBody = safeParse(Types.TestingLabCreateSimpleTestingInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.TestingLabTestingInputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createTestinglabTestingrequestsModule(client: ApiClient): TestinglabTestingrequestsModule {
  return new TestinglabTestingrequestsModule(client);
}
