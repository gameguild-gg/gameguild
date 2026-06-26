"use client"

import { VegaLiteViewer } from "@/components/block-content-editor/extras/vega-lite/vega-lite-viewer"
import { getThemePair } from "@/components/block-content-editor/extras/vega-lite/vega-theme-helper"

interface PreviewVegaLiteProps {
  node: {
    data: {
      spec: string
      title?: string
      caption?: string
      theme?: string
      themeMode?: string
      layout?: "square" | "rectangular"
      size?: number
    }
  }
}

export function PreviewVegaLite({ node }: PreviewVegaLiteProps) {
  const { spec, title, caption, theme, themeMode, layout, size } = node.data
  const themePair = getThemePair((theme as any) || "default", (themeMode as any) || "system")

  return (
    <VegaLiteViewer
      spec={spec}
      layout={layout}
      themeLight={themePair.themeLight}
      themeDark={themePair.themeDark}
      title={title}
      caption={caption}
      size={size}
      showControls={true}
      allowFullscreen={true}
    />
  )
}
