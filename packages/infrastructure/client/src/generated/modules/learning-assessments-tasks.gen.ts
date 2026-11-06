/**
 * @game-guild/client - LearningAssessmentsTasks Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client';
import type { Result } from '../../runtime/result/types';
import type { ApiError } from '../../runtime/errors/types';
import * as Types from '../types.gen';
import { safeParse } from '../../runtime/errors/validation';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class LearningAssessmentsTasksModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getMeTasks(): Promise<Result<Types.LearningAssessmentsTasksDto, ApiError>> {
    const url = '/v1/me/tasks';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningAssessmentsTasksDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createLearningAssessmentsTasksModule(client: ApiClient): LearningAssessmentsTasksModule {
  return new LearningAssessmentsTasksModule(client);
}
