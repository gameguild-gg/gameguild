"use client"

import { useState, useEffect, useContext } from "react"
import { DecoratorNode, type SerializedLexicalNode } from "lexical"
import { $getNodeByKey } from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { 
  Pencil, ExternalLink, Download, Mail, Copy,
  Link, Link2, ArrowDownToLine, FileDown, ClipboardCopy, CopyCheck, AtSign, Send
} from "lucide-react"
import type { JSX } from "react/jsx-runtime"
import { EditorLoadingContext } from "@/components/editor/engines/lexical/lexical-editor"
import { ButtonEditor } from "@/components/editor/extras/button"
import { ContentEditMenu } from "@/components/editor/extras/content-edit-menu"
import {
  BASE_BUTTON_STYLES,
  getSizeStyles,
  getVariantBaseStyles,
  getLayoutStyles,
  getIconSpacingClass,
  getIconSizeClass,
  getColorStyles,
  getFontFamilyClass,
  getFontSizeClass,
} from "@/components/editor/extras/button/button-styles"

export type ButtonVariant = "solid" | "outline" | "soft" | "minimal"
export type ButtonSize = "sm" | "md" | "lg" | "xl" | "xxl"
export type ButtonActionType = "url" | "download" | "copy" | "email"
export type IconVariant = 0 | 1 | 2
export type IconPosition = "left" | "right" | "top" | "bottom"
export type IconSize = "sm" | "md" | "lg"
export type ColorPalette = "blue" | "green" | "orange" | "red" | "custom"
export type FontFamily = "sans" | "display" | "roboto"
export type FontSize = "sm" | "md" | "lg"

export interface ButtonData {
  text: string
  url: string
  actionType: ButtonActionType
  variant: ButtonVariant
  size: ButtonSize
  showIcon: boolean
  iconVariant: IconVariant
  iconPosition: IconPosition
  iconSize: IconSize
  colorPalette: ColorPalette
  customColors?: {
    primary: string
    secondary: string
    text: string
    hoverPrimary: string
    hoverSecondary: string
    hoverText: string
  }
  fontFamily: FontFamily
  fontSize: FontSize
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
      iconVariant: data.iconVariant ?? 0,
      iconPosition: data.iconPosition || "right",
      iconSize: data.iconSize || "md",
      colorPalette: data.colorPalette || "blue",
      customColors: data.customColors,
      fontFamily: data.fontFamily || "sans",
      fontSize: data.fontSize || "md",
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
    const iconSizeClass = getIconSizeClass(data.size, data.iconSize)

    const iconsByType = {
      url: [
        <ExternalLink className={iconSizeClass} key="url-0" />,
        <Link2 className={iconSizeClass} key="url-1" />,
        <Link className={iconSizeClass} key="url-2" />,
      ],
      download: [
        <Download className={iconSizeClass} key="download-0" />,
        <ArrowDownToLine className={iconSizeClass} key="download-1" />,
        <FileDown className={iconSizeClass} key="download-2" />,
      ],
      copy: [
        <Copy className={iconSizeClass} key="copy-0" />,
        <ClipboardCopy className={iconSizeClass} key="copy-1" />,
        <CopyCheck className={iconSizeClass} key="copy-2" />,
      ],
      email: [
        <Mail className={iconSizeClass} key="email-0" />,
        <AtSign className={iconSizeClass} key="email-1" />,
        <Send className={iconSizeClass} key="email-2" />,
      ],
    }

    return iconsByType[data.actionType][data.iconVariant] || iconsByType[data.actionType][0]
  }

  const getButtonStyles = () => {
    const isVerticalIcon = data.showIcon && (data.iconPosition === "top" || data.iconPosition === "bottom")
    return `${BASE_BUTTON_STYLES} ${getSizeStyles(data.size, isVerticalIcon)} ${getVariantBaseStyles(data.variant, data.size)} ${getColorStyles(data.colorPalette, data.variant)} ${getLayoutStyles(data.iconPosition)} ${getFontFamilyClass(data.fontFamily)} ${getFontSizeClass(data.size, data.fontSize)}`
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
    const iconSpacingClass = getIconSpacingClass(data.iconPosition)

    const getCustomStyle = () => {
      if (data.colorPalette === "custom" && data.customColors) {
        const { primary, secondary, text, hoverPrimary, hoverSecondary, hoverText } = data.customColors
        if (data.variant === "solid") {
          return {
            background: `linear-gradient(to right, ${primary}, ${secondary})`,
            color: text,
            "--hover-bg": `linear-gradient(to right, ${hoverPrimary}, ${hoverSecondary})`,
            "--hover-text": hoverText,
          } as React.CSSProperties
        } else if (data.variant === "outline") {
          return {
            borderColor: primary,
            color: text,
            "--hover-bg": hoverPrimary,
            "--hover-border": hoverPrimary,
            "--hover-text": hoverText,
          } as React.CSSProperties
        } else if (data.variant === "soft") {
          return {
            backgroundColor: `${primary}20`,
            color: text,
            "--hover-bg": `${hoverPrimary}30`,
            "--hover-text": hoverText,
          } as React.CSSProperties
        } else if (data.variant === "minimal") {
          return {
            color: text,
            "--hover-border": hoverPrimary,
            "--hover-text": hoverText,
          } as React.CSSProperties
        }
      }
      return {}
    }

    const customClass = data.colorPalette === "custom" ? "custom-button-hover" : ""

    return (
      <div className="my-4 relative flex justify-center">
        <style>{`
          .custom-button-hover:hover {
            background: var(--hover-bg) !important;
            color: var(--hover-text) !important;
            border-color: var(--hover-border) !important;
          }
        `}</style>
        <button 
          className={`${getButtonStyles()} ${customClass}`}
          style={getCustomStyle()}
          onClick={handleButtonAction}
        >
          {data.text}
          {data.showIcon && <span className={iconSpacingClass}>{getActionIcon()}</span>}
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
    iconVariant: data.iconVariant ?? 0,
    iconPosition: data.iconPosition || "right",
    iconSize: data.iconSize || "md",
    colorPalette: data.colorPalette || "blue",
    customColors: data.customColors,
    fontFamily: data.fontFamily || "sans",
    fontSize: data.fontSize || "md",
    isNew: true,
  })
}
