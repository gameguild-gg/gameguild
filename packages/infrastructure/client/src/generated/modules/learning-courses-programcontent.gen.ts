/**
 * @game-guild/client - LearningCoursesProgramcontent Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class LearningCoursesProgramcontentModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getCoursesContent(programId: string, query?: { level?: string }): Promise<Result<Array<Types.LearningCoursesProgramContent>, ApiError>> {
    const url = `/v1/courses/${programId}/content`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<Array<Types.LearningCoursesProgramContent>, ApiError>;
  }

  /**
   */
  async postCoursesContent(programId: string, body: Types.LearningCoursesCreateProgramContent): Promise<Result<Types.LearningCoursesProgramContent, ApiError>> {
    const url = `/v1/courses/${programId}/content`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesCreateProgramContentSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesProgramContentSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getCoursesContentByType(
    programId: string,
    type: Types.LearningCoursesProgramContentType,
  ): Promise<Result<Array<Types.LearningCoursesProgramContent>, ApiError>> {
    const url = `/v1/courses/${programId}/content/by-type/${type}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.LearningCoursesProgramContent>, ApiError>;
  }

  /**
   */
  async getCoursesContentByVisibility(
    programId: string,
    visibility: Types.LearningCoursesVisibility,
  ): Promise<Result<Array<Types.LearningCoursesProgramContent>, ApiError>> {
    const url = `/v1/courses/${programId}/content/by-visibility/${visibility}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.LearningCoursesProgramContent>, ApiError>;
  }

  /**
   */
  async postCoursesContentReorder(programId: string, body: Types.LearningCoursesReorderContent): Promise<Result<void, ApiError>> {
    const url = `/v1/courses/${programId}/content/reorder`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesReorderContentSchema, body, 'request');

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
  async getCoursesContentRequired(programId: string): Promise<Result<Array<Types.LearningCoursesProgramContent>, ApiError>> {
    const url = `/v1/courses/${programId}/content/required`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.LearningCoursesProgramContent>, ApiError>;
  }

  /**
   */
  async postCoursesContentSearch(
    programId: string,
    body: Types.LearningCoursesSearchContent,
  ): Promise<Result<Array<Types.LearningCoursesProgramContent>, ApiError>> {
    const url = `/v1/courses/${programId}/content/search`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesSearchContentSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<Array<Types.LearningCoursesProgramContent>, ApiError>;
  }

  /**
   */
  async getCoursesContentStats(programId: string): Promise<Result<Types.LearningCoursesContentStats, ApiError>> {
    const url = `/v1/courses/${programId}/content/stats`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesContentStatsSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getCoursesContent1(programId: string, id: string): Promise<Result<Types.LearningCoursesProgramContent, ApiError>> {
    const url = `/v1/courses/${programId}/content/${id}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: false,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesProgramContentSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putCoursesContent(
    programId: string,
    id: string,
    body: Types.LearningCoursesUpdateProgramContent,
  ): Promise<Result<Types.LearningCoursesProgramContent, ApiError>> {
    const url = `/v1/courses/${programId}/content/${id}`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesUpdateProgramContentSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesProgramContentSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteCoursesContent(programId: string, id: string): Promise<Result<void, ApiError>> {
    const url = `/v1/courses/${programId}/content/${id}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postCoursesContentMove(programId: string, id: string, body: Types.LearningCoursesMoveContent): Promise<Result<void, ApiError>> {
    const url = `/v1/courses/${programId}/content/${id}/move`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesMoveContentSchema, body, 'request');

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
  async postCoursesContentSubmit(
    programId: string,
    id: string,
    body: Types.LearningCoursesSubmitUserContent,
  ): Promise<Result<Types.LearningCoursesContentInteraction, ApiError>> {
    const url = `/v1/courses/${programId}/content/${id}/submit`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesSubmitUserContentSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesContentInteractionSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getCoursesContentChildren(programId: string, parentId: string): Promise<Result<Array<Types.LearningCoursesProgramContent>, ApiError>> {
    const url = `/v1/courses/${programId}/content/${parentId}/children`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.LearningCoursesProgramContent>, ApiError>;
  }
}

export function createLearningCoursesProgramcontentModule(client: ApiClient): LearningCoursesProgramcontentModule {
  return new LearningCoursesProgramcontentModule(client);
}
