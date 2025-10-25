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
import { useCallback, useEffect, useState } from "react"
import { Button } from "@/components/ui/button"
import { Edit } from "lucide-react"
import { VegaLiteEditor } from "@/components/editor/extras/vega-lite/vega-lite-editor"
import { ContentEditMenu } from "@/components/editor/extras/content-edit-menu"
import { VegaLiteViewer } from "@/components/ui/vega-lite-viewer"
import { getThemePair } from "@/lib/vega-theme-helper"
import type { JSX } from "react/jsx-runtime"

export interface VegaLiteData {
  spec: string // JSON specification for Vega-Lite
  title?: string
  caption?: string
  size?: number
  // Theme configuration: single theme base with mode selector
  theme?: "default" | "excel" | "ggplot2" | "quartz" | "vox" | "fivethirtyeight" | "latimes" | "urbaninstitute" | "googlecharts" | "powerbi"
  themeMode?: "system" | "only-light" | "only-dark" // Mode for theme application
  layout?: "square" | "rectangular" // Layout option
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
  const [hasAutoOpened, setHasAutoOpened] = useState(false)

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

  useEffect(() => {
    return mergeRegister(
      editor.registerCommand(
        CLICK_COMMAND,
        (payload) => {
          const event = payload
          // Only select if clicking within the component area
          const componentElement = event.target as HTMLElement
          const isWithinComponent = componentElement.closest('.vega-lite-node-container')
          
          if (isWithinComponent) {
            if (!event.shiftKey) {
              clearSelection()
            }
            setSelected(!isSelected)
            return true
          }
          
          // Clear selection if clicking outside
          if (isSelected) {
            clearSelection()
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
      <div className="relative group my-4 vega-lite-node-container">
        {/* Use VegaLiteViewer component with all its functionality */}
        {(() => {
          const themePair = getThemePair(data.theme as any || "default", data.themeMode as any || "system")
          return (
            <VegaLiteViewer 
              spec={data.spec}
              layout={data.layout}
              themeLight={themePair.themeLight}
              themeDark={themePair.themeDark}
              title={data.title}
              caption={data.caption}
              size={data.size}
              showControls={true}
              allowFullscreen={true}
              className=""
            />
          )
        })()}

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
      </div>

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