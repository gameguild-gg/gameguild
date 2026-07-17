/**
 * @game-guild/client - LearningCoursesPrerequisites Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class LearningCoursesPrerequisitesModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async postApiPrerequisites(body: Types.LearningCoursesCreatePrerequisiteApiInput): Promise<Result<Types.LearningCoursesPrerequisite, ApiError>> {
    const url = '/api/prerequisites';

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesCreatePrerequisiteApiInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesPrerequisiteSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getApiPrerequisitesCourse(courseId: string): Promise<Result<Array<Types.LearningCoursesPrerequisite>, ApiError>> {
    const url = `/api/prerequisites/course/${courseId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.LearningCoursesPrerequisite>, ApiError>;
  }

  /**
   */
  async getApiPrerequisitesCourseChain(courseId: string): Promise<Result<Array<Types.LearningCoursesPrerequisite>, ApiError>> {
    const url = `/api/prerequisites/course/${courseId}/chain`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.LearningCoursesPrerequisite>, ApiError>;
  }

  /**
   */
  async getApiPrerequisitesCourseCheck(courseId: string): Promise<Result<Types.LearningCoursesPrerequisiteCheckResult, ApiError>> {
    const url = `/api/prerequisites/course/${courseId}/check`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesPrerequisiteCheckResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getApiPrerequisitesCourseCheck1(courseId: string, userId: string): Promise<Result<Types.LearningCoursesPrerequisiteCheckResult, ApiError>> {
    const url = `/api/prerequisites/course/${courseId}/check/${userId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesPrerequisiteCheckResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postApiPrerequisitesCourseReorder(courseId: string, body: Types.LearningCoursesReorderPrerequisitesInput): Promise<Result<void, ApiError>> {
    const url = `/api/prerequisites/course/${courseId}/reorder`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesReorderPrerequisitesInputSchema, body, 'request');

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
  async getApiPrerequisitesCourseWouldCreateCycle(
    courseId: string,
    prerequisiteCourseId: string,
  ): Promise<Result<Types.LearningCoursesCircularDependencyCheckResult, ApiError>> {
    const url = `/api/prerequisites/course/${courseId}/would-create-cycle/${prerequisiteCourseId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesCircularDependencyCheckResultSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getApiPrerequisitesDependents(courseId: string): Promise<Result<Array<Types.LearningCoursesPrerequisite>, ApiError>> {
    const url = `/api/prerequisites/dependents/${courseId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.LearningCoursesPrerequisite>, ApiError>;
  }

  /**
   */
  async getApiPrerequisites(id: string): Promise<Result<Types.LearningCoursesPrerequisite, ApiError>> {
    const url = `/api/prerequisites/${id}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesPrerequisiteSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putApiPrerequisites(id: string, body: Types.LearningCoursesUpdatePrerequisiteApiInput): Promise<Result<Types.LearningCoursesPrerequisite, ApiError>> {
    const url = `/api/prerequisites/${id}`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesUpdatePrerequisiteApiInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesPrerequisiteSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteApiPrerequisites(id: string): Promise<Result<void, ApiError>> {
    const url = `/api/prerequisites/${id}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createLearningCoursesPrerequisitesModule(client: ApiClient): LearningCoursesPrerequisitesModule {
  return new LearningCoursesPrerequisitesModule(client);
}
