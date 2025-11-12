"use client"

import { useState, useEffect, useContext } from "react"
import { DecoratorNode, type SerializedLexicalNode } from "lexical"
import { $getNodeByKey } from "lexical"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { 
  Pencil, ExternalLink, Download, ArrowRight, Mail, Copy,
  Link, Link2, ExternalLinkIcon,
  Download as DownloadIcon, ArrowDownToLine, FileDown,
  Clipboard, ClipboardCopy, CopyCheck,
  Mail as MailIcon, AtSign, Send
} from "lucide-react"
import { Button } from "@/components/ui/button"
import type { JSX } from "react/jsx-runtime"
import { EditorLoadingContext } from "../lexical-editor"
import { ButtonEditor } from "@/components/editor/extras/button"
import { ContentEditMenu } from "@/components/editor/extras/content-edit-menu"

export type ButtonVariant = "solid" | "outline" | "soft" | "minimal"
export type ButtonSize = "sm" | "md" | "lg" | "xl" | "xxl"
export type ButtonActionType = "url" | "download" | "copy" | "email"
export type IconVariant = 0 | 1 | 2
export type IconPosition = "left" | "right" | "top" | "bottom"
export type IconSize = "sm" | "md" | "lg"
export type ColorPalette = "blue" | "green" | "orange" | "red" | "custom"

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
    // Tamanhos base para cada combinação de buttonSize e iconSize
    const iconSizeMap: Record<ButtonSize, Record<IconSize, string>> = {
      sm: { sm: "h-3 w-3", md: "h-3.5 w-3.5", lg: "h-4 w-4"},
      md: { sm: "h-4 w-4", md: "h-5 w-5", lg: "h-6 w-6"},
      lg: { sm: "h-5 w-5", md: "h-6 w-6", lg: "h-7 w-7"},
      xl: { sm: "h-6 w-6", md: "h-8 w-8", lg: "h-10 w-10"},
      xxl: { sm: "h-8 w-8", md: "h-10 w-10", lg: "h-12 w-12"},
    }
    
    const iconSizeClass = iconSizeMap[data.size][data.iconSize]

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

  const getColorStyles = () => {
    const palettes = {
      blue: {
        solid: "from-blue-600 to-indigo-600 shadow-blue-500/30 hover:shadow-blue-500/40 hover:from-blue-700 hover:to-indigo-700",
        outline: "border-blue-600 text-blue-600 dark:text-blue-400 dark:border-blue-400 hover:bg-blue-600 hover:text-white dark:hover:bg-blue-500 dark:hover:text-white",
        soft: "bg-blue-100 text-blue-900 dark:bg-blue-900/30 dark:text-blue-100 hover:bg-blue-200 dark:hover:bg-blue-800/40",
        minimal: "text-blue-600 dark:text-blue-400 hover:border-blue-600 dark:hover:border-blue-400",
      },
      green: {
        solid: "from-green-600 to-emerald-600 shadow-green-500/30 hover:shadow-green-500/40 hover:from-green-700 hover:to-emerald-700",
        outline: "border-green-600 text-green-600 dark:text-green-400 dark:border-green-400 hover:bg-green-600 hover:text-white dark:hover:bg-green-500 dark:hover:text-white",
        soft: "bg-green-100 text-green-900 dark:bg-green-900/30 dark:text-green-100 hover:bg-green-200 dark:hover:bg-green-800/40",
        minimal: "text-green-600 dark:text-green-400 hover:border-green-600 dark:hover:border-green-400",
      },
      orange: {
        solid: "from-orange-600 to-amber-600 shadow-orange-500/30 hover:shadow-orange-500/40 hover:from-orange-700 hover:to-amber-700",
        outline: "border-orange-600 text-orange-600 dark:text-orange-400 dark:border-orange-400 hover:bg-orange-600 hover:text-white dark:hover:bg-orange-500 dark:hover:text-white",
        soft: "bg-orange-100 text-orange-900 dark:bg-orange-900/30 dark:text-orange-100 hover:bg-orange-200 dark:hover:bg-orange-800/40",
        minimal: "text-orange-600 dark:text-orange-400 hover:border-orange-600 dark:hover:border-orange-400",
      },
      red: {
        solid: "from-red-600 to-rose-600 shadow-red-500/30 hover:shadow-red-500/40 hover:from-red-700 hover:to-rose-700",
        outline: "border-red-600 text-red-600 dark:text-red-400 dark:border-red-400 hover:bg-red-600 hover:text-white dark:hover:bg-red-500 dark:hover:text-white",
        soft: "bg-red-100 text-red-900 dark:bg-red-900/30 dark:text-red-100 hover:bg-red-200 dark:hover:bg-red-800/40",
        minimal: "text-red-600 dark:text-red-400 hover:border-red-600 dark:hover:border-red-400",
      },
    }

    return palettes[data.colorPalette === "custom" ? "blue" : data.colorPalette][data.variant]
  }

  const getButtonStyles = () => {
    const baseStyles = "inline-flex items-center justify-center rounded-md font-medium transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50"
    
    // Verifica se o ícone está visível e em posição vertical
    const isVerticalIcon = data.showIcon && (data.iconPosition === "top" || data.iconPosition === "bottom")
    
    const sizeStyles = {
      sm: isVerticalIcon ? "min-h-12 py-2 px-4 text-sm" : "h-9 px-4 text-sm",
      md: isVerticalIcon ? "min-h-16 py-3 px-6 text-base" : "h-12 px-6 text-base",
      lg: isVerticalIcon ? "min-h-20 py-4 px-8 text-lg" : "h-16 px-8 text-lg",
      xl: isVerticalIcon ? "min-h-28 py-6 px-12 text-2xl" : "h-24 px-12 text-2xl",
      xxl: isVerticalIcon ? "min-h-36 py-8 px-16 text-3xl" : "h-32 px-16 text-3xl",
    }

    // Largura da borda baseada no tamanho do botão
    const outlineBorderWidth = {
      sm: "border-2",
      md: "border-2",
      lg: "border-[3px]",
      xl: "border-4",
      xxl: "border-[5px]",
    }[data.size]

    const minimalBorderWidth = {
      sm: "border-b-2",
      md: "border-b-2",
      lg: "border-b-[3px]",
      xl: "border-b-4",
      xxl: "border-b-[5px]",
    }[data.size]

    const variantBaseStyles = {
      solid: "bg-gradient-to-r text-white shadow-lg hover:shadow-2xl hover:scale-105 active:scale-95 transition-all duration-200",
      outline: `${outlineBorderWidth} bg-transparent hover:shadow-md transition-all duration-200`,
      soft: "hover:shadow-sm transition-all duration-200",
      minimal: `bg-transparent ${minimalBorderWidth} border-transparent rounded-none px-2 transition-all duration-200`,
    }

    const layoutStyles = {
      top: "flex-col-reverse",
      bottom: "flex-col",
      left: "flex-row-reverse",
      right: "flex-row",
    }

    if (data.colorPalette === "custom" && data.customColors) {
      const { primary, secondary, text } = data.customColors
      const customStyle = `background: linear-gradient(to right, ${primary}, ${secondary}); color: ${text};`
      return `${baseStyles} ${sizeStyles[data.size]} ${variantBaseStyles[data.variant]} ${layoutStyles[data.iconPosition]}`
    }

    return `${baseStyles} ${sizeStyles[data.size]} ${variantBaseStyles[data.variant]} ${getColorStyles()} ${layoutStyles[data.iconPosition]}`
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
    const iconSpacingClass = {
      top: "mb-2",
      bottom: "mt-2",
      left: "mr-2",
      right: "ml-2",
    }[data.iconPosition]

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
    isNew: true,
  })
}
