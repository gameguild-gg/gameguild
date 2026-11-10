/**
 * @game-guild/client - ProjectsStoreProducts Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client';
import type { Result } from '../../runtime/result/types';
import type { ApiError } from '../../runtime/errors/types';
import * as Types from '../types.gen';
import { safeParse } from '../../runtime/errors/validation';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class ProjectsStoreProductsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getProjectsStoreProducts(projectId: string): Promise<Result<Types.ProjectsProjectStoreProductProjection[], ApiError>> {
    const url = `/v1/projects/${projectId}/store-products`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Types.ProjectsProjectStoreProductProjection[], ApiError>;
  }

  /**
   */
  async postProjectsStoreProducts(
    projectId: string,
    body?: Types.ProjectsLinkProjectStoreProductInput,
  ): Promise<Result<Types.ProjectsProjectStoreProductProjection, ApiError>> {
    const url = `/v1/projects/${projectId}/store-products`;

    // Validate request body
    const validatedBody = body === undefined ? undefined : safeParse(Types.ProjectsLinkProjectStoreProductInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ProjectsProjectStoreProductProjectionSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteProjectsStoreProducts(projectId: string, productId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/projects/${projectId}/store-products/${productId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getStoreProductsProjects(productId: string): Promise<Result<Types.ProjectsProjectStoreProductProjection[], ApiError>> {
    const url = `/v1/store/products/${productId}/projects`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: false,
    });

    return result as Result<Types.ProjectsProjectStoreProductProjection[], ApiError>;
  }
}

export function createProjectsStoreProductsModule(client: ApiClient): ProjectsStoreProductsModule {
  return new ProjectsStoreProductsModule(client);
}
