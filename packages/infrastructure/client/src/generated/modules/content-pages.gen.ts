/**
 * @game-guild/client - ContentPages Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class ContentPagesModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getPages(query?: {
    type?: Types.ContentPagesPageType;
    status?: Types.ContentPagesPageStatus;
    locale?: string;
    parentId?: string;
    skip?: number;
    take?: number;
  }): Promise<Result<Array<Types.ContentPagesPage>, ApiError>> {
    const url = '/v1/pages';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.ContentPagesPage>, ApiError>;
  }

  /**
   */
  async postPages(body: Types.ContentPagesCreatePage): Promise<Result<Types.ContentPagesPage, ApiError>> {
    const url = '/v1/pages';

    // Validate request body
    const validatedBody = safeParse(Types.ContentPagesCreatePageSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ContentPagesPageSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getPagesBySlug(slug: string): Promise<Result<Types.ContentPagesPage, ApiError>> {
    const url = `/v1/pages/by-slug/${slug}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: false,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ContentPagesPageSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getPagesSitemap(query?: { locale?: string }): Promise<Result<Array<Types.ContentPagesSitemapEntry>, ApiError>> {
    const url = '/v1/pages/sitemap';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<Array<Types.ContentPagesSitemapEntry>, ApiError>;
  }

  /**
   */
  async getPagesById(id: string): Promise<Result<Types.ContentPagesPage, ApiError>> {
    const url = `/v1/pages/${id}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ContentPagesPageSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putPages(id: string, body: Types.ContentPagesUpdatePage): Promise<Result<Types.ContentPagesPage, ApiError>> {
    const url = `/v1/pages/${id}`;

    // Validate request body
    const validatedBody = safeParse(Types.ContentPagesUpdatePageSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ContentPagesPageSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deletePages(id: string): Promise<Result<void, ApiError>> {
    const url = `/v1/pages/${id}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postPagesPublish(id: string): Promise<Result<Types.ContentPagesPage, ApiError>> {
    const url = `/v1/pages/${id}/publish`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ContentPagesPageSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postPagesUnpublish(id: string): Promise<Result<Types.ContentPagesPage, ApiError>> {
    const url = `/v1/pages/${id}/unpublish`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ContentPagesPageSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getPagesByPageIdSections(pageId: string): Promise<Result<Array<Types.ContentPagesPageSection>, ApiError>> {
    const url = `/v1/pages/${pageId}/sections`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.ContentPagesPageSection>, ApiError>;
  }

  /**
   */
  async postPagesSections(pageId: string, body: Types.ContentPagesCreatePageSection): Promise<Result<Types.ContentPagesPageSection, ApiError>> {
    const url = `/v1/pages/${pageId}/sections`;

    // Validate request body
    const validatedBody = safeParse(Types.ContentPagesCreatePageSectionSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ContentPagesPageSectionSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postPagesSectionsReorder(pageId: string, body: Array<string>): Promise<Result<void, ApiError>> {
    const url = `/v1/pages/${pageId}/sections/reorder`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: body,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getPagesByPageIdSectionsBySectionId(pageId: string, sectionId: string): Promise<Result<Types.ContentPagesPageSection, ApiError>> {
    const url = `/v1/pages/${pageId}/sections/${sectionId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ContentPagesPageSectionSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putPagesSections(
    pageId: string,
    sectionId: string,
    body: Types.ContentPagesUpdatePageSection,
  ): Promise<Result<Types.ContentPagesPageSection, ApiError>> {
    const url = `/v1/pages/${pageId}/sections/${sectionId}`;

    // Validate request body
    const validatedBody = safeParse(Types.ContentPagesUpdatePageSectionSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.ContentPagesPageSectionSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deletePagesSections(pageId: string, sectionId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/pages/${pageId}/sections/${sectionId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createContentPagesModule(client: ApiClient): ContentPagesModule {
  return new ContentPagesModule(client);
}
