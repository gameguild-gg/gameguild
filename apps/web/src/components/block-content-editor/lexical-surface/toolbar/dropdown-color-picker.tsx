/**
 * Color picker dropdown — replaces the playground `DropdownColorPicker`
 * with a swatch grid + HEX input. Picker with hue/saturation/lightness
 * sliders is deferred to Wave B if needed.
 */
"use client"

import * as React from "react"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { cn } from "@/lib/utils"
import { ChevronDownIcon } from "../icons"

const SWATCHES: string[] = [
  // grayscale
  "#000000", "#525252", "#737373", "#a3a3a3", "#d4d4d4", "#ffffff",
  // reds / oranges / yellows
  "#ef4444", "#f97316", "#f59e0b", "#eab308",
  // greens
  "#84cc16", "#22c55e", "#10b981", "#14b8a6",
  // blues
  "#06b6d4", "#0ea5e9", "#3b82f6", "#6366f1",
  // purples / pinks
  "#8b5cf6", "#a855f7", "#d946ef", "#ec4899",
]

export function DropdownColorPicker({
  color,
  onChange,
  buttonAriaLabel,
  buttonIcon,
  title,
  disabled,
}: {
  color: string
  onChange: (next: string, skipHistoryStack: boolean, skipRefocus: boolean) => void
  buttonAriaLabel?: string
  buttonIcon?: React.ReactNode
  title?: string
  disabled?: boolean
}) {
  const [open, setOpen] = React.useState(false)
  const [hex, setHex] = React.useState(color)

  React.useEffect(() => {
    setHex(color)
  }, [color])

  const commit = (next: string) => {
    if (/^#[0-9a-fA-F]{3,8}$/.test(next)) {
      onChange(next, false, false)
    }
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          type="button"
          disabled={disabled}
          title={title}
          aria-label={buttonAriaLabel}
          className={cn(
            "inline-flex items-center gap-1 h-8 px-2 rounded text-sm",
            "hover:bg-gray-100 dark:hover:bg-gray-800 disabled:opacity-40 disabled:pointer-events-none",
          )}
        >
          <span className="relative inline-flex items-center justify-center w-4 h-4">
            {buttonIcon}
            <span
              aria-hidden
              className="absolute -bottom-1 left-0 right-0 h-1 rounded-sm border border-black/10"
              style={{ background: color }}
            />
          </span>
          <ChevronDownIcon className="w-3 h-3 opacity-60" />
        </button>
      </PopoverTrigger>
      <PopoverContent align="start" sideOffset={4} className="w-[220px] p-3 space-y-3">
        <div className="grid grid-cols-6 gap-1.5">
          {SWATCHES.map((s) => (
            <button
              key={s}
              type="button"
              aria-label={`Color ${s}`}
              onClick={() => {
                setHex(s)
                onChange(s, false, false)
                setOpen(false)
              }}
              className={cn(
                "w-7 h-7 rounded border border-black/10 dark:border-white/10",
                "hover:scale-110 transition-transform",
                color.toLowerCase() === s.toLowerCase() &&
                  "ring-2 ring-blue-500 ring-offset-1 ring-offset-white dark:ring-offset-gray-900",
              )}
              style={{ background: s }}
            />
          ))}
        </div>
        <div className="flex items-center gap-2">
          <label className="text-xs text-gray-500 dark:text-gray-400" htmlFor="hex-input">
            HEX
          </label>
          <input
            id="hex-input"
            type="text"
            value={hex}
            onChange={(e) => setHex(e.target.value)}
            onBlur={() => commit(hex)}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault()
                commit(hex)
                setOpen(false)
              }
            }}
            className={cn(
              "flex-1 h-7 px-2 text-sm rounded border bg-transparent",
              "border-gray-300 dark:border-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500",
            )}
          />
        </div>
      </PopoverContent>
    </Popover>
  )
}
