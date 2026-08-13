"use client";

import type { LexicalSurfaceAdapters } from "@game-guild/lexical-surface";
import { MermaidEditor } from "./extras/mermaid/mermaid-editor";
import { MermaidViewer } from "./extras/mermaid/mermaid-viewer";
import { MediaUploadDialog } from "./extras/media-upload-dialog";
import { VegaLiteEditor } from "./extras/vega-lite/vega-lite-editor";
import { VegaLiteViewer } from "./extras/vega-lite/vega-lite-viewer";
import { isAssetUrl, resolveAssetUrl } from "./lib/storage/assets";

export const lexicalSurfaceAdapters: LexicalSurfaceAdapters = {
  MermaidEditor,
  MermaidViewer,
  VegaLiteEditor,
  VegaLiteViewer,
  MediaUploadDialog,
  assets: {
    isAssetUrl,
    resolveAssetUrl,
  },
};
