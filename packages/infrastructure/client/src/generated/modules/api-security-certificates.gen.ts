/**
 * @game-guild/client - ApiSecurityCertificates Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client';
import type { Result } from '../../runtime/result/types';
import type { ApiError } from '../../runtime/errors/types';
import * as Types from '../types.gen';
import { safeParse } from '../../runtime/errors/validation';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class ApiSecurityCertificatesModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Read asynchronous publication state. Cached state does not prove current chain validity.
   */
  async getApiCertificatesEvm(id: string): Promise<Result<Types.APICoreIntegrationCertificateEvmEvidenceOutput, ApiError>> {
    const url = `/api/certificates/${id}/evm`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.APICoreIntegrationCertificateEvmEvidenceOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Independently verify the original recorded transaction against its configured canonical chain.
   */
  async getApiCertificatesEvmProof(id: string): Promise<Result<Types.APICoreIntegrationCertificateEvmEvidenceOutput, ApiError>> {
    const url = `/api/certificates/${id}/evm/proof`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.APICoreIntegrationCertificateEvmEvidenceOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createApiSecurityCertificatesModule(client: ApiClient): ApiSecurityCertificatesModule {
  return new ApiSecurityCertificatesModule(client);
}
