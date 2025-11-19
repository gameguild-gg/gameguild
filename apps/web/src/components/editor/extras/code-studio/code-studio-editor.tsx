"use client"

import { useState, useEffect, useRef } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { X, Save, Code2, Play, Terminal, Menu, ArrowLeft, Lock, Layout } from "lucide-react"
import type { CodeStudioData, CodeFile, FileTreeFolder, SupportedLanguage, LayoutConfig, PanelConfig, DisplayConfig, PanelType } from "./types"
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
        layout: {
          displays: [
            {
              id: "display-1",
              name: "Display 1",
              panels: [
                { id: "explorer-1", type: "explorer", row: 0, col: 0, rowSpan: 12, colSpan: 3 },
                { id: "editor-1", type: "editor", row: 0, col: 3, rowSpan: 8, colSpan: 9 },
                { id: "output-1", type: "output", row: 8, col: 3, rowSpan: 4, colSpan: 9 },
              ],
            },
            {
              id: "display-2",
              name: "Display 2",
              panels: [
                { id: "explorer-2", type: "explorer", row: 0, col: 0, rowSpan: 12, colSpan: 3 },
                { id: "editor-2", type: "editor", row: 0, col: 3, rowSpan: 12, colSpan: 9 },
              ],
            },
          ],
          activeDisplayId: "display-1",
          editMode: false,
        },
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
        layout: {
          displays: [
            {
              id: "display-1",
              name: "Display 1",
              panels: [
                { id: "explorer-1", type: "explorer", row: 0, col: 0, rowSpan: 12, colSpan: 3 },
                { id: "editor-1", type: "editor", row: 0, col: 3, rowSpan: 8, colSpan: 9 },
                { id: "output-1", type: "output", row: 8, col: 3, rowSpan: 4, colSpan: 9 },
              ],
            },
            {
              id: "display-2",
              name: "Display 2",
              panels: [
                { id: "explorer-2", type: "explorer", row: 0, col: 0, rowSpan: 12, colSpan: 3 },
                { id: "editor-2", type: "editor", row: 0, col: 3, rowSpan: 12, colSpan: 9 },
              ],
            },
          ],
          activeDisplayId: "display-1",
          editMode: false,
        },
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
  const handleFileSelect = (fileId: string) => {
    const file = localData.files.find(f => f.id === fileId)
    if (!file) return

    // Expandir todas as pastas pai do arquivo
    const filePath = file.path
    const pathParts = filePath.split('/')
    
    // Se o arquivo está em uma pasta, expandir todas as pastas pai
    if (pathParts.length > 1) {
      const updatedFolders = (localData.folders || []).map(folder => {
        // Verificar se esta pasta é pai do arquivo
        const folderPathParts = folder.path.split('/')
        
        // Expandir se o caminho do arquivo começa com o caminho da pasta
        if (filePath.startsWith(folder.path + '/')) {
          return { ...folder, isExpanded: true }
        }
        
        return folder
      })

      // Adicionar à lista de abas abertas se não estiver
      if (!localData.openTabs?.includes(fileId)) {
        handleDataChange({ 
          folders: updatedFolders,
          openTabs: [...(localData.openTabs || []), fileId],
          activeFileId: fileId 
        })
      } else {
        handleDataChange({ 
          folders: updatedFolders,
          activeFileId: fileId 
        })
      }
    } else {
      // Arquivo na raiz, não precisa expandir pastas
      if (!localData.openTabs?.includes(fileId)) {
        handleDataChange({ 
          openTabs: [...(localData.openTabs || []), fileId],
          activeFileId: fileId 
        })
      } else {
        handleDataChange({ activeFileId: fileId })
      }
    }
  }

  const handleCloseTab = (fileId: string) => {
    const newOpenTabs = (localData.openTabs || []).filter(id => id !== fileId)
    const updates: Partial<CodeStudioData> = { openTabs: newOpenTabs }
    
    // Se fechou a aba ativa, mudar para outra aba ou limpar
    if (fileId === localData.activeFileId) {
      if (newOpenTabs.length > 0) {
        // Mudar para a última aba da lista
        updates.activeFileId = newOpenTabs[newOpenTabs.length - 1]
      } else {
        // Não há mais abas abertas, limpar activeFileId
        updates.activeFileId = undefined
      }
    }
    
    handleDataChange(updates)
  }

  const handleReorderTabs = (newOrder: string[]) => {
    handleDataChange({ openTabs: newOrder })
  }

  const handleCreateFile = (path: string, name: string) => {
    const language = getLanguageFromExtension(name)
    const fullPath = path ? `${path}/${name}` : name
    
    const newFileId = Date.now().toString()
    const newFile: CodeFile = {
      id: newFileId,
      name,
      content: LANGUAGE_CONFIGS[language].defaultTemplate,
      language,
      isMain: localData.files.length === 0,
      isVisible: true,
      path: fullPath,
    }
    
    // Criar arquivo e abri-lo automaticamente em uma nova aba
    handleDataChange({ 
      files: [...localData.files, newFile],
      openTabs: [...(localData.openTabs || []), newFileId],
      activeFileId: newFileId,
    })
  }

  const handleCreateFolder = (path: string, name: string) => {
    const fullPath = path ? `${path}/${name}` : name
    const newFolder: FileTreeFolder = {
      id: Date.now().toString(),
      name,
      path: fullPath,
      isExpanded: true,
      children: [],
      type: "folder",
    }
    
    handleDataChange({ folders: [...(localData.folders || []), newFolder] })
  }

  const handleDeleteFile = (fileId: string) => {
    const newFiles = localData.files.filter(f => f.id !== fileId)
    const newOpenTabs = (localData.openTabs || []).filter(id => id !== fileId)
    
    handleDataChange({ 
      files: newFiles,
      openTabs: newOpenTabs,
      activeFileId: newOpenTabs.length > 0 ? newOpenTabs[0] : newFiles[0]?.id,
    })
  }

  const handleDeleteFolder = (folderId: string) => {
    const folder = localData.folders?.find(f => f.id === folderId)
    if (!folder) return
    
    // Remover pasta e arquivos dentro dela
    const newFiles = localData.files.filter(f => !f.path.startsWith(folder.path))
    const newFolders = (localData.folders || []).filter(f => f.id !== folderId)
    
    handleDataChange({ files: newFiles, folders: newFolders })
  }

  const handleRenameFile = (fileId: string, newName: string) => {
    const updatedFiles = localData.files.map(f => {
      if (f.id === fileId) {
        const pathParts = f.path.split('/')
        pathParts[pathParts.length - 1] = newName
        return { ...f, name: newName, path: pathParts.join('/') }
      }
      return f
    })
    handleDataChange({ files: updatedFiles })
  }

  const handleRenameFolder = (folderId: string, newName: string) => {
    const folder = localData.folders?.find(f => f.id === folderId)
    if (!folder) return
    
    const oldPath = folder.path
    const pathParts = oldPath.split('/')
    pathParts[pathParts.length - 1] = newName
    const newPath = pathParts.join('/')
    
    // Atualizar pasta
    const updatedFolders = (localData.folders || []).map(f => {
      if (f.id === folderId) {
        return { ...f, name: newName, path: newPath }
      }
      return f
    })
    
    // Atualizar caminhos dos arquivos dentro da pasta
    const updatedFiles = localData.files.map(f => {
      if (f.path.startsWith(oldPath)) {
        return { ...f, path: f.path.replace(oldPath, newPath) }
      }
      return f
    })
    
    handleDataChange({ folders: updatedFolders, files: updatedFiles })
  }

  const handleToggleFolder = (folderId: string) => {
    const updatedFolders = (localData.folders || []).map(f =>
      f.id === folderId ? { ...f, isExpanded: !f.isExpanded } : f
    )
    handleDataChange({ folders: updatedFolders })
  }

  const handleMoveFile = (fileId: string, newPath: string) => {
    const file = localData.files.find(f => f.id === fileId)
    if (!file) return

    const fileName = file.path.split("/").pop() || file.name
    const newFilePath = newPath ? `${newPath}/${fileName}` : fileName

    // Verificar se já existe arquivo com mesmo nome no destino
    const fileExists = localData.files.some(f => f.path === newFilePath && f.id !== fileId)
    if (fileExists) return

    const updatedFiles = localData.files.map(f =>
      f.id === fileId ? { ...f, path: newFilePath, name: fileName } : f
    )
    handleDataChange({ files: updatedFiles })
  }

  const handleMoveFolder = (folderId: string, newPath: string) => {
    const folder = (localData.folders || []).find(f => f.id === folderId)
    if (!folder) return

    const folderName = folder.path.split("/").pop() || folder.name
    const newFolderPath = newPath ? `${newPath}/${folderName}` : folderName

    // Verificar se já existe pasta com mesmo nome no destino
    const folderExists = (localData.folders || []).some(f => f.path === newFolderPath && f.id !== folderId)
    if (folderExists) return

    const oldPath = folder.path
    const updatedFolders = (localData.folders || []).map(f => {
      if (f.id === folderId) {
        return { ...f, path: newFolderPath, name: folderName }
      }
      // Atualizar subpastas
      if (f.path.startsWith(oldPath + "/")) {
        const relativePath = f.path.substring(oldPath.length + 1)
        return { ...f, path: `${newFolderPath}/${relativePath}` }
      }
      return f
    })

    // Atualizar arquivos dentro da pasta
    const updatedFiles = localData.files.map(f => {
      if (f.path.startsWith(oldPath + "/")) {
        const relativePath = f.path.substring(oldPath.length + 1)
        return { ...f, path: `${newFolderPath}/${relativePath}` }
      }
      return f
    })

    handleDataChange({ folders: updatedFolders, files: updatedFiles })
  }

  // Layout handlers
  const getActiveDisplay = (): DisplayConfig | undefined => {
    if (!localData.layout) return undefined
    return localData.layout.displays.find(d => d.id === localData.layout!.activeDisplayId)
  }

  const handleToggleLayoutEdit = () => {
    if (!localData.layout) return
    handleDataChange({
      layout: {
        ...localData.layout,
        editMode: !localData.layout.editMode,
      },
    })
  }

  const handleSelectDisplay = (displayId: string) => {
    if (!localData.layout) return
    handleDataChange({
      layout: {
        ...localData.layout,
        activeDisplayId: displayId,
      },
    })
  }

  const handleCreateDisplay = (name: string) => {
    if (!localData.layout || localData.layout.displays.length >= 4) return
    
    const displayNumber = localData.layout.displays.length + 1
    const newDisplay: DisplayConfig = {
      id: `display-${displayNumber}`,
      name: name || `Display ${displayNumber}`,
      panels: [
        { id: `editor-${Date.now()}`, type: "editor", row: 0, col: 0, rowSpan: 12, colSpan: 12 },
      ],
    }

    handleDataChange({
      layout: {
        ...localData.layout,
        displays: [...localData.layout.displays, newDisplay],
        activeDisplayId: newDisplay.id,
      },
    })
  }

  const handleDeleteDisplay = (displayId: string) => {
    if (!localData.layout || localData.layout.displays.length <= 2) return
    
    const updatedDisplays = localData.layout.displays.filter(d => d.id !== displayId)
    const newActiveId = localData.layout.activeDisplayId === displayId 
      ? updatedDisplays[0]?.id || ""
      : localData.layout.activeDisplayId

    handleDataChange({
      layout: {
        ...localData.layout,
        displays: updatedDisplays,
        activeDisplayId: newActiveId,
      },
    })
  }

  const handleRenameDisplay = (displayId: string, newName: string) => {
    if (!localData.layout) return
    
    const updatedDisplays = localData.layout.displays.map(d =>
      d.id === displayId ? { ...d, name: newName } : d
    )

    handleDataChange({
      layout: {
        ...localData.layout,
        displays: updatedDisplays,
      },
    })
  }

  const handleUpdateCurrentDisplay = (updatedDisplay: DisplayConfig) => {
    if (!localData.layout) return
    
    const updatedDisplays = localData.layout.displays.map(d =>
      d.id === updatedDisplay.id ? updatedDisplay : d
    )

    handleDataChange({
      layout: {
        ...localData.layout,
        displays: updatedDisplays,
      },
    })
  }

  const handleAddPanel = (type: PanelType, row?: number, col?: number) => {
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return
    
    // Se tiver coordenadas de drag-drop, usar elas
    // Senão, encontrar primeira célula vazia no grid 12x12
    let targetRow = row ?? 0
    let targetCol = col ?? 0
    let found = row !== undefined && col !== undefined

    if (!found) {
      // Buscar primeira célula vazia
      for (let r = 0; r < 12 && !found; r++) {
        for (let c = 0; c < 12 && !found; c++) {
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

    // Garantir que o painel não saia do grid (tamanho padrão 4x4)
    const rowSpan = Math.min(4, 12 - targetRow)
    const colSpan = Math.min(4, 12 - targetCol)

    const newPanel: PanelConfig = {
      id: `${type}-${Date.now()}`,
      type,
      row: targetRow,
      col: targetCol,
      rowSpan,
      colSpan,
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

  const handlePanelDragStart = (panelId: string) => {
    // Pode ser usado para feedback visual
    console.log('Dragging panel:', panelId)
  }

  const handlePanelDragEnd = () => {
    // Limpar feedback visual
    console.log('Drag ended')
  }

  // Renderizar conteúdo de cada painel
  const renderPanelContent = (panelType: "explorer" | "editor" | "output") => {
    switch (panelType) {
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
        return (
          <div className="flex flex-col h-full">
            <FileTabs
              files={localData.files}
              openTabs={localData.openTabs || []}
              activeFileId={localData.activeFileId}
              onSelectTab={handleFileSelect}
              onCloseTab={handleCloseTab}
              onReorderTabs={handleReorderTabs}
            />
            <div className="flex-1 min-h-0">
              {activeFile ? (
                <MonacoCodeEditor
                  value={activeFile.content}
                  onChange={handleCodeChange}
                  language={activeFile.language}
                  readonly={localData.readonly}
                  showLineNumbers={localData.showLineNumbers}
                  fontSize={localData.fontSize}
                  shikiTheme={localData.shikiTheme}
                />
              ) : (
                <div className="h-full flex items-center justify-center text-gray-400">
                  <div className="text-center">
                    <Code2 className="h-16 w-16 mx-auto mb-3 opacity-20" />
                    <p className="text-sm">No file selected</p>
                    <p className="text-xs mt-1">Open a file from the explorer or create a new one</p>
                  </div>
                </div>
              )}
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
                      <ArrowLeft className="h-4 w-4" />
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
              {currentMode.label} Mode
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
                onAddPanel={handleAddPanel}
              />
            </div>
          )}

          {/* Grid Container - Fixed 12x12 */}
          <div className="flex-1 min-h-0 overflow-hidden">
            {(() => {
              const activeDisplay = getActiveDisplay()
              if (!activeDisplay) return null

              return (
                <GridDropZone
                  isActive={localData.layout?.editMode || false}
                  onDrop={handleGridDrop}
                >
                  <div
                    ref={gridContainerRef}
                    className="h-full w-full grid gap-3"
                    style={{
                      gridTemplateColumns: "repeat(12, 1fr)",
                      gridTemplateRows: "repeat(12, 1fr)",
                    }}
                  >
                    {activeDisplay.panels.map(panel => (
                      <ResizablePanel
                        key={panel.id}
                        panel={panel}
                        isEditMode={localData.layout?.editMode || false}
                        gridContainerRef={gridContainerRef}
                        onResize={handlePanelResize}
                        onMove={handlePanelMove}
                        onRemove={handleRemovePanel}
                        onDragStart={handlePanelDragStart}
                        onDragEnd={handlePanelDragEnd}
                      >
                        {renderPanelContent(panel.type)}
                      </ResizablePanel>
                    ))}
                  </div>
                </GridDropZone>
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
