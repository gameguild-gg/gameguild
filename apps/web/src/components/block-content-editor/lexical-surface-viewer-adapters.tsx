"use client";

import type { LexicalSurfaceAdapters } from "@game-guild/lexical-surface";
import { MermaidViewer } from "./extras/mermaid/mermaid-viewer";
import { VegaLiteViewer } from "./extras/vega-lite/vega-lite-viewer";
import { isAssetUrl, resolveAssetUrl } from "./lib/storage/assets";

export const lexicalSurfaceViewerAdapters: LexicalSurfaceAdapters = {
  MermaidViewer,
  VegaLiteViewer,
  assets: {
    isAssetUrl,
    resolveAssetUrl,
  },
};
