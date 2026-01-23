/**
 * Cell Structure v0
 * 
 * Modelo celular genérico com separação [data, metadata].
 * Cada célula é uma tupla onde:
 * - índice 0: CellData (conteúdo puro, agnóstico de UI)
 * - índice 1: CellMetadata (propriedades específicas do editor)
 * 
 * Estrutura do documento:
 * { v: 0, u: "lexical", c: [[data, meta], [data, meta], ...] }
 */

// Re-export dos tipos componentizados
export * from "./cell-data"
export * from "./cell-metadata"

// Re-export dos converters (inclui lexicalToCells, cellsToLexical, toCells, fromCells)
export * from "./cell-converters"

import type { 
  CellData, 
  ParagraphData,
  HeadingData,
  QuoteData,
  ListData,
} from "./cell-data"
import type { 
  LexicalMetadata,
  TextLexicalMeta,
  HeadingLexicalMeta,
  ListLexicalMeta,
  DecoratorLexicalMeta,
} from "./cell-metadata"
import { createTextMeta } from "./cell-metadata"

// ============================================================================
// Cell Tuple: [Data, Metadata]
// ============================================================================

/** Tupla genérica: [CellData, Metadata] */
export type CellTuple<D extends CellData = CellData, M extends LexicalMetadata = LexicalMetadata> = [D, M]

// Tuplas tipadas por tipo de célula
export type ParagraphTuple = CellTuple<ParagraphData, TextLexicalMeta>
export type HeadingTuple = CellTuple<HeadingData, HeadingLexicalMeta>
export type QuoteTuple = CellTuple<QuoteData, TextLexicalMeta>
export type ListTuple = CellTuple<ListData, ListLexicalMeta>
export type DecoratorTuple = CellTuple<Exclude<CellData, ParagraphData | HeadingData | QuoteData | ListData>, DecoratorLexicalMeta>

/** Union de todas as tuplas possíveis */
export type Cell = ParagraphTuple | HeadingTuple | QuoteTuple | ListTuple | DecoratorTuple

// ============================================================================
// Cellular Document
// ============================================================================

/** UI de origem do documento */
export type UIOrigin = "lexical" | "slate" | "prosemirror" | "tiptap"

/** Documento celular v0 */
export interface CellularDocument {
  /** schema version */
  v: 0
  /** ui origin */
  u: UIOrigin
  /** cells: array de tuplas [data, metadata] */
  c: Cell[]
}

/** Alias para compatibilidade - agora é o array de células */
export type CellularContent = Cell[]

// ============================================================================
// Helpers para criar documentos
// ============================================================================

export function createEmptyDocument(origin: UIOrigin = "lexical"): CellularDocument {
  return {
    v: 0,
    u: origin,
    c: [[
      { t: "p", c: [] },
      createTextMeta()
    ]]
  }
}
