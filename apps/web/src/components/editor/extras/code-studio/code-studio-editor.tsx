"use client"

import { useState, useEffect, useRef } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { X, Save, Code2, Play, Terminal, Menu, ArrowLeft, Lock, Layout } from "lucide-react"
import type { CodeStudioData, CodeFile, FileTreeFolder, SupportedLanguage, LayoutConfig, PanelConfig, DisplayConfig, PanelType, EditorInstance, AspectRatio } from "./types"
import { MonacoCodeEditor } from "./monaco-code-editor"
import { ResultPanel } from "./result-panel"
import { MODE_CONFIGS, LANGUAGE_CONFIGS, getLanguageFromExtension } from "./types"
import { useTheme } from "next-themes"
import { FileExplorer } from "./file-explorer"
import { FileTabs } from "./file-tabs"
import { SettingsMenu } from "./settings-menu"
import { ResizablePanel } from "./resizable-panel"
import { GridDropZone } from "./grid-drop-zone"
import { DisplayManager } from "./display-manager"
import { cn } from "@/lib/utils"
import { createDefaultLayout } from "./default-layouts"
import * as FileOps from "./file-operations"
import * as LayoutOps from "./layout-operations"

// Helper to get grid dimensions from aspect ratio
function getGridDimensions(aspectRatio: "2:1" | "1:1" | "1:2") {
  switch (aspectRatio) {
    case "2:1": return { cols: 24, rows: 12 } // Landscape
    case "1:1": return { cols: 12, rows: 12 } // Square
    case "1:2": return { cols: 12, rows: 24 } // Portrait
  }
}

// Helper to get container dimensions from aspect ratio
function getContainerDimensions(aspectRatio: "2:1" | "1:1" | "1:2") {
  switch (aspectRatio) {
    case "2:1": return { maxWidth: "1200px", maxHeight: "600px" } // Landscape 2:1
    case "1:1": return { maxWidth: "600px", maxHeight: "600px" } // Square 1:1
    case "1:2": return { maxWidth: "600px", maxHeight: "1200px" } // Portrait 1:2
  }
}

interface CodeStudioEditorProps {
  data: CodeStudioData
  isPreview?: boolean
  onUpdate?: (data: Partial<CodeStudioData>) => void
  onSave?: (data: CodeStudioData) => void
  onCancel?: () => void
  onEdit?: () => void
}

export function CodeStudioEditor({ 
  data, 
  isPreview = false, 
  onUpdate, 
  onSave, 
  onCancel,
  onEdit,
}: CodeStudioEditorProps) {
  const { resolvedTheme } = useTheme()
  const isDarkMode = resolvedTheme === "dark"
  
  const [localData, setLocalData] = useState<CodeStudioData>(() => {
    // Criar layout padrão se não existir
    if (!data.layout) {
      return {
        ...data,
        mode: data.mode || "execution",
        layout: createDefaultLayout(),
      }
    }
    return data
  })
  const [isExecuting, setIsExecuting] = useState(false)
  const [output, setOutput] = useState<string>("")
  const [showSettingsMenu, setShowSettingsMenu] = useState(false)
  const gridContainerRef = useRef<HTMLDivElement | null>(null)

  // Sincronizar com mudanças externas
  useEffect(() => {
    if (!data.layout) {
      setLocalData({
        ...data,
        mode: data.mode || "execution",
        layout: createDefaultLayout(),
      })
    } else {
      setLocalData(data)
    }
  }, [data])

  // Fechar menu de settings quando clicar fora
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      const target = event.target as HTMLElement
      if (showSettingsMenu && !target.closest('.settings-menu-container')) {
        setShowSettingsMenu(false)
      }
    }
    
    if (showSettingsMenu) {
      document.addEventListener('mousedown', handleClickOutside)
      return () => document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [showSettingsMenu])

  // Se não há modo definido, não renderizar nada
  if (!localData.mode) {
    return null
  }

  const currentMode = MODE_CONFIGS[localData.mode]
  const activeFile = localData.files.find(f => f.id === localData.activeFileId)

  const handleDataChange = (newData: Partial<CodeStudioData>) => {
    const updated = { ...localData, ...newData }
    setLocalData(updated)
    
    // Só propagar mudanças se NÃO for preview (ou seja, se for no editor modal)
    // Preview não deve salvar modificações
    if (!isPreview) {
      onUpdate?.(newData)
    }
  }

  const handleCodeChange = (content: string) => {
    if (!activeFile) return
    
    const updatedFiles = localData.files.map(f =>
      f.id === activeFile.id ? { ...f, content } : f
    )
    handleDataChange({ files: updatedFiles })
  }

  // File Management
  const handleFileSelect = (fileId: string, panelId?: string) => {
    const file = localData.files.find(f => f.id === fileId)
    if (!file || !localData.layout) return

    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return

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
    let updatedFolders = localData.folders || []
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
      
      const updatedDisplays = localData.layout.displays.map(d => 
        d.id === activeDisplay.id 
          ? { ...d, uniqueOpenTabs: updatedTabs, uniqueActiveFileId: fileId }
          : d
      )

      handleDataChange({
        folders: updatedFolders,
        activeFileId: fileId, // Atualizar também o activeFileId global para o FileExplorer
        layout: {
          ...localData.layout,
          displays: updatedDisplays,
        },
      })
    } else {
      // Editor múltiplo: abas globais
      const currentTabs = localData.openTabs || []
      const updatedTabs = currentTabs.includes(fileId) ? currentTabs : [...currentTabs, fileId]

      handleDataChange({ 
        folders: updatedFolders,
        openTabs: updatedTabs,
        activeFileId: fileId 
      })
    }
  }

  const handleCloseTab = (fileId: string, panelId?: string) => {
    if (!localData.layout) return

    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return

    const panel = panelId ? activeDisplay.panels.find(p => p.id === panelId) : undefined
    const isUniqueInstance = panel?.type === "editor" && panel?.editorInstance === "unique"

    if (isUniqueInstance) {
      // Editor único: fechar aba específica do display
      const newOpenTabs = (activeDisplay.uniqueOpenTabs || []).filter(id => id !== fileId)
      let newActiveFileId = activeDisplay.uniqueActiveFileId

      if (fileId === activeDisplay.uniqueActiveFileId) {
        newActiveFileId = newOpenTabs.length > 0 ? newOpenTabs[newOpenTabs.length - 1] : undefined
      }

      const updatedDisplays = localData.layout.displays.map(d =>
        d.id === activeDisplay.id
          ? { ...d, uniqueOpenTabs: newOpenTabs, uniqueActiveFileId: newActiveFileId }
          : d
      )

      handleDataChange({
        layout: {
          ...localData.layout,
          displays: updatedDisplays,
        },
      })
    } else {
      // Editor múltiplo: fechar aba global
      const newOpenTabs = (localData.openTabs || []).filter(id => id !== fileId)
      const updates: Partial<CodeStudioData> = { openTabs: newOpenTabs }

      if (fileId === localData.activeFileId) {
        if (newOpenTabs.length > 0) {
          updates.activeFileId = newOpenTabs[newOpenTabs.length - 1]
        } else {
          updates.activeFileId = undefined
        }
      }

      handleDataChange(updates)
    }
  }

  const handleReorderTabs = (newOrder: string[]) => {
    handleDataChange({ openTabs: newOrder })
  }

  const handleCreateFile = (path: string, name: string) => {
    if (!localData.layout) return
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return

    const updates = FileOps.createFile(localData, path, name, activeDisplay.id)
    handleDataChange(updates)
  }

  const handleCreateFolder = (path: string, name: string) => {
    const updates = FileOps.createFolder(localData, path, name)
    handleDataChange(updates)
  }

  const handleDeleteFile = (fileId: string) => {
    const updates = FileOps.deleteFile(localData, fileId)
    handleDataChange(updates)
  }

  const handleDeleteFolder = (folderId: string) => {
    const updates = FileOps.deleteFolder(localData, folderId)
    handleDataChange(updates)
  }

  const handleRenameFile = (fileId: string, newName: string) => {
    const updates = FileOps.renameFile(localData, fileId, newName)
    handleDataChange(updates)
  }

  const handleRenameFolder = (folderId: string, newName: string) => {
    const updates = FileOps.renameFolder(localData, folderId, newName)
    handleDataChange(updates)
  }

  const handleToggleFolder = (folderId: string) => {
    const updates = FileOps.toggleFolder(localData, folderId)
    handleDataChange(updates)
  }

  const handleMoveFile = (fileId: string, newPath: string) => {
    const updates = FileOps.moveFile(localData, fileId, newPath)
    handleDataChange(updates)
  }

  const handleMoveFolder = (folderId: string, newPath: string) => {
    const updates = FileOps.moveFolder(localData, folderId, newPath)
    handleDataChange(updates)
  }

  // Layout handlers
  const getActiveDisplay = (): DisplayConfig | undefined => {
    if (!localData.layout) return undefined
    return localData.layout.displays.find(d => d.id === localData.layout!.activeDisplayId)
  }

  const handleToggleLayoutEdit = () => {
    const updates = LayoutOps.toggleLayoutEdit(localData)
    handleDataChange(updates)
  }

  const handleSelectDisplay = (displayId: string) => {
    const updates = LayoutOps.selectDisplay(localData, displayId)
    handleDataChange(updates)
  }

  const handleCreateDisplay = (name: string, aspectRatio: AspectRatio) => {
    const updates = LayoutOps.createDisplay(localData, name, aspectRatio)
    handleDataChange(updates)
  }

  const handleDeleteDisplay = (displayId: string) => {
    const updates = LayoutOps.deleteDisplay(localData, displayId)
    handleDataChange(updates)
  }

  const handleRenameDisplay = (displayId: string, newName: string) => {
    const updates = LayoutOps.renameDisplay(localData, displayId, newName)
    handleDataChange(updates)
  }

  const handleChangeAspectRatio = (displayId: string, newAspectRatio: AspectRatio) => {
    const updates = LayoutOps.changeAspectRatio(localData, displayId, newAspectRatio)
    handleDataChange(updates)
  }

  const handleUpdateCurrentDisplay = (updatedDisplay: DisplayConfig) => {
    const updates = LayoutOps.updateCurrentDisplay(localData, updatedDisplay)
    handleDataChange(updates)
  }

  const handleAddPanel = (type: PanelType, row?: number, col?: number) => {
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return
    
    const { cols, rows } = getGridDimensions(activeDisplay.aspectRatio)
    
    // Se tiver coordenadas de drag-drop, usar elas
    // Senão, encontrar primeira célula vazia no grid
    let targetRow = row ?? 0
    let targetCol = col ?? 0
    let found = row !== undefined && col !== undefined

    if (!found) {
      // Buscar primeira célula vazia
      for (let r = 0; r < rows && !found; r++) {
        for (let c = 0; c < cols && !found; c++) {
          const occupied = activeDisplay.panels.some(p =>
            r >= p.row && r < p.row + p.rowSpan &&
            c >= p.col && c < p.col + p.colSpan
          )
          if (!occupied) {
            targetRow = r
            targetCol = c
            found = true
          }
        }
      }
    }

    // Garantir que o painel não saia do grid (tamanho padrão 4 linhas x metade das colunas)
    const defaultColSpan = Math.min(8, Math.floor(cols / 2))
    const rowSpan = Math.min(4, rows - targetRow)
    const colSpan = Math.min(defaultColSpan, cols - targetCol)

    const newPanel: PanelConfig = {
      id: `${type}-${Date.now()}`,
      type,
      row: targetRow,
      col: targetCol,
      rowSpan,
      colSpan,
      ...(type === "editor" && { editorInstance: "multiple" as EditorInstance }),
    }

    handleUpdateCurrentDisplay({
      ...activeDisplay,
      panels: [...activeDisplay.panels, newPanel],
    })
  }

  const handleGridDrop = (row: number, col: number, type: PanelType) => {
    handleAddPanel(type, row, col)
  }

  const handlePanelResize = (panelId: string, row: number, col: number, rowSpan: number, colSpan: number) => {
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return
    
    const updatedPanels = activeDisplay.panels.map(p =>
      p.id === panelId ? { ...p, row, col, rowSpan, colSpan } : p
    )
    
    handleUpdateCurrentDisplay({
      ...activeDisplay,
      panels: updatedPanels,
    })
  }

  const handlePanelMove = (panelId: string, row: number, col: number) => {
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return
    
    const updatedPanels = activeDisplay.panels.map(p =>
      p.id === panelId ? { ...p, row, col } : p
    )
    
    handleUpdateCurrentDisplay({
      ...activeDisplay,
      panels: updatedPanels,
    })
  }

  const handleRemovePanel = (panelId: string) => {
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return
    
    const updatedPanels = activeDisplay.panels.filter(p => p.id !== panelId)
    
    handleUpdateCurrentDisplay({
      ...activeDisplay,
      panels: updatedPanels,
    })
  }

  const handleToggleEditorInstance = (panelId: string) => {
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return

    const updatedPanels = activeDisplay.panels.map(p => {
      if (p.id === panelId && p.type === "editor") {
        return {
          ...p,
          editorInstance: (p.editorInstance === "multiple" ? "unique" : "multiple") as EditorInstance,
        }
      }
      return p
    })

    handleUpdateCurrentDisplay({
      ...activeDisplay,
      panels: updatedPanels,
    })
  }

  const handlePanelDragStart = (panelId: string) => {
    // Pode ser usado para feedback visual
    console.log('Dragging panel:', panelId)
  }

  const handlePanelDragEnd = () => {
    // Limpar feedback visual
    console.log('Drag ended')
  }

  // Renderizar conteúdo de cada painel
  const renderPanelContent = (panel: PanelConfig) => {
    switch (panel.type) {
      case "explorer":
        return (
          <FileExplorer
            files={localData.files}
            folders={localData.folders || []}
            activeFileId={localData.activeFileId}
            onFileSelect={handleFileSelect}
            onCreateFile={handleCreateFile}
            onCreateFolder={handleCreateFolder}
            onDeleteFile={handleDeleteFile}
            onDeleteFolder={handleDeleteFolder}
            onRenameFile={handleRenameFile}
            onRenameFolder={handleRenameFolder}
            onToggleFolder={handleToggleFolder}
            onMoveFile={handleMoveFile}
            onMoveFolder={handleMoveFolder}
          />
        )
      
      case "editor":
        const activeDisplay = getActiveDisplay()
        const isUniqueInstance = panel.editorInstance === "unique"
        const currentOpenTabs = isUniqueInstance 
          ? (activeDisplay?.uniqueOpenTabs || [])
          : (localData.openTabs || [])
        const currentActiveFileId = isUniqueInstance
          ? activeDisplay?.uniqueActiveFileId
          : localData.activeFileId
        
        return (
          <div className="flex flex-col h-full relative">
            {/* Editor Instance Switch */}
            {panel.editorInstance && localData.layout?.editMode && (
              <div
                data-no-drag="true"
                className="absolute top-2 right-2 z-50 flex items-center gap-2 bg-white dark:bg-gray-800 px-3 py-1.5 rounded-lg shadow-md border border-gray-200 dark:border-gray-700"
                onMouseDown={(e) => {
                  e.stopPropagation()
                }}
                onClick={(e) => {
                  e.stopPropagation()
                }}
              >
                <span className="text-xs font-medium text-gray-600 dark:text-gray-400">
                  Instance:
                </span>
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation()
                    handleToggleEditorInstance(panel.id)
                  }}
                  className="relative inline-flex h-6 w-11 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
                  style={{
                    backgroundColor: panel.editorInstance === "multiple" ? "#3b82f6" : "#6b7280"
                  }}
                  title={panel.editorInstance === "multiple" ? "Multiple: Opens in all displays" : "Unique: Opens only in this display"}
                >
                  <span
                    className="inline-block h-4 w-4 transform rounded-full bg-white transition-transform pointer-events-none"
                    style={{
                      transform: panel.editorInstance === "multiple" ? "translateX(1.5rem)" : "translateX(0.25rem)"
                    }}
                  />
                </button>
                <span className="text-xs font-bold text-gray-700 dark:text-gray-300 min-w-[1ch]">
                  {panel.editorInstance === "multiple" ? "M" : "U"}
                </span>
              </div>
            )}
            
            <FileTabs
              files={localData.files}
              openTabs={currentOpenTabs}
              activeFileId={currentActiveFileId}
              editorInstance={panel.editorInstance}
              onSelectTab={(fileId) => handleFileSelect(fileId, panel.id)}
              onCloseTab={(fileId) => handleCloseTab(fileId, panel.id)}
              onReorderTabs={handleReorderTabs}
            />
            <div className="flex-1 min-h-0">
              {(() => {
                const currentFile = localData.files.find(f => f.id === currentActiveFileId)
                return currentFile ? (
                  <MonacoCodeEditor
                    value={currentFile.content}
                    onChange={handleCodeChange}
                    language={currentFile.language}
                    readonly={localData.readonly}
                    showLineNumbers={localData.showLineNumbers}
                    fontSize={localData.fontSize}
                    shikiTheme={localData.shikiTheme}
                  />
                ) : (
                  <div className="h-full flex flex-col items-center justify-center bg-gray-50 dark:bg-gray-900 text-gray-500 dark:text-gray-400">
                    <img 
                      src="/assets/images/icons/icon-128x128.png" 
                      alt="GameGuild Icon" 
                      className="w-24 h-24 mb-6 opacity-50"
                    />
                    <h3 className="text-xl font-semibold mb-2">No File Open</h3>
                    <p className="text-sm mb-4 flex items-center gap-2">
                      Open a file from the File Explorer
                    </p>
                  </div>
                )
              })()}
            </div>
          </div>
        )
      
      case "output":
        return (
          <ResultPanel
            output={output}
            isExecuting={isExecuting}
            mode={localData.mode!}
            onExecute={handleExecute}
            testCases={localData.testCases?.[localData.activeFileId || ""] || []}
          />
        )
    }
  }

  const handleExecute = () => {
    if (!activeFile) return
    setIsExecuting(true)
    // Aqui será implementada a lógica de execução
    setTimeout(() => {
      setOutput(`Executed: ${activeFile.name}\n${activeFile.content}`)
      setIsExecuting(false)
    }, 1000)
  }

  const handleSaveClick = () => {
    onSave?.(localData)
  }

  const handleCancelClick = () => {
    onCancel?.()
  }

  // Se for preview (renderizado no documento), não mostra o modal
  if (isPreview) {
    return (
      <div className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden bg-white dark:bg-gray-900">
        {/* Header compacto */}
        <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
          <div className="flex items-center gap-2">
            <Code2 className="h-4 w-4 text-blue-600 dark:text-blue-400" />
            <span className="font-medium text-sm">{localData.title || "Code Studio"}</span>
            <span className="text-xs px-2 py-0.5 bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300 rounded-full">
              {currentMode.label}
            </span>
            {localData.readonly && (
              <span className="text-xs px-2 py-0.5 bg-red-100 dark:bg-red-900 text-red-700 dark:text-red-300 rounded-full flex items-center gap-1">
                <Lock className="h-3 w-3" />
                Read Only
              </span>
            )}
          </div>
        </div>

        {/* Layout baseado no modo - sempre vertical (código acima, resultado abaixo) */}
        <div className="flex flex-col">
          {/* Editor com File Explorer e Tabs */}
          <div className="h-96 border-b border-gray-200 dark:border-gray-800 flex">
            {/* File Explorer - 200px de largura (condicional) */}
            {(localData.showFileExplorer ?? true) && (
              <div className="w-[220px] shrink-0">
                <FileExplorer
                  files={localData.files}
                  folders={localData.folders || []}
                  activeFileId={localData.activeFileId}
                  onFileSelect={handleFileSelect}
                  onCreateFile={handleCreateFile}
                  onCreateFolder={handleCreateFolder}
                  onDeleteFile={handleDeleteFile}
                  onDeleteFolder={handleDeleteFolder}
                  onRenameFile={handleRenameFile}
                  onRenameFolder={handleRenameFolder}
                  onToggleFolder={handleToggleFolder}
                  onMoveFile={handleMoveFile}
                  onMoveFolder={handleMoveFolder}
                />
              </div>
            )}
            
            {/* Editor com Tabs */}
            <div className="flex-1 flex flex-col min-w-0">
              <FileTabs
                files={localData.files}
                openTabs={localData.openTabs || []}
                activeFileId={localData.activeFileId}
                onSelectTab={handleFileSelect}
                onCloseTab={localData.showFileExplorer ?? true ? handleCloseTab : undefined}
                onReorderTabs={handleReorderTabs}
              />
              <div className="flex-1">
                {activeFile ? (
                  <MonacoCodeEditor
                    value={activeFile.content}
                    language={activeFile.language}
                    onChange={handleCodeChange}
                    readonly={localData.readonly || false}
                    theme={isDarkMode ? "vs-dark" : "vs-light"}
                    shikiTheme={localData.shikiTheme || "github"}
                    fontSize={localData.fontSize}
                    showLineNumbers={localData.showLineNumbers}
                  />
                ) : (
                  <div className="h-full flex flex-col items-center justify-center bg-gray-50 dark:bg-gray-900 text-gray-500 dark:text-gray-400">
                    <img 
                      src="/assets/images/icons/icon-128x128.png" 
                      alt="GameGuild Icon" 
                      className="w-24 h-24 mb-6 opacity-50"
                    />
                    <h3 className="text-xl font-semibold mb-2">No File Open</h3>
                    <p className="text-sm mb-4 flex items-center gap-2">
                      Open a file from the File Explorer
                    </p>
                  </div>
                )}
              </div>
            </div>
          </div>

          {/* Resultado */}
          <div className="h-64">
            <ResultPanel
              mode={localData.mode}
              output={output}
              isExecuting={isExecuting}
              onExecute={handleExecute}
              testCases={localData.testCases?.[activeFile?.id || ""] || []}
              activeFile={activeFile}
            />
          </div>
        </div>

        {localData.caption && (
          <div className="p-2 text-xs text-gray-600 dark:text-gray-400 border-t border-gray-200 dark:border-gray-800">
            {localData.caption}
          </div>
        )}
      </div>
    )
  }

  // Modal de edição (fullscreen)
  return (
    <div 
      className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4"
      onClick={handleCancelClick}
    >
      <div 
        className="bg-white dark:bg-gray-900 border dark:border-gray-700 shadow-2xl w-full max-w-7xl h-[90vh] flex flex-col"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-2">
              <Code2 className="h-5 w-5 text-blue-600 dark:text-blue-400" />
              <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Code Studio</h2>
            </div>
            
            <span className="text-sm px-3 py-1 bg-blue-100 dark:bg-blue-900 text-blue-700 dark:text-blue-300 rounded-full font-medium">
              {currentMode.label}
            </span>
          </div>
          
          <Button variant="ghost" size="sm" onClick={handleCancelClick}>
            <X className="h-4 w-4" />
          </Button>
        </div>

        {/* Settings Bar */}
        <div className="flex items-center gap-4 p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
          {/* Settings Menu Button */}
          <div className="relative settings-menu-container">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setShowSettingsMenu(!showSettingsMenu)}
              className="h-8 w-8 p-0"
              title="Settings"
            >
              <Menu className="h-4 w-4" />
            </Button>
            
            {/* Settings Dropdown Menu */}
            {showSettingsMenu && (
              <SettingsMenu
                data={localData}
                onDataChange={handleDataChange}
                onClose={() => setShowSettingsMenu(false)}
              />
            )}
          </div>
          
          <div className="flex items-center gap-2">
            <Label htmlFor="title" className="text-sm font-medium">
              Title:
            </Label>
            <Input
              id="title"
              value={localData.title || ""}
              onChange={(e) => handleDataChange({ title: e.target.value })}
              placeholder="Optional title"
              className="w-48"
            />

            {/* Display Selector - Only when NOT editing layout */}
            {!localData.layout?.editMode && localData.layout && (
              <div className="flex items-center gap-1 ml-4 px-2 py-1 bg-gray-100 dark:bg-gray-800 rounded-md border border-gray-200 dark:border-gray-700">
                {localData.layout.displays.map((display) => (
                  <button
                    key={display.id}
                    onClick={() => handleSelectDisplay(display.id)}
                    className={cn(
                      "px-2.5 py-1 rounded text-xs font-medium transition-all",
                      localData.layout?.activeDisplayId === display.id
                        ? "bg-blue-600 text-white shadow-sm"
                        : "bg-transparent text-gray-600 dark:text-gray-400 hover:bg-gray-200 dark:hover:bg-gray-700"
                    )}
                    title={display.name}
                  >
                    {display.name}
                  </button>
                ))}
              </div>
            )}
          </div>

          {/* Layout Edit Button */}
          <div className="ml-auto flex items-center gap-2">
            <Button
              variant={localData.layout?.editMode ? "default" : "outline"}
              size="sm"
              onClick={handleToggleLayoutEdit}
              className="h-8"
              title={localData.layout?.editMode ? "Exit Layout Edit" : "Edit Layout"}
            >
              <Layout className="h-4 w-4 mr-2" />
              {localData.layout?.editMode ? "Done" : "Layout"}
            </Button>
          </div>
        </div>

        {/* Main Content - Grid Layout Customizável */}
        <div className="flex-1 min-h-0 p-3 bg-gray-100 dark:bg-gray-950 overflow-hidden flex flex-col">
          {/* Layout Edit Tools */}
          {localData.layout?.editMode && (
            <div className="mb-3 p-2 bg-white dark:bg-gray-900 border border-blue-500/30 rounded-lg shrink-0">
              <DisplayManager
                displays={localData.layout.displays}
                activeDisplayId={localData.layout.activeDisplayId}
                onSelectDisplay={handleSelectDisplay}
                onCreateDisplay={handleCreateDisplay}
                onDeleteDisplay={handleDeleteDisplay}
                onRenameDisplay={handleRenameDisplay}
                onChangeAspectRatio={handleChangeAspectRatio}
                onAddPanel={handleAddPanel}
              />
            </div>
          )}

          {/* Grid Container */}
          <div className="flex-1 min-h-0 overflow-hidden flex items-center justify-center p-4">
            {(() => {
              const activeDisplay = getActiveDisplay()
              if (!activeDisplay) return null

              const { cols, rows } = getGridDimensions(activeDisplay.aspectRatio)
              const { maxWidth, maxHeight } = getContainerDimensions(activeDisplay.aspectRatio)

              return (
                <div
                  className="w-full h-full"
                  style={{
                    maxWidth,
                    maxHeight,
                  }}
                >
                  <GridDropZone
                    isActive={localData.layout?.editMode || false}
                    onDrop={handleGridDrop}
                    gridCols={cols}
                    gridRows={rows}
                  >
                    <div
                      ref={gridContainerRef}
                      className="h-full w-full grid gap-3"
                      style={{
                        gridTemplateColumns: `repeat(${cols}, 1fr)`,
                        gridTemplateRows: `repeat(${rows}, 1fr)`,
                      }}
                    >
                    {activeDisplay.panels.map(panel => (
                      <ResizablePanel
                        key={panel.id}
                        panel={panel}
                        isEditMode={localData.layout?.editMode || false}
                        gridContainerRef={gridContainerRef}
                        gridCols={cols}
                        gridRows={rows}
                        onResize={handlePanelResize}
                        onMove={handlePanelMove}
                        onRemove={handleRemovePanel}
                        onDragStart={handlePanelDragStart}
                        onDragEnd={handlePanelDragEnd}
                      >
                        {renderPanelContent(panel)}
                      </ResizablePanel>
                    ))}
                  </div>
                </GridDropZone>
                </div>
              )
            })()}
          </div>
        </div>

        {/* Footer */}
        <div className="p-4 border-t border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
          <div className="flex items-center gap-4">
            <div className="flex-1">
              <Label htmlFor="caption" className="text-sm font-medium">
                Caption:
              </Label>
              <Input
                id="caption"
                value={localData.caption || ""}
                onChange={(e) => handleDataChange({ caption: e.target.value })}
                placeholder="Optional caption"
                className="mt-1"
              />
            </div>

            <div className="flex gap-2">
              <Button variant="outline" onClick={handleCancelClick}>
                Cancel
              </Button>
              <Button onClick={handleSaveClick} className="flex items-center gap-2">
                <Save className="h-4 w-4" />
                Save
              </Button>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
