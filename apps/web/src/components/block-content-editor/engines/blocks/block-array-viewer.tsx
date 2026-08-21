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
 * Each block is projected through `blockToView(block)` to the canonical
 * `{ id, type, data, version }` payload expected by preview components.
 */

import { blockToView } from "@game-guild/block-list"
import type { Block, BlockArray } from "@/components/block-content-editor/lib/storage/editor/block-structure"

// Import all preview components
import { PreviewQuiz } from "@/components/block-content-editor/plugins/preview-components/preview-quiz"
import type { QuizSubmissionMode } from "@game-guild/quiz-surface/player"
import { PreviewImage } from "@/components/block-content-editor/plugins/preview-components/preview-image"
import { PreviewGallery } from "@/components/block-content-editor/plugins/preview-components/preview-gallery"
import { PreviewMarkdown } from "@/components/block-content-editor/plugins/preview-components/preview-markdown"
import { PreviewHTML } from "@/components/block-content-editor/plugins/preview-components/preview-html"
import { PreviewVideo } from "@/components/block-content-editor/plugins/preview-components/preview-video"
import { PreviewAudio } from "@/components/block-content-editor/plugins/preview-components/preview-audio"
import { PreviewHeader } from "@/components/block-content-editor/plugins/preview-components/preview-header"
import { PreviewSource } from "@/components/block-content-editor/plugins/preview-components/preview-source"
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
  quizSubmissionMode?: QuizSubmissionMode
}

export function BlockContentRenderer({ block, quizSubmissionMode }: BlockContentRendererProps) {
  switch (block.type) {
    case "quiz":
      return <PreviewQuiz node={blockToView(block)} submissionMode={quizSubmissionMode} />
    case "code-studio":
      return <PreviewCodeStudio data={block.data} />
    case "image":
      return <PreviewImage node={blockToView(block)} />
    case "video":
      return <PreviewVideo node={blockToView(block)} />
    case "audio":
      return <PreviewAudio node={blockToView(block)} />
    case "gallery":
      return <PreviewGallery node={blockToView(block)} />
    case "mermaid":
      return <PreviewMermaid data={block.data} />
    case "vega-lite":
      return <PreviewVegaLite node={blockToView(block)} />
    case "source":
      return <PreviewSource node={blockToView(block)} />
    case "markdown":
      return <PreviewMarkdown node={blockToView(block)} />
    case "html":
      return <PreviewHTML node={blockToView(block)} />
    case "rich-text":
      return <PreviewRichText node={blockToView(block)} />
    case "header":
      return <PreviewHeader node={blockToView(block)} />
    case "project":
      return <PreviewProject node={blockToView(block)} />
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
  quizSubmissionMode?: QuizSubmissionMode
}

export function BlockArrayViewer({ blocks, className, quizSubmissionMode }: BlockArrayViewerProps) {
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
        <BlockContentRenderer key={block.id} block={block} quizSubmissionMode={quizSubmissionMode} />
      ))}
    </div>
  )
}
