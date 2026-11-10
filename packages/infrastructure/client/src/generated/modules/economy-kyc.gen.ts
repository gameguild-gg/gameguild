/**
 * @game-guild/client - EconomyKyc Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client';
import type { Result } from '../../runtime/result/types';
import type { ApiError } from '../../runtime/errors/types';
import * as Types from '../types.gen';
import { safeParse } from '../../runtime/errors/validation';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class EconomyKycModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async postEconomyKycAccessToken(body?: Types.APIControllersCreateMyKycAccessTokenInput): Promise<Result<Types.ComplianceKYCKycAmlAccessToken, ApiError>> {
    const url = '/api/v1/economy/kyc/access-token';

    // Validate request body
    const validatedBody = body === undefined ? undefined : safeParse(Types.APIControllersCreateMyKycAccessTokenInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceKYCKycAmlAccessTokenSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postEconomyKycOnboarding(body?: Types.APIControllersStartMyKycInput): Promise<Result<Types.ComplianceKYCKycAmlOnboarding, ApiError>> {
    const url = '/api/v1/economy/kyc/onboarding';

    // Validate request body
    const validatedBody = body === undefined ? undefined : safeParse(Types.APIControllersStartMyKycInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ComplianceKYCKycAmlOnboardingSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getEconomyKycStatus(): Promise<Result<Types.APIControllersEconomyKycStatusDto, ApiError>> {
    const url = '/api/v1/economy/kyc/status';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.APIControllersEconomyKycStatusDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createEconomyKycModule(client: ApiClient): EconomyKycModule {
  return new EconomyKycModule(client);
}
