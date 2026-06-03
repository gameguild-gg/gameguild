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
  switch (block.type) {
    case "quiz":
      return <PreviewQuiz node={blockToPreviewNode(block)} />
    case "code-studio":
      return <PreviewCodeStudio data={block.data} />
    case "image":
      return <PreviewImage node={blockToPreviewNode(block)} />
    case "video":
      return <PreviewVideo node={blockToPreviewNode(block)} />
    case "audio":
      return <PreviewAudio node={blockToPreviewNode(block)} />
    case "gallery":
      return <PreviewGallery node={blockToPreviewNode(block)} />
    case "mermaid":
      return <PreviewMermaid data={block.data} />
    case "vega-lite":
      return <PreviewVegaLite node={blockToPreviewNode(block)} />
    case "source":
      return <PreviewSource node={blockToPreviewNode(block)} />
    case "markdown":
      return <PreviewMarkdown node={blockToPreviewNode(block)} />
    case "html":
      return <PreviewHTML node={blockToPreviewNode(block)} />
    case "rich-text":
      return <PreviewRichText node={blockToPreviewNode(block)} />
    case "header":
      return <PreviewHeader node={blockToPreviewNode(block)} />
    case "divider":
      return <PreviewDivider node={blockToPreviewNode(block)} />
    case "button":
      return <PreviewButton node={blockToPreviewNode(block)} />
    case "admonition":
      return <PreviewAdmonition node={blockToPreviewNode(block)} />
    case "project":
      return <PreviewProject node={blockToPreviewNode(block)} />
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
