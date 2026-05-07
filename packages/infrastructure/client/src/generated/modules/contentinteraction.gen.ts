/**
 * @game-guild/client - Contentinteraction Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class ContentinteractionModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async postCourseInteractions(
    query?: { programId?: string },
    body: Types.LearningCoursesStartContentInput,
  ): Promise<Result<Types.LearningCoursesContentInteraction, ApiError>> {
    const url = '/v1/course-interactions';

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesStartContentInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      params: query,
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
  async putCourseInteractionsProgress(
    interactionId: string,
    query?: { programId?: string },
    body: Types.LearningCoursesUpdateProgressInput,
  ): Promise<Result<Types.LearningCoursesContentInteraction, ApiError>> {
    const url = `/v1/course-interactions/${interactionId}/progress`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesUpdateProgressInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      params: query,
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
  async postCourseInteractionsSubmit(
    interactionId: string,
    query?: { programId?: string },
    body: Types.LearningCoursesSubmitContentInput,
  ): Promise<Result<Types.LearningCoursesContentInteraction, ApiError>> {
    const url = `/v1/course-interactions/${interactionId}/submit`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesSubmitContentInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      params: query,
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
  async postCourseInteractionsComplete(
    interactionId: string,
    query?: { programId?: string },
    body: Types.LearningCoursesCompleteContentInput,
  ): Promise<Result<Types.LearningCoursesContentInteraction, ApiError>> {
    const url = `/v1/course-interactions/${interactionId}/complete`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesCompleteContentInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      params: query,
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
  async getCourseInteractionsUserContent(
    programUserId: string,
    contentId: string,
    query?: { programId?: string },
  ): Promise<Result<Types.LearningCoursesContentInteraction, ApiError>> {
    const url = `/v1/course-interactions/user/${programUserId}/content/${contentId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
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
  async getCourseInteractionsUser(
    programUserId: string,
    query?: { programId?: string },
  ): Promise<Result<Array<Types.LearningCoursesContentInteraction>, ApiError>> {
    const url = `/v1/course-interactions/user/${programUserId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.LearningCoursesContentInteraction>, ApiError>;
  }

  /**
   */
  async putCourseInteractionsTimeSpent(
    interactionId: string,
    query?: { programId?: string },
    body: Types.LearningCoursesUpdateTimeSpentInput,
  ): Promise<Result<Types.LearningCoursesContentInteraction, ApiError>> {
    const url = `/v1/course-interactions/${interactionId}/time-spent`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesUpdateTimeSpentInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      params: query,
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
}

export function createContentinteractionModule(client: ApiClient): ContentinteractionModule {
  return new ContentinteractionModule(client);
}
