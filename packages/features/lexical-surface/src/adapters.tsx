"use client";

import * as React from "react";
import type { MermaidData } from "./mermaid/mermaid-data";
import type { VegaLiteData } from "./vega-lite/vega-lite-data";

export interface MermaidEditorAdapterProps {
  initialData?: MermaidData;
  onSave: (data: MermaidData) => void;
  onCancel: () => void;
}

export interface MermaidViewerAdapterProps {
  data: MermaidData;
  title?: string;
  caption?: string;
  size?: number;
  showControls?: boolean;
  allowFullscreen?: boolean;
  className?: string;
}

export interface VegaLiteEditorAdapterProps {
  initialData?: VegaLiteData;
  onSave: (data: VegaLiteData) => void;
  onCancel: () => void;
}

export interface VegaLiteViewerAdapterProps {
  spec: string;
  layout?: "square" | "rectangular";
  themeLight?: string;
  themeDark?: string;
  title?: string;
  caption?: string;
  size?: number;
  showControls?: boolean;
  allowFullscreen?: boolean;
  className?: string;
  data?: Record<string, string>;
}

export interface MediaUploadResult {
  type: "file" | "url";
  data: string;
  name?: string;
  size?: number;
  compressed?: boolean;
  originalSize?: number;
  compressionRatio?: number;
  assetId?: string;
}

export interface MediaUploadDialogAdapterProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onMediaSelected: (result: MediaUploadResult | MediaUploadResult[]) => void;
  title?: string;
  acceptTypes?: string;
  urlPlaceholder?: string;
  multiple?: boolean;
}

export interface AssetResolverAdapter {
  isAssetUrl: (url: string) => boolean;
  resolveAssetUrl: (url: string) => Promise<string | null>;
}

export interface LexicalSurfaceAdapters {
  MermaidEditor?: React.ComponentType<MermaidEditorAdapterProps>;
  MermaidViewer?: React.ComponentType<MermaidViewerAdapterProps>;
  VegaLiteEditor?: React.ComponentType<VegaLiteEditorAdapterProps>;
  VegaLiteViewer?: React.ComponentType<VegaLiteViewerAdapterProps>;
  MediaUploadDialog?: React.ComponentType<MediaUploadDialogAdapterProps>;
  assets?: AssetResolverAdapter;
}

const LexicalSurfaceAdaptersContext = React.createContext<LexicalSurfaceAdapters>({});

export function LexicalSurfaceAdaptersProvider({
  adapters,
  children,
}: React.PropsWithChildren<{ adapters?: LexicalSurfaceAdapters }>) {
  return (
    <LexicalSurfaceAdaptersContext.Provider value={adapters ?? {}}>
      {children}
    </LexicalSurfaceAdaptersContext.Provider>
  );
}

export function useLexicalSurfaceAdapters(): LexicalSurfaceAdapters {
  return React.useContext(LexicalSurfaceAdaptersContext);
}
