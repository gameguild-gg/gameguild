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

  const { text, url, actionType, variant, size, showIcon, iconVariant, iconPosition, iconSize } = node.data

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

  const getButtonStyles = () => {
    const baseStyles = "inline-flex items-center justify-center rounded-md font-medium transition-all duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50 cursor-pointer"
    
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

    const layoutStyles = {
      top: "flex-col",
      bottom: "flex-col-reverse",
      left: "flex-row-reverse",
      right: "flex-row",
    }

    return `${baseStyles} ${sizeStyles[size]} ${variantStyles[variant]} ${layoutStyles[iconPosition || "right"]}`
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
      <button className={getButtonStyles()} onClick={handleButtonAction}>
        {text}
        {showIcon && <span className={iconSpacingClass}>{getActionIcon()}</span>}
      </button>
    </div>
  )
}
