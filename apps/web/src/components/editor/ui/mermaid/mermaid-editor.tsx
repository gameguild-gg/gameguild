"use client"

import { useState, useEffect, useRef } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Switch } from "@/components/ui/switch"
import { X, Save, FileText, GitBranch, Users } from "lucide-react"
import type { MermaidData } from "@/components/editor/nodes/mermaid-node"
import { MermaidTemplateSelector } from "./mermaid-template-selector"

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
  const [showTemplateSelector, setShowTemplateSelector] = useState(!initialData)
  const [error, setError] = useState<string>("")
  const [isLoading, setIsLoading] = useState(false)
  const previewRef = useRef<HTMLDivElement>(null)
  const updateTimeoutRef = useRef<NodeJS.Timeout>()

  const renderDiagram = async (code: string) => {
    if (!code.trim()) {
      setSvgContent("")
      setError("")
      return
    }

    setIsLoading(true)
    setError("")

    try {
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
      })

      // Generate unique ID for this diagram
      const id = `mermaid-editor-${Date.now()}`

      // Render the diagram
      const { svg } = await mermaid.render(id, code)
      setSvgContent(svg)
      setError("")
    } catch (err: any) {
      console.error("Error rendering Mermaid diagram:", err)
      setError(err?.message || "Failed to render diagram")
      setSvgContent("")
    } finally {
      setIsLoading(false)
    }
  }

  const handleCodeChange = (newCode: string) => {
    setData((prev) => ({ ...prev, code: newCode }))

    if (autoUpdate) {
      // Debounce the update
      if (updateTimeoutRef.current) {
        clearTimeout(updateTimeoutRef.current)
      }
      updateTimeoutRef.current = setTimeout(() => {
        renderDiagram(newCode)
      }, 500)
    }
  }

  const handleManualUpdate = () => {
    renderDiagram(data.code)
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
    onSave(data)
  }

  // Initial render
  useEffect(() => {
    if (initialData?.code && autoUpdate) {
      renderDiagram(initialData.code)
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

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-7xl h-[90vh] flex flex-col">
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b">
          <div className="flex items-center gap-2">
            <GitBranch className="h-5 w-5 text-blue-600" />
            <h2 className="text-xl font-semibold">Mermaid Diagram Editor</h2>
          </div>
          <Button variant="ghost" size="sm" onClick={onCancel}>
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
            <div className="flex items-center gap-4 p-4 border-b bg-gray-50">
              <div className="flex items-center gap-2">
                <Label htmlFor="title" className="text-sm font-medium">
                  Title:
                </Label>
                <Input
                  id="title"
                  value={data.title || ""}
                  onChange={(e) => setData((prev) => ({ ...prev, title: e.target.value }))}
                  placeholder="Diagram title (optional)"
                  className="w-48"
                />
              </div>

              <div className="flex items-center gap-2">
                <Label htmlFor="auto-update" className="text-sm font-medium">
                  Auto Update:
                </Label>
                <Switch id="auto-update" checked={autoUpdate} onCheckedChange={setAutoUpdate} />
              </div>

              {!autoUpdate && (
                <Button variant="outline" size="sm" onClick={handleManualUpdate}>
                  Update Preview
                </Button>
              )}

              <div className="flex items-center gap-2 ml-auto">
                <span className="text-sm text-gray-600">Type: {data.type}</span>
                <Button variant="outline" size="sm" onClick={() => setShowTemplateSelector(true)}>
                  Change Template
                </Button>
              </div>
            </div>

            {/* Editor Content */}
            <div className="flex-1 flex min-h-0">
              {/* Left Panel - Code Editor */}
              <div className="w-1/2 border-r flex flex-col">
                <div className="p-4 border-b bg-gray-50">
                  <h3 className="font-medium flex items-center gap-2">
                    <FileText className="h-4 w-4" />
                    Mermaid Code
                  </h3>
                </div>
                <div className="flex-1 p-4">
                  <Textarea
                    value={data.code}
                    onChange={(e) => handleCodeChange(e.target.value)}
                    placeholder="Enter your Mermaid diagram code here..."
                    className="w-full h-full resize-none font-mono text-sm"
                    style={{ minHeight: "400px" }}
                  />
                </div>
              </div>

              {/* Right Panel - Preview */}
              <div className="w-1/2 flex flex-col">
                <div className="p-4 border-b bg-gray-50">
                  <h3 className="font-medium flex items-center gap-2">
                    <Users className="h-4 w-4" />
                    Live Preview
                  </h3>
                </div>
                <div className="flex-1 p-4 overflow-auto">
                  {isLoading ? (
                    <div className="flex items-center justify-center h-full text-gray-500">Rendering diagram...</div>
                  ) : error ? (
                    <div className="text-red-500 p-4 border border-red-300 rounded bg-red-50">
                      <div className="font-medium">Error:</div>
                      <div className="text-sm mt-1">{error}</div>
                    </div>
                  ) : svgContent ? (
                    <div
                      ref={previewRef}
                      className="flex justify-center items-start"
                      dangerouslySetInnerHTML={{ __html: svgContent }}
                    />
                  ) : (
                    <div className="flex items-center justify-center h-full text-gray-500">
                      {data.code.trim() ? "Enter code to see preview" : "Enter code to see preview"}
                    </div>
                  )}
                </div>
              </div>
            </div>

            {/* Footer */}
            <div className="p-4 border-t bg-gray-50">
              <div className="flex items-center gap-4">
                <div className="flex-1">
                  <Label htmlFor="caption" className="text-sm font-medium">
                    Caption:
                  </Label>
                  <Input
                    id="caption"
                    value={data.caption || ""}
                    onChange={(e) => setData((prev) => ({ ...prev, caption: e.target.value }))}
                    placeholder="Optional caption for the diagram"
                    className="mt-1"
                  />
                </div>

                <div className="flex gap-2">
                  <Button variant="outline" onClick={onCancel}>
                    Cancel
                  </Button>
                  <Button onClick={handleSave} className="flex items-center gap-2">
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
