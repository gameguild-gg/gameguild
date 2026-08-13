/**
 * InsertLayoutDialog — picks a `grid-template-columns` template and
 * dispatches `INSERT_LAYOUT_COMMAND`.
 *
 * Mirrors the playground "Insert Columns Layout" modal but uses our
 * shadcn-style primitives.
 */
"use client"

import { useState } from "react"
import type { LexicalEditor } from "lexical"
import { cn } from "@game-guild/ui/lib/utils"
import { INSERT_LAYOUT_COMMAND, LAYOUT_TEMPLATES } from "./layout-plugin"

interface InsertLayoutDialogProps {
  activeEditor: LexicalEditor
  onClose: () => void
}

export function InsertLayoutDialog({ activeEditor, onClose }: InsertLayoutDialogProps) {
  const [layout, setLayout] = useState<string>(LAYOUT_TEMPLATES[0]!.value)

  const onConfirm = () => {
    activeEditor.dispatchCommand(INSERT_LAYOUT_COMMAND, layout)
    onClose()
  }

  return (
    <div className="flex min-w-[260px] flex-col gap-3">
      <label
        className="flex flex-col gap-2 text-sm text-gray-700 dark:text-gray-300"
        htmlFor="layout-template-select"
      >
        Layout
        <select
          id="layout-template-select"
          className={cn(
            "h-8 rounded border px-2 text-sm",
            "border-gray-300 dark:border-gray-700",
            "bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100",
            "focus:outline-none focus:ring-1 focus:ring-blue-500",
          )}
          value={layout}
          onChange={(e) => setLayout(e.target.value)}
        >
          {LAYOUT_TEMPLATES.map(({ label, value }) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
      </label>
      <div className="flex justify-end gap-2 pt-1">
        <button
          type="button"
          onClick={onClose}
          className="h-8 rounded border border-gray-300 px-3 text-sm hover:bg-gray-100 dark:border-gray-700 dark:hover:bg-gray-800"
        >
          Cancel
        </button>
        <button
          type="button"
          onClick={onConfirm}
          className="h-8 rounded bg-blue-600 px-3 text-sm text-white hover:bg-blue-700"
        >
          Insert
        </button>
      </div>
    </div>
  )
}
