/**
 * @game-guild/client - LearningCoursesLessonInteractionEvents Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client';
import type { Result } from '../../runtime/result/types';
import type { ApiError } from '../../runtime/errors/types';
import * as Types from '../types.gen';
import { safeParse } from '../../runtime/errors/validation';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class LearningCoursesLessonInteractionEventsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getCoursesInteractionsEvents(programId: string, interactionId: string): Promise<Result<Types.LearningCoursesContentInteractionEventDto[], ApiError>> {
    const url = `/v1/courses/${programId}/interactions/${interactionId}/events`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Types.LearningCoursesContentInteractionEventDto[], ApiError>;
  }

  /**
   */
  async postCoursesInteractionsEvents(
    programId: string,
    interactionId: string,
    body?: Types.LearningCoursesRecordContentInteractionEventInput,
  ): Promise<Result<Types.LearningCoursesContentInteractionEventDto, ApiError>> {
    const url = `/v1/courses/${programId}/interactions/${interactionId}/events`;

    // Validate request body
    const validatedBody = body === undefined ? undefined : safeParse(Types.LearningCoursesRecordContentInteractionEventInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesContentInteractionEventDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createLearningCoursesLessonInteractionEventsModule(client: ApiClient): LearningCoursesLessonInteractionEventsModule {
  return new LearningCoursesLessonInteractionEventsModule(client);
}
