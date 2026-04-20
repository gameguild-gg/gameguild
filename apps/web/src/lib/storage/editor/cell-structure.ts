/**
 * Cell Structure v0
 * 
 * Modelo celular genérico com separação [data, metadata].
 * Cada célula é uma tupla onde:
 * - índice 0: CellData (conteúdo puro, agnóstico de UI)
 * - índice 1: CellMetadata (propriedades específicas do editor, incluindo type)
 * 
 * Estrutura do documento:
 * { v: 0, u: "lexical", c: [[data, meta], [data, meta], ...] }
 */

import type { 
  AnyCellData,
  TextCellData,
  DecoratorCellData,
} from "./cell-converters/cell-data"
import type { 
  LexicalMetadata,
  ParagraphLexicalMeta,
  QuoteLexicalMeta,
  HeadingLexicalMeta,
  ListLexicalMeta,
  DecoratorLexicalMeta,
} from "./cell-converters/cell-metadata"

// ============================================================================
// Cell Tuple: [Data, Metadata]
// ============================================================================

/** Tupla genérica: [CellData, Metadata] */
export type CellTuple<D extends AnyCellData = AnyCellData, M extends LexicalMetadata = LexicalMetadata> = [D, M]

// Tuplas tipadas por tipo de célula
export type ParagraphTuple = CellTuple<TextCellData, ParagraphLexicalMeta>
export type HeadingTuple = CellTuple<TextCellData, HeadingLexicalMeta>
export type QuoteTuple = CellTuple<TextCellData, QuoteLexicalMeta>
export type ListTuple = CellTuple<TextCellData, ListLexicalMeta>
export type DecoratorTuple = CellTuple<DecoratorCellData<any>, DecoratorLexicalMeta<any>>

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
