/**
 * @game-guild/client - TestinglabSettings Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class TestinglabSettingsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getApiTestingLabSettings(): Promise<Result<Types.TestingLabTestingLabSettings, ApiError>> {
    const url = '/api/testing-lab/settings';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.TestingLabTestingLabSettingsSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putApiTestingLabSettings(body: Types.TestingLabCreateTestingLabSettings): Promise<Result<Types.TestingLabTestingLabSettings, ApiError>> {
    const url = '/api/testing-lab/settings';

    // Validate request body
    const validatedBody = safeParse(Types.TestingLabCreateTestingLabSettingsSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.TestingLabTestingLabSettingsSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async patchApiTestingLabSettings(body: Types.TestingLabUpdateTestingLabSettings): Promise<Result<Types.TestingLabTestingLabSettings, ApiError>> {
    const url = '/api/testing-lab/settings';

    // Validate request body
    const validatedBody = safeParse(Types.TestingLabUpdateTestingLabSettingsSchema, body, 'request');

    const result = await this.client.request({
      method: 'PATCH',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.TestingLabTestingLabSettingsSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postApiTestingLabSettingsReset(): Promise<Result<Types.TestingLabTestingLabSettings, ApiError>> {
    const url = '/api/testing-lab/settings/reset';

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.TestingLabTestingLabSettingsSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getApiTestingLabSettingsExists(): Promise<Result<boolean, ApiError>> {
    const url = '/api/testing-lab/settings/exists';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<boolean, ApiError>;
  }
}

export function createTestinglabSettingsModule(client: ApiClient): TestinglabSettingsModule {
  return new TestinglabSettingsModule(client);
}
