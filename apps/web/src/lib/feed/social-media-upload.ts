"use client";

import type { SocialMediaAsset } from "./contracts";
import {
  SOCIAL_IMAGE_SIZE_ERROR,
  SOCIAL_IMAGE_SIZE_LABEL,
  SOCIAL_MEDIA_FORMAT_ERROR,
  SOCIAL_MEDIA_FORMATS,
  SOCIAL_VIDEO_SIZE_ERROR,
  SOCIAL_VIDEO_SIZE_LABEL,
} from "./social-media-policy";

export interface UploadProgress {
  loaded: number;
  total: number;
  percent: number;
}

export class SocialMediaUploadError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "SocialMediaUploadError";
  }
}

function uploadFailureMessage(status: number, body: unknown): string {
  if (status === 0) return "Check your connection and try the upload again.";
  if (status === 401) return "Sign in again to upload media.";
  if (status === 403) return "You do not have permission to upload media here.";
  if (status === 413) return "The file is too large. Images can be up to " + SOCIAL_IMAGE_SIZE_LABEL + " and MP4 videos up to " + SOCIAL_VIDEO_SIZE_LABEL + ".";
  if (status === 415) return SOCIAL_MEDIA_FORMAT_ERROR;
  if (status === 429) return "Too many uploads. Wait a moment and try again.";
  if (status === 499) return "Media upload cancelled.";
  if (status >= 500 && status < 600) return "Media uploads are temporarily unavailable. Try again shortly.";

  // Only translate the API's known validation contract. Unknown upstream text
  // can contain provider details, credentials, or markup and is never displayed.
  if (status === 400 && body && typeof body === "object" && !Array.isArray(body)) {
    const problem = body as Record<string, unknown>;
    if (problem.title === "A media file is required." || problem.error === "A media file is required.") {
      return "Choose a non-empty media file.";
    }
    if (problem.title === "Invalid social media upload") {
      if (problem.detail === "Only JPEG, PNG, WebP, GIF, and MP4 files are supported.") return SOCIAL_MEDIA_FORMAT_ERROR;
      if (problem.detail === "The file content does not match its declared media type.") {
        return "The file contents do not match its media type. Export it again and try another file.";
      }
      if (problem.detail === "The uploaded file cannot be inspected safely.") {
        return "This file could not be verified. Export it again and try another file.";
      }
      for (const format of SOCIAL_MEDIA_FORMATS) {
        if (problem.detail === "The file exceeds the " + format.maxBytes + " byte limit for " + format.mimeType + ".") {
          return format.kind === "video" ? SOCIAL_VIDEO_SIZE_ERROR : SOCIAL_IMAGE_SIZE_ERROR;
        }
      }
    }
  }
  return "Media upload failed. Try again.";
}

export function uploadSocialMediaWithProgress(
  file: File,
  options: { signal?: AbortSignal; onProgress?: (progress: UploadProgress) => void } = {},
): Promise<SocialMediaAsset> {
  return new Promise((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    let settled = false;
    const finish = (callback: () => void) => {
      if (settled) return;
      settled = true;
      options.signal?.removeEventListener("abort", abort);
      callback();
    };
    const abort = () => {
      xhr.abort();
      finish(() => reject(new SocialMediaUploadError("Media upload cancelled.")));
    };

    if (options.signal?.aborted) {
      abort();
      return;
    }
    options.signal?.addEventListener("abort", abort, { once: true });
    xhr.open("POST", "/api/social/media");
    xhr.responseType = "json";
    xhr.upload.onprogress = (event) => {
      if (!event.lengthComputable || event.total === 0) return;
      options.onProgress?.({ loaded: event.loaded, total: event.total, percent: Math.round((event.loaded / event.total) * 100) });
    };
    xhr.onerror = () => finish(() => reject(new SocialMediaUploadError(uploadFailureMessage(0, null))));
    xhr.onabort = () => finish(() => reject(new SocialMediaUploadError("Media upload cancelled.")));
    xhr.onload = () => finish(() => {
      const body = xhr.response && typeof xhr.response === "object"
        ? xhr.response
        : (() => { try { return JSON.parse(xhr.responseText) as unknown; } catch { return null; } })();
      if (xhr.status < 200 || xhr.status >= 300) {
        reject(new SocialMediaUploadError(uploadFailureMessage(xhr.status, body)));
        return;
      }
      if (!body || typeof body !== "object") {
        reject(new SocialMediaUploadError("Media upload failed. Try again."));
        return;
      }
      const asset = body as Partial<SocialMediaAsset>;
      if (!asset.assetReferenceId || !asset.state) {
        reject(new SocialMediaUploadError("The upload response was invalid. Try again."));
        return;
      }
      resolve({
        assetReferenceId: asset.assetReferenceId,
        deliveryUrl: asset.deliveryUrl ?? null,
        mimeType: asset.mimeType ?? null,
        sizeBytes: asset.sizeBytes ?? file.size,
        state: asset.state,
      });
    });
    const formData = new FormData();
    formData.append("file", file);
    xhr.send(formData);
  });
}
