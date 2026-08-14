"use client";

import * as React from "react";

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
  MediaUploadDialog?: React.ComponentType<MediaUploadDialogAdapterProps>;
  assets?: AssetResolverAdapter;
}

const LexicalSurfaceAdaptersContext =
  React.createContext<LexicalSurfaceAdapters>({});

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
