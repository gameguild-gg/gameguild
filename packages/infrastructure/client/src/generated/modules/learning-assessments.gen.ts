/**
 * @game-guild/client - LearningAssessments Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class LearningAssessmentsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async postAssessments(body: Types.LearningAssessmentsCreateAssessmentInput): Promise<Result<Types.LearningAssessmentsAssessment, ApiError>> {
    const url = '/v1/assessments';

    // Validate request body
    const validatedBody = safeParse(Types.LearningAssessmentsCreateAssessmentInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningAssessmentsAssessmentSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getAssessments(id: string): Promise<Result<Types.LearningAssessmentsAssessment, ApiError>> {
    const url = `/v1/assessments/${id}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningAssessmentsAssessmentSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putAssessments(id: string, body: Types.LearningAssessmentsUpdateAssessmentInput): Promise<Result<Types.LearningAssessmentsAssessment, ApiError>> {
    const url = `/v1/assessments/${id}`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningAssessmentsUpdateAssessmentInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningAssessmentsAssessmentSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteAssessments(id: string): Promise<Result<void, ApiError>> {
    const url = `/v1/assessments/${id}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getAssessmentsCourse(courseId: string): Promise<Result<Array<Types.LearningAssessmentsAssessment>, ApiError>> {
    const url = `/v1/assessments/course/${courseId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.LearningAssessmentsAssessment>, ApiError>;
  }

  /**
   */
  async getAssessmentsCourseGroups(courseId: string): Promise<Result<Array<Types.LearningAssessmentsAssessmentGroup>, ApiError>> {
    const url = `/v1/assessments/course/${courseId}/groups`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.LearningAssessmentsAssessmentGroup>, ApiError>;
  }

  /**
   */
  async getAssessmentsCourseAnalytics(courseId: string): Promise<Result<Types.LearningAssessmentsCourseAssessmentAnalytics, ApiError>> {
    const url = `/v1/assessments/course/${courseId}/analytics`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningAssessmentsCourseAssessmentAnalyticsSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAssessmentsGroups(body: Types.LearningAssessmentsCreateAssessmentGroupInput): Promise<Result<Types.LearningAssessmentsAssessmentGroup, ApiError>> {
    const url = '/v1/assessments/groups';

    // Validate request body
    const validatedBody = safeParse(Types.LearningAssessmentsCreateAssessmentGroupInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningAssessmentsAssessmentGroupSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putAssessmentsGroups(
    id: string,
    body: Types.LearningAssessmentsUpdateAssessmentGroupInput,
  ): Promise<Result<Types.LearningAssessmentsAssessmentGroup, ApiError>> {
    const url = `/v1/assessments/groups/${id}`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningAssessmentsUpdateAssessmentGroupInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningAssessmentsAssessmentGroupSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteAssessmentsGroups(id: string): Promise<Result<void, ApiError>> {
    const url = `/v1/assessments/groups/${id}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async putAssessmentsGroup(
    id: string,
    body: Types.LearningAssessmentsAssignAssessmentGroupInput,
  ): Promise<Result<Types.LearningAssessmentsAssessment, ApiError>> {
    const url = `/v1/assessments/${id}/group`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningAssessmentsAssignAssessmentGroupInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningAssessmentsAssessmentSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAssessmentsSubmissionsStart(
    assessmentId: string,
    body: Types.LearningAssessmentsStartSubmissionInput,
  ): Promise<Result<Types.LearningAssessmentsAssessmentSubmission, ApiError>> {
    const url = `/v1/assessments/${assessmentId}/submissions/start`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningAssessmentsStartSubmissionInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningAssessmentsAssessmentSubmissionSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAssessmentsSubmissionsSubmit(submissionId: string): Promise<Result<Types.LearningAssessmentsAssessmentSubmission, ApiError>> {
    const url = `/v1/assessments/submissions/${submissionId}/submit`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningAssessmentsAssessmentSubmissionSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAssessmentsSubmissionsGrade(
    submissionId: string,
    body: Types.LearningAssessmentsGradeSubmissionInput,
  ): Promise<Result<Types.LearningAssessmentsAssessmentSubmission, ApiError>> {
    const url = `/v1/assessments/submissions/${submissionId}/grade`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningAssessmentsGradeSubmissionInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningAssessmentsAssessmentSubmissionSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getAssessmentsSubmissions(submissionId: string): Promise<Result<Types.LearningAssessmentsAssessmentSubmission, ApiError>> {
    const url = `/v1/assessments/submissions/${submissionId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningAssessmentsAssessmentSubmissionSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getAssessmentsSubmissions1(assessmentId: string): Promise<Result<Array<Types.LearningAssessmentsAssessmentSubmission>, ApiError>> {
    const url = `/v1/assessments/${assessmentId}/submissions`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.LearningAssessmentsAssessmentSubmission>, ApiError>;
  }

  /**
   */
  async getAssessmentsMySubmissions(enrollmentId: string): Promise<Result<Array<Types.LearningAssessmentsAssessmentSubmission>, ApiError>> {
    const url = `/v1/assessments/my-submissions/${enrollmentId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.LearningAssessmentsAssessmentSubmission>, ApiError>;
  }

  /**
   */
  async getAssessmentsCanAttempt(assessmentId: string, enrollmentId: string): Promise<Result<Types.LearningAssessmentsCanAttemptOutput, ApiError>> {
    const url = `/v1/assessments/${assessmentId}/can-attempt/${enrollmentId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningAssessmentsCanAttemptOutputSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createLearningAssessmentsModule(client: ApiClient): LearningAssessmentsModule {
  return new LearningAssessmentsModule(client);
}
