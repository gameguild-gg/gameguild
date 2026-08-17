/**
 * @game-guild/client - AssetsLibraries Module
 *
 * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY
 */

import type { ApiClient } from "../../runtime/client.js";
import type { Result } from "../../runtime/result/types.js";
import type { ApiError } from "../../runtime/errors/types.js";
import * as Types from "../types.gen.js";
import { safeParse } from "../../runtime/errors/validation.js";

/* eslint-disable @typescript-eslint/no-explicit-any */

export class AssetsLibrariesModule {
  constructor(private readonly client: ApiClient) {}

  /**
   */
  async postAssetLibrariesAssetsCopy(
    referenceId: string,
    body: Types.AssetsControllersCopyAssetReferenceInput,
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/asset-libraries/assets/${referenceId}/copy`;

    // Validate request body
    const validatedBody = safeParse(
      Types.AssetsControllersCopyAssetReferenceInputSchema,
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

  /**
   */
  async getAssetLibrariesAssetsRevisions(
    referenceId: string,
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/asset-libraries/assets/${referenceId}/revisions`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postAssetLibrariesAssetsRevisionsRestore(
    referenceId: string,
    revisionId: string,
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/asset-libraries/assets/${referenceId}/revisions/${revisionId}/restore`;

    const result = await this.client.request({
      method: "POST",
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async putAssetLibrariesFoldersRestriction(
    folderId: string,
    body: Types.AssetsControllersRestrictAssetFolderInput,
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/asset-libraries/folders/${folderId}/restriction`;

    // Validate request body
    const validatedBody = safeParse(
      Types.AssetsControllersRestrictAssetFolderInputSchema,
      body,
      "request",
    );

    const result = await this.client.request({
      method: "PUT",
      path: url,
      body: validatedBody,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async getAssetLibraries(
    resourceType: string,
    resourceId: string,
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/asset-libraries/${resourceType}/${resourceId}`;

    const result = await this.client.request({
      method: "GET",
      path: url,
      requiresAuth: true,
    });

    return result as Result<void, ApiError>;
  }

  /**
   */
  async postAssetLibrariesFolders(
    resourceType: string,
    resourceId: string,
    body: Types.AssetsControllersCreateAssetFolderInput,
  ): Promise<Result<void, ApiError>> {
    const url = `/v1/asset-libraries/${resourceType}/${resourceId}/folders`;

    // Validate request body
    const validatedBody = safeParse(
      Types.AssetsControllersCreateAssetFolderInputSchema,
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

export function createAssetsLibrariesModule(
  client: ApiClient,
): AssetsLibrariesModule {
  return new AssetsLibrariesModule(client);
}
