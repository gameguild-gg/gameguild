/**
 * @game-guild/client - LearningCoursesStudents Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class LearningCoursesStudentsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async postCoursesStudentsMessage(
    courseId: string,
    body: Types.LearningCoursesSendCourseStudentMessageInput,
  ): Promise<Result<Types.LearningCoursesSendCourseStudentMessageOutput, ApiError>> {
    const url = `/v1/courses/${courseId}/students/message`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesSendCourseStudentMessageInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesSendCourseStudentMessageOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createLearningCoursesStudentsModule(client: ApiClient): LearningCoursesStudentsModule {
  return new LearningCoursesStudentsModule(client);
}
