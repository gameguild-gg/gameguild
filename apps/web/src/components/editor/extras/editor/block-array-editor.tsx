"use client"

import { useState, useCallback } from "react"
import { Button } from "@/components/ui/button"
import { Plus, GripVertical, Trash2, ChevronUp, ChevronDown } from "lucide-react"
import { BlockTypePicker } from "./block-type-picker"
import { BLOCK_REGISTRY, type BlockCellType } from "./block-component-registry"
import { CELL_TO_LEXICAL_TYPE, type CellType } from "@/lib/storage/editor/cell-converters/cell-data"
import type { Cell, CellularContent } from "@/lib/storage/editor/cell-structure"
import { cellToSerializedNode, BlockContentRenderer } from "@/components/editor/extras/editor/block-array-viewer"

interface BlockArrayEditorProps {
  cells: CellularContent
  onChange: (cells: CellularContent) => void
  readOnly?: boolean
}

export function BlockArrayEditor({ cells, onChange, readOnly = false }: BlockArrayEditorProps) {
  const [pickerOpen, setPickerOpen] = useState(false)
  const [insertIndex, setInsertIndex] = useState<number | null>(null)

  const handleAddBlock = useCallback((cell: Cell) => {
    const idx = insertIndex ?? cells.length
    const next = [...cells]
    next.splice(idx, 0, cell)
    onChange(next)
    setInsertIndex(null)
  }, [cells, onChange, insertIndex])

  const handleRemoveBlock = useCallback((index: number) => {
    onChange(cells.filter((_, i) => i !== index))
  }, [cells, onChange])

  const handleMoveUp = useCallback((index: number) => {
    if (index <= 0) return
    const next = [...cells]
    ;[next[index - 1], next[index]] = [next[index]!, next[index - 1]!]
    onChange(next)
  }, [cells, onChange])

  const handleMoveDown = useCallback((index: number) => {
    if (index >= cells.length - 1) return
    const next = [...cells]
    ;[next[index], next[index + 1]] = [next[index + 1]!, next[index]!]
    onChange(next)
  }, [cells, onChange])

  const handleUpdateBlock = useCallback((index: number, newCell: Cell) => {
    const next = [...cells]
    next[index] = newCell
    onChange(next)
  }, [cells, onChange])

  const openPickerAt = (index: number) => {
    setInsertIndex(index)
    setPickerOpen(true)
  }

  return (
    <div className="space-y-2">
      {cells.length === 0 && !readOnly && (
        <div className="flex flex-col items-center justify-center py-16 border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-lg">
          <p className="text-sm text-gray-500 dark:text-gray-400 mb-4">No blocks yet. Add your first block.</p>
          <Button
            variant="outline"
            size="sm"
            onClick={() => openPickerAt(0)}
            className="gap-2"
          >
            <Plus className="h-4 w-4" />
            Add Block
          </Button>
        </div>
      )}

      {cells.map((cell, index) => {
        const [, meta] = cell
        const cellType = meta.t as CellType
        const config = BLOCK_REGISTRY[cellType as BlockCellType]

        if (!config) return null

        const Icon = config.icon

        return (
          <div key={index} className="group relative">
            {/* Insert-before button (between blocks) */}
            {!readOnly && (
              <div className="flex justify-center -mb-1 opacity-0 group-hover:opacity-100 transition-opacity">
                <button
                  type="button"
                  onClick={() => openPickerAt(index)}
                  className="flex items-center gap-1 px-2 py-0.5 text-[10px] text-gray-400 hover:text-blue-500 dark:hover:text-blue-400 transition-colors"
                >
                  <Plus className="h-3 w-3" />
                  Insert
                </button>
              </div>
            )}

            <div className="flex gap-2 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 overflow-hidden">
              {/* Drag handle + controls */}
              {!readOnly && (
                <div className="flex flex-col items-center justify-between py-2 px-1.5 bg-gray-50 dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700">
                  <div className="flex flex-col gap-0.5">
                    <button
                      type="button"
                      onClick={() => handleMoveUp(index)}
                      disabled={index === 0}
                      className="p-0.5 text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 disabled:opacity-30 disabled:cursor-not-allowed"
                    >
                      <ChevronUp className="h-3.5 w-3.5" />
                    </button>
                    <GripVertical className="h-3.5 w-3.5 text-gray-300 dark:text-gray-600 mx-auto" />
                    <button
                      type="button"
                      onClick={() => handleMoveDown(index)}
                      disabled={index === cells.length - 1}
                      className="p-0.5 text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 disabled:opacity-30 disabled:cursor-not-allowed"
                    >
                      <ChevronDown className="h-3.5 w-3.5" />
                    </button>
                  </div>
                  <button
                    type="button"
                    onClick={() => handleRemoveBlock(index)}
                    className="p-0.5 text-gray-400 hover:text-red-500 dark:hover:text-red-400 transition-colors"
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </button>
                </div>
              )}

              {/* Block content */}
              <div className="flex-1 min-w-0 p-3">
                {/* Type badge */}
                <div className="flex items-center gap-1.5 mb-2 text-xs text-gray-500 dark:text-gray-400">
                  <Icon className="h-3.5 w-3.5" />
                  <span className="font-medium">{config.label}</span>
                  <span className="text-gray-300 dark:text-gray-600">#{index + 1}</span>
                </div>

                {/* Rendered content preview */}
                <div className="prose prose-sm dark:prose-invert max-w-none">
                  <BlockContentRenderer cell={cell} />
                </div>
              </div>
            </div>
          </div>
        )
      })}

      {/* Add block button at the end */}
      {cells.length > 0 && !readOnly && (
        <div className="flex justify-center pt-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => openPickerAt(cells.length)}
            className="gap-2"
          >
            <Plus className="h-4 w-4" />
            Add Block
          </Button>
        </div>
      )}

      <BlockTypePicker
        open={pickerOpen}
        onOpenChange={setPickerOpen}
        onSelect={handleAddBlock}
      />
    </div>
  )
}
