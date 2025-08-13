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
import { MermaidEditor } from "@/components/editor/ui/mermaid/mermaid-editor"
import { ContentEditMenu } from "@/components/editor/ui/content-edit-menu"
import type { JSX } from "react/jsx-runtime"

export interface MermaidData {
  code: string
  type: "flowchart" | "class" | "sequence" | "xyChart" | "radar" | "quadrant" | "sankey" | "state" | "c4context" | "architecture" | "er" | "gantt" | "pie" | "gitgraph" | "mindmap" | "journey" | "timeline" | "quadrantChart" | "requirement" | "c4Context" | "c4Container" | "c4Component" | "c4Dynamic" | "c4Deployment"
  direction?: "TD" | "TB" | "BT" | "RL"
  theme?: "default" | "dark" | "forest" | "neutral"
  fontFamily?: string
  title?: string
  caption?: string
  size?: number
}

export class MermaidNode extends DecoratorNode<JSX.Element> {
  __data: MermaidData

  static getType(): string {
    return "mermaid"
  }

  static clone(node: MermaidNode): MermaidNode {
    return new MermaidNode(node.__data, node.__key)
  }

  constructor(data: MermaidData, key?: NodeKey) {
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

  setData(data: MermaidData): void {
    const writable = this.getWritable()
    writable.__data = data
  }

  getData(): MermaidData {
    return this.getLatest().__data
  }

  decorate(): JSX.Element {
    return <MermaidComponent nodeKey={this.getKey()} data={this.__data} />
  }

  static importJSON(serializedNode: any): MermaidNode {
    const { data } = serializedNode
    return new MermaidNode(data)
  }

  exportJSON() {
    return {
      data: this.__data,
      type: "mermaid",
      version: 1,
    }
  }

  isInline(): false {
    return false
  }
}

interface MermaidComponentProps {
  nodeKey: NodeKey
  data: MermaidData
}

function MermaidComponent({ nodeKey, data }: MermaidComponentProps) {
  const [editor] = useLexicalComposerContext()
  const [isSelected, setSelected, clearSelection] = useLexicalNodeSelection(nodeKey)
  const [showEditor, setShowEditor] = useState(false)
  const [svgContent, setSvgContent] = useState<string>("")
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string>("")
  const [hasAutoOpened, setHasAutoOpened] = useState(false)
  const [zoom, setZoom] = useState(100)
  const [isFullscreen, setIsFullscreen] = useState(false)
  const [fullscreenZoom, setFullscreenZoom] = useState(100)
  const mermaidRef = useRef<HTMLDivElement>(null)

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

  const onSave = (newData: MermaidData) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey) as MermaidNode
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

  useEffect(() => {
    const renderDiagram = async () => {
      if (!data.code || !mermaidRef.current) return

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
        const id = `mermaid-${nodeKey}-${Date.now()}`

        // Clear any existing content
        setSvgContent("")

        // Render the diagram
        const { svg } = await mermaid.render(id, data.code)
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

    renderDiagram()
  }, [data.code, nodeKey])

  useEffect(() => {
    return mergeRegister(
      editor.registerCommand(
        CLICK_COMMAND,
        (payload) => {
          const event = payload
          if (event.target === mermaidRef.current) {
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
    // Only auto-open for truly new diagrams (empty or exact default templates)
    const defaultTemplates = [
      "graph TD\n    A[Start] --> B[Process]\n    B --> C[End]",
      "classDiagram\n    class Animal {\n        +String name\n        +makeSound()\n    }",
      "sequenceDiagram\n    Alice->>Bob: Hello Bob, how are you?\n    Bob-->>Alice: I am good thanks!",
    ]

    const isNewDiagram = !data.code || data.code.trim() === "" || defaultTemplates.includes(data.code.trim())

    if (isNewDiagram && !hasAutoOpened) {
      setShowEditor(true)
      setHasAutoOpened(true)
    }
  }, [data.code, hasAutoOpened])

  return (
    <>
      <div
        ref={mermaidRef}
        className={`relative group my-4 ${isSelected ? "ring-2 ring-blue-500" : ""}`}
        style={{ width: `${data.size || 100}%` }}
      >
        {/* Diagram Container */}
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

          {/* Zoom Controls */}
          <div className="w-full flex justify-center items-center min-h-[500px] relative overflow-auto max-h-[700px]">
            {isLoading ? (
              <div className="text-gray-500 dark:text-gray-400">Rendering diagram...</div>
            ) : error ? (
              <div className="text-red-500 p-4 border border-red-300 rounded bg-red-50 dark:bg-red-900/20 dark:border-red-700 max-w-full">
                <div className="font-medium">Error rendering diagram:</div>
                <div className="text-sm mt-1">{error}</div>
              </div>
            ) : svgContent ? (
              <div
                className="flex justify-center items-center transition-transform duration-200 ease-in-out"
                style={{
                  transform: `scale(${zoom / 100})`,
                  transformOrigin: "center",
                  minWidth: data.type === "sequence" ? "600px" : "auto",
                  minHeight: data.type === "sequence" ? "400px" : "auto",
                }}
                dangerouslySetInnerHTML={{ __html: svgContent }}
              />
            ) : (
              <div className="text-gray-500 dark:text-gray-400">Click Edit to create your diagram</div>
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
              label: "Edit Diagram",
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
                  {data.title || "Mermaid Diagram"}
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
              {isLoading ? (
                <div className="text-gray-700 dark:text-white">Rendering diagram...</div>
              ) : error ? (
                <div className="text-red-600 dark:text-red-400 p-4 border border-red-300 dark:border-red-600 rounded bg-red-50 dark:bg-red-900/20 max-w-full">
                  <div className="font-medium">Error rendering diagram:</div>
                  <div className="text-sm mt-1">{error}</div>
                </div>
              ) : svgContent ? (
                <div
                  className="transition-transform duration-200 ease-in-out"
                  style={{
                    transform: `scale(${fullscreenZoom / 100})`,
                    transformOrigin: "center",
                  }}
                  dangerouslySetInnerHTML={{ __html: svgContent }}
                />
              ) : (
                <div className="text-gray-500 dark:text-gray-400">No diagram to display</div>
              )}
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

      {/* Mermaid Editor Modal */}
      {showEditor && <MermaidEditor initialData={data} onSave={onSave} onCancel={onCancel} />}
    </>
  )
}

export function $createMermaidNode(data: MermaidData): MermaidNode {
  return new MermaidNode(data)
}

export function $isMermaidNode(node: any): node is MermaidNode {
  return node instanceof MermaidNode
}
