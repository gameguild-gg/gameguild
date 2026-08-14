"use client";

import * as React from "react";
import { File, FileAudio, FileText, FileVideo, ImageIcon } from "lucide-react";
import type { AssetRecord } from "../core/asset-contracts";
import { useResolvedAssetUrl } from "./use-assets";

export interface AssetPreviewProps {
  asset: AssetRecord;
  className?: string;
  interactive?: boolean;
}

export function AssetPreview({ asset, className, interactive = false }: AssetPreviewProps) {
  const shouldResolve = asset.kind === "image" || (interactive && (asset.kind === "audio" || asset.kind === "video"));
  const { url } = useResolvedAssetUrl(shouldResolve ? asset.uri : null);
  if (asset.kind === "image" && url) {
    return <img src={url} alt={asset.name} className={className} />;
  }
  if (interactive && asset.kind === "video" && url) {
    return <video src={url} aria-label={asset.name} controls preload="metadata" className={className} />;
  }
  if (interactive && asset.kind === "audio" && url) {
    return <audio src={url} aria-label={asset.name} controls preload="metadata" className={className} />;
  }
  const Icon =
    asset.kind === "video"
      ? FileVideo
      : asset.kind === "audio"
        ? FileAudio
        : asset.kind === "document" || asset.kind === "dataset" || asset.kind === "code"
          ? FileText
          : asset.kind === "image"
            ? ImageIcon
            : File;
  return <Icon aria-label={asset.name} className={className} />;
}
