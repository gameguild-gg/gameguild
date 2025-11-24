import type { CodeStudioData, DisplayConfig } from "./types"
import { openFile, closeFile, setEditorState, getEditorState } from "./editor-state-utils"

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
  
  // Usar utilitário centralizado para reordenar
  setEditorState(display, draft, { openTabs: newOrder })
}
