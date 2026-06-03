"use client"

/**
 * BlockArrayViewer
 *
 * Read-only counterpart to `BlockArrayEditor`. Maps each `Block` to its
 * preview component in `plugins/preview-components/` using a switch on
 * `block.type`. Used by:
 *   - The viewer page (`ViewerField`).
 *   - The static viewer and its section components.
 *   - The editor preview dialog.
 *
 * Each block is wrapped with `blockToPreviewNode(block)` first, which
 * shapes the payload to the `{ type, data | entry, version }` form expected
 * by the preview components.
 */

import type { Block, BlockArray, BlockCellType } from "@/components/block-content-editor/lib/storage/editor/block-structure"
import { blockToPreviewNode } from "@/components/block-content-editor/lib/storage/editor/block-storage"

// Import all preview components
import { PreviewQuiz } from "@/components/block-content-editor/plugins/preview-components/preview-quiz"
import { PreviewImage } from "@/components/block-content-editor/plugins/preview-components/preview-image"
import { PreviewGallery } from "@/components/block-content-editor/plugins/preview-components/preview-gallery"
import { PreviewMarkdown } from "@/components/block-content-editor/plugins/preview-components/preview-markdown"
import { PreviewHTML } from "@/components/block-content-editor/plugins/preview-components/preview-html"
import { PreviewVideo } from "@/components/block-content-editor/plugins/preview-components/preview-video"
import { PreviewAudio } from "@/components/block-content-editor/plugins/preview-components/preview-audio"
import { PreviewHeader } from "@/components/block-content-editor/plugins/preview-components/preview-header"
import { PreviewDivider } from "@/components/block-content-editor/plugins/preview-components/preview-divider"
import { PreviewSource } from "@/components/block-content-editor/plugins/preview-components/preview-source"
import { PreviewAdmonition } from "@/components/block-content-editor/plugins/preview-components/preview-admonition"
import { PreviewButton } from "@/components/block-content-editor/plugins/preview-components/preview-button"
import { PreviewMermaid } from "@/components/block-content-editor/plugins/preview-components/preview-mermaid"
import { PreviewVegaLite } from "@/components/block-content-editor/plugins/preview-components/preview-vega-lite"
import { PreviewCodeStudio } from "@/components/block-content-editor/plugins/preview-components/preview-code-studio"
import { PreviewProject } from "@/components/block-content-editor/plugins/preview-components/preview-project"
import { PreviewRichText } from "@/components/block-content-editor/plugins/preview-components/preview-rich-text"

// ============================================================================
// Block Content Renderer — maps CellType → Preview component
// ============================================================================

interface BlockContentRendererProps {
  block: Block
}

export function BlockContentRenderer({ block }: BlockContentRendererProps) {
  const node = blockToPreviewNode(block)

  switch (block.type as BlockCellType) {
    case "quiz":
      return <PreviewQuiz node={node} />
    case "code-studio":
      return <PreviewCodeStudio data={node.data} />
    case "image":
      return <PreviewImage node={node} />
    case "video":
      return <PreviewVideo node={node} />
    case "audio":
      return <PreviewAudio node={node} />
    case "gallery":
      return <PreviewGallery node={node} />
    case "mermaid":
      return <PreviewMermaid data={node.data} />
    case "vega-lite":
      return <PreviewVegaLite node={node} />
    case "source":
      return <PreviewSource node={node} />
    case "markdown":
      return <PreviewMarkdown node={node} />
    case "html":
      return <PreviewHTML node={node} />
    case "rich-text":
      return <PreviewRichText node={node} />
    case "header":
      return <PreviewHeader node={node} />
    case "divider":
      return <PreviewDivider node={node} />
    case "button":
      return <PreviewButton node={node} />
    case "admonition":
      return <PreviewAdmonition node={node} />
    case "project":
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
