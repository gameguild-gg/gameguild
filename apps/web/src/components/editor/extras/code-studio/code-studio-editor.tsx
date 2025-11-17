"use client"

import { useState, useEffect } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { X, Save, Code2, Play, Eye, Terminal } from "lucide-react"
import { Edit } from "lucide-react"
import type { CodeStudioData, CodeFile, FileTreeFolder, SupportedLanguage } from "./types"
import { MonacoCodeEditor } from "./monaco-code-editor"
import { ResultPanel } from "./result-panel"
import { MODE_CONFIGS, LANGUAGE_CONFIGS, getLanguageFromExtension } from "./types"
import { useTheme } from "next-themes"
import { FileExplorer } from "./file-explorer"
import { FileTabs } from "./file-tabs"

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
  
  const [localData, setLocalData] = useState<CodeStudioData>(data)
  const [isExecuting, setIsExecuting] = useState(false)
  const [output, setOutput] = useState<string>("")

  // Sincronizar com mudanças externas
  useEffect(() => {
    setLocalData(data)
  }, [data])

  // Se não há modo definido, não renderizar nada
  if (!localData.mode) {
    return null
  }

  const currentMode = MODE_CONFIGS[localData.mode]
  const activeFile = localData.files.find(f => f.id === localData.activeFileId) || localData.files[0]
  
  // Determinar o layout baseado no modo e isViewMode
  const isViewMode = localData.isViewMode ?? false
  const isExecutionMode = localData.mode === "execution"
  const isTestMode = localData.mode === "test"

  const handleDataChange = (newData: Partial<CodeStudioData>) => {
    const updated = { ...localData, ...newData }
    setLocalData(updated)
    onUpdate?.(newData)
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
    // Adicionar à lista de abas abertas se não estiver
    if (!localData.openTabs?.includes(fileId)) {
      handleDataChange({ 
        openTabs: [...(localData.openTabs || []), fileId],
        activeFileId: fileId 
      })
    } else {
      handleDataChange({ activeFileId: fileId })
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
          </div>
          <div className="flex items-center gap-2">
            {onEdit && (
              <Button variant="outline" size="sm" onClick={onEdit} className="h-7">
                <Edit className="h-3 w-3 mr-1" />
                Edit
              </Button>
            )}
          </div>
        </div>

        {/* Layout baseado no modo */}
        {isViewMode ? (
          /* VIEW MODE: Apenas editor centralizado */
          <div className="p-4">
            <div className="max-w-4xl mx-auto">
              <div className="h-96 border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
                <FileTabs
                  files={localData.files}
                  openTabs={localData.openTabs || []}
                  activeFileId={localData.activeFileId}
                  onSelectTab={handleFileSelect}
                  onCloseTab={handleCloseTab}
                />
                <MonacoCodeEditor
                  value={activeFile?.content || ""}
                  language={activeFile?.language || "javascript"}
                  onChange={handleCodeChange}
                  readonly={isViewMode || localData.readonly || false}
                  theme={isDarkMode ? "vs-dark" : "vs-light"}
                  fontSize={localData.fontSize}
                  showLineNumbers={localData.showLineNumbers}
                />
              </div>
            </div>
          </div>
        ) : (
          /* EXECUTION/TEST MODE: Layout vertical (código acima, resultado abaixo) */
          <div className="flex flex-col">
            {/* Editor com File Explorer e Tabs */}
            <div className="h-96 border-b border-gray-200 dark:border-gray-800 flex">
              {/* File Explorer - 200px de largura */}
              <div className="w-[200px] shrink-0">
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
                />
              </div>
              
              {/* Editor com Tabs */}
              <div className="flex-1 flex flex-col min-w-0">
                <FileTabs
                  files={localData.files}
                  openTabs={localData.openTabs || []}
                  activeFileId={localData.activeFileId}
                  onSelectTab={handleFileSelect}
                  onCloseTab={handleCloseTab}
                />
                <div className="flex-1">
                  <MonacoCodeEditor
                    value={activeFile?.content || ""}
                    language={activeFile?.language || "javascript"}
                    onChange={handleCodeChange}
                    readonly={localData.readonly || false}
                    theme={isDarkMode ? "vs-dark" : "vs-light"}
                    fontSize={localData.fontSize}
                    showLineNumbers={localData.showLineNumbers}
                  />
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
        )}

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
          </div>

          {isExecutionMode && (
            <div className="flex items-center gap-2 ml-4 px-3 py-1.5 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg">
              <span className="text-xs font-medium text-gray-600 dark:text-gray-400">Display:</span>
              <div className="flex items-center gap-1 bg-gray-100 dark:bg-gray-900 rounded-md p-0.5">
                <button
                  onClick={() => handleDataChange({ isViewMode: false })}
                  className={`flex items-center gap-1.5 px-2 py-1 rounded text-xs font-medium transition-colors ${
                    !isViewMode 
                      ? "bg-white dark:bg-gray-700 text-green-600 dark:text-green-400 shadow-sm" 
                      : "text-gray-500 dark:text-gray-500 hover:text-gray-700 dark:hover:text-gray-300"
                  }`}
                >
                  <Terminal className="h-3.5 w-3.5" />
                  Execution
                </button>
                <button
                  onClick={() => handleDataChange({ isViewMode: true })}
                  className={`flex items-center gap-1.5 px-2 py-1 rounded text-xs font-medium transition-colors ${
                    isViewMode 
                      ? "bg-white dark:bg-gray-700 text-blue-600 dark:text-blue-400 shadow-sm" 
                      : "text-gray-500 dark:text-gray-500 hover:text-gray-700 dark:hover:text-gray-300"
                  }`}
                >
                  <Eye className="h-3.5 w-3.5" />
                  View Only
                </button>
              </div>
            </div>
          )}

          {!isViewMode && (
            <div className="ml-auto flex items-center gap-2">
              <Button
                variant="default"
                size="sm"
                onClick={handleExecute}
                disabled={isExecuting}
                className="flex items-center gap-2"
              >
                <Play className="h-4 w-4" />
                {isExecuting ? "Running..." : "Run Code"}
              </Button>
            </div>
          )}
        </div>

        {/* Main Content - Layout baseado no modo */}
        {isViewMode ? (
          // VIEW MODE: Editor centralizado com tabs
          <div className="flex-1 flex items-center justify-center p-8 overflow-auto">
            <div className="w-full max-w-4xl h-full flex flex-col">
              <FileTabs
                files={localData.files}
                openTabs={localData.openTabs || []}
                activeFileId={localData.activeFileId}
                onSelectTab={handleFileSelect}
                onCloseTab={handleCloseTab}
              />
              <div className="flex-1">
                <MonacoCodeEditor
                  value={activeFile?.content || ""}
                  language={activeFile?.language || "javascript"}
                  onChange={handleCodeChange}
                  readonly={isViewMode}
                  theme={isDarkMode ? "vs-dark" : "vs-light"}
                  fontSize={localData.fontSize}
                  showLineNumbers={localData.showLineNumbers}
                />
              </div>
            </div>
          </div>
        ) : (
          // EXECUTION/TEST MODE: Layout horizontal (File Explorer + Editor à esquerda, resultado à direita)
          <div className="flex-1 flex min-h-0">
            {/* Left Panel - File Explorer + Editor */}
            <div className="w-1/2 border-r border-gray-200 dark:border-gray-800 flex min-w-0">
              {/* File Explorer */}
              <div className="w-[200px] shrink-0">
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
                />
              </div>
              
              {/* Editor com Tabs */}
              <div className="flex-1 flex flex-col min-w-0">
                <FileTabs
                  files={localData.files}
                  openTabs={localData.openTabs || []}
                  activeFileId={localData.activeFileId}
                  onSelectTab={handleFileSelect}
                  onCloseTab={handleCloseTab}
                />
                <div className="flex-1">
                  <MonacoCodeEditor
                    value={activeFile?.content || ""}
                    language={activeFile?.language || "javascript"}
                    onChange={handleCodeChange}
                    readonly={localData.readonly || false}
                    theme={isDarkMode ? "vs-dark" : "vs-light"}
                    fontSize={localData.fontSize}
                    showLineNumbers={localData.showLineNumbers}
                  />
                </div>
              </div>
            </div>

            {/* Right Panel - Result */}
            <div className="w-1/2 flex flex-col">
              <div className="p-3 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
                <h3 className="font-medium text-sm">
                  {currentMode.label}
                </h3>
              </div>
              <div className="flex-1 overflow-auto">
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
          </div>
        )}

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
