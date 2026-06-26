"use client"

import type { MermaidData } from "@/components/block-content-editor/nodes/mermaid-node"
import { MermaidViewer } from "@/components/block-content-editor/extras/mermaid/mermaid-viewer"

interface PreviewMermaidProps {
  data: MermaidData
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
  )
}
