/**
 * @game-guild/client - SocialMentorship Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from '../../runtime/client';
import type { Result } from '../../runtime/result/types';
import type { ApiError } from '../../runtime/errors/types';
import * as Types from '../types.gen';
import { safeParse } from '../../runtime/errors/validation';

/* eslint-disable @typescript-eslint/no-explicit-any */

export class SocialMentorshipModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getMentorshipMentors(query?: {
    skill?: string;
    role?: Types.SocialMentorshipMentorshipProgramRole;
    search?: string;
    offersIntroductoryCall?: boolean;
    skip?: number;
    take?: number;
  }): Promise<Result<Types.SocialMentorshipMentorProfileDto[], ApiError>> {
    const url = '/v1/mentorship/mentors';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<Types.SocialMentorshipMentorProfileDto[], ApiError>;
  }

  /**
   */
  async getMentorProfileById(profileId: string): Promise<Result<Types.SocialMentorshipMentorProfileDto, ApiError>> {
    const url = `/v1/mentorship/mentors/${profileId}`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: false,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialMentorshipMentorProfileDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getMentorshipMentorsAvailability(
    profileId: string,
    query?: { fromUtc?: string; toUtc?: string },
  ): Promise<Result<Types.SocialMentorshipMentorAvailabilitySlotDto[], ApiError>> {
    const url = `/v1/mentorship/mentors/${profileId}/availability`;

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<Types.SocialMentorshipMentorAvailabilitySlotDto[], ApiError>;
  }

  /**
   */
  async getMentorshipProfile(): Promise<Result<Types.SocialMentorshipMentorProfileDto, ApiError>> {
    const url = '/v1/mentorship/profile';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialMentorshipMentorProfileDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putMentorshipProfile(body: Types.SocialMentorshipUpsertMentorProfileInput): Promise<Result<Types.SocialMentorshipMentorProfileDto, ApiError>> {
    const url = '/v1/mentorship/profile';

    // Validate request body
    const validatedBody = safeParse(Types.SocialMentorshipUpsertMentorProfileInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'PUT',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialMentorshipMentorProfileDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postMentorshipProfileAvailability(
    body: Types.SocialMentorshipPublishMentorAvailabilityInput,
  ): Promise<Result<Types.SocialMentorshipMentorAvailabilitySlotDto[], ApiError>> {
    const url = '/v1/mentorship/profile/availability';

    // Validate request body
    const validatedBody = safeParse(Types.SocialMentorshipPublishMentorAvailabilityInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<Types.SocialMentorshipMentorAvailabilitySlotDto[], ApiError>;
  }

  /**
   */
  async deleteMentorshipProfileAvailability(slotId: string): Promise<Result<void, ApiError>> {
    const url = `/v1/mentorship/profile/availability/${slotId}`;

    const result = await this.client.request({
      method: 'DELETE',
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getMentorshipProfileDashboard(): Promise<Result<Types.SocialMentorshipMentorDashboardDto, ApiError>> {
    const url = '/v1/mentorship/profile/dashboard';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialMentorshipMentorDashboardDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postMentorshipProfileStatus(
    body: Types.SocialMentorshipSetMentorProfileStatusInput,
  ): Promise<Result<Types.SocialMentorshipMentorProfileDto, ApiError>> {
    const url = '/v1/mentorship/profile/status';

    // Validate request body
    const validatedBody = safeParse(Types.SocialMentorshipSetMentorProfileStatusInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialMentorshipMentorProfileDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getMentorshipSessions(query?: {
    fromUtc?: string;
    toUtc?: string;
    status?: Types.SocialMentorshipMentorshipSessionStatus;
    asMentorOnly?: boolean;
  }): Promise<Result<Types.SocialMentorshipMentorshipSessionDto[], ApiError>> {
    const url = '/v1/mentorship/sessions';

    const result = await this.client.request({
      method: 'GET',
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Types.SocialMentorshipMentorshipSessionDto[], ApiError>;
  }

  /**
   */
  async postMentorshipSessions(body: Types.SocialMentorshipBookMentorshipSessionInput): Promise<Result<Types.SocialMentorshipMentorshipSessionDto, ApiError>> {
    const url = '/v1/mentorship/sessions';

    // Validate request body
    const validatedBody = safeParse(Types.SocialMentorshipBookMentorshipSessionInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialMentorshipMentorshipSessionDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postMentorshipSessionsCancel(
    sessionId: string,
    body: Types.SocialMentorshipCancelMentorshipSessionInput,
  ): Promise<Result<Types.SocialMentorshipMentorshipSessionDto, ApiError>> {
    const url = `/v1/mentorship/sessions/${sessionId}/cancel`;

    // Validate request body
    const validatedBody = safeParse(Types.SocialMentorshipCancelMentorshipSessionInputSchema, body, 'request');

    const result = await this.client.request({
      method: 'POST',
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialMentorshipMentorshipSessionDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postMentorshipSessionsComplete(sessionId: string): Promise<Result<Types.SocialMentorshipMentorshipSessionDto, ApiError>> {
    const url = `/v1/mentorship/sessions/${sessionId}/complete`;

    const result = await this.client.request({
      method: 'POST',
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(Types.SocialMentorshipMentorshipSessionDtoSchema, result.data, 'response');
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createSocialMentorshipModule(client: ApiClient): SocialMentorshipModule {
  return new SocialMentorshipModule(client);
}
