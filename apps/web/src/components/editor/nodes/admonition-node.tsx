"use client"

import { useState, useEffect, useContext } from "react"
import { DecoratorNode, type SerializedLexicalNode } from "lexical"
import { $getNodeByKey } from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { Pencil } from "lucide-react"
import type { JSX } from "react/jsx-runtime"

import { Admonition as UIAdmonition, type AdmonitionType } from "@/components/editor/extras/admonition"
import { ContentEditMenu } from "@/components/editor/extras/content-edit-menu"
import { AdmonitionEditor } from "@/components/editor/lazy-client-components"
import { EditorLoadingContext } from "@/components/editor/engines/lexical/lexical-editor"

export interface AdmonitionData {
  title: string
  content: string
  type: AdmonitionType
  customBorderColor?: string
  customTextColor?: string
  design?: "default" | "compact" | "bordered" | "vertical-bar"
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
      customBorderColor: data.customBorderColor || "",
      customTextColor: data.customTextColor || "",
      design: data.design || "default",
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

  const handleSave = (newData: AdmonitionData) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (node instanceof AdmonitionNode) {
        node.setData(newData)
      }
    })
    setIsEditing(false)
  }

  const handleCancel = () => {
    setIsEditing(false)
  }

  if (!isEditing) {
    return (
      <div className="my-4 relative">
        <UIAdmonition 
          title={data.title} 
          content={data.content} 
          type={data.type} 
          customBorderColor={data.customBorderColor}
          customTextColor={data.customTextColor}
          design={data.design}
        />
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

  return <AdmonitionEditor initialData={data} onSave={handleSave} onCancel={handleCancel} />
}

export function $createAdmonitionNode(data: Partial<AdmonitionData> = {}): AdmonitionNode {
  return new AdmonitionNode({
    title: data.title || "",
    content: data.content || "",
    type: data.type || "note",
    isNew: true,
  })
}
