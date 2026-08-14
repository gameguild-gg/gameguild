"use client";

import { MermaidViewer, type MermaidData } from "@game-guild/lexical-surface";

interface PreviewMermaidProps {
  data: MermaidData;
}

export function PreviewMermaid({ data }: PreviewMermaidProps) {
  return (
    <MermaidViewer
      data={data}
      title={data.title}
      caption={data.caption}
      size={data.size || 100}
      showControls={true}
      allowFullscreen={true}
    />
  );
}
