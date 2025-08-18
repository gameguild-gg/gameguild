"use client"

import { useState, useEffect, useRef } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { X, Save, FileText, GitBranch, Users, AlertCircle, CheckCircle } from "lucide-react"
import type { MermaidData } from "@/components/editor/nodes/mermaid-node"
import { MermaidTemplateSelector } from "./mermaid-template-selector"
import { MonacoMermaidEditor } from "./monaco-mermaid-editor"
import { MermaidValidator, type MermaidValidationResult } from "./mermaid-validator"

interface MermaidEditorProps {
  initialData?: MermaidData
  onSave: (data: MermaidData) => void
  onCancel: () => void
}

export function MermaidEditor({ initialData, onSave, onCancel }: MermaidEditorProps) {
  const [data, setData] = useState<MermaidData>(
    initialData || {
      code: "",
      type: "flowchart",
      title: "",
      caption: "",
      size: 100,
    },
  )
  const [autoUpdate, setAutoUpdate] = useState(true)
  const [svgContent, setSvgContent] = useState<string>("")
  const [lastValidSvg, setLastValidSvg] = useState<string>("")
  const [showTemplateSelector, setShowTemplateSelector] = useState(!initialData)
  const [error, setError] = useState<string>("")
  const [isLoading, setIsLoading] = useState(false)
  const [validationResult, setValidationResult] = useState<MermaidValidationResult>({ isValid: true })
  const [zoomLevel, setZoomLevel] = useState(100)
  const [errorPanelCollapsed, setErrorPanelCollapsed] = useState(false)
  const [alwaysCollapseErrors, setAlwaysCollapseErrors] = useState(false)
  const previewRef = useRef<HTMLDivElement>(null)
  const updateTimeoutRef = useRef<NodeJS.Timeout | null>(null)

  const renderDiagram = async (code: string, forceValidation = false) => {
    if (!code.trim()) {
      setSvgContent("")
      setLastValidSvg("")
      setError("")
      return
    }

    setIsLoading(true)
    setError("")

    try {
      // Always validate before rendering if forced or if current validation is invalid
      let currentValidation = validationResult
      if (forceValidation || !validationResult.isValid) {
        currentValidation = await MermaidValidator.validateCode(code)
        setValidationResult(currentValidation)
      }

      // Stop rendering if validation fails
      if (!currentValidation.isValid) {
        setError(currentValidation.error || "Invalid Mermaid code")
        setIsLoading(false)
        return
      }

      // Dynamic import to ensure mermaid is loaded
      const mermaid = (await import("mermaid")).default

      // Initialize mermaid with proper configuration
      mermaid.initialize({
        startOnLoad: false,
        theme: "default",
        securityLevel: "loose",
        fontFamily: "inherit",
        flowchart: {
          useMaxWidth: true,
          htmlLabels: true,
        },
        logLevel: "error",
        suppressErrorRendering: true, // Ativado para evitar renderização de erros
      })

      // Generate unique ID for this diagram
      const id = `mermaid-editor-${Date.now()}`

      try {
        const { svg } = await mermaid.render(id, code)
        setSvgContent(svg)
        setLastValidSvg(svg)
        setError("")
      } catch (renderError: any) {
        console.error("Mermaid render error:", renderError)

        const errorMessage = renderError?.message || "Failed to render diagram"

        // Set validation as invalid if render fails
        setValidationResult({ isValid: false, error: errorMessage })
        setError(`Render error: ${errorMessage}`)
      }
    } catch (err: any) {
      console.error("Error loading Mermaid:", err)
      setError("Failed to load Mermaid library. Please try again.")
    } finally {
      setIsLoading(false)
    }
  }

  const handleCodeChange = (newCode: string | undefined) => {
    const code = newCode || ""
    setData((prev) => ({ ...prev, code }))

    if (autoUpdate) {
      // Debounce the update with forced validation
      if (updateTimeoutRef.current) {
        clearTimeout(updateTimeoutRef.current)
      }
      updateTimeoutRef.current = setTimeout(() => {
        renderDiagram(code, true) // Force validation
      }, 500)
    }
  }

  const handleValidationChange = (result: MermaidValidationResult) => {
    setValidationResult(result)

    if (!result.isValid) {
      setError(result.error || "Invalid Mermaid code")
      if (!alwaysCollapseErrors) {
        setErrorPanelCollapsed(false)
      }
    } else {
      setErrorPanelCollapsed(true)
    }
  }

  const handleManualUpdate = () => {
    renderDiagram(data.code, true) // Force validation
  }

  const handleTemplateSelect = (template: { type: MermaidData["type"]; code: string }) => {
    setData((prev) => ({
      ...prev,
      type: template.type,
      code: template.code,
    }))
    setShowTemplateSelector(false)
    if (autoUpdate) {
      renderDiagram(template.code)
    }
  }

  const handleSave = () => {
    if (!data.code.trim()) {
      setError("Please enter some Mermaid code")
      return
    }

    if (!validationResult.isValid) {
      setError("Cannot save diagram with syntax errors. Please fix the errors first.")
      return
    }

    onSave(data)
  }

  // Initial render
  useEffect(() => {
    if (initialData?.code && autoUpdate) {
      renderDiagram(initialData.code, true) // Force validation
    }
  }, [])

  // Cleanup timeout on unmount
  useEffect(() => {
    return () => {
      if (updateTimeoutRef.current) {
        clearTimeout(updateTimeoutRef.current)
      }
    }
  }, [])

  // Functions for zoom control
  const handleZoomIn = () => {
    setZoomLevel((prev) => Math.min(prev + 25, 300))
  }

  const handleZoomOut = () => {
    setZoomLevel((prev) => Math.max(prev - 25, 25))
  }

  const handleZoomReset = () => {
    setZoomLevel(100)
  }

  return (
    <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4">
      <div className="bg-white dark:bg-gray-900 border dark:border-gray-700 rounded-lg shadow-2xl w-full max-w-7xl h-[90vh] flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700 bg-gradient-to-r from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-900">
          <div className="flex items-center gap-2">
            <GitBranch className="h-5 w-5 text-blue-600 dark:text-blue-400" />
            <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Mermaid Diagram Editor</h2>
            <div className="flex items-center gap-1 ml-4">
              {validationResult.isValid ? (
                <div className="flex items-center gap-1 text-green-600 dark:text-green-400 bg-green-50 dark:bg-green-900/30 px-2 py-1 rounded-full">
                  <CheckCircle className="h-4 w-4" />
                  <span className="text-sm font-medium">Valid</span>
                </div>
              ) : (
                <div className="flex items-center gap-1 text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-900/30 px-2 py-1 rounded-full">
                  <AlertCircle className="h-4 w-4" />
                  <span className="text-sm font-medium">Invalid</span>
                </div>
              )}
            </div>
          </div>
          <Button variant="ghost" size="sm" onClick={onCancel} className="hover:bg-gray-100 dark:hover:bg-gray-800">
            <X className="h-4 w-4" />
          </Button>
        </div>

        {/* Template Selector */}
        {showTemplateSelector && (
          <MermaidTemplateSelector onSelect={handleTemplateSelect} onCancel={() => setShowTemplateSelector(false)} />
        )}

        {/* Main Content */}
        {!showTemplateSelector && (
          <>
            {/* Settings Bar */}
            <div className="flex items-center gap-4 p-4 border-b border-gray-200 dark:border-gray-700 bg-gradient-to-r from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-850">
              <div className="flex items-center gap-2">
                <Label htmlFor="title" className="text-sm font-medium text-gray-700 dark:text-gray-300">
                  Title:
                </Label>
                <Input
                  id="title"
                  value={data.title || ""}
                  onChange={(e) => setData((prev) => ({ ...prev, title: e.target.value }))}
                  placeholder="Diagram title (optional)"
                  className="w-48 bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 focus:border-blue-500 dark:focus:border-blue-400"
                />
              </div>

              <div className="flex items-center gap-2">
                <Label htmlFor="auto-update" className="text-sm font-medium text-gray-700 dark:text-gray-300">
                  Auto Update:
                </Label>
                <Switch id="auto-update" checked={autoUpdate} onCheckedChange={setAutoUpdate} />
              </div>

              {!autoUpdate && (
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleManualUpdate}
                  disabled={!validationResult.isValid}
                  className="border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent"
                >
                  Update Preview
                </Button>
              )}

              <div className="flex items-center gap-2 ml-auto">
                <span className="text-sm text-gray-600 dark:text-gray-400 bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded">
                  Type: <span className="font-medium text-gray-800 dark:text-gray-200">{data.type}</span>
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setShowTemplateSelector(true)}
                  className="border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800"
                >
                  Change Template
                </Button>
              </div>
            </div>

            {/* Editor Content */}
            <div className="flex-1 flex min-h-0">
              {/* Left Panel - Code Editor */}
              <div className="w-1/2 border-r border-gray-200 dark:border-gray-700 flex flex-col bg-white dark:bg-gray-900">
                <div className="p-4 border-b border-gray-200 dark:border-gray-700 bg-gradient-to-r from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-850">
                  <h3 className="font-medium flex items-center gap-2 text-gray-800 dark:text-gray-200">
                    <FileText className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                    Mermaid Code
                  </h3>
                </div>

                {/* Editor Area - ajustando altura baseado no estado da aba de erros */}
                <div className="flex-1 flex flex-col min-h-0">
                  <div className={`${!validationResult.isValid && !errorPanelCollapsed ? "h-1/2" : "flex-1"} min-h-0`}>
                    <div className="h-full p-4 bg-gray-50 dark:bg-gray-900">
                      <MonacoMermaidEditor
                        value={data.code}
                        onChange={handleCodeChange}
                        onValidationChange={handleValidationChange}
                        height="100%"
                        theme="light"
                      />
                    </div>
                  </div>

                  {/* Error Panel - aparece dentro do painel esquerdo quando há erros */}
                  {!validationResult.isValid && (
                    <div
                      className={`border-t border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-950/50 transition-all duration-300 ${
                        errorPanelCollapsed ? "h-10" : "h-1/2"
                      }`}
                    >
                      {/* Header da aba de erros */}
                      <div className="flex items-center justify-between p-2 border-b border-red-200 dark:border-red-800 bg-gradient-to-r from-red-100 to-red-50 dark:from-red-900/50 dark:to-red-950/50">
                        <div className="flex items-center gap-2">
                          <AlertCircle className="h-4 w-4 text-red-600 dark:text-red-400" />
                          <span className="text-sm font-medium text-red-800 dark:text-red-300">
                            Validation Errors ({validationResult.error ? "1" : "0"})
                          </span>
                        </div>

                        <div className="flex items-center gap-2">
                          {/* Opção para sempre manter colapsado */}
                          <div className="flex items-center gap-1">
                            <input
                              type="checkbox"
                              id="always-collapse"
                              checked={alwaysCollapseErrors}
                              onChange={(e) => setAlwaysCollapseErrors(e.target.checked)}
                              className="w-3 h-3 accent-red-600 dark:accent-red-400"
                            />
                            <label htmlFor="always-collapse" className="text-xs text-red-700 dark:text-red-400">
                              Always collapse
                            </label>
                          </div>

                          {/* Botão de colapsar/expandir */}
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => setErrorPanelCollapsed(!errorPanelCollapsed)}
                            className="h-6 w-6 p-0 text-red-600 dark:text-red-400 hover:bg-red-200 dark:hover:bg-red-900/50"
                          >
                            {errorPanelCollapsed ? "▲" : "▼"}
                          </Button>
                        </div>
                      </div>

                      {/* Conteúdo da aba de erros */}
                      {!errorPanelCollapsed && (
                        <div className="p-3 overflow-y-auto overflow-x-hidden flex-1 max-h-full">
                          <div className="text-red-700 dark:text-red-300">
                            <div className="font-medium mb-2 flex items-center gap-1">
                              <span>⚠️</span>
                              <span>Syntax Error</span>
                            </div>
                            <div className="text-sm whitespace-pre-line font-mono bg-red-100 dark:bg-red-900/40 border border-red-200 dark:border-red-800 p-3 rounded-lg shadow-sm">
                              {validationResult.error || error}
                            </div>
                            <div className="mt-3 text-xs text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-950/30 p-2 rounded border border-red-200 dark:border-red-800">
                              💡 <strong>Tip:</strong> Check your Mermaid syntax and make sure all connections are
                              properly formed.
                            </div>
                          </div>
                        </div>
                      )}
                    </div>
                  )}
                </div>
              </div>

              {/* Right Panel - Preview */}
              <div className="w-1/2 flex flex-col bg-white dark:bg-gray-900">
                <div className="p-4 border-b border-gray-200 dark:border-gray-700 bg-gradient-to-r from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-850">
                  <div className="flex items-center justify-between">
                    <h3 className="font-medium flex items-center gap-2 text-gray-800 dark:text-gray-200">
                      <Users className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                      Live Preview
                    </h3>
                    {/* Zoom Controls */}
                    <div className="flex items-center gap-2 bg-gray-100 dark:bg-gray-800 p-1 rounded-lg">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={handleZoomOut}
                        disabled={zoomLevel <= 25}
                        className="h-7 w-7 p-0 border-gray-300 dark:border-gray-600 hover:bg-gray-200 dark:hover:bg-gray-700 bg-transparent"
                      >
                        -
                      </Button>
                      <span className="text-sm font-mono min-w-[4rem] text-center text-gray-700 dark:text-gray-300 px-2">
                        {zoomLevel}%
                      </span>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={handleZoomIn}
                        disabled={zoomLevel >= 300}
                        className="h-7 w-7 p-0 border-gray-300 dark:border-gray-600 hover:bg-gray-200 dark:hover:bg-gray-700"
                      >
                        +
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={handleZoomReset}
                        className="h-7 px-2 text-xs border-gray-300 dark:border-gray-600 hover:bg-gray-200 dark:hover:bg-gray-700 bg-transparent"
                      >
                        Reset
                      </Button>
                    </div>
                  </div>
                </div>
                <div className="flex-1 p-4 overflow-auto bg-gray-50 dark:bg-gray-900">
                  {isLoading ? (
                    <div className="flex items-center justify-center h-full text-gray-500 dark:text-gray-400">
                      <div className="flex items-center gap-2">
                        <div className="animate-spin rounded-full h-4 w-4 border-2 border-blue-600 border-t-transparent"></div>
                        Rendering diagram...
                      </div>
                    </div>
                  ) : error && !svgContent && !lastValidSvg ? (
                    <div className="text-red-500 dark:text-red-400 p-4 border border-red-300 dark:border-red-700 rounded-lg bg-red-50 dark:bg-red-950/30 shadow-sm">
                      <div className="font-medium mb-2 flex items-center gap-1">
                        <span>⚠️</span>
                        <span>Diagram Error</span>
                      </div>
                      <div className="text-sm whitespace-pre-line">{error}</div>
                      <div className="mt-3 text-xs text-red-600 dark:text-red-400 bg-red-100 dark:bg-red-900/40 p-2 rounded border border-red-200 dark:border-red-800">
                        💡 <strong>Tip:</strong> Check your Mermaid syntax and make sure all connections are properly
                        formed.
                      </div>
                    </div>
                  ) : svgContent || lastValidSvg ? (
                    <div className="relative">
                      {error && (
                        <div className="absolute top-2 right-2 z-10 bg-red-100 dark:bg-red-900/90 border border-red-300 dark:border-red-700 rounded-lg p-2 shadow-lg max-w-xs backdrop-blur-sm">
                          <div className="flex items-center gap-1 text-red-700 dark:text-red-300 text-xs">
                            <AlertCircle className="h-3 w-3" />
                            <span className="font-medium">Syntax Error</span>
                          </div>
                          <div className="text-xs text-red-600 dark:text-red-400 mt-1">Showing last valid diagram</div>
                        </div>
                      )}
                      <div
                        ref={previewRef}
                        className="flex justify-center items-start p-4 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 shadow-sm"
                        style={{ transform: `scale(${zoomLevel / 100})`, transformOrigin: "top center" }}
                        dangerouslySetInnerHTML={{ __html: svgContent || lastValidSvg }}
                      />
                    </div>
                  ) : (
                    <div className="flex items-center justify-center h-full text-gray-500 dark:text-gray-400">
                      <div className="text-center">
                        <FileText className="h-12 w-12 mx-auto mb-2 text-gray-300 dark:text-gray-600" />
                        <p>Enter Mermaid code to see preview</p>
                      </div>
                    </div>
                  )}
                </div>
              </div>
            </div>

            {/* Footer */}
            <div className="p-4 border-t border-gray-200 dark:border-gray-700 bg-gradient-to-r from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-850">
              <div className="flex items-center gap-4">
                <div className="flex-1">
                  <Label htmlFor="caption" className="text-sm font-medium text-gray-700 dark:text-gray-300">
                    Caption:
                  </Label>
                  <Input
                    id="caption"
                    value={data.caption || ""}
                    onChange={(e) => setData((prev) => ({ ...prev, caption: e.target.value }))}
                    placeholder="Optional caption for the diagram"
                    className="mt-1 bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 focus:border-blue-500 dark:focus:border-blue-400"
                  />
                </div>

                <div className="flex gap-2">
                  <Button
                    variant="outline"
                    onClick={onCancel}
                    className="border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent"
                  >
                    Cancel
                  </Button>
                  <Button
                    onClick={handleSave}
                    className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600"
                    disabled={!validationResult.isValid}
                  >
                    <Save className="h-4 w-4" />
                    Save Diagram
                  </Button>
                </div>
              </div>
            </div>
          </>
        )}
      </div>
    </div>
  )
}
