"use client"

import { useState, useEffect, useContext } from "react"
import { DecoratorNode, type SerializedLexicalNode } from "lexical"
import { $getNodeByKey } from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { ChevronDown, Pencil, Check } from "lucide-react"
import type { JSX } from "react/jsx-runtime"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/ui/dropdown-menu"
import { Admonition as UIAdmonition, type AdmonitionType } from "@/components/editor/extras/admonition"
import { ContentEditMenu } from "@/components/editor/extras/content-edit-menu"
import { EditorLoadingContext } from "../lexical-editor"

export interface AdmonitionData {
  title: string
  content: string
  type: AdmonitionType
  isNew?: boolean
}

export interface SerializedAdmonitionNode extends SerializedLexicalNode {
  type: "admonition"
  data: AdmonitionData
  version: 1
}

export class AdmonitionNode extends DecoratorNode<JSX.Element> {
  __data: AdmonitionData

  static getType(): string {
    return "admonition"
  }

  static clone(node: AdmonitionNode): AdmonitionNode {
    return new AdmonitionNode(node.__data, node.__key)
  }

  constructor(data: AdmonitionData, key?: string) {
    super(key)
    this.__data = {
      title: data.title || "",
      content: data.content || "",
      type: data.type || "note",
      isNew: data.isNew,
    }
  }

  createDOM(): HTMLElement {
    return document.createElement("div")
  }

  updateDOM(): false {
    return false
  }

  setData(data: AdmonitionData): void {
    const writable = this.getWritable()
    writable.__data = data
  }

  exportJSON(): SerializedAdmonitionNode {
    return {
      type: "admonition",
      data: this.__data,
      version: 1,
    }
  }

  static importJSON(serializedNode: SerializedAdmonitionNode): AdmonitionNode {
    return new AdmonitionNode(serializedNode.data)
  }

  decorate(): JSX.Element {
    return <AdmonitionComponent data={this.__data} nodeKey={this.__key} />
  }
}

interface AdmonitionComponentProps {
  data: AdmonitionData
  nodeKey: string
}

function AdmonitionComponent({ data, nodeKey }: AdmonitionComponentProps) {
  const [editor] = useLexicalComposerContext()
  const isLoading = useContext(EditorLoadingContext)
  const [isEditing, setIsEditing] = useState((data.isNew || false) && !isLoading)
  const [title, setTitle] = useState(data.title || "")
  const [content, setContent] = useState(data.content || "")
  const [type, setType] = useState<AdmonitionType>(data.type || "note")

  useEffect(() => {
    if (data.isNew) {
      editor.update(() => {
        const node = $getNodeByKey(nodeKey)
        if (node instanceof AdmonitionNode) {
          const { isNew, ...rest } = data
          node.setData(rest)
        }
      })
    }
  }, [data, editor, nodeKey])

  useEffect(() => {
    if (isLoading) {
      setIsEditing(false)
    }
  }, [isLoading])

  const updateAdmonition = (newData: Partial<AdmonitionData>) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (node instanceof AdmonitionNode) {
        node.setData({ ...data, ...newData })
      }
    })
  }

  const handleTitleChange = (newTitle: string) => {
    setTitle(newTitle)
    updateAdmonition({ title: newTitle })
  }

  const handleContentChange = (newContent: string) => {
    setContent(newContent)
    updateAdmonition({ content: newContent })
  }

  const handleTypeChange = (newType: AdmonitionType) => {
    setType(newType)
    updateAdmonition({ type: newType })
  }

  if (!isEditing) {
    return (
      <div className="my-4 relative">
        <UIAdmonition title={title} content={content} type={type} />
        <ContentEditMenu
          options={[
            {
              id: "edit",
              icon: <Pencil className="h-4 w-4" />,
              label: "Edit Admonition",
              action: () => setIsEditing(true),
            },
          ]}
        />
      </div>
    )
  }

  return (
    <div
      className="fixed inset-0 bg-black/50 flex items-center justify-center z-50"
      onClick={() => setIsEditing(false)}
    >
      <div
        className="bg-white rounded-lg border shadow-lg p-6 max-w-2xl w-full mx-4 max-h-[90vh] overflow-y-auto"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="space-y-4">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-medium">Configure Admonition</h3>
            <Button variant="ghost" size="sm" onClick={() => setIsEditing(false)}>
              <Check className="h-4 w-4 mr-2" />
              Done
            </Button>
          </div>

          <div className="grid gap-4">
            <div className="grid gap-2">
              <label htmlFor="admonition-type" className="text-sm font-medium">
                Type
              </label>
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" className="w-full justify-between">
                    <div className="flex items-center gap-2">{type}</div>
                    <ChevronDown className="h-4 w-4 ml-2" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent className="w-full">
                  <DropdownMenuItem onClick={() => handleTypeChange("note")}>Note</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange("abstract")}>Abstract</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange("info")}>Info</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange("tip")}>Tip</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange("success")}>Success</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange("question")}>Question</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange("warning")}>Warning</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange("failure")}>Failure</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange("danger")}>Danger</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange("bug")}>Bug</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange("example")}>Example</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleTypeChange("quote")}>Quote</DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>

            <div className="grid gap-2">
              <label htmlFor="admonition-title" className="text-sm font-medium">
                Title (optional)
              </label>
              <Input
                id="admonition-title"
                value={title}
                onChange={(e) => handleTitleChange(e.target.value)}
                placeholder="Admonition title"
              />
            </div>

            <div className="grid gap-2">
              <label htmlFor="admonition-content" className="text-sm font-medium">
                Content
              </label>
              <Textarea
                id="admonition-content"
                value={content}
                onChange={(e) => handleContentChange(e.target.value)}
                placeholder="Admonition content"
                rows={4}
              />
            </div>

            <div className="mt-4">
              <h4 className="text-sm font-medium mb-2">Preview</h4>
              <UIAdmonition title={title} content={content} type={type} />
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}

export function $createAdmonitionNode(data: Partial<AdmonitionData> = {}): AdmonitionNode {
  return new AdmonitionNode({
    title: data.title || "",
    content: data.content || "",
    type: data.type || "note",
    isNew: true,
  })
}
