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
  createEmpty: () => Block
}

// ============================================================================
// Registry
// ============================================================================

export const BLOCK_REGISTRY: Record<BlockCellType, BlockTypeConfig> = {
  quiz: {
    icon: HelpCircle,
    label: "Quiz",
    description: "Interactive quiz question",
    createEmpty: () => ({
      id: crypto.randomUUID(),
      type: "quiz",
      data: { type: QuizEntryType.SingleChoice, stem: "", options: [{ id: "o1", text: "" }], correctOptionId: "o1", settings: createDefaultSettings() },
    }),
  },
  code: {
    icon: Code,
    label: "Code Studio",
    description: "Code editor with multiple files",
    createEmpty: () => ({
      id: crypto.randomUUID(),
      type: "code",
      data: { id: crypto.randomUUID(), files: [], folders: [], openTabs: [], mode: "execution", language: "javascript", readonly: false, showLineNumbers: true, fontSize: 14, theme: "system", shikiTheme: "github", testCases: {} },
    }),
  },
  img: {
    icon: Image,
    label: "Image",
    description: "Image with caption",
    createEmpty: () => ({
      id: crypto.randomUUID(),
      type: "img",
      data: { src: "", alt: "", caption: "", size: 100, isNew: true },
    }),
  },
  vid: {
    icon: Video,
    label: "Video",
    description: "Video player",
    createEmpty: () => ({
      id: crypto.randomUUID(),
      type: "vid",
      data: { src: "", alt: "", caption: "", size: 100, isNew: true },
    }),
  },
  aud: {
    icon: Music,
    label: "Audio",
    description: "Audio player",
    createEmpty: () => ({
      id: crypto.randomUUID(),
      type: "aud",
      data: { src: "", caption: "", size: 100, isNew: true },
    }),
  },
  gal: {
    icon: GalleryHorizontalEnd,
    label: "Gallery",
    description: "Image gallery",
    createEmpty: () => ({
      id: crypto.randomUUID(),
      type: "gal",
      data: { images: [], layout: "2", caption: "", defaultDisplayMode: "crop", isNew: true },
    }),
  },
  yt: {
    icon: Youtube,
    label: "YouTube",
    description: "YouTube video embed",
    createEmpty: () => ({
      id: crypto.randomUUID(),
      type: "yt",
      data: { videoId: "", title: "", caption: "", size: 100, isNew: true, startAt: 0, showControls: true, showInfo: true, showRelated: false },
    }),
  },
  spot: {
    icon: Music2,
    label: "Spotify",
    description: "Spotify embed",
    createEmpty: () => ({
      id: crypto.randomUUID(),
      type: "spot",
      data: { spotifyId: "", type: "track", title: "", caption: "", size: 100, isNew: true },
    }),
  },
  mmd: {
    icon: GitGraph,
    label: "Mermaid",
    description: "Mermaid diagram",
    createEmpty: () => ({
      id: crypto.randomUUID(),
      type: "mmd",
      data: { code: "", type: "flowchart", direction: "TD", theme: "default", themeMode: "system" },
    }),
  },
  vega: {
    icon: BarChart3,
    label: "Vega-Lite",
    description: "Data visualization chart",
    createEmpty: () => ({
      id: crypto.randomUUID(),
      type: "vega",
      data: { spec: "", title: "", caption: "", theme: "default", themeMode: "system" },
    }),
  },
  pres: {
    icon: Presentation,
    label: "Presentation",
    description: "Slide presentation",
    createEmpty: () => ({
      id: crypto.randomUUID(),
      type: "pres",
      data: { slides: [], title: "Untitled Presentation", theme: "light", transitionEffect: "fade", autoAdvance: false, autoAdvanceDelay: 5, autoAdvanceLoop: false, showControls: true, isNew: true },
    }),
  },
  src: {
    icon: BookOpen,
    label: "Sources",
    description: "Reference sources",
    createEmpty: () => ({
      id: crypto.randomUUID(),
      type: "src",
      data: { sources: [], title: "References", style: "apa", isNew: true },
    }),
  },
  md: {
    icon: FileText,
    label: "Markdown",
    description: "Markdown content",
    createEmpty: () => ({
      id: crypto.randomUUID(),
      type: "md",
      data: { content: "", title: "", caption: "" },
    }),
  },
  html: {
    icon: FileCode,
    label: "HTML",
    description: "Raw HTML content",
    createEmpty: () => ({
      id: crypto.randomUUID(),
      type: "html",
      data: { content: "" },
    }),
  },
  rt: {
    icon: FileText,
    label: "Rich Text",
    description: "Rich text content with formatting",
    createEmpty: () => ({
      id: crypto.randomUUID(),
      type: "rt",
      data: { content: "" },
    }),
  },
  hdr: {
    icon: Heading,
    label: "Header",
    description: "Section header",
    createEmpty: () => ({
      id: crypto.randomUUID(),
      type: "hdr",
      data: { text: "", level: 1, style: "default" },
    }),
  },
  div: {
    icon: Minus,
    label: "Divider",
    description: "Visual divider",
    createEmpty: () => ({
      id: crypto.randomUUID(),
      type: "div",
      data: { style: "simple", thickness: "thin", spacing: "md", colorPalette: "blue", isNew: true },
    }),
  },
  btn: {
    icon: MousePointerClick,
    label: "Button",
    description: "Clickable button",
    createEmpty: () => ({
      id: crypto.randomUUID(),
      type: "btn",
      data: { text: "Button", url: "", actionType: "url", variant: "solid", size: "md", showIcon: false, iconVariant: 0, iconPosition: "left", iconSize: "md", colorPalette: "blue", fontFamily: "sans", fontSize: "md", isNew: true },
    }),
  },
  adm: {
    icon: AlertTriangle,
    label: "Admonition",
    description: "Info/warning callout",
    createEmpty: () => ({
      id: crypto.randomUUID(),
      type: "adm",
      data: { title: "", content: "", type: "note", design: "default", isNew: true },
    }),
  },
  tbl: {
    icon: Table2,
    label: "Table",
    description: "Data table",
    createEmpty: () => ({
      id: crypto.randomUUID(),
      type: "tbl",
      data: { rows: 3, columns: 3, style: "default", showHeader: true, showBorders: true, cells: [], caption: "", isNew: false },
    }),
  },
  proj: {
    icon: FolderOpen,
    label: "Project",
    description: "Embedded project",
    createEmpty: () => ({
      id: crypto.randomUUID(),
      type: "proj",
      data: { projectId: "", projectName: "", editorState: null, isLocalCopy: false },
    }),
  },
}

/** Get block config by BlockCellType, returns undefined for unknown types */
export function getBlockConfig(type: string): BlockTypeConfig | undefined {
  return BLOCK_REGISTRY[type as BlockCellType]
}
