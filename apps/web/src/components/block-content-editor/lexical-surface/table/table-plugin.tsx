/**
 * TablePlugin wrapper + `INSERT_TABLE_COMMAND` re-export so callers can
 * import everything table-related from one place.
 *
 * The Wave A picker gets a "/Table" option that opens a small dialog
 * asking for rows/columns and dispatches `INSERT_TABLE_COMMAND`.
 *
 * Hover actions / cell resizer / per-cell action menu are intentionally
 * skipped for now; we can layer them on later from the playground.
 */
"use client"

import * as React from "react"
import { useState } from "react"
import { TablePlugin as LexicalTablePlugin } from "@lexical/react/LexicalTablePlugin"
import {
  INSERT_TABLE_COMMAND,
} from "@lexical/table"
import type { LexicalEditor } from "lexical"
import { cn } from "@/lib/utils"

export { INSERT_TABLE_COMMAND }

export function TablePlugin(): React.JSX.Element {
  return <LexicalTablePlugin hasCellMerge hasCellBackgroundColor />
}

export function InsertTableDialog({
  activeEditor,
  onClose,
}: {
  activeEditor: LexicalEditor
  onClose: () => void
}) {
  const [rows, setRows] = useState("3")
  const [columns, setColumns] = useState("3")

  const onConfirm = () => {
    activeEditor.dispatchCommand(INSERT_TABLE_COMMAND, {
      columns,
      rows,
      includeHeaders: true,
    })
    onClose()
  }

  const inputClass = cn(
    "h-8 w-20 px-2 rounded border text-sm",
    "border-gray-300 dark:border-gray-700",
    "bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100",
    "focus:outline-none focus:ring-1 focus:ring-blue-500",
  )

  return (
    <div className="flex flex-col gap-3 min-w-[260px]">
      <label className="flex items-center justify-between gap-3 text-sm text-gray-700 dark:text-gray-300">
        Rows
        <input
          type="number"
          min="1"
          max="50"
          value={rows}
          onChange={(e) => setRows(e.target.value)}
          className={inputClass}
        />
      </label>
      <label className="flex items-center justify-between gap-3 text-sm text-gray-700 dark:text-gray-300">
        Columns
        <input
          type="number"
          min="1"
          max="20"
          value={columns}
          onChange={(e) => setColumns(e.target.value)}
          className={inputClass}
        />
      </label>
      <div className="flex justify-end gap-2 pt-1">
        <button
          type="button"
          onClick={onClose}
          className="h-8 px-3 rounded border text-sm border-gray-300 dark:border-gray-700 hover:bg-gray-100 dark:hover:bg-gray-800"
        >
          Cancel
        </button>
        <button
          type="button"
          onClick={onConfirm}
          className="h-8 px-3 rounded text-sm bg-blue-600 text-white hover:bg-blue-700"
        >
          Insert
        </button>
      </div>
    </div>
  )
}
