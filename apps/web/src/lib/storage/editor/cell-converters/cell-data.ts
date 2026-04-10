/**
 * Cell Data Types
 * 
 * Data contém o conteúdo puro da célula, agnóstico de UI.
 * O `type` fica no metadata, não aqui.
 * 
 * Convenção de chaves JSON (minificadas):
 * - c: content (para text cells)
 * - d: data (para decorator cells)
 */

import type { SerializedLexicalNode } from "lexical"
import type { QuizEntry } from "@/components/editor/extras/quiz"
import type { CodeStudioData } from "@/components/editor/extras/code-studio/types"
import type { ImageData } from "@/components/editor/nodes/image-node"
import type { VideoData } from "@/components/editor/nodes/video-node"
import type { AudioData } from "@/components/editor/nodes/audio-node"
import type { GalleryData } from "@/components/editor/nodes/gallery-node"
import type { YouTubeData } from "@/components/editor/nodes/youtube-node"
import type { MermaidData } from "@/components/editor/nodes/mermaid-node"
import type { VegaLiteData } from "@/components/editor/nodes/vega-lite-node"
import type { PresentationData } from "@/components/editor/nodes/presentation/types"
import type { SourceData } from "@/components/editor/nodes/source-node"
import type { SpotifyData } from "@/components/editor/nodes/spotify-node"
import type { ButtonData } from "@/components/editor/nodes/button-node"
import type { HeaderData } from "@/components/editor/nodes/header-node"
import type { DividerData } from "@/components/editor/nodes/divider-node"
import type { AdmonitionData } from "@/components/editor/nodes/admonition-node"
import type { MarkdownData } from "@/components/editor/nodes/markdown-node"
import type { HTMLData } from "@/components/editor/nodes/html-node"
import type { RichTextData } from "@/components/editor/nodes/rich-text-node"
import type { TableData } from "@/components/editor/nodes/table-node"
import type { ProjectData as ProjectNodeData_Internal } from "@/components/editor/nodes/project-node"

// ============================================================================
// Cell Type Enum (usando strings curtas onde possível)
// ============================================================================

export type CellType = 
  | "p"      // paragraph
  | "h"      // heading
  | "q"      // quote
  | "l"      // list
  | "quiz"
  | "code"   // code-studio
  | "img"    // image
  | "vid"    // video
  | "aud"    // audio
  | "gal"    // gallery
  | "yt"     // youtube
  | "spot"   // spotify
  | "mmd"    // mermaid
  | "vega"   // vega-lite
  | "pres"   // presentation
  | "src"    // source
  | "md"     // markdown
  | "html"
  | "rt"     // rich-text
  | "hdr"    // header
  | "div"    // divider
  | "btn"    // button
  | "adm"    // admonition
  | "tbl"    // table
  | "proj"   // project

// Mapeamento para conversão Lexical <-> Cellular
export const LEXICAL_TO_CELL_TYPE: Record<string, CellType> = {
  "paragraph": "p",
  "heading": "h",
  "quote": "q",
  "list": "l",
  "quiz": "quiz",
  "code-studio": "code",
  "image": "img",
  "video": "vid",
  "audio": "aud",
  "gallery": "gal",
  "youtube": "yt",
  "spotify": "spot",
  "mermaid": "mmd",
  "vega-lite": "vega",
  "presentation": "pres",
  "source": "src",
  "markdown": "md",
  "html": "html",
  "rich-text": "rt",
  "header": "hdr",
  "divider": "div",
  "button": "btn",
  "admonition": "adm",
  "table": "tbl",
  "project": "proj",
}

export const CELL_TO_LEXICAL_TYPE: Record<CellType, string> = {
  "p": "paragraph",
  "h": "heading",
  "q": "quote",
  "l": "list",
  "quiz": "quiz",
  "code": "code-studio",
  "img": "image",
  "vid": "video",
  "aud": "audio",
  "gal": "gallery",
  "yt": "youtube",
  "spot": "spotify",
  "mmd": "mermaid",
  "vega": "vega-lite",
  "pres": "presentation",
  "src": "source",
  "md": "markdown",
  "html": "html",
  "rt": "rich-text",
  "hdr": "header",
  "div": "divider",
  "btn": "button",
  "adm": "admonition",
  "tbl": "table",
  "proj": "project",
}

// ============================================================================
// Base Cell Data (sem type - type fica no metadata)
// ============================================================================

// ============================================================================
// Text Cell Data (paragraph, heading, quote, list)
// ============================================================================

export interface TextCellData {
  /** content: SerializedLexicalNode[] */
  c: SerializedLexicalNode[]
}

// ============================================================================
// Decorator Cell Data (nodes with custom data)
// ============================================================================

export interface DecoratorCellData<D> {
  /** data */
  d: D
}

export type QuizCellData = DecoratorCellData<QuizEntry>
export type CodeStudioCellData = DecoratorCellData<CodeStudioData>
export type ImageCellData = DecoratorCellData<ImageData>
export type VideoCellData = DecoratorCellData<VideoData>
export type AudioCellData = DecoratorCellData<AudioData>
export type GalleryCellData = DecoratorCellData<GalleryData>
export type YouTubeCellData = DecoratorCellData<YouTubeData>
export type SpotifyCellData = DecoratorCellData<SpotifyData>
export type MermaidCellData = DecoratorCellData<MermaidData>
export type VegaLiteCellData = DecoratorCellData<VegaLiteData>
export type PresentationCellData = DecoratorCellData<PresentationData>
export type SourceCellData = DecoratorCellData<SourceData>
export type MarkdownCellData = DecoratorCellData<MarkdownData>
export type HTMLCellData = DecoratorCellData<HTMLData>
export type RichTextCellData = DecoratorCellData<RichTextData>
export type HeaderCellData = DecoratorCellData<HeaderData>
export type DividerCellData = DecoratorCellData<DividerData>
export type ButtonCellData = DecoratorCellData<ButtonData>
export type AdmonitionCellData = DecoratorCellData<AdmonitionData>
export type TableCellData = DecoratorCellData<TableData>
export type ProjectCellData = DecoratorCellData<ProjectNodeData_Internal>

// ============================================================================
// Union de todos os CellData
// ============================================================================

/** Union de todos os tipos de data específicos */
export type CellData = 
  | TextCellData
  | QuizCellData
  | CodeStudioCellData
  | ImageCellData
  | VideoCellData
  | AudioCellData
  | GalleryCellData
  | YouTubeCellData
  | SpotifyCellData
  | MermaidCellData
  | VegaLiteCellData
  | PresentationCellData
  | SourceCellData
  | MarkdownCellData
  | HTMLCellData
  | RichTextCellData
  | HeaderCellData
  | DividerCellData
  | ButtonCellData
  | AdmonitionCellData
  | TableCellData
  | ProjectCellData

/** Tipo genérico para qualquer data (para uso em funções genéricas) */
export type AnyCellData = TextCellData | DecoratorCellData<any>

// ============================================================================
// Type guards
// ============================================================================

export function isTextCellData(data: AnyCellData): data is TextCellData {
  return "c" in data && !("d" in data)
}

export function isDecoratorCellData(data: AnyCellData): data is DecoratorCellData<any> {
  return "d" in data
}
