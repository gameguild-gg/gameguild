/**
 * @game-guild/client - CommercePaymentsFraudDevices Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client';
import type { Result } from '../../runtime/result/types';
import type { ApiError } from '../../runtime/errors/types';
import * as Types from '../types.gen';
import { safeParse } from '../../runtime/errors/validation';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class CommercePaymentsFraudDevicesModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * List device fingerprints shared across multiple customer accounts.
   *
   * Lists device fingerprints presented on multiple customer accounts over a look-back window, with the per-account usage breakdown reviewers use to judge whether the sharing is a household or a fraud ring. Backs the Device.CrossAccountUsage fraud signal. Requires the payments:admin permission.
   */
  async getFraudDevices(query?: {
    tenantId?: string;
    minAccounts?: number;
    lookbackDays?: number;
    page?: number;
    pageSize?: number;
  }): Promise<Result<Types.CommercePaymentsFraudDeviceInsight[], ApiError>> {
    const url = '/api/v1/fraud/devices';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.CommercePaymentsFraudDeviceInsight[], ApiError>;
  }
}

export function createCommercePaymentsFraudDevicesModule(client: ApiClient): CommercePaymentsFraudDevicesModule {
  return new CommercePaymentsFraudDevicesModule(client);
}
