"use client"

import { useEffect, useRef } from "react"
import { useImmer } from "use-immer"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { X, Save, Code2, Play, Terminal, Menu, ArrowLeft, Lock, Layout } from "lucide-react"
import type { CodeStudioData, CodeFile, FileTreeFolder, SupportedLanguage, LayoutConfig, PanelConfig, DisplayConfig, PanelType, EditorInstance, AspectRatio } from "./types"
import { MonacoCodeEditor } from "./monaco-code-editor"
import { ResultPanel } from "./result-panel"
import { MODE_CONFIGS, LANGUAGE_CONFIGS, getLanguageFromExtension } from "./types"
import { useTheme } from "next-themes"
import { FileExplorer } from "./file-system/file-explorer"
import { FileTabs } from "./file-tabs"
import { SettingsMenu } from "./settings-menu"
import { ResizablePanel } from "./resizable-panel"
import { GridDropZone } from "./grid-drop-zone"
import { DisplayManager } from "./display-manager"
import { EditorInstanceSwitch } from "./editor-instance-switch"
import { EmptyEditorState } from "./empty-editor-state"
import { cn } from "@/lib/utils"
import { createDefaultLayout } from "./default-layouts"
import * as FileOps from "./file-operations"
import * as LayoutOps from "./layout-operations"
import * as TabOps from "./tab-operations"
import * as PanelOps from "./panel-operations"
import { getGridDimensions, getContainerDimensions } from "./grid-utils"
import { UnifiedCodeRunner } from "./runners"
import { initializeMonacoFileSystem, syncFilesToMonacoFS, updateMonacoFile, deleteMonacoFile, disposeMonacoFileSystem, registerPathCompletionProvider } from "./monaco-file-system"

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
  
  const [localData, setLocalData] = useImmer<CodeStudioData>(() => {
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
  const [isExecuting, setIsExecuting] = useImmer(false)
  const [output, setOutput] = useImmer<string>("")
  const [showSettingsMenu, setShowSettingsMenu] = useImmer(false)
  const gridContainerRef = useRef<HTMLDivElement | null>(null)
  const codeRunnerRef = useRef<UnifiedCodeRunner | null>(null)
  const initializedRef = useRef(false)

  // Initialize runner and Monaco file system
  useEffect(() => {
    codeRunnerRef.current = new UnifiedCodeRunner({ timeout: 30000 })
    
    // Inicializar sistema de arquivos virtual do Monaco
    initializeMonacoFileSystem().then(() => {
      // Sincronizar arquivos iniciais com instanceId único
      if (localData.files.length > 0) {
        syncFilesToMonacoFS(localData.files, localData.id)
      }
    })
    
    return () => {
      codeRunnerRef.current?.dispose()
      codeRunnerRef.current = null
      disposeMonacoFileSystem()
    }
  }, [])
  
  // Sincronizar arquivos quando mudarem
  useEffect(() => {
    if (localData.files.length > 0) {
      syncFilesToMonacoFS(localData.files, localData.id)
    }
  }, [localData.files, localData.id])

  // Sincronizar com mudanças externas apenas na primeira montagem
  useEffect(() => {
    if (!initializedRef.current) {
      initializedRef.current = true
      return // Skip na primeira vez, pois useState já inicializou
    }
    
    // Só sincronizar quando data mudar externamente (edição fora deste componente)
    setLocalData(draft => {
      if (!data.layout) {
        Object.assign(draft, data)
        draft.mode = data.mode || "execution"
        draft.layout = createDefaultLayout()
      } else {
        // Usar dados do JSON exatamente como salvos
        Object.assign(draft, data)
      }
    })
  }, [data, setLocalData])

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
    setLocalData(draft => {
      Object.assign(draft, newData)
    })
    
    // Propagar mudanças apenas se for preview
    // No modo editor (não preview), só salva quando clicar em Save
    
  }

  const handleCodeChange = (content: string, fileId: string) => {
    setLocalData(draft => {
      const file = draft.files.find(f => f.id === fileId)
      if (file) {
        file.content = content
        // Atualizar também no sistema de arquivos virtual do Monaco com instanceId
        updateMonacoFile(file.path, content, draft.id)
      }
    })
  }

  // File Management
  const handleFileSelect = (fileId: string, panelId?: string) => {
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return

    setLocalData(draft => {
      TabOps.selectFile(draft, fileId, panelId, activeDisplay)
    })
  }

  const handleCloseTab = (fileId: string, panelId?: string) => {
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return

    setLocalData(draft => {
      TabOps.closeTab(draft, fileId, panelId, activeDisplay)
    })
  }

  const handleReorderTabs = (newOrder: string[], panelId?: string) => {
    const activeDisplay = getActiveDisplay()
    setLocalData(draft => {
      TabOps.reorderTabs(draft, newOrder, panelId, activeDisplay)
    })
  }

  const handleCreateFile = (path: string, name: string) => {
    if (!localData.layout) return
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return

    setLocalData(draft => {
      FileOps.createFile(draft, path, name, activeDisplay.id)
    })
  }

  const handleCreateFolder = (path: string, name: string) => {
    setLocalData(draft => {
      FileOps.createFolder(draft, path, name)
    })
  }

  const handleDeleteFile = (fileId: string) => {
    setLocalData(draft => {
      FileOps.deleteFile(draft, fileId)
    })
  }

  const handleDeleteFolder = (folderId: string) => {
    setLocalData(draft => {
      FileOps.deleteFolder(draft, folderId)
    })
  }

  const handleRenameFile = (fileId: string, newName: string) => {
    setLocalData(draft => {
      FileOps.renameFile(draft, fileId, newName)
    })
  }

  const handleRenameFolder = (folderId: string, newName: string) => {
    setLocalData(draft => {
      FileOps.renameFolder(draft, folderId, newName)
    })
  }

  const handleToggleFolder = (folderId: string) => {
    setLocalData(draft => {
      FileOps.toggleFolder(draft, folderId)
    })
  }

  const handleMoveFile = (fileId: string, newPath: string) => {
    setLocalData(draft => {
      FileOps.moveFile(draft, fileId, newPath)
    })
  }

  const handleMoveFolder = (folderId: string, newPath: string) => {
    setLocalData(draft => {
      FileOps.moveFolder(draft, folderId, newPath)
    })
  }

  // Layout handlers
  const getActiveDisplay = (): DisplayConfig | undefined => {
    if (!localData.layout) return undefined
    return localData.layout.displays.find(d => d.id === localData.layout!.activeDisplayId)
  }

  const handleToggleLayoutEdit = () => {
    setLocalData(draft => {
      LayoutOps.toggleLayoutEdit(draft)
    })
  }

  const handleSelectDisplay = (displayId: string) => {
    setLocalData(draft => {
      LayoutOps.selectDisplay(draft, displayId)
      
      // Ao trocar de display, atualizar activeFileId para refletir o estado do novo display
      const newDisplay = draft.layout?.displays.find(d => d.id === displayId)
      if (newDisplay) {
        const editorPanel = newDisplay.panels.find(p => p.type === 'editor')
        if (editorPanel?.editorInstance === 'unique') {
          // Display com editor único: usar uniqueActiveFileId
          draft.activeFileId = newDisplay.uniqueActiveFileId
        } else {
          // Display com editor múltiplo: manter activeFileId global se estiver nas tabs abertas
          if (draft.openTabs && draft.openTabs.length > 0) {
            if (!draft.activeFileId || !draft.openTabs.includes(draft.activeFileId)) {
              draft.activeFileId = draft.openTabs[draft.openTabs.length - 1]
            }
          } else {
            draft.activeFileId = undefined
          }
        }
      }
    })
  }

  const handleCreateDisplay = (name: string, aspectRatio: AspectRatio) => {
    setLocalData(draft => {
      LayoutOps.createDisplay(draft, name, aspectRatio)
    })
  }

  const handleDeleteDisplay = (displayId: string) => {
    setLocalData(draft => {
      LayoutOps.deleteDisplay(draft, displayId)
    })
  }

  const handleRenameDisplay = (displayId: string, newName: string) => {
    setLocalData(draft => {
      LayoutOps.renameDisplay(draft, displayId, newName)
    })
  }

  const handleChangeAspectRatio = (displayId: string, newAspectRatio: AspectRatio) => {
    setLocalData(draft => {
      LayoutOps.changeAspectRatio(draft, displayId, newAspectRatio)
    })
  }

  const handleUpdateCurrentDisplay = (updatedDisplay: DisplayConfig) => {
    setLocalData(draft => {
      LayoutOps.updateCurrentDisplay(draft, updatedDisplay)
    })
  }

  const handleAddPanel = (type: PanelType, row?: number, col?: number) => {
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return
    
    setLocalData(draft => {
      PanelOps.addPanel(draft, activeDisplay, type, row, col)
    })
  }

  const handleGridDrop = (row: number, col: number, type: PanelType) => {
    handleAddPanel(type, row, col)
  }

  const handlePanelResize = (panelId: string, row: number, col: number, rowSpan: number, colSpan: number) => {
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return
    
    setLocalData(draft => {
      PanelOps.resizePanel(draft, activeDisplay, panelId, row, col, rowSpan, colSpan)
    })
  }

  const handlePanelMove = (panelId: string, row: number, col: number) => {
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return
    
    setLocalData(draft => {
      PanelOps.movePanel(draft, activeDisplay, panelId, row, col)
    })
  }

  const handleRemovePanel = (panelId: string) => {
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return
    
    setLocalData(draft => {
      PanelOps.removePanel(draft, activeDisplay, panelId)
    })
  }

  const handleToggleEditorInstance = (panelId: string) => {
    const activeDisplay = getActiveDisplay()
    if (!activeDisplay) return

    setLocalData(draft => {
      PanelOps.toggleEditorInstance(draft, activeDisplay, panelId)
    })
  }

  const handlePanelDragStart = (panelId: string) => {
    PanelOps.onPanelDragStart(panelId)
  }

  const handlePanelDragEnd = () => {
    PanelOps.onPanelDragEnd()
  }

  // Renderizar conteúdo de cada painel
  const renderPanelContent = (panel: PanelConfig, displayConfig?: DisplayConfig) => {
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
        // No preview, usar displayConfig passado; no editor, usar activeDisplay
        const displayToUse = isPreview && displayConfig ? displayConfig : getActiveDisplay()
        const isUniqueInstance = panel.editorInstance === "unique"
        const currentOpenTabs = isUniqueInstance 
          ? (displayToUse?.uniqueOpenTabs || [])
          : (localData.openTabs || [])
        const currentActiveFileId = isUniqueInstance
          ? displayToUse?.uniqueActiveFileId
          : localData.activeFileId
        
        // No preview, verificar se há explorer no Display Base para permitir fechar tabs
        const hasExplorer = displayConfig ? displayConfig.panels.some(p => p.type === 'explorer') : true
        const canCloseTabs = isPreview ? hasExplorer : true
        
        return (
          <div className="flex flex-col h-full relative">
            {/* Editor Instance Switch */}
            {panel.editorInstance && localData.layout?.editMode && (
              <EditorInstanceSwitch
                editorInstance={panel.editorInstance}
                onToggle={() => handleToggleEditorInstance(panel.id)}
              />
            )}
            
            <FileTabs
              files={localData.files}
              openTabs={currentOpenTabs}
              activeFileId={currentActiveFileId}
              editorInstance={panel.editorInstance}
              panelId={panel.id}
              onSelectTab={(fileId) => handleFileSelect(fileId, panel.id)}
              onCloseTab={canCloseTabs ? (fileId) => handleCloseTab(fileId, panel.id) : undefined}
              onReorderTabs={handleReorderTabs}
            />
            <div className="flex-1 min-h-0 relative">
              {currentOpenTabs.length === 0 ? (
                <EmptyEditorState />
              ) : (
                <>
                  {currentOpenTabs.map((fileId) => {
                    const file = localData.files.find(f => f.id === fileId)
                    if (!file) return null
                    const isActive = fileId === currentActiveFileId
                    return (
                      <div
                        key={file.id}
                        className="absolute inset-0"
                        style={{ 
                          visibility: isActive ? 'visible' : 'hidden',
                          pointerEvents: isActive ? 'auto' : 'none',
                          zIndex: isActive ? 1 : 0
                        }}
                      >
                        <MonacoCodeEditor
                          fileId={file.id}
                          filePath={file.path}
                          instanceId={localData.id} // ID único da instância para isolamento completo
                          value={file.content}
                          onChange={(content) => handleCodeChange(content, file.id)}
                          language={file.language}
                          readonly={localData.readonly}
                          showLineNumbers={localData.showLineNumbers}
                          fontSize={localData.fontSize}
                          shikiTheme={localData.shikiTheme}
                        />
                      </div>
                    )
                  })}
                </>
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
            onStop={handleStop}
            testCases={localData.testCases?.[localData.activeFileId || ""] || []}
          />
        )
    }
  }

  const handleExecute = async () => {
    if (!codeRunnerRef.current) return
    
    // Buscar arquivo ativo: primeiro tentar painéis únicos, depois global
    const activeDisplay = getActiveDisplay()
    let fileToExecute = activeFile // Padrão: arquivo global ativo
    
    // Se houver painéis com instância única, usar o arquivo ativo deles
    if (activeDisplay) {
      const uniqueEditorPanel = activeDisplay.panels.find(
        p => p.type === 'editor' && p.editorInstance === 'unique'
      )
      if (uniqueEditorPanel && activeDisplay.uniqueActiveFileId) {
        fileToExecute = localData.files.find(f => f.id === activeDisplay.uniqueActiveFileId)
      }
    }
    
    if (!fileToExecute) return
    
    setIsExecuting(true)
    setOutput('')

    try {
      // Criar mapa de arquivos para suportar imports
      const filesMap: Record<string, string> = {}
      localData.files.forEach(file => {
        filesMap[`/${file.path}`] = file.content
      })

      // Usar runWithFiles para suportar imports entre arquivos
      const result = await codeRunnerRef.current.runWithFiles(
        fileToExecute.language,
        `/${fileToExecute.path}`,
        filesMap
      )

      let output = ''
      if (result.stdout) {
        output += result.stdout
      }
      if (result.stderr) {
        output += (output ? '\n' : '') + '\x1b[31m' + result.stderr + '\x1b[0m'
      }
      if (result.exitCode !== 0) {
        output += (output ? '\n' : '') + `\x1b[33m[Process exited with code ${result.exitCode}]\x1b[0m`
      }
      output += `\n\x1b[90m[Execution time: ${result.executionTime.toFixed(2)}ms]\x1b[0m`

      setOutput(output)
    } catch (error) {
      setOutput(`\x1b[31mExecution error: ${error instanceof Error ? error.message : String(error)}\x1b[0m`)
    } finally {
      setIsExecuting(false)
    }
  }

  const handleStop = async () => {
    if (codeRunnerRef.current) {
      await codeRunnerRef.current.interrupt()
      setIsExecuting(false)
      setOutput(prev => prev + '\n\x1b[33m[Execution interrupted]\x1b[0m')
    }
  }

  const handleSaveClick = () => {
    // Garantir que activeDisplayId seja sempre display-1 ao salvar
    const display1 = localData.layout?.displays.find(d => d.id === 'display-1')
    
    // Determinar o tipo de editor no display-1
    const display1Editor = display1?.panels.find(p => p.type === 'editor')
    const isDisplay1Unique = display1Editor?.editorInstance === 'unique'
    
    // Sincronizar activeFileId com a aba ativa apropriada
    let syncedActiveFileId = localData.activeFileId
    
    if (isDisplay1Unique) {
      // Editor único: usar uniqueActiveFileId do display-1
      if (display1?.uniqueOpenTabs && display1.uniqueOpenTabs.length > 0) {
        syncedActiveFileId = display1.uniqueActiveFileId
      } else {
        syncedActiveFileId = undefined
      }
    } else {
      // Editor múltiplo: usar openTabs global
      if (localData.openTabs && localData.openTabs.length > 0) {
        // Se já tem activeFileId e está nas tabs abertas, manter
        // Senão, usar a última tab aberta
        if (localData.activeFileId && localData.openTabs.includes(localData.activeFileId)) {
          syncedActiveFileId = localData.activeFileId
        } else {
          syncedActiveFileId = localData.openTabs[localData.openTabs.length - 1]
        }
      } else {
        syncedActiveFileId = undefined
      }
    }
    
    const dataToSave = {
      ...localData,
      layout: localData.layout ? {
        ...localData.layout,
        activeDisplayId: 'display-1'
      } : undefined,
      activeFileId: syncedActiveFileId
    }
    onSave?.(dataToSave)
  }

  const handleCancelClick = () => {
    onCancel?.()
  }

  // Se for preview (renderizado no documento), não mostra o modal
  if (isPreview) {
    // Usar Display Base (display-1) como espelho do preview
    const baseDisplay = localData.layout?.displays.find(d => d.id === 'display-1')
    if (!baseDisplay) return null

    // Verificar se há painel explorer no Display Base
    const hasExplorer = baseDisplay.panels.some(p => p.type === 'explorer')

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

        {/* Layout renderizado com base no Display Base */}
        <div 
          className="grid gap-3 p-3"
          style={{
            gridTemplateColumns: `repeat(${baseDisplay.aspectRatio === '2:1' ? 24 : 12}, 1fr)`,
            gridTemplateRows: `repeat(${baseDisplay.aspectRatio === '1:2' ? 24 : 12}, 1fr)`,
            height: baseDisplay.aspectRatio === '2:1' ? '600px' : baseDisplay.aspectRatio === '1:2' ? '1200px' : '600px',
          }}
        >
          {baseDisplay.panels.map(panel => (
            <div
              key={panel.id}
              style={{
                gridColumn: `${panel.col + 1} / span ${panel.colSpan}`,
                gridRow: `${panel.row + 1} / span ${panel.rowSpan}`,
              }}
              className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden bg-white dark:bg-gray-800"
            >
              {renderPanelContent(panel, baseDisplay)}
            </div>
          ))}
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
                        allPanels={activeDisplay.panels}
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
