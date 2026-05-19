import type { SerializedBlockNode } from "./base/serialized-block-node"

export interface VegaLiteData {
  spec: string // JSON specification for Vega-Lite
  title?: string
  caption?: string
  size?: number
  // Theme configuration: single theme base with mode selector
  theme?:
    | "default"
    | "excel"
    | "ggplot2"
    | "quartz"
    | "vox"
    | "fivethirtyeight"
    | "latimes"
    | "urbaninstitute"
    | "googlecharts"
    | "powerbi"
  themeMode?: "system" | "only-light" | "only-dark" // Mode for theme application
  layout?: "square" | "rectangular" // Layout option
  // Data storage - unified for CSV and JSON
  data?: Record<string, string> // Map of filename -> file content (CSV or JSON)
}

export type SerializedVegaLiteNode = SerializedBlockNode<"vega-lite", VegaLiteData>
