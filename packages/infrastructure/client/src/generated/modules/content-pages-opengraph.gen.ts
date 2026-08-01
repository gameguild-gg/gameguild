/**
 * @game-guild/client - ContentPagesOpengraph Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from "../../runtime/client.js";
import type { Result } from "../../runtime/result/types.js";
import type { ApiError } from "../../runtime/errors/types.js";
import * as Types from "../types.gen.js";
import { safeParse } from "../../runtime/errors/validation.js";

/* eslint-disable @typescript-eslint/no-explicit-any */

export class ContentPagesOpengraphModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getOg(
    slug: string,
  ): Promise<Result<Types.ContentPagesOpenGraphMetadata, ApiError>> {
    const url = `/v1/og/${slug}`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: false,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.ContentPagesOpenGraphMetadataSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createContentPagesOpengraphModule(
  client: ApiClient,
): ContentPagesOpengraphModule {
  return new ContentPagesOpengraphModule(client);
}
