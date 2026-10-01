/**
 * @game-guild/client - LearningCoursesProgramLifecycle Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class LearningCoursesProgramLifecycleModule {
  constructor(private readonly client: ApiClient) {}

  /**
   * Approve a program (resource-level approve permission)
   */
  async postCoursesApprove(id: string): Promise<Result<Types.LearningCoursesProgramDto, ApiError>> {
    const url = `/v1/courses/${id}:approve`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesProgramDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Archive a program (resource-level archive permission)
   */
  async postCoursesArchive(id: string): Promise<Result<Types.LearningCoursesProgramDto, ApiError>> {
    const url = `/v1/courses/${id}:archive`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesProgramDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Publish a program (resource-level publish permission)
   */
  async postCoursesPublish(id: string): Promise<Result<Types.LearningCoursesProgramDto, ApiError>> {
    const url = `/v1/courses/${id}:publish`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesProgramDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Reject a program (resource-level reject permission)
   */
  async postCoursesReject(id: string, body: Types.LearningCoursesRejectProgramDto): Promise<Result<Types.LearningCoursesProgramDto, ApiError>> {
    const url = `/v1/courses/${id}:reject`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesRejectProgramDtoSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesProgramDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Restore an archived program (resource-level restore permission)
   */
  async postCoursesRestore(id: string): Promise<Result<Types.LearningCoursesProgramDto, ApiError>> {
    const url = `/v1/courses/${id}:restore`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesProgramDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Schedule a program for publishing (resource-level schedule permission)
   */
  async postCoursesSchedule(id: string, body: Types.LearningCoursesScheduleProgramDto): Promise<Result<Types.LearningCoursesProgramDto, ApiError>> {
    const url = `/v1/courses/${id}:schedule`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesScheduleProgramDtoSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesProgramDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Submit a program for review (resource-level submit permission)
   */
  async postCoursesSubmit(id: string): Promise<Result<Types.LearningCoursesProgramDto, ApiError>> {
    const url = `/v1/courses/${id}:submit`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesProgramDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Unpublish a program (resource-level unpublish permission)
   */
  async postCoursesUnpublish(id: string): Promise<Result<Types.LearningCoursesProgramDto, ApiError>> {
    const url = `/v1/courses/${id}:unpublish`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesProgramDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   * Withdraw a program from review (resource-level withdraw permission)
   */
  async postCoursesWithdraw(id: string): Promise<Result<Types.LearningCoursesProgramDto, ApiError>> {
    const url = `/v1/courses/${id}:withdraw`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesProgramDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createLearningCoursesProgramLifecycleModule(client: ApiClient): LearningCoursesProgramLifecycleModule {
  return new LearningCoursesProgramLifecycleModule(client);
}
