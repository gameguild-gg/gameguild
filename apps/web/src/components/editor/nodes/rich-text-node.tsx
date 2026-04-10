"use client"

import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $getNodeByKey, DecoratorNode, type NodeKey, type SerializedLexicalNode } from "lexical"
import { useContext, useEffect, useRef, useState } from "react"
import { Edit, FileText } from "lucide-react"
import { RichTextEditor } from "@/components/editor/extras/rich-text/rich-text-editor"
import { RichTextPreviewRenderer } from "@/components/editor/extras/rich-text/rich-text-preview-renderer"
import type { JSX } from "react/jsx-runtime"
import { EditorLoadingContext } from "../lexical-editor"

export interface RichTextData {
  /** Serialized Lexical EditorState JSON string */
  content: string
  title?: string
}

export interface SerializedRichTextNode extends SerializedLexicalNode {
  type: "rich-text"
  data: RichTextData
  version: 1
}

export class RichTextNode extends DecoratorNode<JSX.Element> {
  __data: RichTextData

  static getType(): string {
    return "rich-text"
  }

  static clone(node: RichTextNode): RichTextNode {
    return new RichTextNode(node.__data, node.__key)
  }

  constructor(data: RichTextData, key?: NodeKey) {
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

  setData(data: RichTextData): void {
    const writable = this.getWritable()
    writable.__data = data
  }

  getData(): RichTextData {
    return this.getLatest().__data
  }

  exportJSON(): SerializedRichTextNode {
    return {
      type: "rich-text",
      data: this.__data,
      version: 1,
    }
  }

  static importJSON(serializedNode: SerializedRichTextNode): RichTextNode {
    return new RichTextNode(serializedNode.data)
  }

  decorate(): JSX.Element {
    return <RichTextComponent nodeKey={this.getKey()} data={this.__data} />
  }
}

interface RichTextComponentProps {
  data: RichTextData
  nodeKey: string
}

function RichTextComponent({ data, nodeKey }: RichTextComponentProps) {
  const [editor] = useLexicalComposerContext()
  const isLoading = useContext(EditorLoadingContext)
  const [showEditor, setShowEditor] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  const handleSave = (updatedData: RichTextData) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (node instanceof RichTextNode) {
        node.setData(updatedData)
      }
    })
    setShowEditor(false)
  }

  const handleCancel = () => {
    setShowEditor(false)
  }

  const isEmpty = !data.content

  return (
    <>
      <div
        ref={containerRef}
        className="my-2 relative group cursor-pointer"
        onClick={() => !isLoading && setShowEditor(true)}
      >
        <div className="rounded-lg border border-gray-200 dark:border-gray-700 bg-muted/20 hover:border-blue-400 dark:hover:border-blue-500 transition-colors">
          {/* Header bar */}
          <div className="flex items-center justify-between px-3 py-1.5 border-b border-gray-200 dark:border-gray-700 bg-muted/30 rounded-t-lg">
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
              <FileText className="h-4 w-4" />
              Rich Text
              {data.title && <span className="font-medium text-foreground">— {data.title}</span>}
            </div>
            <button
              className="opacity-0 group-hover:opacity-100 transition-opacity p-1 hover:bg-muted rounded"
              onClick={(e) => {
                e.stopPropagation()
                setShowEditor(true)
              }}
            >
              <Edit className="h-3.5 w-3.5 text-muted-foreground" />
            </button>
          </div>

          {/* Content preview */}
          <div className="p-3">
            {isEmpty ? (
              <div className="text-sm text-muted-foreground italic py-4 text-center">
                Click to add rich text content...
              </div>
            ) : (
              <RichTextPreviewRenderer content={data.content} />
            )}
          </div>
        </div>
      </div>

      {showEditor && (
        <RichTextEditor
          initialData={data}
          onSave={handleSave}
          onCancel={handleCancel}
        />
      )}
    </>
  )
}

export function $createRichTextNode(): RichTextNode {
  return new RichTextNode({ content: "" })
}
