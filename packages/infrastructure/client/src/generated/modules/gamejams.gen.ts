/**
 * @game-guild/client - Gamejams Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class GamejamsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getApiGameJams(query?: { status?: Types.GameJamsJamStatus; skip?: number; take?: number }): Promise<Result<Array<Types.GameJamsJamDto>, ApiError>> {
    const url = '/api/game-jams';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.GameJamsJamDto>, ApiError>;
  }

  /**
   */
  async postApiGameJams(body: Types.GameJamsCreateJamInput): Promise<Result<Types.GameJamsJamDto, ApiError>> {
    const url = '/api/game-jams';

    // Validate request body
    const validatedBody = safeParse(Types.GameJamsCreateJamInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.GameJamsJamDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getApiGameJamsById(id: string): Promise<Result<void, ApiError>> {
    const url = `/api/game-jams/${id}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postApiGameJamsStatus(id: string, status: Types.GameJamsJamStatus): Promise<Result<void, ApiError>> {
    const url = `/api/game-jams/${id}/status/${status}`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getApiGameJamsSubmissions(id: string): Promise<Result<Array<Types.GameJamsJamSubmission>, ApiError>> {
    const url = `/api/game-jams/${id}/submissions`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.GameJamsJamSubmission>, ApiError>;
  }

  /**
   */
  async postApiGameJamsSubmissions(id: string, body: Types.GameJamsSubmitJamEntryInput): Promise<Result<Types.GameJamsJamSubmission, ApiError>> {
    const url = `/api/game-jams/${id}/submissions`;

    // Validate request body
    const validatedBody = safeParse(Types.GameJamsSubmitJamEntryInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.GameJamsJamSubmissionSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getApiGameJamsCriteria(id: string): Promise<Result<Array<Types.GameJamsJamCriteria>, ApiError>> {
    const url = `/api/game-jams/${id}/criteria`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.GameJamsJamCriteria>, ApiError>;
  }

  /**
   */
  async postApiGameJamsCriteria(id: string, body: Types.GameJamsAddJamCriteriaInput): Promise<Result<Types.GameJamsJamCriteria, ApiError>> {
    const url = `/api/game-jams/${id}/criteria`;

    // Validate request body
    const validatedBody = safeParse(Types.GameJamsAddJamCriteriaInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.GameJamsJamCriteriaSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postApiGameJamsSubmissionsScores(
    submissionId: string,
    body: Types.GameJamsScoreJamSubmissionInput,
  ): Promise<Result<Types.GameJamsJamScoreDto, ApiError>> {
    const url = `/api/game-jams/submissions/${submissionId}/scores`;

    // Validate request body
    const validatedBody = safeParse(Types.GameJamsScoreJamSubmissionInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.GameJamsJamScoreDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createGamejamsModule(client: ApiClient): GamejamsModule {
  return new GamejamsModule(client);
}
