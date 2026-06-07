/**
 * @game-guild/client - Tags Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class TagsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getApiTags(query?: {
    search?: string;
    type?: Types.TagsTagType;
    tenantId?: string;
    includeInactive?: boolean;
    skip?: number;
    take?: number;
  }): Promise<Result<Array<Types.TagsTag>, ApiError>> {
    const url = '/api/tags';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.TagsTag>, ApiError>;
  }

  /**
   */
  async postApiTags(body: Types.TagsCreateTagInput): Promise<Result<Types.TagsTag, ApiError>> {
    const url = '/api/tags';

    // Validate request body
    const validatedBody = safeParse(Types.TagsCreateTagInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.TagsTagSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getApiTags1(id: string): Promise<Result<void, ApiError>> {
    const url = `/api/tags/${id}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async patchApiTags(id: string, body: Types.TagsUpdateTagInput): Promise<Result<void, ApiError>> {
    const url = `/api/tags/${id}`;

    // Validate request body
    const validatedBody = safeParse(Types.TagsUpdateTagInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PATCH',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getApiTagsRelationships(id: string): Promise<Result<Array<Types.TagsTagRelationship>, ApiError>> {
    const url = `/api/tags/${id}/relationships`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.TagsTagRelationship>, ApiError>;
  }

  /**
   */
  async postApiTagsRelationships(body: Types.TagsCreateTagRelationshipInput): Promise<Result<Types.TagsTagRelationship, ApiError>> {
    const url = '/api/tags/relationships';

    // Validate request body
    const validatedBody = safeParse(Types.TagsCreateTagRelationshipInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.TagsTagRelationshipSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getApiTagsProficiencies(query?: {
    type?: Types.TagsTagType;
    level?: Types.TagsSkillProficiencyLevel;
    includeInactive?: boolean;
  }): Promise<Result<Array<Types.TagsTagProficiency>, ApiError>> {
    const url = '/api/tags/proficiencies';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.TagsTagProficiency>, ApiError>;
  }

  /**
   */
  async postApiTagsProficiencies(body: Types.TagsCreateTagProficiencyInput): Promise<Result<Types.TagsTagProficiency, ApiError>> {
    const url = '/api/tags/proficiencies';

    // Validate request body
    const validatedBody = safeParse(Types.TagsCreateTagProficiencyInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.TagsTagProficiencySchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createTagsModule(client: ApiClient): TagsModule {
  return new TagsModule(client);
}
