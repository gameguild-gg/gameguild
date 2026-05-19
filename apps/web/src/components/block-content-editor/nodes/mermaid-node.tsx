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
import { Edit, Trash2 } from "lucide-react"
import { MermaidEditor } from "@/components/block-content-editor/lazy-client-components"
import { ContentEditMenu } from "@/components/block-content-editor/extras/content-edit-menu"
import { MermaidViewer } from "@/components/block-content-editor/extras/mermaid/mermaid-viewer"
import type { JSX } from "react/jsx-runtime"

export interface MermaidData {
  code: string
  type: "flowchart" | "class" | "sequence" | "xyChart" | "radar" | "quadrant" | "sankey" | "state" | "c4context" | "architecture" | "er" | "gantt" | "pie" | "gitgraph" | "mindmap" | "journey" | "timeline" | "quadrantChart" | "requirement" | "c4Context" | "c4Container" | "c4Component" | "c4Dynamic" | "c4Deployment" | "treemap-beta" | "kanban"
  direction?: "TD" | "TB" | "BT" | "RL"
  theme?: "default" | "dark" | "forest" | "neutral" | "base" | "default-dark" | "forest-dark" | "neutral-dark" | "base-dark"
  themeMode?: "system" | "light" | "dark" | "both"
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
  const [hasAutoOpened, setHasAutoOpened] = useState(false)
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
        className={`relative ${isSelected ? "ring-2 ring-blue-500 rounded-lg" : ""}`}
      >
        {/* MermaidViewer with integrated zoom, pan, and fullscreen */}
        <MermaidViewer
          data={data}
          title={data.title}
          caption={data.caption}
          size={data.size || 100}
          showControls={true}
          allowFullscreen={true}
        />

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

        {/* Controls - Edit and Delete (only when selected) */}
        {isSelected && (
          <div className="absolute top-2 right-14 flex gap-1 bg-white dark:bg-gray-900 rounded-md shadow-lg border dark:border-gray-700 p-1 z-60">
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