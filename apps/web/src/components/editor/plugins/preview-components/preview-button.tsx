"use client"

import type { SerializedButtonNode } from "../../nodes/button-node"
import { 
  ArrowRight, Copy, Download, ExternalLink, Mail,
  Link, Link2,
  ArrowDownToLine, FileDown,
  ClipboardCopy, CopyCheck,
  AtSign, Send
} from "lucide-react"

export function PreviewButton({ node }: { node: SerializedButtonNode }) {
  if (!node?.data) {
    console.error("Invalid button node structure:", node)
    return null
  }

  const { text, url, actionType, variant, size, showIcon, iconVariant, iconPosition, iconSize, colorPalette, customColors } = node.data

  const getActionIcon = () => {
    const iconSizeClass = {
      sm: "h-3 w-3",
      md: "h-4 w-4",
      lg: "h-5 w-5",
    }[iconSize || "md"]

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

    return iconsByType[actionType][iconVariant || 0] || iconsByType[actionType][0]
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

    const palette = colorPalette || "blue"
    return palettes[palette === "custom" ? "blue" : palette][variant]
  }

  const getButtonStyles = () => {
    const baseStyles = "inline-flex items-center justify-center rounded-md font-medium transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 cursor-pointer"
    
    const sizeStyles = {
      sm: "h-9 px-4 text-sm",
      md: "h-12 px-6 text-base",
      lg: "h-16 px-8 text-lg",
      xl: "h-24 px-12 text-2xl",
    }

    const variantBaseStyles = {
      solid: "bg-gradient-to-r text-white shadow-lg hover:shadow-2xl hover:scale-105 active:scale-95 transition-all duration-200",
      outline: "border-2 bg-transparent hover:shadow-md transition-all duration-200",
      soft: "hover:shadow-sm transition-all duration-200",
      minimal: "bg-transparent border-b-2 border-transparent rounded-none px-2 transition-all duration-200",
    }

    const layoutStyles = {
      top: "flex-col",
      bottom: "flex-col-reverse",
      left: "flex-row-reverse",
      right: "flex-row",
    }

    return `${baseStyles} ${sizeStyles[size]} ${variantBaseStyles[variant]} ${getColorStyles()} ${layoutStyles[iconPosition || "right"]}`
  }

  const getCustomStyle = () => {
    if (colorPalette === "custom" && customColors) {
      const { primary, secondary, text, hoverPrimary, hoverSecondary, hoverText } = customColors
      if (variant === "solid") {
        return {
          background: `linear-gradient(to right, ${primary}, ${secondary})`,
          color: text,
          "--hover-bg": `linear-gradient(to right, ${hoverPrimary}, ${hoverSecondary})`,
          "--hover-text": hoverText,
        } as React.CSSProperties
      } else if (variant === "outline") {
        return {
          borderColor: primary,
          color: text,
          "--hover-bg": hoverPrimary,
          "--hover-border": hoverPrimary,
          "--hover-text": hoverText,
        } as React.CSSProperties
      } else if (variant === "soft") {
        return {
          backgroundColor: `${primary}20`,
          color: text,
          "--hover-bg": `${hoverPrimary}30`,
          "--hover-text": hoverText,
        } as React.CSSProperties
      } else if (variant === "minimal") {
        return {
          color: text,
          "--hover-border": hoverPrimary,
          "--hover-text": hoverText,
        } as React.CSSProperties
      }
    }
    return {}
  }

  const handleButtonAction = () => {
    switch (actionType) {
      case "url":
        window.open(url, "_blank")
        break
      case "download":
        const link = document.createElement("a")
        link.href = url
        link.download = ""
        document.body.appendChild(link)
        link.click()
        document.body.removeChild(link)
        break
      case "copy":
        navigator.clipboard.writeText(url)
        break
      case "email":
        window.location.href = `mailto:${url}`
        break
    }
  }

  const iconSpacingClass = {
    top: "mb-2",
    bottom: "mt-2",
    left: "mr-2",
    right: "ml-2",
  }[iconPosition || "right"]

  return (
    <div className="my-4 flex justify-center">
      <style>{`
        .custom-button-hover:hover {
          background: var(--hover-bg) !important;
          color: var(--hover-text) !important;
          border-color: var(--hover-border) !important;
        }
      `}</style>
      <button 
        className={`${getButtonStyles()} ${colorPalette === "custom" ? "custom-button-hover" : ""}`}
        style={getCustomStyle()}
        onClick={handleButtonAction}
      >
        {text}
        {showIcon && <span className={iconSpacingClass}>{getActionIcon()}</span>}
      </button>
    </div>
  )
}
