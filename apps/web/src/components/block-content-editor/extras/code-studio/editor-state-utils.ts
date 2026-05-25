import type { CodeStudioData, DisplayConfig } from "./types"
import { displayHasUniqueEditor } from "./tree-operations"

/**
 * Utilitários centralizados para gerenciar estado de abas entre editores unique e multiple
 * 
 * REGRAS:
 * - Editor UNIQUE: Cada display tem suas próprias abas (uniqueOpenTabs, uniqueActiveFileId)
 * - Editor MULTIPLE: Compartilhado entre todos os displays (openTabs, activeFileId)
 */

interface EditorState {
  isUnique: boolean
  openTabs: string[]
  activeFileId: string | undefined
}

/**
 * Obtém o estado atual do editor para um display específico
 */
export function getEditorState(data: CodeStudioData, displayId: string): EditorState {
  const display = data.layout?.displays.find(d => d.id === displayId)
  
  if (!display) {
    // Fallback: editor multiple
    return {
      isUnique: false,
      openTabs: data.openTabs || [],
      activeFileId: data.activeFileId,
    }
  }

  const hasUniqueEditor = displayHasUniqueEditor(display)

  if (hasUniqueEditor) {
    // Editor unique: estado específico do display
    return {
      isUnique: true,
      openTabs: display.uniqueOpenTabs || [],
      activeFileId: display.uniqueActiveFileId,
    }
  } else {
    // Editor multiple: estado global
    return {
      isUnique: false,
      openTabs: data.openTabs || [],
      activeFileId: data.activeFileId,
    }
  }
}

/**
 * Atualiza o estado do editor (abas e arquivo ativo)
 */
export function setEditorState(
  display: DisplayConfig | undefined,
  data: CodeStudioData,
  updates: { openTabs?: string[]; activeFileId?: string | undefined }
): void {
  if (!display) {
    // Sem display, atualizar global (multiple)
    if (updates.openTabs !== undefined) {
      data.openTabs = updates.openTabs
    }
    if (updates.activeFileId !== undefined) {
      data.activeFileId = updates.activeFileId
    }
    return
  }

  const hasUniqueEditor = displayHasUniqueEditor(display)

  if (hasUniqueEditor) {
    // Editor unique: atualizar estado do display
    if (updates.openTabs !== undefined) {
      display.uniqueOpenTabs = updates.openTabs
    }
    if (updates.activeFileId !== undefined) {
      display.uniqueActiveFileId = updates.activeFileId
    }
  } else {
    // Editor multiple: atualizar estado global
    if (updates.openTabs !== undefined) {
      data.openTabs = updates.openTabs
    }
    if (updates.activeFileId !== undefined) {
      data.activeFileId = updates.activeFileId
    }
  }
}

/**
 * Adiciona um arquivo às abas abertas e o define como ativo
 */
export function openFile(
  data: CodeStudioData,
  displayId: string,
  fileId: string
): void {
  const display = data.layout?.displays.find(d => d.id === displayId)
  const state = getEditorState(data, displayId)

  // Adicionar às abas se não estiver
  const newOpenTabs = state.openTabs.includes(fileId)
    ? state.openTabs
    : [...state.openTabs, fileId]

  setEditorState(display, data, {
    openTabs: newOpenTabs,
    activeFileId: fileId,
  })
}

/**
 * Remove um arquivo das abas abertas
 */
export function closeFile(
  data: CodeStudioData,
  displayId: string,
  fileId: string
): void {
  const display = data.layout?.displays.find(d => d.id === displayId)
  const state = getEditorState(data, displayId)

  const newOpenTabs = state.openTabs.filter(id => id !== fileId)
  
  // Se fechou a aba ativa, selecionar outra
  let newActiveFileId = state.activeFileId
  if (state.activeFileId === fileId) {
    newActiveFileId = newOpenTabs.length > 0 ? newOpenTabs[newOpenTabs.length - 1] : undefined
  }

  setEditorState(display, data, {
    openTabs: newOpenTabs,
    activeFileId: newActiveFileId,
  })
}

/**
 * Define qual arquivo está ativo (sem adicionar às abas)
 */
export function setActiveFile(
  data: CodeStudioData,
  displayId: string,
  fileId: string | undefined
): void {
  const display = data.layout?.displays.find(d => d.id === displayId)
  setEditorState(display, data, { activeFileId: fileId })
}

/**
 * Obtém o arquivo ativo para um display
 */
export function getActiveFileId(data: CodeStudioData, displayId: string): string | undefined {
  return getEditorState(data, displayId).activeFileId
}

/**
 * Obtém as abas abertas para um display
 */
export function getOpenTabs(data: CodeStudioData, displayId: string): string[] {
  return getEditorState(data, displayId).openTabs
}
