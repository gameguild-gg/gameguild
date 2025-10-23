"use client"

import { VegaLiteViewer } from "@/components/ui/vega-lite-viewer"

interface PreviewVegaLiteProps {
  node: {
    data: {
      spec: string
      title?: string
      caption?: string
      theme?: string
      layout?: "square" | "rectangular"
      size?: number
    }
  }
}

export function PreviewVegaLite({ node }: PreviewVegaLiteProps) {
  const { spec, title, caption, theme, layout, size } = node.data

  return (
    <VegaLiteViewer
      spec={spec}
      layout={layout}
      theme={theme}
      title={title}
      caption={caption}
      size={size}
      showControls={true}
      allowFullscreen={true}
    />
  )
}
