"use client"

import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { useLexicalNodeSelection } from "@lexical/react/useLexicalNodeSelection"
import { mergeRegister } from "@lexical/utils"
import {
  $getNodeByKey,
  $getSelection,
  $isNodeSelection,
  CLICK_COMMAND,
  COMMAND_PRIORITY_LOW,
  DecoratorNode,
  KEY_BACKSPACE_COMMAND,
  KEY_DELETE_COMMAND,
  type NodeKey,
  SELECTION_CHANGE_COMMAND,
} from "lexical"
import { useCallback, useEffect, useRef, useState } from "react"
import { Button } from "@/components/ui/button"
import { Edit, Trash2, ZoomIn, ZoomOut, RotateCcw, Maximize2, X } from "lucide-react"
import { VegaLiteEditor } from "@/components/editor/extras/vega-lite/vega-lite-editor"
import { ContentEditMenu } from "@/components/editor/extras/content-edit-menu"
import type { JSX } from "react/jsx-runtime"

export interface VegaLiteData {
  spec: string // JSON specification for Vega-Lite
  title?: string
  caption?: string
  size?: number
  theme?: "default" | "dark" | "excel" | "ggplot2" | "quartz" | "vox" | "fivethirtyeight" | "latimes"
  layout?: "square" | "rectangular" // New layout option
}

export class VegaLiteNode extends DecoratorNode<JSX.Element> {
  __data: VegaLiteData

  static getType(): string {
    return "vega-lite"
  }

  static clone(node: VegaLiteNode): VegaLiteNode {
    return new VegaLiteNode(node.__data, node.__key)
  }

  constructor(data: VegaLiteData, key?: NodeKey) {
    super(key)
    this.__data = data
  }

  createDOM(): HTMLElement {
    const div = document.createElement("div")
    div.style.display = "contents"
    return div
  }

  updateDOM(): false {
    return false
  }

  setData(data: VegaLiteData): void {
    const writable = this.getWritable()
    writable.__data = data
  }

  getData(): VegaLiteData {
    return this.getLatest().__data
  }

  decorate(): JSX.Element {
    return <VegaLiteComponent nodeKey={this.getKey()} data={this.__data} />
  }

  static importJSON(serializedNode: any): VegaLiteNode {
    const { data } = serializedNode
    return new VegaLiteNode(data)
  }

  exportJSON() {
    return {
      data: this.__data,
      type: "vega-lite",
      version: 1,
    }
  }

  isInline(): false {
    return false
  }
}

interface VegaLiteComponentProps {
  nodeKey: NodeKey
  data: VegaLiteData
}

function VegaLiteComponent({ nodeKey, data }: VegaLiteComponentProps) {
  const [editor] = useLexicalComposerContext()
  const [isSelected, setSelected, clearSelection] = useLexicalNodeSelection(nodeKey)
  const [showEditor, setShowEditor] = useState(false)
  const [chartElement, setChartElement] = useState<HTMLElement | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string>("")
  const [hasAutoOpened, setHasAutoOpened] = useState(false)
  const [zoom, setZoom] = useState(100)
  const [isFullscreen, setIsFullscreen] = useState(false)
  const [fullscreenZoom, setFullscreenZoom] = useState(100)
  const vegaRef = useRef<HTMLDivElement>(null)
  const fullscreenVegaRef = useRef<HTMLDivElement>(null)

  const onDelete = useCallback(
    (payload: KeyboardEvent) => {
      if (isSelected && $isNodeSelection($getSelection())) {
        const event: KeyboardEvent = payload
        event.preventDefault()
        const node = $getNodeByKey(nodeKey)
        if (node) {
          node.remove()
        }
      }
      return false
    },
    [isSelected, nodeKey],
  )

  const onEdit = () => {
    setShowEditor(true)
  }

  const onSave = (newData: VegaLiteData) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey) as VegaLiteNode
      if (node) {
        node.setData(newData)
      }
    })
    setShowEditor(false)
  }

  const onCancel = () => {
    setShowEditor(false)
  }

  const handleZoomIn = () => {
    setZoom((prev) => Math.min(prev + 25, 300))
  }

  const handleZoomOut = () => {
    setZoom((prev) => Math.max(prev - 25, 25))
  }

  const handleZoomReset = () => {
    setZoom(100)
  }

  const handleFullscreenZoomIn = () => {
    setFullscreenZoom((prev) => Math.min(prev + 25, 500))
  }

  const handleFullscreenZoomOut = () => {
    setFullscreenZoom((prev) => Math.max(prev - 25, 25))
  }

  const handleFullscreenZoomReset = () => {
    setFullscreenZoom(100)
  }

  const toggleFullscreen = () => {
    setIsFullscreen(!isFullscreen)
    if (!isFullscreen) {
      setFullscreenZoom(100) // Reset fullscreen zoom when opening
    }
  }

  const renderChart = async (container: HTMLElement, spec: string) => {
    if (!container || !spec) return

    try {
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
      if (data.layout === "square") {
        // Square layout: 400x400
        parsedSpec.width = 400
        parsedSpec.height = 400
      } else if (data.layout === "rectangular") {
        // Rectangular layout: full width, proportional height
        parsedSpec.width = "container"
        parsedSpec.height = 300
      }

      // Try to use Vega-Lite if available, otherwise show placeholder
      try {
        // This will be caught if vega-lite is not installed
        const vegaLiteImport = await import("vega-lite" as any).catch(() => null)
        const vegaImport = await import("vega" as any).catch(() => null)
        
        if (!vegaLiteImport || !vegaImport) {
          throw new Error("Vega-Lite not available")
        }

        // Compile Vega-Lite spec to Vega spec
        const vegaSpec = vegaLiteImport.compile(parsedSpec).spec

        // Create a new view and render
        const view = new vegaImport.View(vegaImport.parse(vegaSpec))
          .renderer("svg")
          .initialize(container)
          .hover()

        await view.runAsync()
        
        // Apply centering styles for square layout
        if (data.layout === "square" && container.firstElementChild) {
          const svgElement = container.firstElementChild as HTMLElement
          svgElement.style.display = "block"
          svgElement.style.margin = "0 auto"
        }
        return view
      } catch (vegaError) {
        // Show a placeholder when vega-lite is not available
        container.innerHTML = `
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
            <h3 style="margin: 16px 0 8px 0; font-size: 16px; font-weight: 600;">Vega-Lite Chart</h3>
            <p style="margin: 0; font-size: 14px; text-align: center; max-width: 300px;">
              Chart preview requires vega-lite package.<br/>
              Install it to see your visualization.
            </p>
            <div style="margin-top: 12px; padding: 8px 12px; background: #e9ecef; border-radius: 4px; font-family: monospace; font-size: 12px;">
              npm install vega-lite vega
            </div>
          </div>
        `
        return null
      }
    } catch (err: any) {
      console.error("Error rendering Vega-Lite chart:", err)
      throw new Error(err?.message || "Failed to render chart")
    }
  }

  useEffect(() => {
    const renderVegaChart = async () => {
      if (!data.spec || !vegaRef.current) return

      setIsLoading(true)
      setError("")

      try {
        // Clear any existing content
        vegaRef.current.innerHTML = ""

        await renderChart(vegaRef.current, data.spec)
        setError("")
      } catch (err: any) {
        console.error("Error rendering Vega-Lite chart:", err)
        setError(err?.message || "Failed to render chart")
        
        // Show error message in container
        if (vegaRef.current) {
          vegaRef.current.innerHTML = `
            <div class="text-red-500 p-4 border border-red-300 rounded bg-red-50 dark:bg-red-900/20 dark:border-red-700">
              <div class="font-medium">Error rendering chart:</div>
              <div class="text-sm mt-1">${err?.message || "Unknown error"}</div>
            </div>
          `
        }
      } finally {
        setIsLoading(false)
      }
    }

    renderVegaChart()
  }, [data.spec, data.theme, data.layout, nodeKey])

  // Render fullscreen chart when fullscreen is opened
  useEffect(() => {
    if (isFullscreen && fullscreenVegaRef.current && data.spec) {
      const renderFullscreenChart = async () => {
        try {
          fullscreenVegaRef.current!.innerHTML = ""
          await renderChart(fullscreenVegaRef.current!, data.spec)
        } catch (err: any) {
          console.error("Error rendering fullscreen chart:", err)
          if (fullscreenVegaRef.current) {
            fullscreenVegaRef.current.innerHTML = `
              <div class="text-red-500 p-4 border border-red-300 rounded bg-red-50">
                <div class="font-medium">Error rendering chart:</div>
                <div class="text-sm mt-1">${err?.message || "Unknown error"}</div>
              </div>
            `
          }
        }
      }
      renderFullscreenChart()
    }
  }, [isFullscreen, data.spec, data.theme, data.layout])

  useEffect(() => {
    return mergeRegister(
      editor.registerCommand(
        CLICK_COMMAND,
        (payload) => {
          const event = payload
          if (event.target === vegaRef.current || vegaRef.current?.contains(event.target as Node)) {
            if (!event.shiftKey) {
              clearSelection()
            }
            setSelected(!isSelected)
            return true
          }
          return false
        },
        COMMAND_PRIORITY_LOW,
      ),
      editor.registerCommand(KEY_DELETE_COMMAND, onDelete, COMMAND_PRIORITY_LOW),
      editor.registerCommand(KEY_BACKSPACE_COMMAND, onDelete, COMMAND_PRIORITY_LOW),
      editor.registerCommand(
        SELECTION_CHANGE_COMMAND,
        () => {
          if ($isNodeSelection($getSelection())) {
            return false
          }
          clearSelection()
          return false
        },
        COMMAND_PRIORITY_LOW,
      ),
    )
  }, [clearSelection, editor, isSelected, nodeKey, onDelete, setSelected])

  useEffect(() => {
    // Auto-open for new charts with empty or default specs
    const isNewChart = !data.spec || data.spec.trim() === "" || data.spec === "{}"

    if (isNewChart && !hasAutoOpened) {
      setShowEditor(true)
      setHasAutoOpened(true)
    }
  }, [data.spec, hasAutoOpened])

  return (
    <>
      <div
        ref={vegaRef}
        className={`relative group my-4 ${isSelected ? "ring-2 ring-blue-500" : ""}`}
        style={{ width: `${data.size || 100}%` }}
      >
        {/* Chart Container */}
        <div className="border rounded-lg bg-white dark:bg-gray-800 p-4 shadow-sm relative flex flex-col items-center">
          {data.title && <h3 className="text-lg font-semibold mb-2 text-center dark:text-white">{data.title}</h3>}

          {/* Zoom Controls */}
          <div className="absolute top-2 right-2 flex gap-1 bg-white dark:bg-gray-800 rounded-md shadow-lg border dark:border-gray-600 p-1 opacity-0 group-hover:opacity-100 transition-opacity z-50">
            <Button
              variant="ghost"
              size="sm"
              onClick={handleZoomOut}
              disabled={zoom <= 25}
              className="h-8 w-8 p-0 relative z-50"
              title="Zoom Out"
            >
              <ZoomOut className="h-4 w-4" />
            </Button>
            <span className="text-xs px-2 py-1 bg-gray-100 dark:bg-gray-700 rounded text-gray-700 dark:text-gray-300 min-w-[3rem] text-center">
              {zoom}%
            </span>
            <Button
              variant="ghost"
              size="sm"
              onClick={handleZoomIn}
              disabled={zoom >= 300}
              className="h-8 w-8 p-0 relative z-50"
              title="Zoom In"
            >
              <ZoomIn className="h-4 w-4" />
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={handleZoomReset}
              className="h-8 w-8 p-0 relative z-50"
              title="Reset Zoom"
            >
              <RotateCcw className="h-4 w-4" />
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={toggleFullscreen}
              className="h-8 w-8 p-0 relative z-50"
              title="Fullscreen"
            >
              <Maximize2 className="h-4 w-4" />
            </Button>
          </div>

          {/* Chart Content */}
          <div className={`w-full flex justify-center items-center relative overflow-auto max-h-[700px] ${
            data.layout === "square" ? "min-h-[450px]" : "min-h-[350px]"
          }`}>
            {isLoading ? (
              <div className="text-gray-500 dark:text-gray-400">Rendering chart...</div>
            ) : error ? (
              <div className="text-red-500 p-4 border border-red-300 rounded bg-red-50 dark:bg-red-900/20 dark:border-red-700 max-w-full">
                <div className="font-medium">Error rendering chart:</div>
                <div className="text-sm mt-1">{error}</div>
              </div>
            ) : data.spec ? (
              <div
                className="flex justify-center items-center transition-transform duration-200 ease-in-out"
                style={{
                  transform: `scale(${zoom / 100})`,
                  transformOrigin: "center",
                }}
                // Chart will be rendered directly into vegaRef
              />
            ) : (
              <div className="text-gray-500 dark:text-gray-400">Click Edit to create your chart</div>
            )}
          </div>

          {data.caption && (
            <p className="text-sm text-gray-600 dark:text-gray-300 mt-2 text-center italic">{data.caption}</p>
          )}
        </div>

        {/* ContentEditMenu for lateral edit button */}
        <ContentEditMenu
          options={[
            {
              id: "edit",
              icon: <Edit className="h-4 w-4" />,
              label: "Edit Chart",
              action: onEdit,
            },
          ]}
        />

        {/* Controls */}
        {isSelected && (
          <div className="absolute top-2 right-2 flex gap-1 bg-white rounded-md shadow-lg border p-1">
            <Button variant="ghost" size="sm" onClick={onEdit} className="h-8 w-8 p-0">
              <Edit className="h-4 w-4" />
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => onDelete(new KeyboardEvent("keydown"))}
              className="h-8 w-8 p-0 text-red-600 hover:text-red-700"
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          </div>
        )}
      </div>

      {/* Fullscreen Modal */}
      {isFullscreen && (
        <div className="fixed inset-0 bg-black/90 dark:bg-black/90 z-50 flex items-center justify-center">
          <div className="w-full h-full flex flex-col">
            {/* Fullscreen Header */}
            <div className="flex justify-between items-center p-4 bg-white dark:bg-gray-900 border-b dark:border-gray-700">
              <div className="flex items-center gap-4">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                  {data.title || "Vega-Lite Chart"}
                </h3>
                <div className="flex gap-1 bg-gray-100 dark:bg-gray-800 rounded-md p-1">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={handleFullscreenZoomOut}
                    disabled={fullscreenZoom <= 25}
                    className="h-8 w-8 p-0 text-gray-700 dark:text-white hover:bg-gray-200 dark:hover:bg-gray-700"
                    title="Zoom Out"
                  >
                    <ZoomOut className="h-4 w-4" />
                  </Button>
                  <span className="text-xs px-2 py-1 bg-gray-200 dark:bg-gray-700 rounded text-gray-700 dark:text-white min-w-[3rem] text-center">
                    {fullscreenZoom}%
                  </span>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={handleFullscreenZoomIn}
                    disabled={fullscreenZoom >= 500}
                    className="h-8 w-8 p-0 text-gray-700 dark:text-white hover:bg-gray-200 dark:hover:bg-gray-700"
                    title="Zoom In"
                  >
                    <ZoomIn className="h-4 w-4" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={handleFullscreenZoomReset}
                    className="h-8 w-8 p-0 text-gray-700 dark:text-white hover:bg-gray-200 dark:hover:bg-gray-700"
                    title="Reset Zoom"
                  >
                    <RotateCcw className="h-4 w-4" />
                  </Button>
                </div>
              </div>
              <Button
                variant="ghost"
                size="sm"
                onClick={toggleFullscreen}
                className="h-8 w-8 p-0 text-gray-700 dark:text-white hover:bg-gray-200 dark:hover:bg-gray-700"
                title="Exit Fullscreen"
              >
                <X className="h-4 w-4" />
              </Button>
            </div>

            {/* Fullscreen Content */}
            <div className="flex-1 flex items-center justify-center p-8 overflow-auto bg-gray-50 dark:bg-gray-900">
              <div
                ref={fullscreenVegaRef}
                className="transition-transform duration-200 ease-in-out"
                style={{
                  transform: `scale(${fullscreenZoom / 100})`,
                  transformOrigin: "center",
                }}
              />
            </div>

            {/* Fullscreen Footer */}
            {data.caption && (
              <div className="p-4 bg-white dark:bg-gray-900 border-t dark:border-gray-700 text-center">
                <p className="text-gray-600 dark:text-gray-300 italic">{data.caption}</p>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Vega-Lite Editor Modal */}
      {showEditor && <VegaLiteEditor initialData={data} onSave={onSave} onCancel={onCancel} />}
    </>
  )
}

export function $createVegaLiteNode(data: VegaLiteData): VegaLiteNode {
  return new VegaLiteNode(data)
}

export function $isVegaLiteNode(node: any): node is VegaLiteNode {
  return node instanceof VegaLiteNode
}