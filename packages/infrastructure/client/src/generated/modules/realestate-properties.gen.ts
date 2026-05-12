/**
 * @game-guild/client - RealestateProperties Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class RealestatePropertiesModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getProperties(query?: {
    search?: string;
    status?: Types.RealEstateEnumsPropertyStatus;
    purpose?: Types.RealEstateEnumsListingPurpose;
    state?: string;
    minBedrooms?: number;
    maxRent?: number;
    skip?: number;
    take?: number;
  }): Promise<Result<Array<Types.RealEstateModelsProperty>, ApiError>> {
    const url = '/v1/properties';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.RealEstateModelsProperty>, ApiError>;
  }

  /**
   */
  async postProperties(body: Types.RealEstateModelsCreatePropertyInput): Promise<Result<Types.RealEstateModelsProperty, ApiError>> {
    const url = '/v1/properties';

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsCreatePropertyInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsPropertySchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getPropertyById(id: string): Promise<Result<Types.RealEstateModelsProperty, ApiError>> {
    const url = `/v1/properties/${id}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsPropertySchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putProperties(id: string, body: Types.RealEstateModelsUpdatePropertyInput): Promise<Result<Types.RealEstateModelsProperty, ApiError>> {
    const url = `/v1/properties/${id}`;

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsUpdatePropertyInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsPropertySchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteProperties(id: string): Promise<Result<void, ApiError>> {
    const url = `/v1/properties/${id}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getPropertiesWorkspace(id: string): Promise<Result<Types.RealEstateModelsPropertyWorkspace, ApiError>> {
    const url = `/v1/properties/${id}/workspace`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsPropertyWorkspaceSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postPropertiesOwners(id: string, body: Types.RealEstateModelsAssignOwnerInput): Promise<Result<void, ApiError>> {
    const url = `/v1/properties/${id}/owners`;

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsAssignOwnerInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postPropertiesMedia(id: string, body: Types.RealEstateModelsCreatePropertyMediaInput): Promise<Result<Types.RealEstateModelsPropertyMedia, ApiError>> {
    const url = `/v1/properties/${id}/media`;

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsCreatePropertyMediaInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsPropertyMediaSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deletePropertiesMedia(id: string, mediaId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/properties/${id}/media/${mediaId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postPropertiesDocuments(
    id: string,
    body: Types.RealEstateModelsCreatePropertyDocumentInput,
  ): Promise<Result<Types.RealEstateModelsPropertyDocumentSummary, ApiError>> {
    const url = `/v1/properties/${id}/documents`;

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsCreatePropertyDocumentInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsPropertyDocumentSummarySchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postPropertiesInspections(
    id: string,
    body: Types.RealEstateModelsCreatePropertyInspectionInput,
  ): Promise<Result<Types.RealEstateModelsPropertyInspection, ApiError>> {
    const url = `/v1/properties/${id}/inspections`;

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsCreatePropertyInspectionInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsPropertyInspectionSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postPropertiesInspectionsComplete(
    id: string,
    inspectionId: string,
    body: Types.RealEstateModelsCompletePropertyInspectionInput,
  ): Promise<Result<Types.RealEstateModelsPropertyInspection, ApiError>> {
    const url = `/v1/properties/${id}/inspections/${inspectionId}/complete`;

    // Validate request body
    const validatedBody = safeParse(Types.RealEstateModelsCompletePropertyInspectionInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsPropertyInspectionSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deletePropertiesDocuments(id: string, documentId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/properties/${id}/documents/${documentId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async deletePropertiesOwners(id: string, ownerId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/properties/${id}/owners/${ownerId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postPropertiesPublish(id: string): Promise<Result<Types.RealEstateModelsProperty, ApiError>> {
    const url = `/v1/properties/${id}/publish`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.RealEstateModelsPropertySchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createRealestatePropertiesModule(client: ApiClient): RealestatePropertiesModule {
  return new RealestatePropertiesModule(client);
}
