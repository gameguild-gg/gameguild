/**
 * @game-guild/client - TestinglabTestingevents Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from "../../runtime/client.js";
import type { Result } from "../../runtime/result/types.js";
import type { ApiError } from "../../runtime/errors/types.js";
import * as Types from "../types.gen.js";
import { safeParse } from "../../runtime/errors/validation.js";

/* eslint-disable @typescript-eslint/no-explicit-any */

export class TestinglabTestingeventsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getTestingEvents(query?: {
    status?: Types.TestingLabTestingEventStatus;
    skip?: number;
    take?: number;
  }): Promise<Result<Array<Types.TestingLabTestingEventProjection>, ApiError>> {
    const url = "/v1/testing/events";

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<
      Array<Types.TestingLabTestingEventProjection>,
      ApiError
    >;
  }

  /**
   */
  async postTestingEvents(
    body: Types.TestingLabCreateTestingEventInput,
  ): Promise<Result<Types.TestingLabTestingEventProjection, ApiError>> {
    const url = "/v1/testing/events";

    // Validate request body
    const validatedBody = safeParse(
      Types.TestingLabCreateTestingEventInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingEventProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getTestingEventsApplicationsMe(query?: {
    eventId?: string;
  }): Promise<
    Result<Array<Types.TestingLabTestingProjectApplicationProjection>, ApiError>
  > {
    const url = "/v1/testing/events/applications/me";

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<
      Array<Types.TestingLabTestingProjectApplicationProjection>,
      ApiError
    >;
  }

  /**
   */
  async getTestingEventsApplications(
    applicationId: string,
  ): Promise<
    Result<Types.TestingLabTestingProjectApplicationProjection, ApiError>
  > {
    const url = `/v1/testing/events/applications/${applicationId}`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingProjectApplicationProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putTestingEventsApplicationsSlot(
    applicationId: string,
    body: Types.TestingLabAssignTestingProjectApplicationSlotInput,
  ): Promise<
    Result<Types.TestingLabTestingProjectApplicationProjection, ApiError>
  > {
    const url = `/v1/testing/events/applications/${applicationId}/slot`;

    // Validate request body
    const validatedBody = safeParse(
      Types.TestingLabAssignTestingProjectApplicationSlotInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "PUT",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingProjectApplicationProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postTestingEventsApplicationsVotes(
    applicationId: string,
    body: Types.TestingLabCastTestingApplicationVoteInput,
  ): Promise<
    Result<Types.TestingLabTestingApplicationVoteProjection, ApiError>
  > {
    const url = `/v1/testing/events/applications/${applicationId}/votes`;

    // Validate request body
    const validatedBody = safeParse(
      Types.TestingLabCastTestingApplicationVoteInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingApplicationVoteProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postTestingEventsApplicationsApprove(
    applicationId: string,
    body: Types.TestingLabDecideTestingProjectApplicationInput,
  ): Promise<
    Result<Types.TestingLabTestingProjectApplicationProjection, ApiError>
  > {
    const url = `/v1/testing/events/applications/${applicationId}:approve`;

    // Validate request body
    const validatedBody = safeParse(
      Types.TestingLabDecideTestingProjectApplicationInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingProjectApplicationProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postTestingEventsApplicationsReject(
    applicationId: string,
    body: Types.TestingLabDecideTestingProjectApplicationInput,
  ): Promise<
    Result<Types.TestingLabTestingProjectApplicationProjection, ApiError>
  > {
    const url = `/v1/testing/events/applications/${applicationId}:reject`;

    // Validate request body
    const validatedBody = safeParse(
      Types.TestingLabDecideTestingProjectApplicationInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingProjectApplicationProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postTestingEventsApplicationsReview(
    applicationId: string,
  ): Promise<
    Result<Types.TestingLabTestingProjectApplicationProjection, ApiError>
  > {
    const url = `/v1/testing/events/applications/${applicationId}:review`;

    const result = await this.client.request({
      method: "POST",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingProjectApplicationProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postTestingEventsApplicationsWaitlist(
    applicationId: string,
    body: Types.TestingLabDecideTestingProjectApplicationInput,
  ): Promise<
    Result<Types.TestingLabTestingProjectApplicationProjection, ApiError>
  > {
    const url = `/v1/testing/events/applications/${applicationId}:waitlist`;

    // Validate request body
    const validatedBody = safeParse(
      Types.TestingLabDecideTestingProjectApplicationInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingProjectApplicationProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postTestingEventsApplicationsWithdraw(
    applicationId: string,
  ): Promise<
    Result<Types.TestingLabTestingProjectApplicationProjection, ApiError>
  > {
    const url = `/v1/testing/events/applications/${applicationId}:withdraw`;

    const result = await this.client.request({
      method: "POST",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingProjectApplicationProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getTestingEventsPublic(query?: {
    skip?: number;
    take?: number;
  }): Promise<
    Result<Array<Types.TestingLabPublicTestingEventProjection>, ApiError>
  > {
    const url = "/v1/testing/events/public";

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: false,
    });

    return result as Result<
      Array<Types.TestingLabPublicTestingEventProjection>,
      ApiError
    >;
  }

  /**
   */
  async getTestingEventsPublic1(
    eventId: string,
  ): Promise<Result<Types.TestingLabPublicTestingEventProjection, ApiError>> {
    const url = `/v1/testing/events/public/${eventId}`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: false,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabPublicTestingEventProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getTestingEvents1(
    eventId: string,
  ): Promise<Result<Types.TestingLabTestingEventProjection, ApiError>> {
    const url = `/v1/testing/events/${eventId}`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingEventProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putTestingEvents(
    eventId: string,
    body: Types.TestingLabUpdateTestingEventInput,
  ): Promise<Result<Types.TestingLabTestingEventProjection, ApiError>> {
    const url = `/v1/testing/events/${eventId}`;

    // Validate request body
    const validatedBody = safeParse(
      Types.TestingLabUpdateTestingEventInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "PUT",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingEventProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteTestingEvents(
    eventId: string,
  ): Promise<Result<boolean, ApiError>> {
    const url = `/v1/testing/events/${eventId}`;

    const result = await this.client.request({
      method: "DELETE",
      path: url,
      requiresAuth: true,
    });

    return result as Result<boolean, ApiError>;
  }

  /**
   */
  async getTestingEventsApplications1(
    eventId: string,
    query?: {
      status?: Types.TestingLabTestingApplicationStatus;
      skip?: number;
      take?: number;
    },
  ): Promise<
    Result<Array<Types.TestingLabTestingProjectApplicationProjection>, ApiError>
  > {
    const url = `/v1/testing/events/${eventId}/applications`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<
      Array<Types.TestingLabTestingProjectApplicationProjection>,
      ApiError
    >;
  }

  /**
   */
  async postTestingEventsApplications(
    eventId: string,
    body: Types.TestingLabSubmitTestingProjectApplicationInput,
  ): Promise<
    Result<Types.TestingLabTestingProjectApplicationProjection, ApiError>
  > {
    const url = `/v1/testing/events/${eventId}/applications`;

    // Validate request body
    const validatedBody = safeParse(
      Types.TestingLabSubmitTestingProjectApplicationInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingProjectApplicationProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getTestingEventsCommittee(
    eventId: string,
  ): Promise<
    Result<
      Array<Types.TestingLabTestingEventCommitteeMemberProjection>,
      ApiError
    >
  > {
    const url = `/v1/testing/events/${eventId}/committee`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    return result as Result<
      Array<Types.TestingLabTestingEventCommitteeMemberProjection>,
      ApiError
    >;
  }

  /**
   */
  async postTestingEventsCommittee(
    eventId: string,
    body: Types.TestingLabAddTestingEventCommitteeMemberInput,
  ): Promise<
    Result<Types.TestingLabTestingEventCommitteeMemberProjection, ApiError>
  > {
    const url = `/v1/testing/events/${eventId}/committee`;

    // Validate request body
    const validatedBody = safeParse(
      Types.TestingLabAddTestingEventCommitteeMemberInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingEventCommitteeMemberProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteTestingEventsCommittee(
    eventId: string,
    userId: string,
  ): Promise<Result<boolean, ApiError>> {
    const url = `/v1/testing/events/${eventId}/committee/${userId}`;

    const result = await this.client.request({
      method: "DELETE",
      path: url,
      requiresAuth: true,
    });

    return result as Result<boolean, ApiError>;
  }

  /**
   */
  async putTestingEventsLearning(
    eventId: string,
    body: Types.TestingLabConfigureTestingEventLearningInput,
  ): Promise<Result<Types.TestingLabTestingEventProjection, ApiError>> {
    const url = `/v1/testing/events/${eventId}/learning`;

    // Validate request body
    const validatedBody = safeParse(
      Types.TestingLabConfigureTestingEventLearningInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "PUT",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingEventProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getTestingEventsSlots(
    eventId: string,
  ): Promise<
    Result<Array<Types.TestingLabTestingEventSlotProjection>, ApiError>
  > {
    const url = `/v1/testing/events/${eventId}/slots`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    return result as Result<
      Array<Types.TestingLabTestingEventSlotProjection>,
      ApiError
    >;
  }

  /**
   */
  async postTestingEventsSlots(
    eventId: string,
    body: Types.TestingLabUpsertTestingEventSlotInput,
  ): Promise<Result<Types.TestingLabTestingEventSlotProjection, ApiError>> {
    const url = `/v1/testing/events/${eventId}/slots`;

    // Validate request body
    const validatedBody = safeParse(
      Types.TestingLabUpsertTestingEventSlotInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingEventSlotProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putTestingEventsSlots(
    eventId: string,
    slotId: string,
    body: Types.TestingLabUpsertTestingEventSlotInput,
  ): Promise<Result<Types.TestingLabTestingEventSlotProjection, ApiError>> {
    const url = `/v1/testing/events/${eventId}/slots/${slotId}`;

    // Validate request body
    const validatedBody = safeParse(
      Types.TestingLabUpsertTestingEventSlotInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "PUT",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingEventSlotProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteTestingEventsSlots(
    eventId: string,
    slotId: string,
  ): Promise<Result<boolean, ApiError>> {
    const url = `/v1/testing/events/${eventId}/slots/${slotId}`;

    const result = await this.client.request({
      method: "DELETE",
      path: url,
      requiresAuth: true,
    });

    return result as Result<boolean, ApiError>;
  }

  /**
   */
  async postTestingEventsActivate(
    eventId: string,
  ): Promise<Result<Types.TestingLabTestingEventProjection, ApiError>> {
    const url = `/v1/testing/events/${eventId}:activate`;

    const result = await this.client.request({
      method: "POST",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingEventProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postTestingEventsCancel(
    eventId: string,
    body: Types.TestingLabCancelTestingEventInput,
  ): Promise<Result<Types.TestingLabTestingEventProjection, ApiError>> {
    const url = `/v1/testing/events/${eventId}:cancel`;

    // Validate request body
    const validatedBody = safeParse(
      Types.TestingLabCancelTestingEventInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingEventProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postTestingEventsCloseApplications(
    eventId: string,
  ): Promise<Result<Types.TestingLabTestingEventProjection, ApiError>> {
    const url = `/v1/testing/events/${eventId}:close-applications`;

    const result = await this.client.request({
      method: "POST",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingEventProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postTestingEventsComplete(
    eventId: string,
  ): Promise<Result<Types.TestingLabTestingEventProjection, ApiError>> {
    const url = `/v1/testing/events/${eventId}:complete`;

    const result = await this.client.request({
      method: "POST",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingEventProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postTestingEventsOpenApplications(
    eventId: string,
  ): Promise<Result<Types.TestingLabTestingEventProjection, ApiError>> {
    const url = `/v1/testing/events/${eventId}:open-applications`;

    const result = await this.client.request({
      method: "POST",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingEventProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postTestingEventsSchedule(
    eventId: string,
  ): Promise<Result<Types.TestingLabTestingEventProjection, ApiError>> {
    const url = `/v1/testing/events/${eventId}:schedule`;

    const result = await this.client.request({
      method: "POST",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.TestingLabTestingEventProjectionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createTestinglabTestingeventsModule(
  client: ApiClient,
): TestinglabTestingeventsModule {
  return new TestinglabTestingeventsModule(client);
}
