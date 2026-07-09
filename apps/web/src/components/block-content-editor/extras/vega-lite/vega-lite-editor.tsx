"use client"

import { useState, useEffect, useRef } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Save, FileText, BarChart3, AlertCircle, CheckCircle, Square, RectangleHorizontal } from "lucide-react"
import type { VegaLiteData } from "@/components/block-content-editor/nodes/vega-lite-node"
import { useEditorSettings } from "../settings-menu"
import { BlockEditorShell } from "@/components/block-content-editor/extras/block-editor-shell"
import { VegaLiteTemplateSelector } from "./vega-lite-template-selector"
import { MonacoVegaLiteEditor } from "./monaco-vega-lite-editor"
import { type VegaLiteValidationResult } from "./vega-lite-validator"
import { VegaLiteExport } from "./vega-lite-export"
import { ControlledVegaLiteViewer } from "./controlled-vega-lite-viewer"
import { getThemePair, AVAILABLE_THEMES, THEME_DESCRIPTIONS, THEME_MODE_DESCRIPTIONS } from "@/components/block-content-editor/extras/vega-lite/vega-theme-helper"
import { VegaLiteManager } from "./vega-lite-manager"
import { useTheme } from "next-themes"

interface VegaLiteEditorProps {
  initialData?: VegaLiteData
  onSave: (data: VegaLiteData) => void
  onCancel: () => void
}

export function VegaLiteEditor({ initialData, onSave, onCancel }: VegaLiteEditorProps) {
  const { resolvedTheme } = useTheme()
  const isDarkMode = resolvedTheme === "dark"
  
  const [data, setData] = useState<VegaLiteData>(
    initialData || {
      spec: "",
      title: "",
      caption: "",
      size: 100,
      theme: "default",
      themeMode: "system",
      layout: "rectangular",
      data: {},
    },
  )
  const [autoUpdate, setAutoUpdate] = useState(true)
  const [showTemplateSelector, setShowTemplateSelector] = useState(!initialData)
  const [validationResult, setValidationResult] = useState<VegaLiteValidationResult>({ isValid: true })
  const [errorPanelCollapsed, setErrorPanelCollapsed] = useState(false)
  const [alwaysCollapseErrors, setAlwaysCollapseErrors] = useState(false)
  const [manualUpdateKey, setManualUpdateKey] = useState(0)
  const settings = useEditorSettings("vega")
  const [previewSpec, setPreviewSpec] = useState(initialData?.spec || "")
  const [previewData, setPreviewData] = useState<VegaLiteData>(initialData || {
    spec: "",
    title: "",
    caption: "",
    size: 100,
    theme: "default",
    themeMode: "system",
    layout: "rectangular",
    data: {},
  })
  const updateTimeoutRef = useRef<NodeJS.Timeout | null>(null)

  const handleSpecChange = (newSpec: string | undefined) => {
    const spec = newSpec || ""
    console.log("Spec changed, new length:", spec.length)
    setData((prev) => ({ ...prev, spec }))
  }

  const handleValidationChange = (result: VegaLiteValidationResult) => {
    setValidationResult(result)

    if (!result.isValid) {
      if (!alwaysCollapseErrors) {
        setErrorPanelCollapsed(false)
      }
    } else {
      setErrorPanelCollapsed(true)
    }
  }

  const handleTemplateSelect = (template: { type: string; spec: string; title?: string }) => {
    console.log("Template selected:", template.type, "Spec length:", template.spec.length)
    
    // Update the data state
    const newData = {
      ...data,
      spec: template.spec,
      title: template.title || data.title,
    }
    
    setData(newData)
    
    // Close template selector
    setShowTemplateSelector(false)
    
    // Always update preview when template is selected, regardless of auto-update setting
    setPreviewData(newData)
    setPreviewSpec(newData.spec)
    setManualUpdateKey(prev => prev + 1)
  }

  const handleSave = () => {
    if (!data.spec.trim()) {
      return
    }

    if (!validationResult.isValid) {
      return
    }

    onSave(data)
  }

  const handleCancel = () => {
    onCancel()
  }

  const handleManualUpdate = () => {
    // Update preview data with current editor data
    setPreviewData(data)
    setPreviewSpec(data.spec)
    // Force update by changing the trigger
    setManualUpdateKey(prev => prev + 1)
  }

  // Effect to handle auto-update
  useEffect(() => {
    if (autoUpdate) {
      // Debounce auto-updates
      if (updateTimeoutRef.current) {
        clearTimeout(updateTimeoutRef.current)
      }
      
      updateTimeoutRef.current = setTimeout(() => {
        setPreviewData(data)
        setPreviewSpec(data.spec)
        setManualUpdateKey(prev => prev + 1) // Trigger update
      }, 500) // 500ms debounce
    }
  }, [data, autoUpdate])

  // Initial preview update when component mounts
  useEffect(() => {
    if (initialData?.spec && autoUpdate) {
      setPreviewData(data)
      setPreviewSpec(data.spec)
      setManualUpdateKey(prev => prev + 1) // Trigger initial update
    }
  }, []) // Only run on mount

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
      icon={<BarChart3 className="h-5 w-5 text-blue-600 dark:text-blue-400" />}
      title="Vega-Lite Chart Editor"
      headerMeta={
        <>
          <div className="text-sm">
            <span className="text-gray-600 dark:text-gray-400">Theme:</span>
            <span className="ml-2 font-medium text-gray-800 dark:text-gray-200">
              {data.theme ? THEME_DESCRIPTIONS[data.theme] : "Default"}
            </span>
          </div>
          <div className="text-sm">
            <span className="text-gray-600 dark:text-gray-400">Mode:</span>
            <span className="ml-2 font-medium text-gray-800 dark:text-gray-200">
              {THEME_MODE_DESCRIPTIONS[data.themeMode || "system"].label}
            </span>
          </div>
          {(() => {
            const pair = getThemePair((data.theme as any) || "default", (data.themeMode as any) || "system")
            return (
              <div className="text-xs text-gray-500 dark:text-gray-400">
                ({pair.themeLight} / {pair.themeDark})
              </div>
            )
          })()}
          <div className="flex items-center gap-1 ml-auto">
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
          <VegaLiteTemplateSelector onSelect={handleTemplateSelect} onCancel={() => setShowTemplateSelector(false)} />
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
                  placeholder="Chart title (optional)"
                  className="w-48 bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 focus:border-blue-500 dark:focus:border-blue-400"
                />
              </div>

              {/* Theme Selector */}
              <div className="flex items-center gap-2">
                <Label htmlFor="theme" className="text-sm font-medium text-gray-700 dark:text-gray-300">
                  Theme:
                </Label>
                <Select value={data.theme || "default"} onValueChange={(value) => setData((prev) => ({ ...prev, theme: value as any }))}>
                  <SelectTrigger className="w-40">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent className="max-h-60 overflow-y-auto">
                    {AVAILABLE_THEMES.map((theme) => (
                      <SelectItem key={theme} value={theme}>
                        {THEME_DESCRIPTIONS[theme]}
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
                <Select value={data.themeMode || "system"} onValueChange={(value) => setData((prev) => ({ ...prev, themeMode: value as any }))}>
                  <SelectTrigger className="w-48">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {Object.entries(THEME_MODE_DESCRIPTIONS).map(([mode, { label, description }]) => (
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
                  className="border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-white dark:bg-gray-800"
                >
                  Update Preview
                </Button>
              )}

              <div className="flex items-center gap-2 ml-auto">
                <VegaLiteManager
                  data={data.data || {}}
                  onDataChange={(data) => setData((prev) => ({ ...prev, data }))}
                />
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
              {/* Left Panel - Spec Editor */}
              <div className="w-1/2 border-r border-gray-200 dark:border-gray-800 flex flex-col bg-white dark:bg-gray-900">
                <div className="p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
                  <h3 className="font-medium flex items-center gap-2 text-gray-800 dark:text-gray-200">
                    <FileText className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                    Vega-Lite Specification
                  </h3>
                </div>

                {/* Editor Area */}
                <div className="flex-1 flex flex-col min-h-0">
                  <div className={`${!validationResult.isValid && !errorPanelCollapsed ? "h-1/2" : "flex-1"} min-h-0`}>
                    <div className="h-full p-4 bg-white dark:bg-gray-950">
                      <MonacoVegaLiteEditor
                        value={data.spec}
                        onChange={handleSpecChange}
                        onValidationChange={handleValidationChange}
                        height="100%"
                        theme={isDarkMode ? "dark" : "light"}
                        options={settings.editor}
                      />
                    </div>
                  </div>

                  {/* Error Panel */}
                  {!validationResult.isValid && (
                    <div
                      className={`border-t border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-950/50 transition-all duration-300 ${
                        errorPanelCollapsed ? "h-10" : "h-1/2"
                      }`}
                    >
                      <div className="flex items-center justify-between p-2 border-b border-red-200 dark:border-red-900 bg-red-50 dark:bg-red-950/50">
                        <div className="flex items-center gap-2">
                          <AlertCircle className="h-4 w-4 text-red-600 dark:text-red-400" />
                          <span className="text-sm font-medium text-red-800 dark:text-red-300">
                            Validation Errors ({validationResult.error ? "1" : "0"})
                          </span>
                        </div>

                        <div className="flex items-center gap-2">
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

                      {!errorPanelCollapsed && (
                        <div className="p-3 overflow-y-auto overflow-x-hidden flex-1 max-h-full">
                          <div className="text-red-700 dark:text-red-300">
                            <div className="font-medium mb-2 flex items-center gap-1">
                              <span>⚠️</span>
                              <span>Specification Error</span>
                            </div>
                            <div className="text-sm whitespace-pre-line font-mono bg-red-100 dark:bg-red-900/40 border border-red-200 dark:border-red-800 p-3 rounded-lg shadow-sm">
                              {validationResult.error}
                            </div>
                            <div className="mt-3 text-xs text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-950/30 p-2 rounded border border-red-200 dark:border-red-800">
                              💡 <strong>Tip:</strong> Check your JSON syntax and Vega-Lite specification structure.
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
                  <div className="flex items-center justify-between">
                    <h3 className="font-medium flex items-center gap-2 text-gray-800 dark:text-gray-200">
                      <BarChart3 className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                      Live Preview
                    </h3>
                    
                    {/* Export Buttons */}
                    {(() => {
                      const themePair = getThemePair(previewData.theme as any || "default", previewData.themeMode as any || "system")
                      return (
                        <VegaLiteExport
                          spec={previewSpec}
                          themeLight={themePair.themeLight}
                          themeDark={themePair.themeDark}
                          layout={previewData.layout}
                          title={previewData.title}
                          isValid={validationResult.isValid && previewSpec.trim() !== ""}
                          data={previewData.data}
                        />
                      )
                    })()}
                  </div>
                </div>
                <div className="flex-1 p-4 overflow-auto bg-white dark:bg-gray-950">
                  {/* Use ControlledVegaLiteViewer for smooth updates */}
                  {(() => {
                    const themePair = getThemePair(previewData.theme as any || "default", previewData.themeMode as any || "system")
                    return (
                      <ControlledVegaLiteViewer 
                        spec={previewSpec}
                        layout={previewData.layout}
                        themeLight={themePair.themeLight}
                        themeDark={themePair.themeDark}
                        title={previewData.title}
                        showControls={true}
                        allowFullscreen={false}
                        className="h-full"
                        updateTrigger={manualUpdateKey}
                        data={previewData.data}
                      />
                    )
                  })()}
                </div>
              </div>
            </div>

            {/* Footer */}
            <div className="p-4 border-t border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
              <div className="flex items-center gap-4">
                <div className="flex-1 grid grid-cols-2 gap-4">
                  <div>
                    <Label htmlFor="caption" className="text-sm font-medium text-gray-700 dark:text-gray-300">
                      Caption:
                    </Label>
                    <Input
                      id="caption"
                      value={data.caption || ""}
                      onChange={(e) => setData((prev) => ({ ...prev, caption: e.target.value }))}
                      placeholder="Optional caption"
                      className="mt-1 bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 focus:border-blue-500 dark:focus:border-blue-400"
                    />
                  </div>
                  <div>
                    <Label htmlFor="layout" className="text-sm font-medium text-gray-700 dark:text-gray-300">
                      Layout:
                    </Label>
                    <Select 
                      value={data.layout || "rectangular"} 
                      onValueChange={(value) => {
                        console.log("Layout changed to:", value)
                        setData((prev) => ({ ...prev, layout: value as "square" | "rectangular" }))
                      }}
                    >
                      <SelectTrigger className="mt-1">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="rectangular">
                          <div className="flex items-center gap-2">
                            <RectangleHorizontal className="h-4 w-4" />
                            <div>
                              <div className="font-medium">Rectangular</div>
                              <div className="text-xs text-gray-500">Full width, proportional height</div>
                            </div>
                          </div>
                        </SelectItem>
                        <SelectItem value="square">
                          <div className="flex items-center gap-2">
                            <Square className="h-4 w-4" />
                            <div>
                              <div className="font-medium">Square</div>
                              <div className="text-xs text-gray-500">Centered 400x400 pixels</div>
                            </div>
                          </div>
                        </SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
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
                    Save Chart
                  </Button>
                </div>
              </div>
            </div>
          </>
        )}
    </BlockEditorShell>
  )
}