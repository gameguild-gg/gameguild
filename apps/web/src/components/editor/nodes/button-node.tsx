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

export type ButtonVariant = "default" | "destructive" | "outline" | "secondary" | "ghost" | "link"
export type ButtonSize = "default" | "sm" | "lg" | "icon"
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
      variant: data.variant || "default",
      size: data.size || "default",
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
        <Button variant={data.variant} size={data.size} onClick={handleButtonAction}>
          {data.text}
          {data.showIcon && <span className="ml-2">{getActionIcon()}</span>}
        </Button>
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
    variant: data.variant || "default",
    size: data.size || "default",
    showIcon: data.showIcon ?? true,
    isNew: true,
  })
}
