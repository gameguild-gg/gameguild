/**
 * @game-guild/client - ResourcesContentsContracts Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client';
import type { Result } from '../../runtime/result/types';
import type { ApiError } from '../../runtime/errors/types';
import * as Types from '../types.gen';
import { safeParse } from '../../runtime/errors/validation';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class ResourcesContentsContractsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async postDocumentContractsGenerate(
    body: Types.ResourcesContentsGenerateContractInput,
  ): Promise<Result<Types.ResourcesContentsGeneratedContractOutput, ApiError>> {
    const url = '/v1/document-contracts/generate';

    // Validate request body
    const validatedBody = safeParse(Types.ResourcesContentsGenerateContractInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ResourcesContentsGeneratedContractOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postDocumentContractsGenerateBulk(
    body: Types.ResourcesContentsBulkGenerateContractsInput,
  ): Promise<Result<Types.ResourcesContentsBulkGeneratedContractsOutput, ApiError>> {
    const url = '/v1/document-contracts/generate:bulk';

    // Validate request body
    const validatedBody = safeParse(Types.ResourcesContentsBulkGenerateContractsInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ResourcesContentsBulkGeneratedContractsOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createResourcesContentsContractsModule(client: ApiClient): ResourcesContentsContractsModule {
  return new ResourcesContentsContractsModule(client);
}
