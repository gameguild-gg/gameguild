/**
 * Cell Converters Router
 * 
 * Módulo de roteamento que verifica a UI de origem
 * e direciona para o converter correto.
 */

import type { SerializedEditorState } from "lexical"
import type { CellularDocument, UIOrigin } from "../cell-structure"

// Converters por UI
import * as lexicalConverter from "./lexical"

// ============================================================================
// Registry de Converters
// ============================================================================

interface Converter<TEditorState> {
  /** Converte do formato do editor para celular */
  toCells: (editorState: TEditorState) => CellularDocument
  /** Converte de celular para formato do editor */
  fromCells: (doc: CellularDocument) => TEditorState
}

const converters: Partial<Record<UIOrigin, Converter<any>>> = {
  lexical: {
    toCells: lexicalConverter.lexicalToCells,
    fromCells: lexicalConverter.cellsToLexical,
  },
  // Adicionar outros converters conforme implementados:
  // slate: { toCells: slateToCells, fromCells: cellsToSlate },
  // prosemirror: { toCells: prosemirrorToCells, fromCells: cellsToProsemirror },
  // tiptap: { toCells: tiptapToCells, fromCells: cellsToTiptap },
}

// ============================================================================
// Funções de Roteamento
// ============================================================================

/**
 * Converte de formato de editor para celular.
 * A UI de origem é inferida pelo tipo de estado passado.
 */
export function toCells(
  origin: UIOrigin, 
  editorState: any
): CellularDocument {
  const converter = converters[origin]
  
  if (!converter) {
    throw new Error(`Converter para UI "${origin}" não implementado`)
  }
  
  return converter.toCells(editorState)
}

/**
 * Converte de celular para formato de editor específico.
 * Verifica o campo `u` (ui origin) do documento para rotear.
 */
export function fromCells<T = any>(
  doc: CellularDocument,
  targetUI?: UIOrigin
): T {
  // Usa a UI de destino especificada ou a UI de origem do documento
  const ui = targetUI ?? doc.u
  const converter = converters[ui]
  
  if (!converter) {
    throw new Error(`Converter para UI "${ui}" não implementado`)
  }
  
  return converter.fromCells(doc) as T
}

/**
 * Verifica se um converter está disponível para uma UI.
 */
export function hasConverter(ui: UIOrigin): boolean {
  return ui in converters
}

/**
 * Retorna lista de UIs suportadas.
 */
export function getSupportedUIs(): UIOrigin[] {
  return Object.keys(converters) as UIOrigin[]
}

// Re-export converters específicos para uso direto
export { lexicalConverter }

// Re-export funções Lexical para compatibilidade
export const lexicalToCells = lexicalConverter.lexicalToCells
export const cellsToLexical = lexicalConverter.cellsToLexical
