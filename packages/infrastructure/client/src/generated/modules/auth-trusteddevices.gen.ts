/**
 * @game-guild/client - AuthTrusteddevices Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from "../../runtime/client.js";
import type { Result } from "../../runtime/result/types.js";
import type { ApiError } from "../../runtime/errors/types.js";
import * as Types from "../types.gen.js";
import { safeParse } from "../../runtime/errors/validation.js";

/* eslint-disable @typescript-eslint/no-explicit-any */

export class AuthTrusteddevicesModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Get trusted devices
   *
   * Retrieves a list of devices that have been marked as trusted for the current user.
   */
  async getAuthTrustedDevices(): Promise<
    Result<Array<Types.IdentityAuthenticationTrustedDeviceOutput>, ApiError>
  > {
    const url = "/v1/auth/trusted-devices";

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    return result as Result<
      Array<Types.IdentityAuthenticationTrustedDeviceOutput>,
      ApiError
    >;
  }

  /**
   * Trust current device
   *
   * Marks the current device as trusted, allowing faster authentication in the future.
   */
  async postAuthTrustedDevices(
    body: Types.IdentityAuthenticationTrustDeviceInput,
  ): Promise<
    Result<Types.IdentityAuthenticationSessionSuccessOutput, ApiError>
  > {
    const url = "/v1/auth/trusted-devices";

    // Validate request body
    const validatedBody = safeParse(
      Types.IdentityAuthenticationTrustDeviceInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.IdentityAuthenticationSessionSuccessOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Revoke device trust
   *
   * Removes a device from the trusted devices list.
   */
  async deleteAuthTrustedDevices(
    deviceId: string,
  ): Promise<
    Result<Types.IdentityAuthenticationSessionSuccessOutput, ApiError>
  > {
    const url = `/v1/auth/trusted-devices/${deviceId}`;

    const result = await this.client.request({
      method: "DELETE",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.IdentityAuthenticationSessionSuccessOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createAuthTrusteddevicesModule(
  client: ApiClient,
): AuthTrusteddevicesModule {
  return new AuthTrusteddevicesModule(client);
}
