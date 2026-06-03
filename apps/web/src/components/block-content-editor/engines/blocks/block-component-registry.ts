/**
 * Block Component Registry
 *
 * Central mapping of CellType → { icon, label, description, createEmpty() }
 * Used by the Block Array Engine to know what block types are available
 * and how to create empty instances of each.
 */

import type { LucideIcon } from "lucide-react"
import {
  HelpCircle,
  Code,
  Image,
  Video,
  Music,
  GalleryHorizontalEnd,
  Youtube,
  Music2,
  GitGraph,
  BarChart3,
  Presentation,
  BookOpen,
  FileText,
  FileCode,
  Heading,
  Minus,
  MousePointerClick,
  AlertTriangle,
  Table2,
  FolderOpen,
} from "lucide-react"
import type { Block, BlockCellType } from "@/components/block-content-editor/lib/storage/editor/block-structure"
import { BLOCK_CELL_TYPES } from "@/components/block-content-editor/lib/storage/editor/block-structure"
import { QuizEntryType, createDefaultSettings } from "@/components/block-content-editor/extras/quiz"
import { createDefaultHTMLData } from "@/components/block-content-editor/extras/html/html-utils"

// Re-export for consumers that import from here
export type { BlockCellType } from "@/components/block-content-editor/lib/storage/editor/block-structure"
export { BLOCK_CELL_TYPES } from "@/components/block-content-editor/lib/storage/editor/block-structure"

// ============================================================================
// Block Type Configuration
// ============================================================================

export interface BlockTypeConfig {
  icon: LucideIcon
  label: string
  description: string
  /**
   * Build a fresh empty block. The caller is responsible for providing the
   * block id — typically via `nextBlockId(currentBlocks)` for top-level
   * inserts.
   */
  createEmpty: (id: string) => Block
}

// ============================================================================
// Registry
// ============================================================================

export const BLOCK_REGISTRY: Record<BlockCellType, BlockTypeConfig> = {
  "quiz": {
    icon: HelpCircle,
    label: "Quiz",
    description: "Interactive quiz question",
    createEmpty: (id) => ({
      id,
      type: "quiz",
      data: { type: QuizEntryType.SingleChoice, stem: "", options: [{ id: "o1", text: "" }], correctOptionId: "o1", settings: createDefaultSettings() },
    }),
  },
  "code-studio": {
    icon: Code,
    label: "Code Studio",
    description: "Code editor with multiple files",
    createEmpty: (id) => ({
      id,
      type: "code-studio",
      data: { id: crypto.randomUUID(), files: [], folders: [], openTabs: [], mode: "execution", language: "javascript", readonly: false, showLineNumbers: true, fontSize: 14, theme: "system", shikiTheme: "github", testCases: {} },
    }),
  },
  "image": {
    icon: Image,
    label: "Image",
    description: "Image with caption",
    createEmpty: (id) => ({
      id,
      type: "image",
      data: { src: "", alt: "", caption: "", size: 100, isNew: true },
    }),
  },
  "video": {
    icon: Video,
    label: "Video",
    description: "Video player",
    createEmpty: (id) => ({
      id,
      type: "video",
      data: { src: "", alt: "", caption: "", size: 100, isNew: true },
    }),
  },
  "audio": {
    icon: Music,
    label: "Audio",
    description: "Audio player",
    createEmpty: (id) => ({
      id,
      type: "audio",
      data: { src: "", caption: "", size: 100, isNew: true },
    }),
  },
  "gallery": {
    icon: GalleryHorizontalEnd,
    label: "Gallery",
    description: "Image gallery",
    createEmpty: (id) => ({
      id,
      type: "gallery",
      data: { images: [], layout: "2", caption: "", defaultDisplayMode: "crop", isNew: true },
    }),
  },
  "mermaid": {
    icon: GitGraph,
    label: "Mermaid",
    description: "Mermaid diagram",
    createEmpty: (id) => ({
      id,
      type: "mermaid",
      data: { code: "", type: "flowchart", direction: "TD", theme: "default", themeMode: "system" },
    }),
  },
  "vega-lite": {
    icon: BarChart3,
    label: "Vega-Lite",
    description: "Data visualization chart",
    createEmpty: (id) => ({
      id,
      type: "vega-lite",
      data: { spec: "", title: "", caption: "", theme: "default", themeMode: "system" },
    }),
  },
  "presentation": {
    icon: Presentation,
    label: "Presentation",
    description: "Slide presentation",
    createEmpty: (id) => ({
      id,
      type: "presentation",
      data: { slides: [], title: "Untitled Presentation", theme: "light", transitionEffect: "fade", autoAdvance: false, autoAdvanceDelay: 5, autoAdvanceLoop: false, showControls: true, isNew: true },
    }),
  },
  "source": {
    icon: BookOpen,
    label: "Sources",
    description: "Reference sources",
    createEmpty: (id) => ({
      id,
      type: "source",
      data: { sources: [], title: "References", style: "apa", isNew: true },
    }),
  },
  "markdown": {
    icon: FileText,
    label: "Markdown",
    description: "Markdown content",
    createEmpty: (id) => ({
      id,
      type: "markdown",
      data: { content: "", title: "", caption: "" },
    }),
  },
  "html": {
    icon: FileCode,
    label: "HTML",
    description: "Custom HTML/CSS/XML block",
    createEmpty: (id) => ({
      id,
      type: "html",
      data: createDefaultHTMLData(),
    }),
  },
  "rich-text": {
    icon: FileText,
    label: "Rich Text",
    description: "Rich text content with formatting",
    createEmpty: (id) => ({
      id,
      type: "rich-text",
      data: { content: null },
    }),
  },
  "header": {
    icon: Heading,
    label: "Header",
    description: "Section header",
    createEmpty: (id) => ({
      id,
      type: "header",
      data: { text: "", level: 1, style: "default" },
    }),
  },
  "divider": {
    icon: Minus,
    label: "Divider",
    description: "Visual divider",
    createEmpty: (id) => ({
      id,
      type: "divider",
      data: { style: "simple", thickness: "thin", spacing: "md", colorPalette: "blue", isNew: true },
    }),
  },
  "button": {
    icon: MousePointerClick,
    label: "Button",
    description: "Clickable button",
    createEmpty: (id) => ({
      id,
      type: "button",
      data: { text: "Button", url: "", actionType: "url", variant: "solid", size: "md", showIcon: false, iconVariant: 0, iconPosition: "left", iconSize: "md", colorPalette: "blue", fontFamily: "sans", fontSize: "md", isNew: true },
    }),
  },
  "admonition": {
    icon: AlertTriangle,
    label: "Admonition",
    description: "Info/warning callout",
    createEmpty: (id) => ({
      id,
      type: "admonition",
      data: { title: "", content: "", type: "note", design: "default", isNew: true },
    }),
  },
  "project": {
    icon: FolderOpen,
    label: "Project",
    description: "Embedded project",
    createEmpty: (id) => ({
      id,
      type: "project",
      data: { projectId: "", projectName: "", editorState: null, isLocalCopy: false },
    }),
  },
}

/** Get block config by BlockCellType, returns undefined for unknown types */
export function getBlockConfig(type: string): BlockTypeConfig | undefined {
  return BLOCK_REGISTRY[type as BlockCellType]
}
