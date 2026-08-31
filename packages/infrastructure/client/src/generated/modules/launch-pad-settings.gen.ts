/**
 * @game-guild/client - LaunchPadSettings Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class LaunchPadSettingsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getLaunchPadSettings(): Promise<Result<Types.LaunchPadLaunchPadSettingsProjection, ApiError>> {
    const url = '/v1/launch-pad/settings';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LaunchPadLaunchPadSettingsProjectionSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putLaunchPadSettings(body: Types.LaunchPadUpdateLaunchPadSettingsInput): Promise<Result<Types.LaunchPadLaunchPadSettingsProjection, ApiError>> {
    const url = '/v1/launch-pad/settings';

    // Validate request body
    const validatedBody = safeParse(Types.LaunchPadUpdateLaunchPadSettingsInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LaunchPadLaunchPadSettingsProjectionSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createLaunchPadSettingsModule(client: ApiClient): LaunchPadSettingsModule {
  return new LaunchPadSettingsModule(client);
}
