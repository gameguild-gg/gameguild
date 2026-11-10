/**
 * @game-guild/client - AuthTrustedDevices Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client';
import type { Result } from '../../runtime/result/types';
import type { ApiError } from '../../runtime/errors/types';
import * as Types from '../types.gen';
import { safeParse } from '../../runtime/errors/validation';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class AuthTrustedDevicesModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Get trusted devices for the current user
   *
   * Retrieves a list of devices that have been marked as trusted for the current user.
   */
  async getAuthTrustedDevices(): Promise<Result<Types.IdentityAuthenticationTrustedDeviceOutput[], ApiError>> {
    const url = '/v1/auth/trusted-devices';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Types.IdentityAuthenticationTrustedDeviceOutput[], ApiError>;
  }

  /**
   * Trust the current device
   *
   * Trusting a device is a security-sensitive grant, so it requires fresh verification
   * (issues #1144 and #1191). A request without a receipt returns HTTP 403 with a persisted
   * challenge, its server-derived operation binding, expiry, and available supported factors.
   * Verify the challenge with an enrolled TOTP, backup code, or active WebAuthn credential,
   * then retry the same device name with the returned receipt as `stepUpReceipt`.
   */
  async postAuthTrustedDevices(
    body?: Types.IdentityAuthenticationTrustDeviceInput,
  ): Promise<Result<Types.IdentityAuthenticationSessionSuccessOutput, ApiError>> {
    const url = '/v1/auth/trusted-devices';

    // Validate request body
    const validatedBody = body === undefined ? undefined : safeParse(Types.IdentityAuthenticationTrustDeviceInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.IdentityAuthenticationSessionSuccessOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Revoke trust for a specific device
   *
   * Removes a device from the trusted devices list.
   */
  async deleteAuthTrustedDevices(deviceId: string): Promise<Result<Types.IdentityAuthenticationSessionSuccessOutput, ApiError>> {
    const url = `/v1/auth/trusted-devices/${deviceId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.IdentityAuthenticationSessionSuccessOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Name or rename one of the current user's trusted devices
   *
   * Assigns a user-defined name to one of the current user's own trusted devices. The acting user is taken from the authenticated principal, never from the body.
   */
  async patchAuthTrustedDevicesName(
    deviceId: string,
    body?: Types.IdentityAuthenticationRenameTrustedDeviceInput,
  ): Promise<Result<Types.IdentityAuthenticationSessionSuccessOutput, ApiError>> {
    const url = `/v1/auth/trusted-devices/${deviceId}/name`;

    // Validate request body
    const validatedBody = body === undefined ? undefined : safeParse(Types.IdentityAuthenticationRenameTrustedDeviceInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PATCH',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.IdentityAuthenticationSessionSuccessOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createAuthTrustedDevicesModule(client: ApiClient): AuthTrustedDevicesModule {
  return new AuthTrustedDevicesModule(client);
}
