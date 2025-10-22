"use client"

import { useState, useEffect, useRef } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { X, Save, FileText, BarChart3, AlertCircle, CheckCircle, Square, RectangleHorizontal } from "lucide-react"
import type { VegaLiteData } from "@/components/editor/nodes/vega-lite-node"
import { VegaLiteTemplateSelector } from "./vega-lite-template-selector"
import { MonacoVegaLiteEditor } from "./monaco-vega-lite-editor"
import { VegaLiteValidator, type VegaLiteValidationResult } from "./vega-lite-validator"

interface VegaLiteEditorProps {
  initialData?: VegaLiteData
  onSave: (data: VegaLiteData) => void
  onCancel: () => void
}

export function VegaLiteEditor({ initialData, onSave, onCancel }: VegaLiteEditorProps) {
  const [data, setData] = useState<VegaLiteData>(
    initialData || {
      spec: "",
      title: "",
      caption: "",
      size: 100,
      theme: "default",
      layout: "rectangular", // Default to rectangular
    },
  )
  const [autoUpdate, setAutoUpdate] = useState(true)
  const [chartElement, setChartElement] = useState<HTMLElement | null>(null)
  const [showTemplateSelector, setShowTemplateSelector] = useState(!initialData)
  const [error, setError] = useState<string>("")
  const [isLoading, setIsLoading] = useState(false)
  const [validationResult, setValidationResult] = useState<VegaLiteValidationResult>({ isValid: true })
  const [zoomLevel, setZoomLevel] = useState(100)
  const [errorPanelCollapsed, setErrorPanelCollapsed] = useState(false)
  const [alwaysCollapseErrors, setAlwaysCollapseErrors] = useState(false)
  const [templateJustSelected, setTemplateJustSelected] = useState(false)
  const previewRef = useRef<HTMLDivElement>(null)
  const updateTimeoutRef = useRef<NodeJS.Timeout | null>(null)

  const renderChart = async (spec: string, forceValidation = false, layoutOverride?: "square" | "rectangular") => {
    const currentLayout = layoutOverride || data.layout || "rectangular"
    console.log("renderChart called with spec length:", spec.length, "forceValidation:", forceValidation, "layout:", currentLayout)
    
    if (!spec.trim()) {
      if (previewRef.current) {
        previewRef.current.innerHTML = ""
      }
      setError("")
      return
    }

    // Ensure preview container exists before proceeding
    if (!previewRef.current) {
      // If the template selector is still open, the preview isn't mounted yet; wait for close effect
      if (showTemplateSelector) {
        console.log("Preview ref not ready (template selector open); skipping render for now")
        return
      }
      console.log("Preview ref not ready; deferring render by 50ms")
      setTimeout(() => {
        // Use latest layout on retry
        renderChart(spec, forceValidation, currentLayout)
      }, 50)
      return
    }

    setIsLoading(true)
    setError("")

    try {
      // Always validate before rendering if forced or if current validation is invalid
      let currentValidation = validationResult
      if (forceValidation || !validationResult.isValid) {
        currentValidation = await VegaLiteValidator.validateSpec(spec)
        setValidationResult(currentValidation)
        console.log("Validation result:", currentValidation)
      }

      // Only stop rendering if validation fails with a critical error
      if (!currentValidation.isValid && currentValidation.error?.includes("Invalid JSON")) {
        setError(currentValidation.error || "Invalid Vega-Lite specification")
        setIsLoading(false)
        return
      }

      if (!previewRef.current) {
        // If container disappears during async steps, safely abort and retry later
        console.log("Preview ref disappeared during render; aborting this cycle")
        return
      }

      // Parse the specification
      let parsedSpec
      try {
        parsedSpec = typeof spec === 'string' ? JSON.parse(spec) : spec
      } catch (parseError) {
        throw new Error("Invalid JSON specification")
      }

      // Apply theme if specified
      if (data.theme && data.theme !== "default") {
        parsedSpec.config = parsedSpec.config || {}
        parsedSpec.config.theme = data.theme
      }

      // Apply layout settings
      if (currentLayout === "square") {
        // Square layout: 400x400
        parsedSpec.width = 400
        parsedSpec.height = 400
      } else if (currentLayout === "rectangular") {
        // Rectangular layout: full width, proportional height
        parsedSpec.width = "container"
        parsedSpec.height = 300
      }

      try {
        // Dynamic import of Vega-Lite and Vega
        const vegaLiteImport = await import("vega-lite" as any).catch(() => null)
        const vegaImport = await import("vega" as any).catch(() => null)
        
        if (!vegaLiteImport || !vegaImport) {
          throw new Error("Vega-Lite not available")
        }

        // Compile Vega-Lite spec to Vega spec
        const vegaSpec = vegaLiteImport.compile(parsedSpec).spec

        // Clear previous content (guard again)
        if (previewRef.current) {
          previewRef.current.innerHTML = ""
        }

        // Create a new view and render
        const view = new vegaImport.View(vegaImport.parse(vegaSpec))
          .renderer("svg")
          .initialize(previewRef.current)
          .hover()

        await view.runAsync()
        console.log("Chart rendered successfully")
        setError("")
      } catch (renderError: any) {
        console.error("Vega-Lite render error:", renderError)
        
        if (renderError.message?.includes("Vega-Lite not available")) {
          // Show installation message instead of chart
          if (previewRef.current) {
            previewRef.current.innerHTML = `
              <div style="
                display: flex; 
                align-items: center; 
                justify-content: center; 
                height: 300px; 
                background: #f8f9fa; 
                border: 2px dashed #dee2e6; 
                border-radius: 8px;
                flex-direction: column;
                color: #6c757d;
                font-family: system-ui, -apple-system, sans-serif;
              ">
                <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M3 3v18h18"/>
                  <path d="m19 9-5 5-4-4-3 3"/>
                </svg>
                <h3 style="margin: 16px 0 8px 0; font-size: 16px; font-weight: 600;">Vega-Lite Preview</h3>
                <p style="margin: 0; font-size: 14px; text-align: center; max-width: 300px;">
                  Preview requires vega-lite package.<br/>
                  Install it to see live chart rendering.
                </p>
                <div style="margin-top: 12px; padding: 8px 12px; background: #e9ecef; border-radius: 4px; font-family: monospace; font-size: 12px;">
                  npm install vega-lite vega
                </div>
              </div>
            `
          }
          setError("")
          return
        }
        
        const errorMessage = renderError?.message || "Failed to render chart"
        
        // Set validation as invalid if render fails
        setValidationResult({ isValid: false, error: errorMessage })
        setError(`Render error: ${errorMessage}`)
      }
    } catch (err: any) {
      console.error("Error loading Vega-Lite:", err)
      setError("Failed to load Vega-Lite library. Please try again.")
    } finally {
      setIsLoading(false)
    }
  }

  const handleSpecChange = (newSpec: string | undefined) => {
    const spec = newSpec || ""
    console.log("Spec changed, new length:", spec.length)
    setData((prev) => ({ ...prev, spec }))

    if (autoUpdate && spec.trim() !== "") {
      // Debounce the update with forced validation
      if (updateTimeoutRef.current) {
        clearTimeout(updateTimeoutRef.current)
      }
      updateTimeoutRef.current = setTimeout(() => {
        console.log("Auto-updating from spec change...")
        renderChart(spec, true) // Force validation
      }, 500)
    }
  }

  const handleValidationChange = (result: VegaLiteValidationResult) => {
    setValidationResult(result)

    if (!result.isValid) {
      setError(result.error || "Invalid Vega-Lite specification")
      if (!alwaysCollapseErrors) {
        setErrorPanelCollapsed(false)
      }
    } else {
      setErrorPanelCollapsed(true)
    }
  }

  const handleManualUpdate = () => {
    renderChart(data.spec, true) // Force validation
  }

  const handleTemplateSelect = (template: { type: string; spec: string; title?: string }) => {
    console.log("Template selected:", template.type, "Spec length:", template.spec.length)
    
    // Set flag to indicate template was just selected
    setTemplateJustSelected(true)
    
    // Update the data state
    setData((prev) => ({
      ...prev,
      spec: template.spec,
      title: template.title || prev.title,
    }))
    
    // Close template selector
    setShowTemplateSelector(false)
  }

  const handleSave = () => {
    if (!data.spec.trim()) {
      setError("Please enter a Vega-Lite specification")
      return
    }

    if (!validationResult.isValid) {
      setError("Cannot save chart with validation errors. Please fix the errors first.")
      return
    }

    onSave(data)
  }

  // Initial render
  useEffect(() => {
    if (initialData?.spec && autoUpdate) {
      renderChart(initialData.spec, true) // Force validation
    }
  }, [])

  // When closing the template selector, ensure first render happens once the preview is mounted
  useEffect(() => {
    if (!showTemplateSelector && data.spec && data.spec.trim() !== "") {
      // Defer to next tick to let the preview container mount
      setTimeout(() => {
        console.log("Template selector closed; triggering initial render of selected template")
        renderChart(data.spec, true, data.layout)
        // Reset the template-selected flag after the first render trigger
        setTemplateJustSelected(false)
      }, 0)
    }
  }, [showTemplateSelector])

  // Re-render when data.spec changes (for template selection and other updates)
  useEffect(() => {
    console.log("useEffect triggered - spec length:", data.spec.length, "autoUpdate:", autoUpdate, "templateJustSelected:", templateJustSelected)
    
    if (data.spec && data.spec.trim() !== "") {
      // If template was just selected, skip this useEffect as renderChart was already called
      if (templateJustSelected) {
        console.log("Skipping useEffect because template was just selected")
        return
      }
      
      // For manual typing, respect autoUpdate setting
      if (autoUpdate) {
        // Clear timeout to avoid duplicate renders
        if (updateTimeoutRef.current) {
          clearTimeout(updateTimeoutRef.current)
        }
        // Render with delay for user typing
        updateTimeoutRef.current = setTimeout(() => {
          console.log("Auto-updating from useEffect...")
          renderChart(data.spec, true)
        }, 300)
      }
    }
  }, [data.spec, autoUpdate, templateJustSelected])

  // Separate useEffect for layout changes to ensure immediate re-render
  useEffect(() => {
    if (data.spec && data.spec.trim() !== "" && !templateJustSelected) {
      console.log("Layout changed, re-rendering chart (raf)...")
      const raf = requestAnimationFrame(() => {
        renderChart(data.spec, true, data.layout)
      })
      return () => cancelAnimationFrame(raf)
    }
  }, [data.layout, templateJustSelected])

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
            <BarChart3 className="h-5 w-5 text-blue-600 dark:text-blue-400" />
            <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Vega-Lite Chart Editor</h2>
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
          <VegaLiteTemplateSelector onSelect={handleTemplateSelect} onCancel={() => setShowTemplateSelector(false)} />
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
                  placeholder="Chart title (optional)"
                  className="w-48 bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 focus:border-blue-500 dark:focus:border-blue-400"
                />
              </div>

              <div className="flex items-center gap-2">
                <Label htmlFor="theme" className="text-sm font-medium text-gray-700 dark:text-gray-300">
                  Theme:
                </Label>
                <Select value={data.theme} onValueChange={(value) => setData((prev) => ({ ...prev, theme: value as any }))}>
                  <SelectTrigger className="w-32">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="default">Default</SelectItem>
                    <SelectItem value="dark">Dark</SelectItem>
                    <SelectItem value="excel">Excel</SelectItem>
                    <SelectItem value="ggplot2">ggplot2</SelectItem>
                    <SelectItem value="quartz">Quartz</SelectItem>
                    <SelectItem value="vox">Vox</SelectItem>
                    <SelectItem value="fivethirtyeight">FiveThirtyEight</SelectItem>
                    <SelectItem value="latimes">LA Times</SelectItem>
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
              <div className="w-1/2 border-r border-gray-200 dark:border-gray-700 flex flex-col bg-white dark:bg-gray-900">
                <div className="p-4 border-b border-gray-200 dark:border-gray-700 bg-gradient-to-r from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-850">
                  <h3 className="font-medium flex items-center gap-2 text-gray-800 dark:text-gray-200">
                    <FileText className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                    Vega-Lite Specification
                  </h3>
                </div>

                {/* Editor Area */}
                <div className="flex-1 flex flex-col min-h-0">
                  <div className={`${!validationResult.isValid && !errorPanelCollapsed ? "h-1/2" : "flex-1"} min-h-0`}>
                    <div className="h-full p-4 bg-gray-50 dark:bg-gray-900">
                      <MonacoVegaLiteEditor
                        value={data.spec}
                        onChange={handleSpecChange}
                        onValidationChange={handleValidationChange}
                        height="100%"
                        theme="light"
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
                      <div className="flex items-center justify-between p-2 border-b border-red-200 dark:border-red-800 bg-gradient-to-r from-red-100 to-red-50 dark:from-red-900/50 dark:to-red-950/50">
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
                              {validationResult.error || error}
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
                <div className="p-4 border-b border-gray-200 dark:border-gray-700 bg-gradient-to-r from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-850">
                  <div className="flex items-center justify-between">
                    <h3 className="font-medium flex items-center gap-2 text-gray-800 dark:text-gray-200">
                      <BarChart3 className="h-4 w-4 text-blue-600 dark:text-blue-400" />
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
                        Rendering chart...
                      </div>
                    </div>
                  ) : error && !previewRef.current?.hasChildNodes() ? (
                    <div className="text-red-500 dark:text-red-400 p-4 border border-red-300 dark:border-red-700 rounded-lg bg-red-50 dark:bg-red-950/30 shadow-sm">
                      <div className="font-medium mb-2 flex items-center gap-1">
                        <span>⚠️</span>
                        <span>Chart Error</span>
                      </div>
                      <div className="text-sm whitespace-pre-line">{error}</div>
                      <div className="mt-3 text-xs text-red-600 dark:text-red-400 bg-red-100 dark:bg-red-900/40 p-2 rounded border border-red-200 dark:border-red-800">
                        💡 <strong>Tip:</strong> Check your Vega-Lite specification and make sure it follows the correct schema.
                      </div>
                    </div>
                  ) : (
                    <div className="relative">
                      {error && (
                        <div className="absolute top-2 right-2 z-10 bg-red-100 dark:bg-red-900/90 border border-red-300 dark:border-red-700 rounded-lg p-2 shadow-lg max-w-xs backdrop-blur-sm">
                          <div className="flex items-center gap-1 text-red-700 dark:text-red-300 text-xs">
                            <AlertCircle className="h-3 w-3" />
                            <span className="font-medium">Validation Error</span>
                          </div>
                          <div className="text-xs text-red-600 dark:text-red-400 mt-1">Showing last valid chart</div>
                        </div>
                      )}
                      <div
                        ref={previewRef}
                        className={`flex justify-center items-start p-4 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 shadow-sm transition-all duration-300 ${
                          data.layout === "square" ? "min-h-[450px]" : "min-h-[350px]"
                        }`}
                        style={{ transform: `scale(${zoomLevel / 100})`, transformOrigin: "top center" }}
                      />
                      {!previewRef.current?.hasChildNodes() && !isLoading && (!data.spec || data.spec.trim() === "") && (
                        <div className="absolute inset-0 flex items-center justify-center text-gray-500 dark:text-gray-400">
                          <div className="text-center">
                            <BarChart3 className="h-12 w-12 mx-auto mb-2 text-gray-300 dark:text-gray-600" />
                            <p>Enter Vega-Lite specification to see preview</p>
                          </div>
                        </div>
                      )}
                    </div>
                  )}
                </div>
              </div>
            </div>

            {/* Footer */}
            <div className="p-4 border-t border-gray-200 dark:border-gray-700 bg-gradient-to-r from-gray-50 to-gray-100 dark:from-gray-800 dark:to-gray-850">
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
                    Save Chart
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