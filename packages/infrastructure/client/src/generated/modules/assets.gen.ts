/**
 * @game-guild/client - Assets Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from "../../runtime/client.js";
import type { Result } from "../../runtime/result/types.js";
import type { ApiError } from "../../runtime/errors/types.js";
import * as Types from "../types.gen.js";
import { safeParse } from "../../runtime/errors/validation.js";

/* eslint-disable @typescript-eslint/no-explicit-any */

export class AssetsModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async getAssetsForGetAssets(query?: {
    owner?: string;
    parentType?: string;
    parentId?: string;
    skip?: number;
    take?: number;
  }): Promise<Result<void, ApiError>> {
    const url = "/v1/assets";

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postAssets(query?: {
    displayName?: string;
    accessPolicy?: Types.AssetsAssetAccessPolicy;
    parentResourceType?: string;
    parentResourceId?: string;
    folderId?: string;
  }): Promise<Result<void, ApiError>> {
    const url = "/v1/assets";

    const result = await this.client.request({
      method: "POST",
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postAssetsBulkDelete(
    body: Types.AssetsControllersBulkDeleteAssetsInput,
  ): Promise<Result<Types.AssetsCommandsBulkDeleteAssetsOutput, ApiError>> {
    const url = "/v1/assets/bulk-delete";

    // Validate request body
    const validatedBody = safeParse(
      Types.AssetsControllersBulkDeleteAssetsInputSchema,
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
        Types.AssetsCommandsBulkDeleteAssetsOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAssetsBulkDownload(
    body: Types.AssetsControllersBulkAssetAccessUrlInput,
  ): Promise<Result<Types.AssetsQueriesBulkAssetAccessUrlsOutput, ApiError>> {
    const url = "/v1/assets/bulk-download";

    // Validate request body
    const validatedBody = safeParse(
      Types.AssetsControllersBulkAssetAccessUrlInputSchema,
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
        Types.AssetsQueriesBulkAssetAccessUrlsOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAssetsBulkUpload(query?: {
    accessPolicy?: Types.AssetsAssetAccessPolicy;
    parentResourceType?: string;
    parentResourceId?: string;
    folderId?: string;
  }): Promise<Result<Types.AssetsCommandsBulkUploadAssetsOutput, ApiError>> {
    const url = "/v1/assets/bulk-upload";

    const result = await this.client.request({
      method: "POST",
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.AssetsCommandsBulkUploadAssetsOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async postAssetsChunkedUploads(query?: {
    fileName?: string;
    mimeType?: string;
    totalSize?: number;
  }): Promise<Result<Types.AssetsChunkedUploadSession, ApiError>> {
    const url = "/v1/assets/chunked-uploads";

    const result = await this.client.request({
      method: "POST",
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.AssetsChunkedUploadSessionSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async deleteAssetsChunkedUploads(
    uploadId: string,
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/assets/chunked-uploads/${uploadId}`;

    const result = await this.client.request({
      method: "DELETE",
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postAssetsChunkedUploadsParts(
    uploadId: string,
    query?: { chunkIndex?: number },
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/assets/chunked-uploads/${uploadId}/parts`;

    const result = await this.client.request({
      method: "POST",
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postAssetsChunkedUploadsComplete(
    uploadId: string,
    query?: {
      displayName?: string;
      accessPolicy?: Types.AssetsAssetAccessPolicy;
      parentResourceType?: string;
      parentResourceId?: string;
      folderId?: string;
    },
  ): Promise<Result<Types.AssetsAssetUploadResult, ApiError>> {
    const url = `/v1/assets/chunked-uploads/${uploadId}:complete`;

    const result = await this.client.request({
      method: "POST",
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.AssetsAssetUploadResultSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getAssetsSearch(query?: {
    q?: string;
    kind?: Types.AssetsAssetKind;
    parentType?: string;
    parentId?: string;
    skip?: number;
    take?: number;
  }): Promise<Result<Types.AssetsQueriesAssetSearchOutput, ApiError>> {
    const url = "/v1/assets/search";

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.AssetsQueriesAssetSearchOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getAssetsForGetAssetsById(
    id: string,
    query?: { includeContent?: boolean },
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/assets/${id}`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async deleteAssets(id: string): Promise<Result<void, ApiError>> {
    const url = `/v1/assets/${id}`;

    const result = await this.client.request({
      method: "DELETE",
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async patchAssets(
    id: string,
    body: Types.AssetsControllersUpdateAssetInput,
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/assets/${id}`;

    // Validate request body
    const validatedBody = safeParse(
      Types.AssetsControllersUpdateAssetInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "PATCH",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getAssetsContent(
    id: string,
    query?: { token?: string; transform?: string },
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/assets/${id}/content`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getAssetExtractedText(
    id: string,
  ): Promise<
    Result<Types.AssetsControllersAssetExtractedTextOutput, ApiError>
  > {
    const url = `/v1/assets/${id}/extracted-text`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.AssetsControllersAssetExtractedTextOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getAssetsPreview(
    id: string,
    query?: {
      includeExtractedText?: boolean;
      thumbnailWidth?: number;
      thumbnailHeight?: number;
    },
  ): Promise<Result<Types.AssetsQueriesAssetPreviewOutput, ApiError>> {
    const url = `/v1/assets/${id}/preview`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    // Validate response
    if (result.ok) {
      const validatedData = safeParse(
        Types.AssetsQueriesAssetPreviewOutputSchema,
        result.data,
        "response",
      );
      return { ok: true, data: validatedData };
    }

    return result;
  }

  /**
   */
  async getSignedAssetExtractedText(
    id: string,
    query?: { token?: string },
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/assets/${id}:extracted-text`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postAssetsGenerateAccessUrl(
    id: string,
    query?: {
      width?: number;
      height?: number;
      fit?: Types.AssetsImageFit;
      format?: Types.AssetsImageFormat;
      quality?: number;
      direct?: boolean;
    },
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/assets/${id}:generate-access-url`;

    const result = await this.client.request({
      method: "POST",
      path: url,
      params: query,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postAssetsReport(
    id: string,
    body: Types.AssetsControllersReportAssetInput,
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/assets/${id}:report`;

    // Validate request body
    const validatedBody = safeParse(
      Types.AssetsControllersReportAssetInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "POST",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }
}

export function createAssetsModule(client: ApiClient): AssetsModule {
  return new AssetsModule(client);
}
