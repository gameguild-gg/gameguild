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
// Text Cell Data (paragraph, heading, quote, list)
// ============================================================================

export interface TextCellData {
  /** content: array of serialized inline nodes */
  c: any[]
}

// ============================================================================
// Decorator Cell Data (nodes with custom data)
// ============================================================================

export interface DecoratorCellData<D = any> {
  /** data */
  d: D
}

/** Tipo genérico para qualquer data (para uso em funções genéricas) */
export type AnyCellData = TextCellData | DecoratorCellData<any>
