"use client"

import type { Block, BlockArray, BlockCellType } from "@/lib/storage/editor/block-structure"
import { blockToSerializedNode } from "@/lib/storage/editor/cell-converters/blocks"

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
import { PreviewRichText } from "@/components/editor/plugins/preview-components/preview-rich-text"

// ============================================================================
// Block Content Renderer — maps CellType → Preview component
// ============================================================================

interface BlockContentRendererProps {
  block: Block
}

export function BlockContentRenderer({ block }: BlockContentRendererProps) {
  const node = blockToSerializedNode(block)

  switch (block.type as BlockCellType) {
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
    case "rt":
      return <PreviewRichText node={node} />
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
          Unknown block type: {block.type}
        </div>
      )
  }
}

// ============================================================================
// Block Array Viewer — read-only vertical renderer
// ============================================================================

interface BlockArrayViewerProps {
  blocks: BlockArray
  className?: string
}

export function BlockArrayViewer({ blocks, className }: BlockArrayViewerProps) {
  if (!blocks || blocks.length === 0) {
    return (
      <div className="py-16 text-center text-sm text-gray-500 dark:text-gray-400">
        This project has no content yet.
      </div>
    )
  }

  return (
    <div className={className ?? "prose prose-stone dark:prose-invert max-w-none space-y-4"}>
      {blocks.map((block) => (
        <BlockContentRenderer key={block.id} block={block} />
      ))}
    </div>
  )
}
