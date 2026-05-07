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
  type SerializedLexicalNode,
} from "lexical"
import { useCallback, useEffect, useRef, useState } from "react"
import { Edit } from "lucide-react"
import { MarkdownEditor } from "@/components/block-content-editor/lazy-client-components"
import { ContentEditMenu } from "@/components/block-content-editor/extras/content-edit-menu"
import { useMarkdownComponents } from "@/components/block-content-editor/extras/markdown/markdown-components"
import ReactMarkdown from "react-markdown"
import remarkGfm from "remark-gfm"
import rehypeRaw from "rehype-raw"
import type { JSX } from "react/jsx-runtime"

export interface MarkdownData {
  content: string
  title?: string
  caption?: string
}

export interface SerializedMarkdownNode extends SerializedLexicalNode {
  type: "markdown"
  data: MarkdownData
  version: 1
}

export class MarkdownNode extends DecoratorNode<JSX.Element> {
  __data: MarkdownData

  static getType(): string {
    return "markdown"
  }

  static clone(node: MarkdownNode): MarkdownNode {
    return new MarkdownNode(node.__data, node.__key)
  }

  constructor(data: MarkdownData, key?: NodeKey) {
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

  setData(data: MarkdownData): void {
    const writable = this.getWritable()
    writable.__data = data
  }

  getData(): MarkdownData {
    return this.getLatest().__data
  }

  decorate(): JSX.Element {
    return <MarkdownComponent nodeKey={this.getKey()} data={this.__data} />
  }

  static importJSON(serializedNode: SerializedMarkdownNode): MarkdownNode {
    const { data } = serializedNode
    return new MarkdownNode(data)
  }

  exportJSON(): SerializedMarkdownNode {
    return {
      data: this.__data,
      type: "markdown",
      version: 1,
    }
  }

  isInline(): false {
    return false
  }
}

interface MarkdownComponentProps {
  nodeKey: NodeKey
  data: MarkdownData
}

function MarkdownComponent({ nodeKey, data }: MarkdownComponentProps) {
  const [editor] = useLexicalComposerContext()
  const [isSelected, setSelected, clearSelection] = useLexicalNodeSelection(nodeKey)
  const [showEditor, setShowEditor] = useState(false)
  const [hasAutoOpened, setHasAutoOpened] = useState(false)
  const markdownRef = useRef<HTMLDivElement>(null)
  const markdownComponents = useMarkdownComponents()

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

  const onSave = (newData: MarkdownData) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey) as MarkdownNode
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
          if (event.target === markdownRef.current) {
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
    // Auto-open for new markdown blocks
    const isNewMarkdown = !data.content || data.content.trim() === ""

    if (isNewMarkdown && !hasAutoOpened) {
      setShowEditor(true)
      setHasAutoOpened(true)
    }
  }, [data.content, hasAutoOpened])

  return (
    <>
      <div
        ref={markdownRef}
        className={`relative my-4 ${isSelected ? "ring-2 ring-blue-500 rounded-lg" : ""}`}
      >
        <div className="">
          {data.content ? (
            <ReactMarkdown 
              remarkPlugins={[remarkGfm]}
              rehypePlugins={[rehypeRaw]}
              components={markdownComponents}
            >
              {data.content}
            </ReactMarkdown>
          ) : (
            <p className="text-gray-400 dark:text-gray-600 italic">
              Click to add markdown content...
            </p>
          )}
        </div>

        {/* ContentEditMenu for lateral edit button */}
        <ContentEditMenu
          options={[
            {
              id: "edit",
              icon: <Edit className="h-4 w-4" />,
              label: "Edit Markdown",
              action: onEdit,
            },
          ]}
        />
      </div>

      {/* Markdown Editor Modal */}
      {showEditor && <MarkdownEditor initialData={data} onSave={onSave} onCancel={onCancel} />}
    </>
  )
}

export function $createMarkdownNode(data?: Partial<MarkdownData>): MarkdownNode {
  return new MarkdownNode({
    content: data?.content || "",
    title: data?.title,
    caption: data?.caption,
  })
}

export function $isMarkdownNode(node: any): node is MarkdownNode {
  return node instanceof MarkdownNode
}
