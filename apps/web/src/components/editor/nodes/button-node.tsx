"use client"

import { useState, useEffect, useContext } from "react"
import { DecoratorNode, type SerializedLexicalNode } from "lexical"
import { $getNodeByKey } from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { Pencil, ExternalLink, Download, ArrowRight, Mail, Copy } from "lucide-react"
import { Button } from "@/components/ui/button"
import type { JSX } from "react/jsx-runtime"
import { EditorLoadingContext } from "../lexical-editor"
import { ButtonEditor } from "@/components/editor/extras/button"
import { ContentEditMenu } from "@/components/editor/extras/content-edit-menu"

export type ButtonVariant = "solid" | "outline" | "soft" | "minimal"
export type ButtonSize = "sm" | "md" | "lg" | "xl"
export type ButtonActionType = "url" | "download" | "copy" | "email"

export interface ButtonData {
  text: string
  url: string
  actionType: ButtonActionType
  variant: ButtonVariant
  size: ButtonSize
  showIcon: boolean
  isNew?: boolean
}

export interface SerializedButtonNode extends SerializedLexicalNode {
  type: "button"
  data: ButtonData
  version: 1
}

export class ButtonNode extends DecoratorNode<JSX.Element> {
  __data: ButtonData

  static getType(): string {
    return "button"
  }

  static clone(node: ButtonNode): ButtonNode {
    return new ButtonNode(node.__data, node.__key)
  }

  constructor(data: ButtonData, key?: string) {
    super(key)
    this.__data = {
      text: data.text || "Click me",
      url: data.url || "",
      actionType: data.actionType || "url",
      variant: data.variant || "solid",
      size: data.size || "md",
      showIcon: data.showIcon ?? true,
      isNew: data.isNew,
    }
  }

  createDOM(): HTMLElement {
    return document.createElement("div")
  }

  updateDOM(): false {
    return false
  }

  setData(data: ButtonData): void {
    const writable = this.getWritable()
    writable.__data = data
  }

  exportJSON(): SerializedButtonNode {
    return {
      type: "button",
      data: this.__data,
      version: 1,
    }
  }

  static importJSON(serializedNode: SerializedButtonNode): ButtonNode {
    return new ButtonNode(serializedNode.data)
  }

  decorate(): JSX.Element {
    return <ButtonComponent data={this.__data} nodeKey={this.__key} />
  }
}

interface ButtonComponentProps {
  data: ButtonData
  nodeKey: string
}

function ButtonComponent({ data, nodeKey }: ButtonComponentProps) {
  const [editor] = useLexicalComposerContext()
  const isLoading = useContext(EditorLoadingContext)
  const [isEditing, setIsEditing] = useState((data.isNew || false) && !isLoading)

  useEffect(() => {
    if (data.isNew) {
      editor.update(() => {
        const node = $getNodeByKey(nodeKey)
        if (node instanceof ButtonNode) {
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

  const handleSave = (newData: ButtonData) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (node instanceof ButtonNode) {
        node.setData(newData)
      }
    })
    setIsEditing(false)
  }

  const handleCancel = () => {
    setIsEditing(false)
  }

  const getActionIcon = () => {
    switch (data.actionType) {
      case "url":
        return <ExternalLink className="h-4 w-4" />
      case "download":
        return <Download className="h-4 w-4" />
      case "copy":
        return <Copy className="h-4 w-4" />
      case "email":
        return <Mail className="h-4 w-4" />
      default:
        return <ArrowRight className="h-4 w-4" />
    }
  }

  const getButtonStyles = () => {
    const baseStyles = "inline-flex items-center justify-center rounded-md font-medium transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50"
    
    const sizeStyles = {
      sm: "h-9 px-4 text-sm",
      md: "h-12 px-6 text-base",
      lg: "h-16 px-8 text-lg",
      xl: "h-24 px-12 text-2xl",
    }

    const variantStyles = {
      solid: "bg-gradient-to-r from-blue-600 to-purple-600 text-white shadow-lg shadow-blue-500/30 hover:shadow-xl hover:shadow-blue-500/40 hover:from-blue-700 hover:to-purple-700 active:scale-95",
      outline: "border-2 border-blue-600 text-blue-600 dark:text-blue-400 dark:border-blue-400 bg-transparent hover:bg-blue-600 hover:text-white dark:hover:bg-blue-500 hover:shadow-md",
      soft: "bg-blue-100 text-blue-900 dark:bg-blue-900/30 dark:text-blue-100 hover:bg-blue-200 dark:hover:bg-blue-800/40 hover:shadow-sm",
      minimal: "text-blue-600 dark:text-blue-400 bg-transparent border-b-2 border-transparent hover:border-blue-600 dark:hover:border-blue-400 rounded-none px-2",
    }

    return `${baseStyles} ${sizeStyles[data.size]} ${variantStyles[data.variant]}`
  }

  const handleButtonAction = () => {
    switch (data.actionType) {
      case "url":
        window.open(data.url, "_blank")
        break
      case "download":
        const link = document.createElement("a")
        link.href = data.url
        link.download = ""
        document.body.appendChild(link)
        link.click()
        document.body.removeChild(link)
        break
      case "copy":
        navigator.clipboard.writeText(data.url)
        break
      case "email":
        window.location.href = `mailto:${data.url}`
        break
    }
  }

  if (!isEditing) {
    return (
      <div className="my-4 relative flex justify-center">
        <button className={getButtonStyles()} onClick={handleButtonAction}>
          {data.text}
          {data.showIcon && <span className="ml-2">{getActionIcon()}</span>}
        </button>
        <ContentEditMenu
          options={[
            {
              id: "edit",
              icon: <Pencil className="h-4 w-4" />,
              label: "Edit Button",
              action: () => setIsEditing(true),
            },
          ]}
        />
      </div>
    )
  }

  return <ButtonEditor initialData={data} onSave={handleSave} onCancel={handleCancel} />
}

export function $createButtonNode(data: Partial<ButtonData> = {}): ButtonNode {
  return new ButtonNode({
    text: data.text || "Click me",
    url: data.url || "",
    actionType: data.actionType || "url",
    variant: data.variant || "solid",
    size: data.size || "md",
    showIcon: data.showIcon ?? true,
    isNew: true,
  })
}
