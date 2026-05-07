/**
 * Cell Metadata Types
 * 
 * Metadata contém propriedades específicas do editor/UI,
 * incluindo o tipo da célula (t).
 * Cada editor (Lexical, Slate, etc.) terá seus próprios tipos.
 * 
 * Convenção de chaves JSON (minificadas):
 * - t: type (CellType)
 * - d: direction
 * - f: format
 * - i: indent
 * - v: version
 * - tg: tag (heading h1-h6)
 * - lt: listType
 * - s: start (numbered list)
 */

import type { CellType } from "./cell-data"

// ============================================================================
// Base Metadata (comum a todos editores)
// ============================================================================

export interface BaseMetadata<T extends CellType = CellType> {
  /** type */
  t: T
  /** version */
  v: number
}

// ============================================================================
// Lexical Metadata
// ============================================================================

export interface LexicalMeta<T extends CellType = CellType> extends BaseMetadata<T> {
  /** direction: "ltr" | "rtl" | null */
  d: "ltr" | "rtl" | null
  /** format: string | number */
  f: string | number
  /** indent: number */
  i: number
}

/** Metadata para paragraph cells */
export interface ParagraphLexicalMeta extends LexicalMeta<"p"> {}

/** Metadata para quote cells */
export interface QuoteLexicalMeta extends LexicalMeta<"q"> {}

/** Metadata para heading cells */
export interface HeadingLexicalMeta extends LexicalMeta<"h"> {
  /** tag: h1-h6 */
  tg: "h1" | "h2" | "h3" | "h4" | "h5" | "h6"
}

/** Metadata para list cells */
export interface ListLexicalMeta extends LexicalMeta<"l"> {
  /** listType */
  lt: "number" | "bullet" | "check"
  /** start (para listas numeradas) */
  s: number
  /** tag: ul | ol */
  tg: "ul" | "ol"
}

/** Metadata para decorator nodes (quiz, image, etc.) - minimal */
export interface DecoratorLexicalMeta<T extends CellType = CellType> extends BaseMetadata<T> {}

// ============================================================================
// Union de todos os metadatas Lexical
// ============================================================================

export type LexicalMetadata = 
  | ParagraphLexicalMeta
  | QuoteLexicalMeta
  | HeadingLexicalMeta 
  | ListLexicalMeta 
  | DecoratorLexicalMeta

// ============================================================================
// Factory functions para criar metadata com defaults
// ============================================================================

export function createParagraphMeta(
  direction: "ltr" | "rtl" | null = null,
  format: string | number = "",
  indent: number = 0
): ParagraphLexicalMeta {
  return { t: "p", v: 1, d: direction, f: format, i: indent }
}

export function createQuoteMeta(
  direction: "ltr" | "rtl" | null = null,
  format: string | number = "",
  indent: number = 0
): QuoteLexicalMeta {
  return { t: "q", v: 1, d: direction, f: format, i: indent }
}

export function createHeadingMeta(
  tag: "h1" | "h2" | "h3" | "h4" | "h5" | "h6" = "h1",
  direction: "ltr" | "rtl" | null = null,
  format: string | number = "",
  indent: number = 0
): HeadingLexicalMeta {
  return { t: "h", v: 1, d: direction, f: format, i: indent, tg: tag }
}

export function createListMeta(
  listType: "number" | "bullet" | "check" = "bullet",
  start: number = 1,
  direction: "ltr" | "rtl" | null = null,
  format: string | number = "",
  indent: number = 0
): ListLexicalMeta {
  return { 
    t: "l",
    v: 1, 
    d: direction, 
    f: format, 
    i: indent, 
    lt: listType, 
    s: start,
    tg: listType === "number" ? "ol" : "ul"
  }
}

export function createDecoratorMeta<T extends CellType>(type: T): DecoratorLexicalMeta<T> {
  return { t: type, v: 1 }
}
