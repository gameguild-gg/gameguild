/**
 * @game-guild/client - AiPrompttemplates Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from "../../runtime/client.js";
import type { Result } from "../../runtime/result/types.js";
import type { ApiError } from "../../runtime/errors/types.js";
import * as Types from "../types.gen.js";
import { safeParse } from "../../runtime/errors/validation.js";

/* eslint-disable @typescript-eslint/no-explicit-any */

export class AiPrompttemplatesModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getAiPromptTemplates(query?: {
    category?: string;
    includeInactive?: boolean;
  }): Promise<Result<Array<Types.AIAiPromptTemplate>, ApiError>> {
    const url = "/v1/ai/prompt-templates";

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<Array<Types.AIAiPromptTemplate>, ApiError>;
  }

  /**
   */
  async postAiPromptTemplates(
    body: Types.AICreateAiPromptTemplateInput,
  ): Promise<Result<Types.AIAiPromptTemplate, ApiError>> {
    const url = "/v1/ai/prompt-templates";

    // Validate request body
    const validatedBody = safeParse(
      Types.AICreateAiPromptTemplateInputSchema,
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
        Types.AIAiPromptTemplateSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getAiPromptTemplates1(
    id: string,
  ): Promise<Result<Types.AIAiPromptTemplate, ApiError>> {
    const url = `/v1/ai/prompt-templates/${id}`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.AIAiPromptTemplateSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async putAiPromptTemplates(
    id: string,
    body: Types.AIUpdateAiPromptTemplateInput,
  ): Promise<Result<Types.AIAiPromptTemplate, ApiError>> {
    const url = `/v1/ai/prompt-templates/${id}`;

    // Validate request body
    const validatedBody = safeParse(
      Types.AIUpdateAiPromptTemplateInputSchema,
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
        Types.AIAiPromptTemplateSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteAiPromptTemplates(id: string): Promise<Result<void, ApiError>> {
    const url = `/v1/ai/prompt-templates/${id}`;

    const result = await this.client.request({
      method: "DELETE",
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postAiPromptTemplatesGenerate(
    id: string,
    body: Types.AIAiPromptTemplateGenerateInput,
  ): Promise<Result<Types.AIAiCompletionOutput, ApiError>> {
    const url = `/v1/ai/prompt-templates/${id}/generate`;

    // Validate request body
    const validatedBody = safeParse(
      Types.AIAiPromptTemplateGenerateInputSchema,
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
        Types.AIAiCompletionOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAiPromptTemplatesRender(
    id: string,
    body: Types.AIAiPromptTemplateRenderInput,
  ): Promise<Result<Types.AIAiPromptTemplateRenderOutput, ApiError>> {
    const url = `/v1/ai/prompt-templates/${id}/render`;

    // Validate request body
    const validatedBody = safeParse(
      Types.AIAiPromptTemplateRenderInputSchema,
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
        Types.AIAiPromptTemplateRenderOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }
}

export function createAiPrompttemplatesModule(
  client: ApiClient,
): AiPrompttemplatesModule {
  return new AiPrompttemplatesModule(client);
}
