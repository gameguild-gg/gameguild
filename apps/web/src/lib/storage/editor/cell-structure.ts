/**
 * Cell Structure v0
 * 
 * Modelo celular genérico com separação [data, metadata].
 * Cada célula é uma tupla onde:
 * - índice 0: CellData (conteúdo puro, agnóstico de UI)
 * - índice 1: CellMetadata (propriedades específicas do editor)
 * 
 * Estrutura do documento:
 * { v: 0, c: [[data, meta], [data, meta], ...] }
 */

import type { SerializedEditorState, SerializedLexicalNode } from "lexical"

// Re-export dos tipos componentizados
export * from "./cell-data"
export * from "./cell-metadata"

import type { 
  CellData, 
  CellType,
  ParagraphData,
  HeadingData,
  QuoteData,
  ListData,
} from "./cell-data"
import { 
  LEXICAL_TO_CELL_TYPE, 
  CELL_TO_LEXICAL_TYPE,
  isTextCell,
} from "./cell-data"
import type { 
  LexicalMetadata,
  TextLexicalMeta,
  HeadingLexicalMeta,
  ListLexicalMeta,
  DecoratorLexicalMeta,
} from "./cell-metadata"
import {
  createTextMeta,
  createHeadingMeta,
  createListMeta,
  createDecoratorMeta,
} from "./cell-metadata"

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

/** Documento celular v0 */
export interface CellularDocument {
  /** schema version */
  v: 0
  /** cells: array de tuplas [data, metadata] */
  c: Cell[]
}

/** Alias para compatibilidade - agora é o array de células */
export type CellularContent = Cell[]

// ============================================================================
// Conversão: Lexical -> Cellular
// ============================================================================

export function lexicalToCells(editorState: SerializedEditorState): CellularDocument {
  const cells: Cell[] = []

  function processNode(node: SerializedLexicalNode) {
    const lexicalType = node.type
    const cellType = LEXICAL_TO_CELL_TYPE[lexicalType]
    
    if (!cellType) {
      // Tipo não mapeado - processa filhos recursivamente
      if ((node as any).children) {
        (node as any).children.forEach(processNode)
      }
      return
    }

    switch (cellType) {
      // Text cells
      case "p":
        cells.push([
          { t: "p", c: (node as any).children || [] },
          createTextMeta((node as any).direction, (node as any).format, (node as any).indent)
        ])
        break
      case "h":
        cells.push([
          { t: "h", c: (node as any).children || [] },
          createHeadingMeta((node as any).tag, (node as any).direction, (node as any).format, (node as any).indent)
        ])
        break
      case "q":
        cells.push([
          { t: "q", c: (node as any).children || [] },
          createTextMeta((node as any).direction, (node as any).format, (node as any).indent)
        ])
        break
      case "l":
        cells.push([
          { t: "l", c: (node as any).children || [] },
          createListMeta((node as any).listType, (node as any).start, (node as any).direction, (node as any).format, (node as any).indent)
        ])
        break
      // Decorator cells - todos seguem o mesmo padrão
      default:
        cells.push([
          { t: cellType, d: (node as any).data } as any,
          createDecoratorMeta()
        ])
        break
    }
  }

  if (editorState.root?.children) {
    editorState.root.children.forEach(processNode)
  }

  return { v: 0, c: cells }
}

// ============================================================================
// Conversão: Cellular -> Lexical
// ============================================================================

/** Cria estado Lexical vazio mas válido */
function createEmptyLexicalState(): SerializedEditorState {
  return {
    root: {
      type: "root",
      format: "",
      indent: 0,
      version: 1,
      children: [{
        type: "paragraph",
        children: [],
        direction: null,
        format: "",
        indent: 0,
        version: 1,
      } as any],
      direction: "ltr",
    },
  }
}

export function cellsToLexical(doc: CellularDocument | CellularContent | any): SerializedEditorState {
  // Extrai array de células do documento ou usa diretamente se for array
  let cells: Cell[]
  
  if (!doc) {
    return createEmptyLexicalState()
  }
  
  // Se é CellularDocument (tem v e c)
  if (typeof doc === "object" && "v" in doc && "c" in doc) {
    cells = doc.c
  } 
  // Se é array direto (CellularContent)
  else if (Array.isArray(doc)) {
    cells = doc
  } 
  else {
    return createEmptyLexicalState()
  }

  if (cells.length === 0) {
    return createEmptyLexicalState()
  }
  
  const children: SerializedLexicalNode[] = []

  for (const tuple of cells) {
    const [data, meta] = tuple
    const lexicalType = CELL_TO_LEXICAL_TYPE[data.t]
    
    if (!lexicalType) continue

    switch (data.t) {
      // Text cells
      case "p":
        children.push({
          type: "paragraph",
          children: data.c,
          direction: (meta as TextLexicalMeta).d,
          format: (meta as TextLexicalMeta).f,
          indent: (meta as TextLexicalMeta).i,
          version: meta.v,
        } as any)
        break
      case "h":
        children.push({
          type: "heading",
          children: data.c,
          tag: (meta as HeadingLexicalMeta).t,
          direction: (meta as HeadingLexicalMeta).d,
          format: (meta as HeadingLexicalMeta).f,
          indent: (meta as HeadingLexicalMeta).i,
          version: meta.v,
        } as any)
        break
      case "q":
        children.push({
          type: "quote",
          children: data.c,
          direction: (meta as TextLexicalMeta).d,
          format: (meta as TextLexicalMeta).f,
          indent: (meta as TextLexicalMeta).i,
          version: meta.v,
        } as any)
        break
      case "l": {
        const listMeta = meta as ListLexicalMeta
        children.push({
          type: listMeta.lt === "number" ? "number" : "bullet",
          children: data.c,
          listType: listMeta.lt,
          start: listMeta.s,
          tag: listMeta.t,
          direction: listMeta.d,
          format: listMeta.f,
          indent: listMeta.i,
          version: meta.v,
        } as any)
        break
      }
      // Decorator cells
      default:
        children.push({
          type: lexicalType,
          data: (data as any).d,
          version: meta.v,
        } as any)
        break
    }
  }

  // Lexical requer ao menos um filho
  if (children.length === 0) {
    return createEmptyLexicalState()
  }

  return {
    root: {
      type: "root",
      format: "",
      indent: 0,
      version: 1,
      children,
      direction: "ltr",
    },
  }
}

// ============================================================================
// Helpers para criar documentos
// ============================================================================

export function createEmptyDocument(): CellularDocument {
  return {
    v: 0,
    c: [[
      { t: "p", c: [] },
      createTextMeta()
    ]]
  }
}
