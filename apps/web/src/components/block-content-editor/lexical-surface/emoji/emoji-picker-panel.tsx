/**
 * EmojiPickerPanel — searchable grid panel for picking emojis.
 *
 * Used by the toolbar Insert menu to show a popover where the user can
 * search by alias/tag and click a glyph to insert. Calls `onSelect`
 * with the chosen emoji string.
 */
"use client"

import * as React from "react"
import { useEffect, useMemo, useState } from "react"
import { cn } from "@/lib/utils"
import type { Emoji } from "./emoji-list"

interface EmojiPickerPanelProps {
  onSelect: (emoji: string) => void
  autoFocus?: boolean
  className?: string
}

export function EmojiPickerPanel({ onSelect, autoFocus = true, className }: EmojiPickerPanelProps) {
  const [emojis, setEmojis] = useState<Emoji[]>([])
  const [query, setQuery] = useState("")

  useEffect(() => {
    let cancelled = false
    void import("./emoji-list").then((mod) => {
      if (!cancelled) setEmojis(mod.default)
    })
    return () => {
      cancelled = true
    }
  }, [])

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase()
    if (!q) return emojis
    return emojis.filter(
      (e) =>
        e.aliases.some((a) => a.toLowerCase().includes(q)) ||
        e.tags.some((t) => t.toLowerCase().includes(q)),
    )
  }, [emojis, query])

  return (
    <div className={cn("flex w-[280px] flex-col gap-2", className)}>
      <input
        type="text"
        autoFocus={autoFocus}
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        placeholder="Search emoji…"
        aria-label="Search emoji"
        className={cn(
          "h-8 w-full rounded border px-2 text-sm outline-none",
          "border-gray-200 bg-white text-gray-900",
          "dark:border-gray-700 dark:bg-gray-900 dark:text-white",
          "focus:border-blue-500",
        )}
      />
      <div
        role="listbox"
        aria-label="Emoji"
        className="grid max-h-[260px] grid-cols-8 gap-0.5 overflow-y-auto pr-0.5"
      >
        {filtered.length === 0 && (
          <div className="col-span-8 py-6 text-center text-xs text-gray-500 dark:text-gray-400">
            No emoji found
          </div>
        )}
        {filtered.map((e) => {
          const label = e.aliases[0] ?? "emoji"
          return (
            <button
              key={label + e.emoji}
              type="button"
              role="option"
              title={label}
              aria-label={label}
              onClick={() => onSelect(e.emoji)}
              className={cn(
                "flex h-8 w-8 items-center justify-center rounded text-lg leading-none",
                "hover:bg-gray-100 dark:hover:bg-gray-800",
                "focus:bg-gray-100 dark:focus:bg-gray-800 focus:outline-none",
              )}
            >
              {e.emoji}
            </button>
          )
        })}
      </div>
    </div>
  )
}
