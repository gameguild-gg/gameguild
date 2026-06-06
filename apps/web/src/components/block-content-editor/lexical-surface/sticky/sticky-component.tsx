/**
 * StickyComponent — Renderer for StickyNode with:
 *   - Custom HEX color via DropdownColorPicker
 *   - Classic / Formal / Modern styles
 *   - Wide / Compact size toggle
 *   - Drag-to-reposition (relative offset from natural position)
 *   - readOnly awareness
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
  GripVertical,
  Maximize2,
  Minimize2,
} from "lucide-react"
import { StickyStyle, StickySize, $isStickyNode } from "./sticky-node"
import { DropdownColorPicker } from "../toolbar/dropdown-color-picker"

interface StickyComponentProps {
  text: string
  color: string
  style: StickyStyle
  size: StickySize
  xOffset: number
  yOffset: number
  nodeKey: string
}

// ── Helpers ──────────────────────────────────────────────────────────────

function hexToRgb(hex: string) {
  const shorthandRegex = /^#?([a-f\d])([a-f\d])([a-f\d])$/i
  const fullHex = hex.replace(shorthandRegex, (_m, r, g, b) => r + r + g + g + b + b)
  const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(fullHex)
  return result
    ? { r: parseInt(result[1]!, 16), g: parseInt(result[2]!, 16), b: parseInt(result[3]!, 16) }
    : { r: 254, g: 243, b: 199 }
}

function getContrastColor(hex: string) {
  const { r, g, b } = hexToRgb(hex)
  const yiq = (r * 299 + g * 587 + b * 114) / 1000
  return yiq >= 128 ? "text-gray-950" : "text-gray-50"
}

function getRgba(hex: string, alpha: number) {
  const { r, g, b } = hexToRgb(hex)
  return `rgba(${r}, ${g}, ${b}, ${alpha})`
}

function getFormalIcon(hex: string) {
  const { r, g, b } = hexToRgb(hex)
  if (g > r && g > b) return CheckCircle2
  if (b > r && b > g) return Info
  if (r > g && r > b && g > 150) return AlertTriangle
  return AlertCircle
}

// ── Component ────────────────────────────────────────────────────────────

export function StickyComponent({
  text, color, style, size, xOffset, yOffset, nodeKey,
}: StickyComponentProps) {
  const [editor] = useLexicalComposerContext()
  const [value, setValue] = useState(text)
  const [isFocused, setIsFocused] = useState(false)
  const [isEditable, setIsEditable] = useState(() => editor.isEditable())
  const [isDragging, setIsDragging] = useState(false)
  const textareaRef = useRef<HTMLTextAreaElement>(null)
  const wrapperRef = useRef<HTMLDivElement>(null)

  // ── Autogrow ──
  const adjustHeight = useCallback(() => {
    const textarea = textareaRef.current
    if (textarea) {
      textarea.style.height = "auto"
      textarea.style.height = `${textarea.scrollHeight}px`
    }
  }, [])

  useEffect(() => { setValue(text) }, [text])
  useEffect(() => { adjustHeight() }, [value, adjustHeight])

  useEffect(() => {
    return editor.registerEditableListener((editable) => setIsEditable(editable))
  }, [editor])

  // Auto-focus on newly created (empty) notes
  useEffect(() => {
    if (text === "" && isEditable && textareaRef.current) {
      const timer = setTimeout(() => {
        editor.getRootElement()?.blur()
        textareaRef.current?.focus()
      }, 50)
      return () => clearTimeout(timer)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  // ── Event helpers ──
  const stopLexicalPropagation = useCallback(
    (e: React.SyntheticEvent) => { e.stopPropagation() }, [],
  )

  const handleWrapperMouseDown = useCallback(
    (e: React.MouseEvent) => {
      if (!isEditable) return
      const target = e.target as HTMLElement
      if (target.closest("button") || target.closest("[data-radix-popper-content-wrapper]")) return
      // Don't steal focus when drag handle is being used
      if (target.closest("[data-sticky-drag]")) return
      editor.getRootElement()?.blur()
      requestAnimationFrame(() => { textareaRef.current?.focus() })
    },
    [editor, isEditable],
  )

  // ── Handlers ──
  const handleTextChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    e.stopPropagation()
    const val = e.target.value
    setValue(val)
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if ($isStickyNode(node)) node.setText(val)
    })
  }

  const handleColorChange = (newColor: string) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if ($isStickyNode(node)) node.setColor(newColor)
    })
    requestAnimationFrame(() => { textareaRef.current?.focus() })
  }

  const handleStyleChange = (newStyle: StickyStyle) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if ($isStickyNode(node)) node.setStyle(newStyle)
    })
    requestAnimationFrame(() => { textareaRef.current?.focus() })
  }

  const handleSizeToggle = () => {
    const newSize: StickySize = size === "wide" ? "compact" : "wide"
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if ($isStickyNode(node)) node.setSize(newSize)
    })
    requestAnimationFrame(() => { textareaRef.current?.focus() })
  }

  const handleDelete = () => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if (node) node.remove()
    })
  }

  // ── Drag-to-reposition (GPU-accelerated, no transitions) ──
  const handleDragStart = useCallback(
    (e: React.MouseEvent) => {
      if (!isEditable) return
      e.preventDefault()
      e.stopPropagation()
      setIsDragging(true)

      const startX = e.clientX
      const startY = e.clientY
      const startOffsetX = xOffset
      const startOffsetY = yOffset
      const wrapper = wrapperRef.current

      // GPU-accelerate and kill transitions for 60 fps drag
      if (wrapper) {
        wrapper.style.willChange = "transform"
        wrapper.style.transition = "none"
      }

      const onMouseMove = (ev: MouseEvent) => {
        if (wrapper) {
          const dx = ev.clientX - startX
          const dy = ev.clientY - startY
          wrapper.style.transform = `translate(${startOffsetX + dx}px, ${startOffsetY + dy}px)`
        }
      }

      const onMouseUp = (ev: MouseEvent) => {
        const dx = ev.clientX - startX
        const dy = ev.clientY - startY
        const finalX = startOffsetX + dx
        const finalY = startOffsetY + dy
        document.removeEventListener("mousemove", onMouseMove)
        document.removeEventListener("mouseup", onMouseUp)

        // Restore transitions
        if (wrapper) {
          wrapper.style.willChange = ""
          wrapper.style.transition = ""
        }
        setIsDragging(false)
        editor.update(() => {
          const node = $getNodeByKey(nodeKey)
          if ($isStickyNode(node)) node.setPosition(finalX, finalY)
        })
      }

      document.addEventListener("mousemove", onMouseMove)
      document.addEventListener("mouseup", onMouseUp)
    },
    [editor, isEditable, nodeKey, xOffset, yOffset],
  )

  // Reset position on double-click the handle
  const handleResetPosition = useCallback(() => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey)
      if ($isStickyNode(node)) node.setPosition(0, 0)
    })
  }, [editor, nodeKey])

  // ── Visuals ──
  const getRotationClass = () => {
    if (style !== "classic") return ""
    const num = parseInt(nodeKey, 10)
    if (isNaN(num)) return "rotate-1"
    return num % 2 === 0 ? "rotate-1" : "-rotate-1"
  }

  const getStyleProps = (): React.CSSProperties => {
    const base: React.CSSProperties = {
      transform: `translate(${xOffset}px, ${yOffset}px)`,
      // Re-enable pointer events on the card itself (wrapper is none)
      pointerEvents: "auto",
      position: "relative",
      zIndex: 10,
    }
    switch (style) {
      case "formal":
        return {
          ...base,
          backgroundColor: getRgba(color, 0.08),
          borderLeftColor: color,
          borderLeftWidth: "4px",
        }
      case "modern":
        return {
          ...base,
          borderTopColor: color,
          borderTopWidth: "4px",
        }
      case "classic":
      default:
        return {
          ...base,
          backgroundColor: color,
          borderColor: getRgba(color, 0.4),
        }
    }
  }

  const STYLES_LIST: { id: StickyStyle; label: string; Icon: React.ComponentType<{ className?: string }> }[] = [
    { id: "classic", label: "Classic", Icon: Pin },
    { id: "formal", label: "Formal", Icon: FileText },
    { id: "modern", label: "Modern", Icon: Sparkles },
  ]

  const FormalIcon = getFormalIcon(color)

  return (
    <div
      ref={wrapperRef}
      className={cn(
        "group p-5 border",
        // Only animate when NOT dragging — transitions kill drag fluidity
        !isDragging && "transition-shadow duration-150",
        size === "wide" ? "w-full max-w-lg mx-auto" : "w-full max-w-[16rem]",
        style === "formal" && "border-y-gray-200 border-r-gray-200 dark:border-y-gray-800 dark:border-r-gray-800 rounded-r-md shadow-sm",
        style === "modern" && "bg-white dark:bg-gray-900 border-gray-200 dark:border-gray-800 shadow-sm rounded-xl",
        style === "classic" && "rounded-lg shadow-md",
        getRotationClass(),
        isDragging && "opacity-80 shadow-2xl cursor-grabbing select-none",
        isEditable && !isDragging && (isFocused ? "shadow-lg" : "hover:shadow-lg"),
      )}
      style={getStyleProps()}
      onFocus={() => isEditable && setIsFocused(true)}
      onBlur={(e) => {
        if (!e.currentTarget.contains(e.relatedTarget)) setIsFocused(false)
      }}
      onMouseDown={handleWrapperMouseDown}
      onKeyDown={stopLexicalPropagation}
      onKeyUp={stopLexicalPropagation}
      onInput={stopLexicalPropagation}
      onBeforeInput={stopLexicalPropagation}
      onCopy={stopLexicalPropagation}
      onCut={stopLexicalPropagation}
      onPaste={stopLexicalPropagation}
      tabIndex={isEditable ? 0 : -1}
    >
      {/* ── Drag Handle (top-right) ── */}
      {isEditable && (
        <div
          data-sticky-drag
          onMouseDown={handleDragStart}
          onDoubleClick={handleResetPosition}
          className={cn(
            "absolute top-1.5 right-1.5 cursor-grab active:cursor-grabbing p-0.5 rounded opacity-0 group-hover:opacity-60 group-focus-within:opacity-60 hover:!opacity-100 transition-opacity",
            "text-black/40 dark:text-white/40 hover:text-black/70 dark:hover:text-white/70",
          )}
          title="Drag to reposition · Double-click to reset"
        >
          <GripVertical className="w-4 h-4" />
        </div>
      )}

      {/* Pin Icon (Classic Style Only) */}
      {style === "classic" && (
        <div className="absolute top-2 left-1/2 -translate-x-1/2 opacity-80 text-rose-500 pointer-events-none">
          <Pin className="w-5 h-5 fill-rose-500 drop-shadow-sm" style={{ color }} />
        </div>
      )}

      {/* Main content body */}
      <div className={cn("flex items-start gap-3", style === "classic" && "mt-3")}>
        {style === "formal" && (
          <div className="mt-0.5 shrink-0" style={{ color }}>
            <FormalIcon className="w-5 h-5" />
          </div>
        )}

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
            !isEditable && "cursor-default",
          )}
          rows={2}
        />
      </div>

      {/* ── Action Toolbar ── */}
      {isEditable && (
        <div
          className={cn(
            "flex flex-wrap items-center justify-between gap-2 mt-4 pt-3 border-t border-black/5 dark:border-white/5 opacity-0 group-hover:opacity-100 group-focus-within:opacity-100 transition-opacity duration-200",
          )}
        >
          {/* Style selector */}
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
                    : "text-gray-500 hover:text-gray-900 dark:text-gray-400 dark:hover:text-gray-200",
                )}
                title={`Style: ${label}`}
              >
                <StyleIcon className="w-3.5 h-3.5" />
                <span className="text-xxs sm:text-xs">{label}</span>
              </button>
            ))}
          </div>

          <div className="flex items-center gap-1.5">
            {/* Size toggle */}
            <button
              type="button"
              onClick={handleSizeToggle}
              className="p-1 rounded hover:bg-black/5 dark:hover:bg-white/5 text-black/50 dark:text-white/50 transition-colors focus:outline-none"
              title={size === "wide" ? "Switch to compact" : "Switch to wide"}
            >
              {size === "wide"
                ? <Minimize2 className="w-4 h-4" />
                : <Maximize2 className="w-4 h-4" />
              }
            </button>

            {/* Color picker */}
            <DropdownColorPicker
              color={color}
              onChange={(nextColor) => handleColorChange(nextColor)}
              title="Change note color"
              buttonIcon={<Palette className="w-4 h-4 text-gray-500 dark:text-gray-400" />}
            />

            {/* Delete */}
            <button
              type="button"
              onClick={handleDelete}
              className="p-1 rounded hover:bg-black/5 dark:hover:bg-white/5 text-black/50 hover:text-red-600 dark:text-white/50 dark:hover:text-red-400 transition-colors focus:outline-none"
              title="Delete note"
            >
              <Trash2 className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
