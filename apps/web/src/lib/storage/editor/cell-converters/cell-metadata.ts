/**
 * Cell Metadata Types
 * 
 * Metadata contém propriedades específicas do editor/UI.
 * Cada editor (Lexical, Slate, etc.) terá seus próprios tipos.
 * 
 * Convenção de chaves JSON (minificadas):
 * - d: direction
 * - f: format
 * - i: indent
 * - v: version
 * - t: tag (heading)
 * - lt: listType
 * - s: start (numbered list)
 */

// ============================================================================
// Base Metadata (comum a todos editores)
// ============================================================================

export interface BaseMetadata {
  /** version */
  v: number
}

// ============================================================================
// Lexical Metadata
// ============================================================================

export interface LexicalMeta extends BaseMetadata {
  /** direction: "ltr" | "rtl" | null */
  d: "ltr" | "rtl" | null
  /** format: string | number */
  f: string | number
  /** indent: number */
  i: number
}

/** Metadata para text cells (paragraph, quote) */
export interface TextLexicalMeta extends LexicalMeta {}

/** Metadata para heading cells */
export interface HeadingLexicalMeta extends LexicalMeta {
  /** tag: h1-h6 */
  t: "h1" | "h2" | "h3" | "h4" | "h5" | "h6"
}

/** Metadata para list cells */
export interface ListLexicalMeta extends LexicalMeta {
  /** listType */
  lt: "number" | "bullet" | "check"
  /** start (para listas numeradas) */
  s: number
  /** tag: ul | ol */
  t: "ul" | "ol"
}

/** Metadata para decorator nodes (quiz, image, etc.) - minimal */
export interface DecoratorLexicalMeta extends BaseMetadata {}

// ============================================================================
// Union de todos os metadatas Lexical
// ============================================================================

export type LexicalMetadata = 
  | TextLexicalMeta 
  | HeadingLexicalMeta 
  | ListLexicalMeta 
  | DecoratorLexicalMeta

// ============================================================================
// Factory functions para criar metadata com defaults
// ============================================================================

export function createTextMeta(
  direction: "ltr" | "rtl" | null = null,
  format: string | number = "",
  indent: number = 0
): TextLexicalMeta {
  return { v: 1, d: direction, f: format, i: indent }
}

export function createHeadingMeta(
  tag: "h1" | "h2" | "h3" | "h4" | "h5" | "h6" = "h1",
  direction: "ltr" | "rtl" | null = null,
  format: string | number = "",
  indent: number = 0
): HeadingLexicalMeta {
  return { v: 1, d: direction, f: format, i: indent, t: tag }
}

export function createListMeta(
  listType: "number" | "bullet" | "check" = "bullet",
  start: number = 1,
  direction: "ltr" | "rtl" | null = null,
  format: string | number = "",
  indent: number = 0
): ListLexicalMeta {
  return { 
    v: 1, 
    d: direction, 
    f: format, 
    i: indent, 
    lt: listType, 
    s: start,
    t: listType === "number" ? "ol" : "ul"
  }
}

export function createDecoratorMeta(): DecoratorLexicalMeta {
  return { v: 1 }
}
