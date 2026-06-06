/**
 * StickyComponent — React renderer for StickyNode with support for custom HEX colors
 * using the editor's DropdownColorPicker, Classic/Formal/Modern styles, and readOnly checks.
 */
"use client"

import * as React from "react"
import { useCallback, useEffect, useRef, useState } from "react"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import { $getNodeByKey } from "lexical"
import { cn } from "@/lib/utils"
import {
  Pin,
  Trash2,
  FileText,
  Sparkles,
  Info,
  AlertTriangle,
  AlertCircle,
  CheckCircle2,
  Palette,
} from "lucide-react"
import { StickyStyle, $isStickyNode } from "./sticky-node"
import { DropdownColorPicker } from "../toolbar/dropdown-color-picker"

interface StickyComponentProps {
  text: string
  color: string // Hex string
  style: StickyStyle
  nodeKey: string
}

// Convert Hex to RGB helper
function hexToRgb(hex: string) {
  const shorthandRegex = /^#?([a-f\d])([a-f\d])([a-f\d])$/i
  const fullHex = hex.replace(shorthandRegex, (m, r, g, b) => r + r + g + g + b + b)
  const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(fullHex)
  return result
    ? {
      r: parseInt(result[1]!, 16),
      g: parseInt(result[2]!, 16),
      b: parseInt(result[3]!, 16),
    }
    : { r: 254, g: 243, b: 199 } // Default amber-100/yellow
}

// Check contrast to choose light or dark text
function getContrastColor(hex: string) {
  const { r, g, b } = hexToRgb(hex)
  const yiq = (r * 299 + g * 587 + b * 114) / 1000
  return yiq >= 128 ? "text-gray-950" : "text-gray-50"
}

// Return RGBA string from Hex
function getRgba(hex: string, alpha: number) {
  const { r, g, b } = hexToRgb(hex)
  return `rgba(${r}, ${g}, ${b}, ${alpha})`
}

// Map color hues to formal status icons
function getFormalIcon(hex: string) {
  const { r, g, b } = hexToRgb(hex)
  // Simple heuristic: which channel is dominant?
  if (g > r && g > b) return CheckCircle2 // green-ish -> Success
  if (b > r && b > g) return Info // blue-ish -> Info
  if (r > g && r > b && g > 150) return AlertTriangle // yellow/orange-ish -> Warning
  return AlertCircle // red/pink-ish or default -> Alert/Error
}

export function StickyComponent({ text, color, style, nodeKey }: StickyComponentProps) {
  const [editor] = useLexicalComposerContext()
  const [value, setValue] = useState(text)
  const [isFocused, setIsFocused] = useState(false)
  const [isEditable, setIsEditable] = useState(() => editor.isEditable())
  const textareaRef = useRef<HTMLTextAreaElement>(null)

  // Autogrow height
  const adjustHeight = useCallback(() => {
    const textarea = textareaRef.current
    if (textarea) {
      textarea.style.height = "auto"
      textarea.style.height = `${textarea.scrollHeight}px`
    }
  }, [])

  useEffect(() => {
    setValue(text)
  }, [text])

  useEffect(() => {
    adjustHeight()
  }, [value, adjustHeight])

  useEffect(() => {
    return editor.registerEditableListener((editable) => {
      setIsEditable(editable)
    })
  }, [editor])

  // Auto-focus the textarea when a new sticky note is created (text is empty).
  // We need a small delay so the DecoratorNode finishes rendering first,
  // then we blur the Lexical root to release its selection lock, and
  // finally focus the textarea.
  useEffect(() => {
    if (text === "" && isEditable && textareaRef.current) {
      const timer = setTimeout(() => {
        const rootElement = editor.getRootElement()
        if (rootElement) {
          rootElement.blur()
        }
        textareaRef.current?.focus()
      }, 50)
      return () => clearTimeout(timer)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []) // Only on mount

  // Stop keyboard/input events from bubbling up to Lexical's handlers.
  // Without this, Lexical intercepts all keystrokes inside its
  // contentEditable tree, so the textarea never receives characters.
  const stopLexicalPropagation = useCallback(
    (e: React.SyntheticEvent) => {
      e.stopPropagation()
    },
    [],
  )

  // When the user clicks anywhere inside the sticky wrapper, we need to
  // blur Lexical's root element first so it releases selection, then
  // focus the textarea. Without this, Lexical intercepts the mousedown.
  const handleWrapperMouseDown = useCallback(
    (e: React.MouseEvent) => {
      if (!isEditable) return
      // Only intervene when the click target is the wrapper or the textarea
      // area — not toolbar buttons (which have their own handlers).
      const target = e.target as HTMLElement
      const isToolbarClick = target.closest("button") || target.closest("[data-radix-popper-content-wrapper]")
      if (isToolbarClick) return

      const rootElement = editor.getRootElement()
      if (rootElement) {
        rootElement.blur()
      }
      // Focus the textarea on the next tick after Lexical releases
      requestAnimationFrame(() => {
        textareaRef.current?.focus()
      })
    },
    [editor, isEditable],
  )

  const handleTextChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    e.stopPropagation()
    const val = e.target.value
    setValue(val)
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if ($isStickyNode(node)) {
        node.setText(val)
      }
    })
  }

  const handleColorChange = (newColor: string) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if ($isStickyNode(node)) {
        node.setColor(newColor)
      }
    })
    // Re-focus the textarea after Lexical re-renders the decorator
    requestAnimationFrame(() => {
      textareaRef.current?.focus()
    })
  }

  const handleStyleChange = (newStyle: StickyStyle) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if ($isStickyNode(node)) {
        node.setStyle(newStyle)
      }
    })
    // Re-focus the textarea after Lexical re-renders the decorator
    requestAnimationFrame(() => {
      textareaRef.current?.focus()
    })
  }

  const handleDelete = () => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (node) {
        node.remove()
      }
    })
  }

  // Organic rotation effect based on nodeKey hash (only for classic style)
  const getRotationClass = () => {
    if (style !== "classic") return ""
    const num = parseInt(nodeKey, 10)
    if (isNaN(num)) return "rotate-1"
    return num % 2 === 0 ? "rotate-1" : "-rotate-1"
  }

  // Calculate inline styling based on chosen Style and Custom Hex Color
  const getStyleProps = () => {
    switch (style) {
      case "formal":
        return {
          backgroundColor: getRgba(color, 0.08),
          borderLeftColor: color,
          borderLeftWidth: "4px",
        }
      case "modern":
        return {
          borderTopColor: color,
          borderTopWidth: "4px",
        }
      case "classic":
      default:
        return {
          backgroundColor: color,
          borderColor: getRgba(color, 0.4),
        }
    }
  }

  const STYLES_LIST: { id: StickyStyle; label: string; Icon: any }[] = [
    { id: "classic", label: "Classic", Icon: Pin },
    { id: "formal", label: "Formal", Icon: FileText },
    { id: "modern", label: "Modern", Icon: Sparkles },
  ]

  const FormalIcon = getFormalIcon(color)

  return (
    <div
      className={cn(
        "group relative mx-auto my-6 p-5 w-full max-w-lg transition-all duration-200 border",
        style === "formal" && "border-y-gray-200 border-r-gray-200 dark:border-y-gray-800 dark:border-r-gray-800 rounded-r-md shadow-sm",
        style === "modern" && "bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-800 shadow-sm rounded-xl",
        style === "classic" && "rounded-lg shadow-md",
        getRotationClass(),
        isEditable && (isFocused ? "shadow-lg scale-[1.01] border-gray-400 dark:border-gray-500" : "hover:shadow-lg hover:scale-[1.01]")
      )}
      style={getStyleProps()}
      onFocus={() => isEditable && setIsFocused(true)}
      onBlur={(e) => {
        if (!e.currentTarget.contains(e.relatedTarget)) {
          setIsFocused(false)
        }
      }}
      onMouseDown={handleWrapperMouseDown}
      // Prevent Lexical from intercepting keyboard events
      // (e.g. Backspace/Delete would otherwise remove the node)
      onKeyDown={stopLexicalPropagation}
      onKeyUp={stopLexicalPropagation}
      onInput={stopLexicalPropagation}
      onBeforeInput={stopLexicalPropagation}
      onCopy={stopLexicalPropagation}
      onCut={stopLexicalPropagation}
      onPaste={stopLexicalPropagation}
      tabIndex={isEditable ? 0 : -1}
    >
      {/* Pin Icon (Classic Style Only) */}
      {style === "classic" && (
        <div className="absolute top-2 left-1/2 -translate-x-1/2 opacity-80 text-rose-500 pointer-events-none">
          <Pin className="w-5 h-5 fill-rose-500 drop-shadow-sm" style={{ color }} />
        </div>
      )}

      {/* Main content body */}
      <div className={cn("flex items-start gap-3", style === "classic" && "mt-3")}>
        {/* Left Side Icon for Formal Style */}
        {style === "formal" && (
          <div className="mt-0.5 shrink-0" style={{ color }}>
            <FormalIcon className="w-5 h-5" />
          </div>
        )}

        {/* Text Area */}
        <textarea
          ref={textareaRef}
          value={value}
          onChange={handleTextChange}
          disabled={!isEditable}
          readOnly={!isEditable}
          placeholder={isEditable ? "Type something here..." : ""}
          className={cn(
            "w-full resize-none bg-transparent outline-none border-none p-0 font-sans text-base leading-relaxed placeholder-black/35 dark:placeholder-white/35",
            style === "classic" ? getContrastColor(color) : "text-gray-800 dark:text-gray-200",
            !isEditable && "cursor-default"
          )}
          rows={2}
        />
      </div>

      {/* Action Toolbar */}
      {isEditable && (
        <div
          className={cn(
            "flex flex-wrap items-center justify-between gap-3 mt-4 pt-3 border-t border-black/5 dark:border-white/5 opacity-0 group-hover:opacity-100 group-focus-within:opacity-100 transition-opacity duration-200"
          )}
        >
          {/* Style Selector */}
          <div className="flex items-center gap-1 bg-black/5 dark:bg-white/5 p-0.5 rounded-md">
            {STYLES_LIST.map(({ id, label, Icon: StyleIcon }) => (
              <button
                key={id}
                type="button"
                onClick={() => handleStyleChange(id)}
                className={cn(
                  "px-2 py-0.5 rounded text-xs flex items-center gap-1 font-medium transition-colors focus:outline-none",
                  style === id
                    ? "bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 shadow-sm"
                    : "text-gray-500 hover:text-gray-900 dark:text-gray-400 dark:hover:text-gray-200"
                )}
                title={`Style: ${label}`}
              >
                <StyleIcon className="w-3.5 h-3.5" />
                <span className="text-xxs sm:text-xs">{label}</span>
              </button>
            ))}
          </div>

          <div className="flex items-center gap-3">
            {/* Color selector using our custom color picker */}
            <DropdownColorPicker
              color={color}
              onChange={(nextColor) => handleColorChange(nextColor)}
              title="Change note color"
              buttonIcon={<Palette className="w-4.5 h-4.5 text-gray-500 dark:text-gray-400" />}
            />

            {/* Delete button */}
            <button
              type="button"
              onClick={handleDelete}
              className={cn(
                "p-1 rounded hover:bg-black/5 dark:hover:bg-white/5 text-black/50 hover:text-red-600 dark:text-white/50 dark:hover:text-red-400 transition-colors focus:outline-none"
              )}
              title="Delete node"
            >
              <Trash2 className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
