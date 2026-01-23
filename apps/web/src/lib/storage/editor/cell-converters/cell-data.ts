/**
 * Cell Data Types
 * 
 * Data contém o conteúdo puro da célula, agnóstico de UI.
 * O `type` fica apenas aqui, não no metadata.
 * 
 * Convenção de chaves JSON (minificadas):
 * - t: type
 * - c: content (para text cells)
 * - d: data (para decorator cells)
 */

import type { SerializedLexicalNode } from "lexical"
import type { QuizData } from "@/components/editor/nodes/quiz-node"
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
  "hdr": "header",
  "div": "divider",
  "btn": "button",
  "adm": "admonition",
  "tbl": "table",
  "proj": "project",
}

// ============================================================================
// Base Cell Data
// ============================================================================

interface BaseCellData<T extends CellType> {
  /** type */
  t: T
}

// ============================================================================
// Text Cell Data (paragraph, heading, quote, list)
// ============================================================================

export interface TextCellData<T extends "p" | "h" | "q" | "l"> extends BaseCellData<T> {
  /** content: SerializedLexicalNode[] */
  c: SerializedLexicalNode[]
}

export type ParagraphData = TextCellData<"p">
export type HeadingData = TextCellData<"h">
export type QuoteData = TextCellData<"q">
export type ListData = TextCellData<"l">

// ============================================================================
// Decorator Cell Data (nodes with custom data)
// ============================================================================

interface DecoratorCellData<T extends CellType, D> extends BaseCellData<T> {
  /** data */
  d: D
}

export type QuizCellData = DecoratorCellData<"quiz", QuizData>
export type CodeStudioCellData = DecoratorCellData<"code", CodeStudioData>
export type ImageCellData = DecoratorCellData<"img", ImageData>
export type VideoCellData = DecoratorCellData<"vid", VideoData>
export type AudioCellData = DecoratorCellData<"aud", AudioData>
export type GalleryCellData = DecoratorCellData<"gal", GalleryData>
export type YouTubeCellData = DecoratorCellData<"yt", YouTubeData>
export type SpotifyCellData = DecoratorCellData<"spot", SpotifyData>
export type MermaidCellData = DecoratorCellData<"mmd", MermaidData>
export type VegaLiteCellData = DecoratorCellData<"vega", VegaLiteData>
export type PresentationCellData = DecoratorCellData<"pres", PresentationData>
export type SourceCellData = DecoratorCellData<"src", SourceData>
export type MarkdownCellData = DecoratorCellData<"md", MarkdownData>
export type HTMLCellData = DecoratorCellData<"html", HTMLData>
export type HeaderCellData = DecoratorCellData<"hdr", HeaderData>
export type DividerCellData = DecoratorCellData<"div", DividerData>
export type ButtonCellData = DecoratorCellData<"btn", ButtonData>
export type AdmonitionCellData = DecoratorCellData<"adm", AdmonitionData>
export type TableCellData = DecoratorCellData<"tbl", TableData>
export type ProjectCellData = DecoratorCellData<"proj", ProjectNodeData_Internal>

// ============================================================================
// Union de todos os CellData
// ============================================================================

export type CellData = 
  | ParagraphData
  | HeadingData
  | QuoteData
  | ListData
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
  | HeaderCellData
  | DividerCellData
  | ButtonCellData
  | AdmonitionCellData
  | TableCellData
  | ProjectCellData

// ============================================================================
// Type guards
// ============================================================================

export function isTextCell(data: CellData): data is TextCellData<"p" | "h" | "q" | "l"> {
  return data.t === "p" || data.t === "h" || data.t === "q" || data.t === "l"
}

export function isDecoratorCell(data: CellData): data is Exclude<CellData, TextCellData<"p" | "h" | "q" | "l">> {
  return !isTextCell(data)
}
