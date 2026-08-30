/**
 * @game-guild/client - LearningCoursesProgram Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client.js';
import type { Result } from '../../runtime/result/types.js';
import type { ApiError } from '../../runtime/errors/types.js';
import * as Types from '../types.gen.js';
import { safeParse } from '../../runtime/errors/validation.js';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class LearningCoursesProgramModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getCoursesForGetCourses(query?: {
    status?: string;
    category?: Types.ProgramCategory;
    difficulty?: Types.LearningCoursesProgramDifficulty;
    creatorId?: string;
    q?: string;
    sort?: string;
    skip?: number;
    take?: number;
  }): Promise<Result<Array<Types.LearningCoursesProgram>, ApiError>> {
    const url = '/v1/courses';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.LearningCoursesProgram>, ApiError>;
  }

  /**
   */
  async postCourses(body: Types.LearningCoursesCreateProgram): Promise<Result<Types.LearningCoursesProgram, ApiError>> {
    const url = '/v1/courses';

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesCreateProgramSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesProgramSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getCoursesForGetCoursesById(id: string): Promise<Result<Types.LearningCoursesProgram, ApiError>> {
    const url = `/v1/courses/${id}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesProgramSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putCourses(id: string, body: Types.LearningCoursesUpdateProgram): Promise<Result<Types.LearningCoursesProgram, ApiError>> {
    const url = `/v1/courses/${id}`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesUpdateProgramSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesProgramSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteCourses(id: string): Promise<Result<void, ApiError>> {
    const url = `/v1/courses/${id}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postCoursesClone(id: string, body: Types.LearningCoursesCloneProgram): Promise<Result<Types.LearningCoursesProgram, ApiError>> {
    const url = `/v1/courses/${id}:clone`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesCloneProgramSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesProgramSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postCoursesCreateProduct(id: string, body: Types.LearningCoursesCreateProductFromProgram): Promise<Result<string, ApiError>> {
    const url = `/v1/courses/${id}:create-product`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesCreateProductFromProgramSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<string, ApiError>;
  }

  /**
   */
  async postCoursesDisableMonetization(id: string): Promise<Result<Types.LearningCoursesProgram, ApiError>> {
    const url = `/v1/courses/${id}:disable-monetization`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesProgramSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postCoursesLinkProduct(id: string, productId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/courses/${id}:link-product/${productId}`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postCoursesMonetize(id: string, body: Types.LearningCoursesMonetization): Promise<Result<Types.LearningCoursesProgram, ApiError>> {
    const url = `/v1/courses/${id}:monetize`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesMonetizationSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesProgramSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postCoursesSelfEnroll(id: string): Promise<Result<Types.LearningCoursesUserProgress, ApiError>> {
    const url = `/v1/courses/${id}:self-enroll`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesUserProgressSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteCoursesUnlinkProduct(id: string, productId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/courses/${id}:unlink-product/${productId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getCoursesAnalytics(id: string): Promise<Result<Types.LearningCoursesProgramAnalytics, ApiError>> {
    const url = `/v1/courses/${id}/analytics`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesProgramAnalyticsSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getCoursesAnalyticsCompletionRates(id: string): Promise<Result<Types.LearningCoursesCompletionRates, ApiError>> {
    const url = `/v1/courses/${id}/analytics/completion-rates`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesCompletionRatesSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getCoursesAnalyticsEngagement(id: string): Promise<Result<Types.LearningCoursesEngagementMetrics, ApiError>> {
    const url = `/v1/courses/${id}/analytics/engagement`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesEngagementMetricsSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getCoursesAnalyticsRevenue(id: string): Promise<Result<Types.LearningCoursesRevenueAnalytics, ApiError>> {
    const url = `/v1/courses/${id}/analytics/revenue`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesRevenueAnalyticsSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postCoursesMeContentComplete(id: string, contentId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/courses/${id}/me/content/${contentId}:complete`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getCoursesMeProgress(id: string): Promise<Result<Types.LearningCoursesUserProgress, ApiError>> {
    const url = `/v1/courses/${id}/me/progress`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesUserProgressSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putCoursesMeProgress(id: string, body: Types.LearningCoursesUpdateProgress): Promise<Result<Types.LearningCoursesUserProgress, ApiError>> {
    const url = `/v1/courses/${id}/me/progress`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesUpdateProgressSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesUserProgressSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getCoursesPricing(id: string): Promise<Result<Types.LearningCoursesPricing, ApiError>> {
    const url = `/v1/courses/${id}/pricing`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesPricingSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putCoursesPricing(id: string, body: Types.LearningCoursesUpdatePricing): Promise<Result<Types.LearningCoursesPricing, ApiError>> {
    const url = `/v1/courses/${id}/pricing`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesUpdatePricingSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesPricingSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getCoursesProducts(id: string): Promise<Result<Array<string>, ApiError>> {
    const url = `/v1/courses/${id}/products`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<string>, ApiError>;
  }

  /**
   */
  async getCoursesUsers(id: string, query?: { skip?: number; take?: number }): Promise<Result<Array<Types.LearningCoursesUserProgress>, ApiError>> {
    const url = `/v1/courses/${id}/users`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.LearningCoursesUserProgress>, ApiError>;
  }

  /**
   */
  async postCoursesUsers(id: string, userId: string): Promise<Result<Types.LearningCoursesUserProgress, ApiError>> {
    const url = `/v1/courses/${id}/users/${userId}`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesUserProgressSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteCoursesUsers(id: string, userId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/courses/${id}/users/${userId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postCoursesUsersReset(id: string, userId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/courses/${id}/users/${userId}:reset`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postCoursesUsersContentComplete(id: string, userId: string, contentId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/courses/${id}/users/${userId}/content/${contentId}:complete`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getCoursesUsersProgress(id: string, userId: string): Promise<Result<Types.LearningCoursesUserProgress, ApiError>> {
    const url = `/v1/courses/${id}/users/${userId}/progress`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesUserProgressSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putCoursesUsersProgress(
    id: string,
    userId: string,
    body: Types.LearningCoursesUpdateProgress,
  ): Promise<Result<Types.LearningCoursesUserProgress, ApiError>> {
    const url = `/v1/courses/${id}/users/${userId}/progress`;

    // Validate request body
    const validatedBody = safeParse(Types.LearningCoursesUpdateProgressSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesUserProgressSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getCoursesWithContent(id: string): Promise<Result<Types.LearningCoursesProgram, ApiError>> {
    const url = `/v1/courses/${id}/with-content`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesProgramSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getCoursesMe(): Promise<Result<Array<Types.LearningCoursesProgram>, ApiError>> {
    const url = '/v1/courses/me';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    return result as Result<Array<Types.LearningCoursesProgram>, ApiError>;
  }

  /**
   */
  async getCoursesPublic(query?: { skip?: number; take?: number }): Promise<Result<Array<Types.LearningCoursesProgram>, ApiError>> {
    const url = '/v1/courses/public';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.LearningCoursesProgram>, ApiError>;
  }

  /**
   */
  async getCoursesSlug(slug: string): Promise<Result<Types.LearningCoursesProgram, ApiError>> {
    const url = `/v1/courses/slug/${slug}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.LearningCoursesProgramSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createLearningCoursesProgramModule(client: ApiClient): LearningCoursesProgramModule {
  return new LearningCoursesProgramModule(client);
}
