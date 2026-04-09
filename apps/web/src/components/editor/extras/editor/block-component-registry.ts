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
import type { CellType } from "@/lib/storage/editor/cell-converters/cell-data"
import type { DecoratorCellData } from "@/lib/storage/editor/cell-converters/cell-data"
import { createDecoratorMeta } from "@/lib/storage/editor/cell-converters/cell-metadata"
import type { DecoratorLexicalMeta } from "@/lib/storage/editor/cell-converters/cell-metadata"

// ============================================================================
// Block Type Configuration
// ============================================================================

export interface BlockTypeConfig {
  icon: LucideIcon
  label: string
  description: string
  createEmpty: () => [DecoratorCellData<any>, DecoratorLexicalMeta<any>]
}

/** Decorator CellTypes supported by the Block Array Engine */
export const BLOCK_CELL_TYPES = [
  "quiz", "code", "img", "vid", "aud", "gal", "yt", "spot",
  "mmd", "vega", "pres", "src", "md", "html", "hdr", "div",
  "btn", "adm", "tbl", "proj",
] as const satisfies readonly CellType[]

export type BlockCellType = typeof BLOCK_CELL_TYPES[number]

// ============================================================================
// Registry
// ============================================================================

export const BLOCK_REGISTRY: Record<BlockCellType, BlockTypeConfig> = {
  quiz: {
    icon: HelpCircle,
    label: "Quiz",
    description: "Interactive quiz question",
    createEmpty: () => [
      { d: { type: "SINGLE_CHOICE", stem: "", options: [{ id: "o1", text: "" }], correctOptionId: "o1", settings: {} } },
      createDecoratorMeta("quiz"),
    ],
  },
  code: {
    icon: Code,
    label: "Code Studio",
    description: "Code editor with multiple files",
    createEmpty: () => [
      { d: { id: crypto.randomUUID(), files: [], folders: [], openTabs: [], activeFileId: null, mode: "single", language: "javascript", readonly: false, showLineNumbers: true, fontSize: 14, theme: "system", shikiTheme: "github", testCases: {} } },
      createDecoratorMeta("code"),
    ],
  },
  img: {
    icon: Image,
    label: "Image",
    description: "Image with caption",
    createEmpty: () => [
      { d: { src: "", alt: "", caption: "", size: 100, isNew: true } },
      createDecoratorMeta("img"),
    ],
  },
  vid: {
    icon: Video,
    label: "Video",
    description: "Video player",
    createEmpty: () => [
      { d: { src: "", alt: "", caption: "", size: 100, isNew: true } },
      createDecoratorMeta("vid"),
    ],
  },
  aud: {
    icon: Music,
    label: "Audio",
    description: "Audio player",
    createEmpty: () => [
      { d: { src: "", caption: "", size: 100, isNew: true } },
      createDecoratorMeta("aud"),
    ],
  },
  gal: {
    icon: GalleryHorizontalEnd,
    label: "Gallery",
    description: "Image gallery",
    createEmpty: () => [
      { d: { images: [], layout: "2", caption: "", defaultDisplayMode: "crop", isNew: true } },
      createDecoratorMeta("gal"),
    ],
  },
  yt: {
    icon: Youtube,
    label: "YouTube",
    description: "YouTube video embed",
    createEmpty: () => [
      { d: { videoId: "", title: "", caption: "", size: 100, isNew: true, startAt: 0, showControls: true, showInfo: true, showRelated: false } },
      createDecoratorMeta("yt"),
    ],
  },
  spot: {
    icon: Music2,
    label: "Spotify",
    description: "Spotify embed",
    createEmpty: () => [
      { d: { spotifyId: "", type: "track", title: "", caption: "", size: 100, isNew: true } },
      createDecoratorMeta("spot"),
    ],
  },
  mmd: {
    icon: GitGraph,
    label: "Mermaid",
    description: "Mermaid diagram",
    createEmpty: () => [
      { d: { code: "", type: "flowchart", direction: "TD", theme: "default", themeMode: "system" } },
      createDecoratorMeta("mmd"),
    ],
  },
  vega: {
    icon: BarChart3,
    label: "Vega-Lite",
    description: "Data visualization chart",
    createEmpty: () => [
      { d: { spec: "", title: "", caption: "", theme: "default", themeMode: "system" } },
      createDecoratorMeta("vega"),
    ],
  },
  pres: {
    icon: Presentation,
    label: "Presentation",
    description: "Slide presentation",
    createEmpty: () => [
      { d: { slides: [], title: "Untitled Presentation", theme: "light", transitionEffect: "fade", autoAdvance: false, autoAdvanceDelay: 5, autoAdvanceLoop: false, showControls: true, isNew: true } },
      createDecoratorMeta("pres"),
    ],
  },
  src: {
    icon: BookOpen,
    label: "Sources",
    description: "Reference sources",
    createEmpty: () => [
      { d: { sources: [], title: "References", style: "apa", isNew: true } },
      createDecoratorMeta("src"),
    ],
  },
  md: {
    icon: FileText,
    label: "Markdown",
    description: "Markdown content",
    createEmpty: () => [
      { d: { content: "", title: "", caption: "" } },
      createDecoratorMeta("md"),
    ],
  },
  html: {
    icon: FileCode,
    label: "HTML",
    description: "Raw HTML content",
    createEmpty: () => [
      { d: { content: "" } },
      createDecoratorMeta("html"),
    ],
  },
  hdr: {
    icon: Heading,
    label: "Header",
    description: "Section header",
    createEmpty: () => [
      { d: { text: "", level: 1, style: "default" } },
      createDecoratorMeta("hdr"),
    ],
  },
  div: {
    icon: Minus,
    label: "Divider",
    description: "Visual divider",
    createEmpty: () => [
      { d: { style: "simple", thickness: "thin", spacing: "md", colorPalette: "blue", isNew: true } },
      createDecoratorMeta("div"),
    ],
  },
  btn: {
    icon: MousePointerClick,
    label: "Button",
    description: "Clickable button",
    createEmpty: () => [
      { d: { text: "Button", url: "", actionType: "url", variant: "solid", size: "md", showIcon: false, iconVariant: 0, iconPosition: "left", iconSize: "md", colorPalette: "blue", fontFamily: "sans", fontSize: "md", isNew: true } },
      createDecoratorMeta("btn"),
    ],
  },
  adm: {
    icon: AlertTriangle,
    label: "Admonition",
    description: "Info/warning callout",
    createEmpty: () => [
      { d: { title: "", content: "", type: "note", design: "default", isNew: true } },
      createDecoratorMeta("adm"),
    ],
  },
  tbl: {
    icon: Table2,
    label: "Table",
    description: "Data table",
    createEmpty: () => [
      { d: { rows: 3, columns: 3, style: "default", showHeader: true, showBorders: true, cells: {}, caption: "", isNew: false } },
      createDecoratorMeta("tbl"),
    ],
  },
  proj: {
    icon: FolderOpen,
    label: "Project",
    description: "Embedded project",
    createEmpty: () => [
      { d: { projectId: "", projectName: "", projectType: "type1", editorState: null, isLocalCopy: false } },
      createDecoratorMeta("proj"),
    ],
  },
}

/** Get block config by CellType, returns undefined for text types (p, h, q, l) */
export function getBlockConfig(cellType: CellType): BlockTypeConfig | undefined {
  return BLOCK_REGISTRY[cellType as BlockCellType]
}
