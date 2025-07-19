"use client"

import { useState, useEffect, useContext } from "react"
import { DecoratorNode, type SerializedLexicalNode } from "lexical"
import { $getNodeByKey } from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { ChevronDown, Pencil, Check } from "lucide-react"
import type { JSX } from "react/jsx-runtime" // Import JSX to fix the undeclared variable error

import { Button } from "@/components/editor/ui/button"
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@/components/editor/ui/dropdown-menu"
import { EditorLoadingContext } from "../lexical-editor"

export type DividerStyle = "simple" | "double" | "dashed" | "dotted" | "gradient" | "icon"

export interface DividerData {
  style: DividerStyle
  isNew?: boolean // Flag to indicate if this is a newly created divider
}

export interface SerializedDividerNode extends SerializedLexicalNode {
  type: "divider"
  data: DividerData
  version: 1
}

export class DividerNode extends DecoratorNode<JSX.Element> {
  __data: DividerData

  static getType(): string {
    return "divider"
  }

  static clone(node: DividerNode): DividerNode {
    return new DividerNode(node.__data, node.__key)
  }

  constructor(data: DividerData, key?: string) {
    super(key)
    this.__data = {
      style: data.style || "simple",
      isNew: data.isNew,
    }
  }

  createDOM(): HTMLElement {
    return document.createElement("div")
  }

  updateDOM(): false {
    return false
  }

  setData(data: DividerData): void {
    const writable = this.getWritable()
    writable.__data = data
  }

  exportJSON(): SerializedDividerNode {
    return {
      type: "divider",
      data: this.__data,
      version: 1,
    }
  }

  static importJSON(serializedNode: SerializedDividerNode): DividerNode {
    return new DividerNode(serializedNode.data)
  }

  decorate(): JSX.Element {
    return <DividerComponent data={this.__data} nodeKey={this.__key} />
  }
}

interface DividerComponentProps {
  data: DividerData
  nodeKey: string
}

function DividerComponent({ data, nodeKey }: DividerComponentProps) {
  const [editor] = useLexicalComposerContext()
  const isLoading = useContext(EditorLoadingContext)
  const [style, setStyle] = useState<DividerStyle>(data.style || "simple")
  const [isEditing, setIsEditing] = useState((data.isNew || false) && !isLoading)

  useEffect(() => {
    if (data.isNew) {
      editor.update(() => {
        const node = $getNodeByKey(nodeKey)
        if (node instanceof DividerNode) {
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

  const updateDivider = (newData: Partial<DividerData>) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (node instanceof DividerNode) {
        node.setData({ ...data, ...newData })
      }
    })
  }

  const handleStyleChange = (newStyle: DividerStyle) => {
    setStyle(newStyle)
    updateDivider({ style: newStyle })
  }

  const renderDivider = () => {
    switch (style) {
      case "simple":
        return <hr className="my-6 border-t border-gray-300 dark:border-gray-700" />
      case "double":
        return <hr className="my-6 border-t-2 border-double border-gray-300 dark:border-gray-700" />
      case "dashed":
        return <hr className="my-6 border-t-2 border-dashed border-gray-300 dark:border-gray-700" />
      case "dotted":
        return <hr className="my-6 border-t-2 border-dotted border-gray-300 dark:border-gray-700" />
      case "gradient":
        return (
          <div className="my-6 h-px bg-gradient-to-r from-transparent via-primary to-transparent" aria-hidden="true" />
        )
      case "icon":
        return (
          <div className="my-6 flex items-center justify-center">
            <div className="flex-1 border-t border-gray-300 dark:border-gray-700" />
            <div className="mx-4 text-gray-500 dark:text-gray-400">●</div>
            <div className="flex-1 border-t border-gray-300 dark:border-gray-700" />
          </div>
        )
      default:
        return <hr className="my-6 border-t border-gray-300 dark:border-gray-700" />
    }
  }

  const getStyleName = (style: DividerStyle): string => {
    switch (style) {
      case "simple":
        return "Simple"
      case "double":
        return "Double"
      case "dashed":
        return "Dashed"
      case "dotted":
        return "Dotted"
      case "gradient":
        return "Gradient"
      case "icon":
        return "With icon"
      default:
        return "Simple"
    }
  }

  if (!isEditing) {
    return (
      <div className="relative group">
        {renderDivider()}
        <Button
          variant="ghost"
          size="icon"
          className="absolute top-1/2 right-2 -translate-y-1/2 opacity-0 group-hover:opacity-100 transition-opacity"
          onClick={() => setIsEditing(true)}
        >
          <Pencil className="h-4 w-4" />
        </Button>
      </div>
    )
  }

  return (
    <>
      {renderDivider()}
      <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
        <div className="bg-white dark:bg-gray-800 rounded-lg border p-4 max-w-md w-full mx-4">
          <div className="mb-4 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="outline" className="flex items-center gap-2">
                    {`Style: ${getStyleName(style)}`} <ChevronDown className="h-4 w-4" />
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent>
                  <DropdownMenuItem onClick={() => handleStyleChange("simple")}>Simple</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleStyleChange("double")}>Double</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleStyleChange("dashed")}>Dashed</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleStyleChange("dotted")}>Dotted</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleStyleChange("gradient")}>Gradient</DropdownMenuItem>
                  <DropdownMenuItem onClick={() => handleStyleChange("icon")}>With icon</DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </div>
            <Button variant="ghost" size="sm" onClick={() => setIsEditing(false)}>
              <Check className="h-4 w-4 mr-2" />
              Done
            </Button>
          </div>
          <div className="preview">{renderDivider()}</div>
        </div>
      </div>
    </>
  )
}

export function $createDividerNode(data: Partial<DividerData> = {}): DividerNode {
  return new DividerNode({
    style: data.style || "simple",
    isNew: true,
  })
}
