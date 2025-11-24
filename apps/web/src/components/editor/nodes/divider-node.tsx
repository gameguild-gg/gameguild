"use client"

import { useState, useEffect, useContext } from "react"
import { DecoratorNode, type SerializedLexicalNode } from "lexical"
import { $getNodeByKey } from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { Pencil } from "lucide-react"
import type { JSX } from "react/jsx-runtime"

import { EditorLoadingContext } from "../lexical-editor"
import { DividerEditor } from "@/components/editor/extras/divider"
import { ContentEditMenu } from "@/components/editor/extras/content-edit-menu"
import {
  getThicknessStyles,
  getSpacingStyles,
  getColorStyles,
  getStyleClasses,
  getPaletteColor,
} from "@/components/editor/extras/divider/divider-styles"

export type DividerStyle = "simple" | "double" | "dashed" | "dotted" | "gradient"
export type DividerThickness = "thin" | "medium" | "thick"
export type DividerSpacing = "xs" | "sm" | "md" | "lg" | "xl"
export type ColorPalette = "blue" | "green" | "orange" | "red" | "purple" | "custom"

export interface DividerData {
  style: DividerStyle
  thickness: DividerThickness
  spacing: DividerSpacing
  colorPalette: ColorPalette
  customColor?: string
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
      thickness: data.thickness || "thin",
      spacing: data.spacing || "md",
      colorPalette: data.colorPalette || "blue",
      customColor: data.customColor,
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

  const handleSave = (newData: DividerData) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (node instanceof DividerNode) {
        node.setData(newData)
      }
    })
    setIsEditing(false)
  }

  const handleCancel = () => {
    setIsEditing(false)
  }

  const renderDivider = () => {
    const spacingClass = getSpacingStyles(data.spacing)
    const thicknessClass = getThicknessStyles(data.thickness, data.style)
    const colorClass = getColorStyles(data.colorPalette, data.style)
    const styleClass = getStyleClasses(data.style)
    const paletteColor = getPaletteColor(data.colorPalette, data.customColor)

    const customStyle = data.colorPalette === "custom" && data.customColor ? {
      borderColor: data.customColor,
      backgroundColor: data.customColor,
    } : {}

    switch (data.style) {
      case "gradient":
        return (
          <div className={`${spacingClass} ${thicknessClass} ${colorClass}`} style={customStyle} aria-hidden="true" />
        )
      case "double":
        // Duas linhas perpendiculares (paralelas horizontais)
        const doubleThickness = data.thickness === "thin" ? "1px" : data.thickness === "medium" ? "2px" : "3px"
        const doubleGap = data.thickness === "thin" ? "2px" : data.thickness === "medium" ? "3px" : "4px"
        return (
          <div className={spacingClass}>
            <div 
              className="relative"
              style={{ 
                height: `calc(${doubleThickness} * 2 + ${doubleGap})`,
              }}
            >
              <div 
                className="absolute top-0 left-0 right-0"
                style={{ 
                  height: doubleThickness,
                  backgroundColor: paletteColor
                }}
              />
              <div 
                className="absolute bottom-0 left-0 right-0"
                style={{ 
                  height: doubleThickness,
                  backgroundColor: paletteColor
                }}
              />
            </div>
          </div>
        )
      default:
        return <hr className={`${spacingClass} ${thicknessClass} ${colorClass} ${styleClass}`} style={customStyle} />
    }
  }

  if (!isEditing) {
    return (
      <div className="my-4 relative">
        {renderDivider()}
        <ContentEditMenu
          options={[
            {
              id: "edit",
              icon: <Pencil className="h-4 w-4" />,
              label: "Edit Divider",
              action: () => setIsEditing(true),
            },
          ]}
        />
      </div>
    )
  }

  return <DividerEditor initialData={data} onSave={handleSave} onCancel={handleCancel} />
}

export function $createDividerNode(data: Partial<DividerData> = {}): DividerNode {
  return new DividerNode({
    style: data.style || "simple",
    thickness: data.thickness || "thin",
    spacing: data.spacing || "md",
    colorPalette: data.colorPalette || "blue",
    customColor: data.customColor,
    isNew: true,
  })
}
