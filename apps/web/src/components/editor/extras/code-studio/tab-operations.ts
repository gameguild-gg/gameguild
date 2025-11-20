import type { CodeStudioData, DisplayConfig } from "./types"

export function selectFile(
  data: CodeStudioData,
  fileId: string,
  panelId: string | undefined,
  activeDisplay: DisplayConfig
): Partial<CodeStudioData> {
  const file = data.files.find(f => f.id === fileId)
  if (!file || !data.layout) return {}

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
  let updatedFolders = data.folders || []
  if (pathParts.length > 1) {
    updatedFolders = updatedFolders.map(folder => {
      if (filePath.startsWith(folder.path + '/')) {
        return { ...folder, isExpanded: true }
      }
      return folder
    })
  }

  if (isUniqueInstance) {
    // Editor único: abas específicas do display
    const currentTabs = activeDisplay.uniqueOpenTabs || []
    const updatedTabs = currentTabs.includes(fileId) ? currentTabs : [...currentTabs, fileId]
    
    const updatedDisplays = data.layout.displays.map(d => 
      d.id === activeDisplay.id 
        ? { ...d, uniqueOpenTabs: updatedTabs, uniqueActiveFileId: fileId }
        : d
    )

    return {
      folders: updatedFolders,
      activeFileId: fileId, // Atualizar também o activeFileId global para o FileExplorer
      layout: {
        ...data.layout,
        displays: updatedDisplays,
      },
    }
  } else {
    // Editor múltiplo: abas globais
    const currentTabs = data.openTabs || []
    const updatedTabs = currentTabs.includes(fileId) ? currentTabs : [...currentTabs, fileId]

    return { 
      folders: updatedFolders,
      openTabs: updatedTabs,
      activeFileId: fileId 
    }
  }
}

export function closeTab(
  data: CodeStudioData,
  fileId: string,
  panelId: string | undefined,
  activeDisplay: DisplayConfig
): Partial<CodeStudioData> {
  if (!data.layout) return {}

  const panel = panelId ? activeDisplay.panels.find(p => p.id === panelId) : undefined
  const isUniqueInstance = panel?.type === "editor" && panel?.editorInstance === "unique"

  if (isUniqueInstance) {
    // Editor único: fechar aba específica do display
    const newOpenTabs = (activeDisplay.uniqueOpenTabs || []).filter(id => id !== fileId)
    let newActiveFileId = activeDisplay.uniqueActiveFileId

    if (fileId === activeDisplay.uniqueActiveFileId) {
      newActiveFileId = newOpenTabs.length > 0 ? newOpenTabs[newOpenTabs.length - 1] : undefined
    }

    const updatedDisplays = data.layout.displays.map(d =>
      d.id === activeDisplay.id
        ? { ...d, uniqueOpenTabs: newOpenTabs, uniqueActiveFileId: newActiveFileId }
        : d
    )

    return {
      layout: {
        ...data.layout,
        displays: updatedDisplays,
      },
    }
  } else {
    // Editor múltiplo: fechar aba global
    const newOpenTabs = (data.openTabs || []).filter(id => id !== fileId)
    const updates: Partial<CodeStudioData> = { openTabs: newOpenTabs }

    if (fileId === data.activeFileId) {
      if (newOpenTabs.length > 0) {
        updates.activeFileId = newOpenTabs[newOpenTabs.length - 1]
      } else {
        updates.activeFileId = undefined
      }
    }

    return updates
  }
}

export function reorderTabs(
  data: CodeStudioData,
  newOrder: string[]
): Partial<CodeStudioData> {
  return { openTabs: newOrder }
}
