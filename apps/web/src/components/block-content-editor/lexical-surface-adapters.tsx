"use client";

import type { LexicalSurfaceAdapters } from "@game-guild/lexical-surface";
import { MediaUploadDialog } from "./extras/media-upload-dialog";
import { isAssetUrl, resolveAssetUrl } from "./lib/storage/assets";

export const lexicalSurfaceAdapters: LexicalSurfaceAdapters = {
  MediaUploadDialog,
  assets: {
    isAssetUrl,
    resolveAssetUrl,
  },
};
