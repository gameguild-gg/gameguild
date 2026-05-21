"use client"

import { useState, useEffect, useRef } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Save, FileText, GitBranch, Users, AlertCircle, CheckCircle } from "lucide-react"
import type { MermaidData } from "@/components/block-content-editor/nodes/mermaid-node"
import { useEditorSettings } from "../settings-menu"
import { BlockEditorShell } from "@/components/block-content-editor/extras/block-editor-shell"
import { MermaidTemplateSelector } from "./mermaid-template-selector"
import { MonacoMermaidEditor } from "./monaco-mermaid-editor"
import { MermaidValidator, type MermaidValidationResult } from "./mermaid-validator"
import { MermaidViewer } from "@/components/block-content-editor/extras/mermaid/mermaid-viewer"
import {
  getMermaidThemePair,
  AVAILABLE_MERMAID_THEMES,
  MERMAID_THEME_DESCRIPTIONS,
  MERMAID_THEME_MODE_DESCRIPTIONS,
} from "@/components/block-content-editor/extras/mermaid/mermaid-theme-helper"
import { useTheme } from "next-themes"

interface MermaidEditorProps {
  initialData?: MermaidData
  onSave: (data: MermaidData) => void
  onCancel: () => void
}

export function MermaidEditor({ initialData, onSave, onCancel }: MermaidEditorProps) {
  const { resolvedTheme } = useTheme()
  const isDarkMode = resolvedTheme === "dark"
  
  const [data, setData] = useState<MermaidData>(
    initialData || {
      code: "",
      type: "flowchart",
      title: "",
      caption: "",
      size: 100,
      theme: "default",
      themeMode: "system",
    },
  )
  const [autoUpdate, setAutoUpdate] = useState(true)
  const [lastValidSvg, setLastValidSvg] = useState<string>("")
  const [showTemplateSelector, setShowTemplateSelector] = useState(!initialData)
  const [renderError, setRenderError] = useState<string>("")
  const [validationResult, setValidationResult] = useState<MermaidValidationResult>({ isValid: true })
  const [errorPanelCollapsed, setErrorPanelCollapsed] = useState(false)
  const [alwaysCollapseErrors, setAlwaysCollapseErrors] = useState(false)
  const settings = useEditorSettings("mermaid")
  const updateTimeoutRef = useRef<NodeJS.Timeout | null>(null)

  const handleCodeChange = (newCode: string | undefined) => {
    const code = newCode || ""
    setData((prev) => ({ ...prev, code }))

    if (autoUpdate) {
      // Debounce the validation
      if (updateTimeoutRef.current) {
        clearTimeout(updateTimeoutRef.current)
      }
      updateTimeoutRef.current = setTimeout(async () => {
        if (code.trim()) {
          const result = await MermaidValidator.validateCode(code)
          setValidationResult(result)
        } else {
          setValidationResult({ isValid: true })
        }
      }, 500)
    }
  }

  const handleManualUpdate = async () => {
    if (!data.code.trim()) {
      setRenderError("Please enter some Mermaid code")
      return
    }

    const result = await MermaidValidator.validateCode(data.code)
    setValidationResult(result)

    if (!result.isValid) {
      setRenderError("Cannot update diagram with syntax errors. Please fix the errors first.")
    }
  }

  const handleValidationChange = (result: MermaidValidationResult) => {
    setValidationResult(result)

    if (!result.isValid && !alwaysCollapseErrors) {
      setErrorPanelCollapsed(false)
    } else if (result.isValid) {
      setErrorPanelCollapsed(true)
    }
  }

  const handleTemplateSelect = (template: { type: MermaidData["type"]; code: string }) => {
    setData((prev) => ({
      ...prev,
      type: template.type,
      code: template.code,
    }))
    setShowTemplateSelector(false)
  }

  const handleSave = () => {
    if (!data.code.trim()) {
      setRenderError("Please enter some Mermaid code")
      return
    }

    if (!validationResult.isValid) {
      setRenderError("Cannot save diagram with syntax errors. Please fix the errors first.")
      return
    }

    onSave(data)
  }

  const handleCancel = () => {
    onCancel()
  }

  // Cleanup timeout on unmount
  useEffect(() => {
    return () => {
      if (updateTimeoutRef.current) {
        clearTimeout(updateTimeoutRef.current)
      }
    }
  }, [])

  return (
    <BlockEditorShell
      settings={settings}
      onClose={handleCancel}
      icon={<GitBranch className="h-5 w-5 text-blue-600 dark:text-blue-400" />}
      title="Mermaid Diagram Editor"
      headerMeta={
        <>
          <div className="text-sm">
            <span className="text-gray-600 dark:text-gray-400">Theme:</span>
            <span className="ml-2 font-medium text-gray-800 dark:text-gray-200">
              {data.theme ? MERMAID_THEME_DESCRIPTIONS[data.theme] : "Default"}
            </span>
          </div>
          <div className="text-sm">
            <span className="text-gray-600 dark:text-gray-400">Mode:</span>
            <span className="ml-2 font-medium text-gray-800 dark:text-gray-200">
              {MERMAID_THEME_MODE_DESCRIPTIONS[data.themeMode || "system"].label}
            </span>
          </div>
          {(() => {
            const pair = getMermaidThemePair((data.theme as any) || "default", (data.themeMode as any) || "system")
            return (
              <div className="text-xs text-gray-500 dark:text-gray-400">
                ({pair.themeLight} / {pair.themeDark})
              </div>
            )
          })()}
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
        </>
      }
    >

        {/* Template Selector */}
        {showTemplateSelector && (
          <MermaidTemplateSelector onSelect={handleTemplateSelect} onCancel={handleCancel} />
        )}

        {/* Main Content */}
        {!showTemplateSelector && (
          <>
            {/* Settings Bar */}
            <div className="flex items-center gap-4 p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
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

              {/* Theme Selector */}
              <div className="flex items-center gap-2">
                <Label htmlFor="theme" className="text-sm font-medium text-gray-700 dark:text-gray-300">
                  Theme:
                </Label>
                <Select 
                  value={data.theme || "default"} 
                  onValueChange={(value) => setData((prev) => ({ ...prev, theme: value as any }))}
                >
                  <SelectTrigger className="w-32">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {AVAILABLE_MERMAID_THEMES.map((theme) => (
                      <SelectItem key={theme} value={theme}>
                        {MERMAID_THEME_DESCRIPTIONS[theme]}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>

              {/* Theme Mode Selector */}
              <div className="flex items-center gap-2">
                <Label htmlFor="theme-mode" className="text-sm font-medium text-gray-700 dark:text-gray-300">
                  Mode:
                </Label>
                <Select 
                  value={data.themeMode || "system"} 
                  onValueChange={(value) => setData((prev) => ({ ...prev, themeMode: value as any }))}
                >
                  <SelectTrigger className="w-36">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {Object.entries(MERMAID_THEME_MODE_DESCRIPTIONS).map(([mode, { label, description }]) => (
                      <SelectItem key={mode} value={mode}>
                        <div>
                          <div className="font-medium">{label}</div>
                          <div className="text-xs text-gray-500">{description}</div>
                        </div>
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
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
              <div className="w-1/2 border-r border-gray-200 dark:border-gray-800 flex flex-col bg-white dark:bg-gray-900">
                <div className="p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
                  <h3 className="font-medium flex items-center gap-2 text-gray-800 dark:text-gray-200">
                    <FileText className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                    Mermaid Code
                  </h3>
                </div>

                {/* Editor Area - ajustando altura baseado no estado da aba de erros */}
                <div className="flex-1 flex flex-col min-h-0">
                  <div className={`${!validationResult.isValid && !errorPanelCollapsed ? "h-1/2" : "flex-1"} min-h-0`}>
                    <div className="h-full p-4 bg-white dark:bg-gray-950">
                      <MonacoMermaidEditor
                        value={data.code}
                        onChange={handleCodeChange}
                        onValidationChange={handleValidationChange}
                        height="100%"
                        theme={isDarkMode ? "dark" : "light"}
                        options={settings.editor}
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
                      <div className="flex items-center justify-between p-2 border-b border-red-200 dark:border-red-900 bg-red-50 dark:bg-red-950/50">
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
                              {validationResult.error || "Unknown error"}
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
                <div className="p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
                  <h3 className="font-medium flex items-center gap-2 text-gray-800 dark:text-gray-200">
                    <Users className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                    Live Preview
                  </h3>
                </div>
                <div className="flex-1 p-4 overflow-auto bg-white dark:bg-gray-950">
                  {renderError && (
                    <div className="mb-4 text-red-500 dark:text-red-400 p-4 border border-red-300 dark:border-red-700 rounded-lg bg-red-50 dark:bg-red-950/30 shadow-sm">
                      <div className="font-medium mb-2 flex items-center gap-1">
                        <span>⚠️</span>
                        <span>Editor Error</span>
                      </div>
                      <div className="text-sm whitespace-pre-line">{renderError}</div>
                    </div>
                  )}
                  <MermaidViewer
                    data={data}
                    size={100}
                    showControls={true}
                    allowFullscreen={true}
                    className="min-h-[400px]"
                  />
                </div>
              </div>
            </div>

            {/* Footer */}
            <div className="p-4 border-t border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
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
                    onClick={handleCancel}
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
    </BlockEditorShell>
  )
}
