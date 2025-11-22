import { produce } from "immer"
import type { CodeStudioData, DisplayConfig } from "./types"

export function selectFile(
  data: CodeStudioData,
  fileId: string,
  panelId: string | undefined,
  activeDisplay: DisplayConfig
): Partial<CodeStudioData> {
  return produce(data, draft => {
    const file = draft.files.find(f => f.id === fileId)
    if (!file || !draft.layout) return

    // Determinar se o painel é único ou múltiplo
    let panel = panelId ? activeDisplay.panels.find(p => p.id === panelId) : undefined
    
    // Se não foi passado panelId (clique do FileExplorer), verificar se há editor único no display atual
    if (!panel) {
      const uniqueEditorInDisplay = activeDisplay.panels.find(
        p => p.type === "editor" && p.editorInstance === "unique"
      )
      if (uniqueEditorInDisplay) {
        panel = uniqueEditorInDisplay
      }
    }
    
    const isUniqueInstance = panel?.type === "editor" && panel?.editorInstance === "unique"

    // Expandir todas as pastas pai do arquivo
    const filePath = file.path
    const pathParts = filePath.split('/')
    
    // Se o arquivo está em uma pasta, expandir todas as pastas pai
    if (pathParts.length > 1 && draft.folders) {
      draft.folders.forEach(folder => {
        if (filePath.startsWith(folder.path + '/')) {
          folder.isExpanded = true
        }
      })
    }

    const display = draft.layout.displays.find(d => d.id === activeDisplay.id)
    if (!display) return

    if (isUniqueInstance) {
      // Editor único: abas específicas do display
      if (!display.uniqueOpenTabs) {
        display.uniqueOpenTabs = []
      }
      if (!display.uniqueOpenTabs.includes(fileId)) {
        display.uniqueOpenTabs.push(fileId)
      }
      display.uniqueActiveFileId = fileId
      draft.activeFileId = fileId // Atualizar também o activeFileId global para o FileExplorer
    } else {
      // Editor múltiplo: abas globais compartilhadas
      if (!draft.openTabs?.includes(fileId)) {
        draft.openTabs = draft.openTabs || []
        draft.openTabs.push(fileId)
      }
      draft.activeFileId = fileId
    }
  })
}

export function closeTab(
  data: CodeStudioData,
  fileId: string,
  panelId: string | undefined,
  activeDisplay: DisplayConfig
): Partial<CodeStudioData> {
  return produce(data, draft => {
    if (!draft.layout) return

    const panel = panelId ? activeDisplay.panels.find(p => p.id === panelId) : undefined
    const isUniqueInstance = panel?.type === "editor" && panel?.editorInstance === "unique"

    const display = draft.layout.displays.find(d => d.id === activeDisplay.id)
    if (!display) return

    if (isUniqueInstance) {
      // Editor único: fechar aba específica do display
      if (!display.uniqueOpenTabs) return
      
      display.uniqueOpenTabs = display.uniqueOpenTabs.filter(id => id !== fileId)
      
      if (fileId === display.uniqueActiveFileId) {
        display.uniqueActiveFileId = display.uniqueOpenTabs.length > 0 
          ? display.uniqueOpenTabs[display.uniqueOpenTabs.length - 1] 
          : undefined
      }
    } else {
      // Editor múltiplo: fechar aba global
      if (!draft.openTabs) return
      
      draft.openTabs = draft.openTabs.filter(id => id !== fileId)
      
      if (fileId === draft.activeFileId) {
        draft.activeFileId = draft.openTabs.length > 0
          ? draft.openTabs[draft.openTabs.length - 1]
          : undefined
      }
    }
  })
}

export function reorderTabs(
  data: CodeStudioData,
  newOrder: string[],
  panelId: string | undefined,
  activeDisplay: DisplayConfig | undefined
): Partial<CodeStudioData> {
  return produce(data, draft => {
    if (!draft.layout || !activeDisplay || !panelId) {
      // Se não tiver informações do painel, atualizar tabs globais (modo múltiplo)
      draft.openTabs = newOrder
      return
    }

    const panel = activeDisplay.panels.find(p => p.id === panelId)
    const isUniqueInstance = panel?.type === "editor" && panel?.editorInstance === "unique"

    const display = draft.layout.displays.find(d => d.id === activeDisplay.id)
    if (!display) return

    if (isUniqueInstance) {
      // Editor único: reordenar abas específicas do display
      display.uniqueOpenTabs = newOrder
    } else {
      // Editor múltiplo: reordenar abas globais
      draft.openTabs = newOrder
    }
  })
}
