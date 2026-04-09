"use client"

import { useState } from "react"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { BLOCK_REGISTRY, BLOCK_CELL_TYPES, type BlockCellType } from "./block-component-registry"
import type { Cell } from "@/lib/storage/editor/cell-structure"
import { Search } from "lucide-react"

interface BlockTypePickerProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  onSelect: (cell: Cell) => void
}

export function BlockTypePicker({ open, onOpenChange, onSelect }: BlockTypePickerProps) {
  const [search, setSearch] = useState("")

  const filtered = BLOCK_CELL_TYPES.filter((type) => {
    if (!search.trim()) return true
    const config = BLOCK_REGISTRY[type]
    const q = search.toLowerCase()
    return (
      config.label.toLowerCase().includes(q) ||
      config.description.toLowerCase().includes(q) ||
      type.includes(q)
    )
  })

  const handleSelect = (type: BlockCellType) => {
    const config = BLOCK_REGISTRY[type]
    const [data, meta] = config.createEmpty()
    onSelect([data, meta] as Cell)
    onOpenChange(false)
    setSearch("")
  }

  return (
    <Dialog open={open} onOpenChange={(v) => { onOpenChange(v); if (!v) setSearch("") }}>
      <DialogContent className="max-w-2xl max-h-[80vh] overflow-hidden flex flex-col">
        <DialogHeader>
          <DialogTitle>Add Block</DialogTitle>
        </DialogHeader>

        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-gray-400" />
          <Input
            placeholder="Search block types..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="pl-9"
            autoFocus
          />
        </div>

        <div className="overflow-y-auto flex-1 mt-2">
          <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 gap-2">
            {filtered.map((type) => {
              const config = BLOCK_REGISTRY[type]
              const Icon = config.icon
              return (
                <button
                  key={type}
                  type="button"
                  onClick={() => handleSelect(type)}
                  className="flex flex-col items-center gap-1.5 p-3 rounded-lg border border-gray-200 dark:border-gray-700 hover:border-blue-400 dark:hover:border-blue-500 hover:bg-blue-50 dark:hover:bg-blue-950 transition-all text-center"
                >
                  <Icon className="h-6 w-6 text-gray-600 dark:text-gray-300" />
                  <span className="text-xs font-medium text-gray-800 dark:text-gray-200">{config.label}</span>
                  <span className="text-[10px] text-gray-500 dark:text-gray-400 leading-tight">{config.description}</span>
                </button>
              )
            })}
          </div>

          {filtered.length === 0 && (
            <div className="py-8 text-center text-sm text-gray-500 dark:text-gray-400">
              No block types matching "{search}"
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  )
}
