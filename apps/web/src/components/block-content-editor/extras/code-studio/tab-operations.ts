import type { CodeStudioData, DisplayConfig } from "./types"
import { openFile, closeFile, setActiveFile, getEditorState } from "./editor-state-utils"
import { displayHasUniqueEditor } from "./tree-operations"

export function selectFile(
  draft: CodeStudioData,
  fileId: string,
  panelId: string | undefined,
  activeDisplay: DisplayConfig
): void {
  const file = draft.files.find(f => f.id === fileId)
  if (!file) return

  // Expandir todas as pastas pai do arquivo
  const filePath = file.path
  const pathParts = filePath.split('/')
  
  if (pathParts.length > 1 && draft.folders) {
    draft.folders.forEach(folder => {
      if (filePath.startsWith(folder.path + '/')) {
        folder.isExpanded = true
      }
    })
  }

  // Usar utilitário centralizado para abrir arquivo
  openFile(draft, activeDisplay.id, fileId)
}

export function closeTab(
  draft: CodeStudioData,
  fileId: string,
  panelId: string | undefined,
  activeDisplay: DisplayConfig
): void {
  // Usar utilitário centralizado para fechar arquivo
  closeFile(draft, activeDisplay.id, fileId)
}

export function reorderTabs(
  draft: CodeStudioData,
  newOrder: string[],
  panelId: string | undefined,
  activeDisplay: DisplayConfig | undefined
): void {
  const displayId = activeDisplay?.id || 'display-1'
  const display = draft.layout?.displays.find(d => d.id === displayId)
  
  // Obter estado atual
  const currentState = getEditorState(draft, displayId)
  const currentActiveFile = currentState.activeFileId
  
  // Remover duplicatas da nova ordem, mantendo apenas a primeira ocorrência
  const uniqueNewOrder = Array.from(new Set(newOrder))
  
  // Atualizar a ordem das abas
  const hasUniqueEditor = display ? displayHasUniqueEditor(display) : false

  if (hasUniqueEditor && display) {
    // Editor unique: atualizar estado do display
    display.uniqueOpenTabs = uniqueNewOrder
  } else {
    // Editor multiple: atualizar estado global
    draft.openTabs = uniqueNewOrder
  }
  
  // Manter o arquivo ativo se ainda estiver na lista
  if (currentActiveFile && uniqueNewOrder.includes(currentActiveFile)) {
    setActiveFile(draft, displayId, currentActiveFile)
  } else if (uniqueNewOrder.length > 0) {
    setActiveFile(draft, displayId, uniqueNewOrder[0])
  } else {
    setActiveFile(draft, displayId, undefined)
  }
}
