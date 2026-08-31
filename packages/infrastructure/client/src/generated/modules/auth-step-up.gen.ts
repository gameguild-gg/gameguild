/**
 * @game-guild/client - AuthStepUp Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class AuthStepUpModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async postAuthStepUpChallenges(
    body: Types.IdentityAuthenticationCreateStepUpChallengeInput,
  ): Promise<Result<Types.IdentityAuthenticationStepUpChallengeOutput, ApiError>> {
    const url = '/v1/auth/step-up/challenges';

    // Validate request body
    const validatedBody = safeParse(Types.IdentityAuthenticationCreateStepUpChallengeInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.IdentityAuthenticationStepUpChallengeOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAuthStepUpChallengesVerify(
    challengeId: string,
    body: Types.IdentityAuthenticationVerifyStepUpChallengeInput,
  ): Promise<Result<Types.IdentityAuthenticationStepUpReceiptOutput, ApiError>> {
    const url = `/v1/auth/step-up/challenges/${challengeId}:verify`;

    // Validate request body
    const validatedBody = safeParse(Types.IdentityAuthenticationVerifyStepUpChallengeInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.IdentityAuthenticationStepUpReceiptOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAuthStepUpChallengesWebauthnOptions(
    challengeId: string,
  ): Promise<Result<Types.IdentityAuthenticationWebAuthnAuthenticationOptionsResult, ApiError>> {
    const url = `/v1/auth/step-up/challenges/${challengeId}:webauthn-options`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.IdentityAuthenticationWebAuthnAuthenticationOptionsResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createAuthStepUpModule(client: ApiClient): AuthStepUpModule {
  return new AuthStepUpModule(client);
}
