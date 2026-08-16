/**
 * @game-guild/client - LearningCoursesCheckout Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from "../../runtime/client.js";
import type { Result } from "../../runtime/result/types.js";
import type { ApiError } from "../../runtime/errors/types.js";
import * as Types from "../types.gen.js";
import { safeParse } from "../../runtime/errors/validation.js";

/* eslint-disable @typescript-eslint/no-explicit-any */

export class LearningCoursesCheckoutModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async postVCoursesCheckoutComplete(
    courseId: string,
    version: string,
    body: Types.LearningCoursesCompleteCourseCheckoutInput,
  ): Promise<
    Result<Types.LearningCoursesCompleteCourseCheckoutOutput, ApiError>
  > {
    const url = `/v${version}/courses/${courseId}/checkout/complete`;

    // Validate request body
    const validatedBody = safeParse(
      Types.LearningCoursesCompleteCourseCheckoutInputSchema,
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
        Types.LearningCoursesCompleteCourseCheckoutOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createLearningCoursesCheckoutModule(
  client: ApiClient,
): LearningCoursesCheckoutModule {
  return new LearningCoursesCheckoutModule(client);
}
