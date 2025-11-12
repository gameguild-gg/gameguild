"use client"

import { useState, useEffect } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { 
  X, Save, MousePointerClick, Eye, ExternalLink, Download, Copy, Mail, ArrowRight,
  Link, Link2, ExternalLinkIcon,
  ArrowDownToLine, FileDown,
  Clipboard, ClipboardCopy, CopyCheck,
  AtSign, Send,
  MoveUp, MoveDown, MoveLeft, MoveRight
} from "lucide-react"
import type { ButtonData, ButtonActionType, ButtonVariant, ButtonSize, IconVariant, IconPosition, IconSize, ColorPalette } from "@/components/editor/nodes/button-node"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"

interface ButtonEditorProps {
  initialData?: ButtonData
  onSave: (data: ButtonData) => void
  onCancel: () => void
}

const actionTypes: { value: ButtonActionType; label: string; icon: React.ReactNode }[] = [
  { value: "url", label: "Open Page", icon: <ExternalLink className="h-4 w-4" /> },
  { value: "download", label: "Download File", icon: <Download className="h-4 w-4" /> },
  { value: "copy", label: "Copy Text", icon: <Copy className="h-4 w-4" /> },
  { value: "email", label: "Send Email", icon: <Mail className="h-4 w-4" /> },
]

const variants: { value: ButtonVariant; label: string; description: string }[] = [
  { value: "solid", label: "Solid", description: "Bold button with vibrant gradient background" },
  { value: "outline", label: "Outline", description: "Elegant border with hover fill effect" },
  { value: "soft", label: "Soft", description: "Subtle background with smooth transitions" },
  { value: "minimal", label: "Minimal", description: "Clean underline style, text-focused" },
]

const sizes: { value: ButtonSize; label: string }[] = [
  { value: "sm", label: "Small" },
  { value: "md", label: "Medium" },
  { value: "lg", label: "Large" },
  { value: "xl", label: "Extra Large" },
  { value: "xxl", label: "Ultra Large" },
]

const iconSizes: { value: IconSize; label: string }[] = [
  { value: "sm", label: "Small" },
  { value: "md", label: "Medium" },
  { value: "lg", label: "Large" },
]

const iconPositions: { value: IconPosition; label: string; icon: React.ReactNode }[] = [
  { value: "left", label: "Left", icon: <MoveLeft className="h-4 w-4" /> },
  { value: "right", label: "Right", icon: <MoveRight className="h-4 w-4" /> },
  { value: "top", label: "Top", icon: <MoveUp className="h-4 w-4" /> },
  { value: "bottom", label: "Bottom", icon: <MoveDown className="h-4 w-4" /> },
]

const getIconVariantsByType = (type: ButtonActionType) => {
  const variants = {
    url: [
      { value: 0 as IconVariant, icon: <ExternalLink className="h-5 w-5" />, label: "External Link" },
      { value: 1 as IconVariant, icon: <Link2 className="h-5 w-5" />, label: "Chain Link" },
      { value: 2 as IconVariant, icon: <Link className="h-5 w-5" />, label: "Simple Link" },
    ],
    download: [
      { value: 0 as IconVariant, icon: <Download className="h-5 w-5" />, label: "Download Arrow" },
      { value: 1 as IconVariant, icon: <ArrowDownToLine className="h-5 w-5" />, label: "Arrow to Line" },
      { value: 2 as IconVariant, icon: <FileDown className="h-5 w-5" />, label: "File Download" },
    ],
    copy: [
      { value: 0 as IconVariant, icon: <Copy className="h-5 w-5" />, label: "Copy" },
      { value: 1 as IconVariant, icon: <ClipboardCopy className="h-5 w-5" />, label: "Clipboard" },
      { value: 2 as IconVariant, icon: <CopyCheck className="h-5 w-5" />, label: "Copy Check" },
    ],
    email: [
      { value: 0 as IconVariant, icon: <Mail className="h-5 w-5" />, label: "Mail" },
      { value: 1 as IconVariant, icon: <AtSign className="h-5 w-5" />, label: "At Sign" },
      { value: 2 as IconVariant, icon: <Send className="h-5 w-5" />, label: "Send" },
    ],
  }
  return variants[type] || variants.url
}

const colorPalettes: { value: ColorPalette; label: string; description: string; colors: { primary: string; secondary: string } }[] = [
  { 
    value: "blue", 
    label: "Blue", 
    description: "Trust, professionalism, stability",
    colors: { primary: "#2563eb", secondary: "#6366f1" }
  },
  { 
    value: "green", 
    label: "Green", 
    description: "Success, growth, eco-friendly",
    colors: { primary: "#16a34a", secondary: "#10b981" }
  },
  { 
    value: "orange", 
    label: "Orange", 
    description: "Energy, creativity, enthusiasm",
    colors: { primary: "#ea580c", secondary: "#f59e0b" }
  },
  { 
    value: "red", 
    label: "Red", 
    description: "Urgency, passion, importance",
    colors: { primary: "#dc2626", secondary: "#f43f5e" }
  },
  { 
    value: "custom", 
    label: "Custom", 
    description: "Define your own colors",
    colors: { primary: "#3b82f6", secondary: "#8b5cf6" }
  },
]

export function ButtonEditor({ initialData, onSave, onCancel }: ButtonEditorProps) {
  const [data, setData] = useState<ButtonData>(
    initialData || {
      text: "Click me",
      url: "",
      actionType: "url",
      variant: "solid",
      size: "md",
      showIcon: true,
      iconVariant: 0,
      iconPosition: "right",
      iconSize: "md",
      colorPalette: "blue",
      customColors: {
        primary: "#3b82f6",
        secondary: "#8b5cf6",
        text: "#ffffff",
        hoverPrimary: "#1d4ed8",
        hoverSecondary: "#7c3aed",
        hoverText: "#ffffff",
      },
    }
  )

  // Block body scroll and pointer events when modal is open
  useEffect(() => {
    const originalOverflow = document.body.style.overflow
    const originalPointerEvents = document.body.style.pointerEvents

    document.body.style.overflow = "hidden"
    document.body.style.pointerEvents = "none"

    return () => {
      document.body.style.overflow = originalOverflow
      document.body.style.pointerEvents = originalPointerEvents
    }
  }, [])

  const handleSave = () => {
    // Restore body styles before closing
    document.body.style.overflow = ""
    document.body.style.pointerEvents = ""

    onSave(data)
  }

  const handleCancel = () => {
    // Restore body styles before closing
    document.body.style.overflow = ""
    document.body.style.pointerEvents = ""

    onCancel()
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
    const baseStyles = "inline-flex items-center justify-center rounded-md font-medium transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 cursor-pointer"
    
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

    return `${baseStyles} ${sizeStyles[data.size]} ${variantBaseStyles[data.variant]} ${getColorStyles()} ${layoutStyles[data.iconPosition]}`
  }

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

  const getUrlPlaceholder = () => {
    switch (data.actionType) {
      case "url":
        return "https://example.com"
      case "download":
        return "https://example.com/file.pdf"
      case "copy":
        return "Text to be copied"
      case "email":
        return "email@example.com"
      default:
        return ""
    }
  }

  const getUrlLabel = () => {
    switch (data.actionType) {
      case "url":
        return "Page URL"
      case "download":
        return "File URL"
      case "copy":
        return "Text to Copy"
      case "email":
        return "Email Address"
      default:
        return "URL"
    }
  }

  return (
    <div
      className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4"
      style={{ pointerEvents: "auto" }}
      onClick={handleCancel}
      onMouseDown={(e) => e.stopPropagation()}
      onKeyDown={(e) => {
        if (e.key === "Escape") {
          handleCancel()
        }
        e.stopPropagation()
      }}
    >
      <div
        className="bg-white dark:bg-gray-900 border dark:border-gray-700 rounded-lg shadow-2xl w-full max-w-6xl h-[85vh] flex flex-col"
        style={{ pointerEvents: "auto" }}
        onClick={(e) => e.stopPropagation()}
        onKeyDown={(e) => e.stopPropagation()}
        onKeyUp={(e) => e.stopPropagation()}
        onKeyPress={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
          <div className="flex items-center gap-2">
            <MousePointerClick className="h-5 w-5 text-blue-600 dark:text-blue-400" />
            <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">Button Editor</h2>

            {/* Current settings display */}
            <div className="ml-4 flex items-center gap-3 pl-4 border-l border-gray-300 dark:border-gray-600">
              <div className="flex items-center gap-2 text-sm">
                <span className="text-gray-600 dark:text-gray-400">Action:</span>
                <div className="flex items-center gap-1 font-medium text-gray-800 dark:text-gray-200 capitalize bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded">
                  {actionTypes.find((a) => a.value === data.actionType)?.icon}
                  <span>{actionTypes.find((a) => a.value === data.actionType)?.label}</span>
                </div>
              </div>
              <div className="flex items-center gap-2 text-sm">
                <span className="text-gray-600 dark:text-gray-400">Style:</span>
                <span className="font-medium text-gray-800 dark:text-gray-200 capitalize bg-gray-100 dark:bg-gray-800 px-2 py-1 rounded">
                  {variants.find((v) => v.value === data.variant)?.label}
                </span>
              </div>
            </div>
          </div>
          <Button
            variant="ghost"
            size="sm"
            onClick={handleCancel}
            className="hover:bg-gray-100 dark:hover:bg-gray-800"
          >
            <X className="h-4 w-4" />
          </Button>
        </div>

        {/* Settings Bar */}
        <div className="flex items-center gap-4 p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
          <div className="flex items-center gap-2">
            <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
              Action Type:
            </Label>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="outline"
                  className="gap-2 bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700"
                >
                  {actionTypes.find((a) => a.value === data.actionType)?.icon}
                  <span>{actionTypes.find((a) => a.value === data.actionType)?.label}</span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent className="bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-700">
                {actionTypes.map((action) => (
                  <DropdownMenuItem
                    key={action.value}
                    onClick={() => setData((prev) => ({ ...prev, actionType: action.value }))}
                    className="cursor-pointer dark:hover:bg-gray-700 dark:focus:bg-gray-700"
                  >
                    <div className="flex items-center gap-2">
                      {action.icon}
                      <span>{action.label}</span>
                    </div>
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>

          <div className="flex items-center gap-2">
            <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
              Style:
            </Label>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="outline"
                  className="gap-2 bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700"
                >
                  <span className="capitalize">{variants.find((v) => v.value === data.variant)?.label}</span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent className="w-[400px] bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-700 p-3">
                <div className="space-y-2">
                  {variants.map((variant) => {
                    const miniVariantStyles = {
                      solid: "bg-gradient-to-r from-blue-600 to-purple-600 text-white shadow-md",
                      outline: "border-2 border-blue-600 text-blue-600 dark:text-blue-400 dark:border-blue-400 bg-transparent",
                      soft: "bg-blue-100 text-blue-900 dark:bg-blue-900/30 dark:text-blue-100",
                      minimal: "text-blue-600 dark:text-blue-400 bg-transparent border-b-2 border-blue-600 dark:border-blue-400 rounded-none",
                    }
                    
                    return (
                      <button
                        key={variant.value}
                        onClick={() => setData((prev) => ({ ...prev, variant: variant.value }))}
                        className={`w-full text-left p-3 rounded transition-all hover:bg-gray-100 dark:hover:bg-gray-700 ${
                          data.variant === variant.value
                            ? "bg-blue-50 dark:bg-blue-950/30 ring-2 ring-blue-500 dark:ring-blue-400"
                            : "border border-gray-200 dark:border-gray-600"
                        }`}
                      >
                        <div className="flex items-center justify-between gap-3">
                          <div className="flex-1">
                            <div className="font-medium text-sm text-gray-900 dark:text-gray-100 mb-1">
                              {variant.label}
                            </div>
                            <div className="text-xs text-gray-500 dark:text-gray-400">
                              {variant.description}
                            </div>
                          </div>
                          <div className="shrink-0">
                            <div 
                              className={`px-4 py-2 text-xs font-medium rounded inline-flex items-center ${miniVariantStyles[variant.value]}`}
                            >
                              Preview
                            </div>
                          </div>
                        </div>
                      </button>
                    )
                  })}
                </div>
              </DropdownMenuContent>
            </DropdownMenu>
          </div>

          <div className="flex items-center gap-2">
            <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
              Size:
            </Label>
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button
                  variant="outline"
                  className="gap-2 bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700"
                >
                  <span>{sizes.find((s) => s.value === data.size)?.label}</span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent className="bg-white dark:bg-gray-800 border-gray-200 dark:border-gray-700">
                {sizes.map((size) => (
                  <DropdownMenuItem
                    key={size.value}
                    onClick={() => setData((prev) => ({ ...prev, size: size.value }))}
                    className="cursor-pointer dark:hover:bg-gray-700 dark:focus:bg-gray-700"
                  >
                    {size.label}
                  </DropdownMenuItem>
                ))}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>

        {/* Editor Content */}
        <div className="flex-1 flex min-h-0">
          {/* Left Panel - Editor */}
          <div className="w-1/2 border-r border-gray-200 dark:border-gray-800 flex flex-col bg-white dark:bg-gray-900">
            <div className="p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
              <h3 className="font-medium flex items-center gap-2 text-gray-800 dark:text-gray-200">
                <MousePointerClick className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                Button Settings
              </h3>
            </div>

            <div className="flex-1 p-6 overflow-auto bg-white dark:bg-gray-950">
              <div className="space-y-6">
                {/* Button Text */}
                <div className="space-y-2">
                  <Label htmlFor="text" className="text-sm font-medium text-gray-700 dark:text-gray-300">
                    Button Text *
                  </Label>
                  <Input
                    id="text"
                    value={data.text}
                    onChange={(e) => setData((prev) => ({ ...prev, text: e.target.value }))}
                    placeholder="Click me"
                    className="bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 focus:border-blue-500 dark:focus:border-blue-400"
                  />
                </div>

                {/* URL/Target */}
                <div className="space-y-2">
                  <Label htmlFor="url" className="text-sm font-medium text-gray-700 dark:text-gray-300">
                    {getUrlLabel()}
                  </Label>
                  <Input
                    id="url"
                    value={data.url}
                    onChange={(e) => setData((prev) => ({ ...prev, url: e.target.value }))}
                    placeholder={getUrlPlaceholder()}
                    className="bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 focus:border-blue-500 dark:focus:border-blue-400"
                  />
                </div>

                {/* Show Icon Toggle */}
                <div className="flex items-center space-x-3 p-4 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700">
                  <Switch
                    id="show-icon"
                    checked={data.showIcon}
                    onCheckedChange={(checked) => setData((prev) => ({ ...prev, showIcon: checked }))}
                  />
                  <Label htmlFor="show-icon" className="text-sm font-medium text-gray-700 dark:text-gray-300 cursor-pointer">
                    Show action icon
                  </Label>
                </div>

                {/* Icon Settings */}
                {data.showIcon && (
                  <div className="space-y-4 p-4 bg-blue-50 dark:bg-blue-950/20 rounded-lg border border-blue-200 dark:border-blue-800">
                    <h4 className="text-sm font-semibold text-blue-900 dark:text-blue-100">Icon Settings</h4>
                    
                    {/* Icon Variant */}
                    <div className="space-y-2">
                      <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                        Icon Style
                      </Label>
                      <div className="grid grid-cols-3 gap-2">
                        {getIconVariantsByType(data.actionType).map((icon) => (
                          <button
                            key={icon.value}
                            onClick={() => setData((prev) => ({ ...prev, iconVariant: icon.value }))}
                            className={`p-3 rounded-lg border-2 transition-all flex flex-col items-center gap-2 ${
                              data.iconVariant === icon.value
                                ? "border-blue-500 bg-blue-100 dark:bg-blue-900/30"
                                : "border-gray-200 dark:border-gray-700 hover:border-blue-300 dark:hover:border-blue-700"
                            }`}
                          >
                            <div className="text-gray-700 dark:text-gray-300">{icon.icon}</div>
                            <span className="text-xs text-gray-600 dark:text-gray-400">{icon.label}</span>
                          </button>
                        ))}
                      </div>
                    </div>

                    {/* Icon Size */}
                    <div className="space-y-2">
                      <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                        Icon Size (scales with button size)
                      </Label>
                      <div className="grid grid-cols-3 gap-2">
                        {iconSizes.map((size) => (
                          <button
                            key={size.value}
                            onClick={() => setData((prev) => ({ ...prev, iconSize: size.value }))}
                            className={`p-2 rounded-lg border-2 transition-all text-sm ${
                              data.iconSize === size.value
                                ? "border-blue-500 bg-blue-100 dark:bg-blue-900/30 font-medium"
                                : "border-gray-200 dark:border-gray-700 hover:border-blue-300 dark:hover:border-blue-700"
                            }`}
                          >
                            {size.label}
                          </button>
                        ))}
                      </div>
                    </div>

                    {/* Icon Position */}
                    <div className="space-y-2">
                      <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                        Icon Position
                      </Label>
                      <div className="grid grid-cols-2 gap-2">
                        {iconPositions.map((position) => (
                          <button
                            key={position.value}
                            onClick={() => setData((prev) => ({ ...prev, iconPosition: position.value }))}
                            className={`p-3 rounded-lg border-2 transition-all flex items-center gap-2 justify-center ${
                              data.iconPosition === position.value
                                ? "border-blue-500 bg-blue-100 dark:bg-blue-900/30 font-medium"
                                : "border-gray-200 dark:border-gray-700 hover:border-blue-300 dark:hover:border-blue-700"
                            }`}
                          >
                            {position.icon}
                            <span className="text-sm">{position.label}</span>
                          </button>
                        ))}
                      </div>
                    </div>
                  </div>
                )}

                {/* Color Palette */}
                <div className="space-y-4 p-4 bg-purple-50 dark:bg-purple-950/20 rounded-lg border border-purple-200 dark:border-purple-800">
                  <h4 className="text-sm font-semibold text-purple-900 dark:text-purple-100">Color Palette</h4>
                  
                  <div className="space-y-2">
                    <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                      Choose Color Theme
                    </Label>
                    <div className="grid grid-cols-2 gap-3">
                      {colorPalettes.map((palette) => (
                        <button
                          key={palette.value}
                          onClick={() => setData((prev) => ({ 
                            ...prev, 
                            colorPalette: palette.value,
                            customColors: palette.value === "custom" ? {
                              primary: prev.customColors?.primary || "#3b82f6",
                              secondary: prev.customColors?.secondary || "#8b5cf6",
                              text: prev.customColors?.text || "#ffffff",
                              hoverPrimary: prev.customColors?.hoverPrimary || "#1d4ed8",
                              hoverSecondary: prev.customColors?.hoverSecondary || "#7c3aed",
                              hoverText: prev.customColors?.hoverText || "#ffffff",
                            } : prev.customColors
                          }))}
                          className={`p-3 rounded-lg border-2 transition-all text-left ${
                            data.colorPalette === palette.value
                              ? "border-purple-500 bg-purple-100 dark:bg-purple-900/30"
                              : "border-gray-200 dark:border-gray-700 hover:border-purple-300 dark:hover:border-purple-700"
                          }`}
                        >
                          <div className="flex items-center gap-2 mb-1">
                            <div className="flex gap-1">
                              <div 
                                className="w-4 h-4 rounded"
                                style={{ backgroundColor: palette.colors.primary }}
                              />
                              <div 
                                className="w-4 h-4 rounded"
                                style={{ backgroundColor: palette.colors.secondary }}
                              />
                            </div>
                            <span className="font-medium text-sm text-gray-900 dark:text-gray-100">
                              {palette.label}
                            </span>
                          </div>
                          <p className="text-xs text-gray-600 dark:text-gray-400">
                            {palette.description}
                          </p>
                        </button>
                      ))}
                    </div>
                  </div>

                  {/* Custom Colors */}
                  {data.colorPalette === "custom" && (
                    <div className="space-y-4 pt-3 border-t border-purple-200 dark:border-purple-800">
                      <div className="space-y-3">
                        <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                          Normal State Colors
                        </Label>
                        <div className="grid grid-cols-3 gap-3">
                          <div className="space-y-1">
                            <Label htmlFor="primary-color" className="text-xs text-gray-600 dark:text-gray-400">
                              Primary
                            </Label>
                            <div className="flex items-center gap-2">
                              <Input
                                id="primary-color"
                                type="color"
                                value={data.customColors?.primary || "#3b82f6"}
                                onChange={(e) => setData((prev) => ({
                                  ...prev,
                                  customColors: {
                                    ...prev.customColors!,
                                    primary: e.target.value,
                                  }
                                }))}
                                className="h-10 w-full cursor-pointer"
                              />
                            </div>
                          </div>
                          <div className="space-y-1">
                            <Label htmlFor="secondary-color" className="text-xs text-gray-600 dark:text-gray-400">
                              Secondary
                            </Label>
                            <div className="flex items-center gap-2">
                              <Input
                                id="secondary-color"
                                type="color"
                                value={data.customColors?.secondary || "#8b5cf6"}
                                onChange={(e) => setData((prev) => ({
                                  ...prev,
                                  customColors: {
                                    ...prev.customColors!,
                                    secondary: e.target.value,
                                  }
                                }))}
                                className="h-10 w-full cursor-pointer"
                              />
                            </div>
                          </div>
                          <div className="space-y-1">
                            <Label htmlFor="text-color" className="text-xs text-gray-600 dark:text-gray-400">
                              Text
                            </Label>
                            <div className="flex items-center gap-2">
                              <Input
                                id="text-color"
                                type="color"
                                value={data.customColors?.text || "#ffffff"}
                                onChange={(e) => setData((prev) => ({
                                  ...prev,
                                  customColors: {
                                    ...prev.customColors!,
                                    text: e.target.value,
                                  }
                                }))}
                                className="h-10 w-full cursor-pointer"
                              />
                            </div>
                          </div>
                        </div>
                      </div>

                      <div className="space-y-3">
                        <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
                          Hover State Colors
                        </Label>
                        <div className="grid grid-cols-3 gap-3">
                          <div className="space-y-1">
                            <Label htmlFor="hover-primary-color" className="text-xs text-gray-600 dark:text-gray-400">
                              Primary
                            </Label>
                            <div className="flex items-center gap-2">
                              <Input
                                id="hover-primary-color"
                                type="color"
                                value={data.customColors?.hoverPrimary || "#1d4ed8"}
                                onChange={(e) => setData((prev) => ({
                                  ...prev,
                                  customColors: {
                                    ...prev.customColors!,
                                    hoverPrimary: e.target.value,
                                  }
                                }))}
                                className="h-10 w-full cursor-pointer"
                              />
                            </div>
                          </div>
                          <div className="space-y-1">
                            <Label htmlFor="hover-secondary-color" className="text-xs text-gray-600 dark:text-gray-400">
                              Secondary
                            </Label>
                            <div className="flex items-center gap-2">
                              <Input
                                id="hover-secondary-color"
                                type="color"
                                value={data.customColors?.hoverSecondary || "#7c3aed"}
                                onChange={(e) => setData((prev) => ({
                                  ...prev,
                                  customColors: {
                                    ...prev.customColors!,
                                    hoverSecondary: e.target.value,
                                  }
                                }))}
                                className="h-10 w-full cursor-pointer"
                              />
                            </div>
                          </div>
                          <div className="space-y-1">
                            <Label htmlFor="hover-text-color" className="text-xs text-gray-600 dark:text-gray-400">
                              Text
                            </Label>
                            <div className="flex items-center gap-2">
                              <Input
                                id="hover-text-color"
                                type="color"
                                value={data.customColors?.hoverText || "#ffffff"}
                                onChange={(e) => setData((prev) => ({
                                  ...prev,
                                  customColors: {
                                    ...prev.customColors!,
                                    hoverText: e.target.value,
                                  }
                                }))}
                                className="h-10 w-full cursor-pointer"
                              />
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  )}
                </div>

                {/* Info Section */}
                <div className="p-4 bg-blue-50 dark:bg-blue-950/30 border border-blue-200 dark:border-blue-800 rounded-lg">
                  <h4 className="text-sm font-medium text-blue-900 dark:text-blue-100 mb-2">
                    Action Behavior
                  </h4>
                  <p className="text-xs text-blue-700 dark:text-blue-300">
                    {data.actionType === "url" && "Opens the specified URL in a new tab"}
                    {data.actionType === "download" && "Downloads the file from the specified URL"}
                    {data.actionType === "copy" && "Copies the specified text to clipboard"}
                    {data.actionType === "email" && "Opens default email client with the specified address"}
                  </p>
                </div>
              </div>
            </div>
          </div>

          {/* Right Panel - Preview */}
          <div className="w-1/2 flex flex-col bg-white dark:bg-gray-900">
            <div className="p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
              <h3 className="font-medium flex items-center gap-2 text-gray-800 dark:text-gray-200">
                <Eye className="h-4 w-4 text-blue-600 dark:text-blue-400" />
                Live Preview
              </h3>
            </div>
            <div className="flex-1 p-8 overflow-auto bg-white dark:bg-gray-950 flex items-center justify-center">
              <style>{`
                .custom-button-hover:hover {
                  background: var(--hover-bg) !important;
                  color: var(--hover-text) !important;
                  border-color: var(--hover-border) !important;
                }
              `}</style>
              <div className="space-y-4 w-full max-w-md">
                <div className="text-center text-sm text-gray-600 dark:text-gray-400 mb-8">
                  Button appearance in your content
                </div>
                <div className="flex justify-center">
                  <button 
                    className={`${getButtonStyles()} ${data.colorPalette === "custom" ? "custom-button-hover" : ""}`}
                    style={getCustomStyle()}
                  >
                    {data.text}
                    {data.showIcon && (
                      <span className={{
                        top: "mb-2",
                        bottom: "mt-2",
                        left: "mr-2",
                        right: "ml-2",
                      }[data.iconPosition]}>
                        {getActionIcon()}
                      </span>
                    )}
                  </button>
                </div>
                
                {/* Visual indicators */}
                <div className="mt-8 p-4 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700">
                  <div className="space-y-2 text-xs">
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Text:</span>
                      <span className="font-medium text-gray-900 dark:text-gray-100">{data.text || "(empty)"}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Action:</span>
                      <span className="font-medium text-gray-900 dark:text-gray-100">{actionTypes.find((a) => a.value === data.actionType)?.label}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Style:</span>
                      <span className="font-medium text-gray-900 dark:text-gray-100">{variants.find((v) => v.value === data.variant)?.label}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Size:</span>
                      <span className="font-medium text-gray-900 dark:text-gray-100">{sizes.find((s) => s.value === data.size)?.label}</span>
                    </div>
                    <div className="flex justify-between">
                      <span className="text-gray-600 dark:text-gray-400">Icon:</span>
                      <span className="font-medium text-gray-900 dark:text-gray-100">
                        {data.showIcon ? `${iconPositions.find((p) => p.value === data.iconPosition)?.label} (${iconSizes.find((s) => s.value === data.iconSize)?.label})` : "Hidden"}
                      </span>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Footer */}
        <div className="p-4 border-t border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
          <div className="flex items-center justify-end gap-2">
            <Button
              variant="outline"
              onClick={handleCancel}
              className="border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent"
            >
              Cancel
            </Button>
            <Button
              onClick={handleSave}
              className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600"
            >
              <Save className="h-4 w-4" />
              Save Button
            </Button>
          </div>
        </div>
      </div>
    </div>
  )
}
