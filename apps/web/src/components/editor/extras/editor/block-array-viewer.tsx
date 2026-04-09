"use client"

import { CELL_TO_LEXICAL_TYPE, type CellType } from "@/lib/storage/editor/cell-converters/cell-data"
import type { Cell, CellularContent } from "@/lib/storage/editor/cell-structure"

// Import all preview components
import { PreviewQuiz } from "@/components/editor/plugins/preview-components/preview-quiz"
import { PreviewImage } from "@/components/editor/plugins/preview-components/preview-image"
import { PreviewGallery } from "@/components/editor/plugins/preview-components/preview-gallery"
import { PreviewMarkdown } from "@/components/editor/plugins/preview-components/preview-markdown"
import { PreviewHTML } from "@/components/editor/plugins/preview-components/preview-html"
import { PreviewVideo } from "@/components/editor/plugins/preview-components/preview-video"
import { PreviewAudio } from "@/components/editor/plugins/preview-components/preview-audio"
import { PreviewHeader } from "@/components/editor/plugins/preview-components/preview-header"
import { PreviewDivider } from "@/components/editor/plugins/preview-components/preview-divider"
import { PreviewPresentation } from "@/components/editor/plugins/preview-components/preview-presentation"
import { PreviewSource } from "@/components/editor/plugins/preview-components/preview-source"
import { PreviewYouTube } from "@/components/editor/plugins/preview-components/preview-youtube"
import { PreviewSpotify } from "@/components/editor/plugins/preview-components/preview-spotify"
import { PreviewAdmonition } from "@/components/editor/plugins/preview-components/preview-admonition"
import { PreviewButton } from "@/components/editor/plugins/preview-components/preview-button"
import { PreviewMermaid } from "@/components/editor/plugins/preview-components/preview-mermaid"
import { PreviewVegaLite } from "@/components/editor/plugins/preview-components/preview-vega-lite"
import { PreviewTable } from "@/components/editor/plugins/preview-components/preview-table"
import { PreviewCodeStudio } from "@/components/editor/plugins/preview-components/preview-code-studio"
import { PreviewProject } from "@/components/editor/plugins/preview-components/preview-project"

// ============================================================================
// Cell → Serialized Node converter
// ============================================================================

/**
 * Convert a Cell tuple to a fake serialized Lexical node
 * that the preview components can consume.
 *
 * Most decorators use `{ type, data, version }`.
 * Quiz is the exception: it uses `entry` instead of `data`.
 */
export function cellToSerializedNode(cell: Cell): any {
  const [cellData, meta] = cell
  const cellType = meta.t as CellType
  const lexicalType = CELL_TO_LEXICAL_TYPE[cellType]
  const d = (cellData as any).d

  if (cellType === "quiz") {
    return { type: lexicalType, entry: d, version: meta.v }
  }

  return { type: lexicalType, data: d, version: meta.v }
}

// ============================================================================
// Block Content Renderer — maps CellType → Preview component
// ============================================================================

interface BlockContentRendererProps {
  cell: Cell
}

export function BlockContentRenderer({ cell }: BlockContentRendererProps) {
  const [, meta] = cell
  const cellType = meta.t as CellType
  const node = cellToSerializedNode(cell)

  switch (cellType) {
    case "quiz":
      return <PreviewQuiz node={node} />
    case "code":
      return <PreviewCodeStudio data={node.data} />
    case "img":
      return <PreviewImage node={node} />
    case "vid":
      return <PreviewVideo node={node} />
    case "aud":
      return <PreviewAudio node={node} />
    case "gal":
      return <PreviewGallery node={node} />
    case "yt":
      return <PreviewYouTube node={node} />
    case "spot":
      return <PreviewSpotify node={node} />
    case "mmd":
      return <PreviewMermaid data={node.data} />
    case "vega":
      return <PreviewVegaLite node={node} />
    case "pres":
      return <PreviewPresentation node={node} />
    case "src":
      return <PreviewSource node={node} />
    case "md":
      return <PreviewMarkdown node={node} />
    case "html":
      return <PreviewHTML node={node} />
    case "hdr":
      return <PreviewHeader node={node} />
    case "div":
      return <PreviewDivider node={node} />
    case "btn":
      return <PreviewButton node={node} />
    case "adm":
      return <PreviewAdmonition node={node} />
    case "tbl":
      return <PreviewTable node={node} />
    case "proj":
      return <PreviewProject node={node} />
    default:
      return (
        <div className="p-3 border border-gray-200 dark:border-gray-700 rounded text-sm text-gray-500 dark:text-gray-400">
          Unknown block type: {cellType}
        </div>
      )
  }
}

// ============================================================================
// Block Array Viewer — read-only vertical renderer
// ============================================================================

interface BlockArrayViewerProps {
  cells: CellularContent
  className?: string
}

export function BlockArrayViewer({ cells, className }: BlockArrayViewerProps) {
  if (!cells || cells.length === 0) {
    return (
      <div className="py-16 text-center text-sm text-gray-500 dark:text-gray-400">
        This project has no content yet.
      </div>
    )
  }

  return (
    <div className={className ?? "prose prose-stone dark:prose-invert max-w-none space-y-4"}>
      {cells.map((cell, index) => (
        <BlockContentRenderer key={index} cell={cell} />
      ))}
    </div>
  )
}
