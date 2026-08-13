"use client";

import type { LexicalSurfaceAdapters } from "@game-guild/lexical-surface";
import { isAssetUrl, resolveAssetUrl } from "./lib/storage/assets";

export const lexicalSurfaceViewerAdapters: LexicalSurfaceAdapters = {
  assets: {
    isAssetUrl,
    resolveAssetUrl,
  },
};
