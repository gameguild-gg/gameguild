"use client"

import { useState, useEffect } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { X, Save, Code2, Play } from "lucide-react"
import type { CodeStudioData } from "./types"
import { ModeSelector } from "./mode-selector"
import { MonacoCodeEditor } from "./monaco-code-editor"
import { ResultPanel } from "./result-panel"
import { MODE_CONFIGS } from "./types"
import { useTheme } from "next-themes"

interface CodeStudioEditorProps {
  data: CodeStudioData
  isPreview?: boolean
  onUpdate?: (data: Partial<CodeStudioData>) => void
  onSave?: () => void
  onCancel?: () => void
}

export function CodeStudioEditor({ 
  data, 
  isPreview = false, 
  onUpdate, 
  onSave, 
  onCancel 
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

  const currentMode = MODE_CONFIGS[localData.mode]
  const activeFile = localData.files.find(f => f.id === localData.activeFileId) || localData.files[0]

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

  const handleModeChange = (mode: CodeStudioData["mode"]) => {
    handleDataChange({ mode })
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
    onSave?.()
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
          </div>
          <ModeSelector 
            currentMode={localData.mode} 
            onModeChange={handleModeChange}
            compact
          />
        </div>

        {/* Layout vertical para preview (código acima, resultado abaixo) */}
        <div className="flex flex-col">
          {/* Editor */}
          <div className="h-96 border-b border-gray-200 dark:border-gray-800">
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
            
            <ModeSelector 
              currentMode={localData.mode} 
              onModeChange={handleModeChange}
            />
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
        </div>

        {/* Main Content - Layout horizontal (editor à esquerda, resultado à direita) */}
        <div className="flex-1 flex min-h-0">
          {/* Left Panel - Editor */}
          <div className="w-1/2 border-r border-gray-200 dark:border-gray-800 flex flex-col">
            <div className="p-3 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
              <h3 className="font-medium text-sm flex items-center gap-2">
                <Code2 className="h-4 w-4" />
                {activeFile?.name || "Code"}
              </h3>
            </div>
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
