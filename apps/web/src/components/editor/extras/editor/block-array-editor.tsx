"use client"

import { useState, useCallback, useRef, useEffect } from "react"
import { Plus, GripVertical, Trash2, ChevronUp, ChevronDown, Pencil } from "lucide-react"
import { BlockTypePicker } from "./block-type-picker"
import { BlockEditorModal } from "./block-editor-modal"
import { BLOCK_REGISTRY, type BlockCellType } from "./block-component-registry"
import { DeleteConfirmDialog } from "@/components/editor/extras/dialogs/delete-confirm-dialog"
import type { Block, BlockArray } from "./block-types"
import { BlockContentRenderer } from "@/components/editor/extras/editor/block-array-viewer"

// ============================================================================
// Insert Line — the "seam" between blocks where new blocks can be added
// ============================================================================

function InsertLine({ onInsert }: { onInsert: () => void }) {
  return (
    <div className="group/insert relative flex items-center py-1.5">
      {/* Horizontal line */}
      <div className="flex-1 h-px bg-gray-200 dark:bg-gray-700 group-hover/insert:bg-blue-400 dark:group-hover/insert:bg-blue-500 transition-colors" />
      {/* Centered + button */}
      <button
        type="button"
        onClick={onInsert}
        className="relative z-10 flex items-center justify-center w-7 h-7 mx-2 rounded-full border-2 border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-400 opacity-40 group-hover/insert:opacity-100 group-hover/insert:border-blue-400 group-hover/insert:text-blue-500 dark:group-hover/insert:border-blue-500 dark:group-hover/insert:text-blue-400 hover:scale-110 transition-all cursor-pointer"
        title="Insert block here"
      >
        <Plus className="h-4 w-4" />
      </button>
      <div className="flex-1 h-px bg-gray-200 dark:bg-gray-700 group-hover/insert:bg-blue-400 dark:group-hover/insert:bg-blue-500 transition-colors" />
    </div>
  )
}

// ============================================================================
// Block Card — a single block with header bar and content area
// ============================================================================

interface BlockCardProps {
  block: Block
  index: number
  total: number
  onMoveUp: () => void
  onMoveDown: () => void
  onRemove: () => void
  onEdit: () => void
  readOnly: boolean
}

function BlockCard({ block, index, total, onMoveUp, onMoveDown, onRemove, onEdit, readOnly }: BlockCardProps) {
  const config = BLOCK_REGISTRY[block.type]

  if (!config) return null

  const Icon = config.icon

  return (
    <div className="group/card rounded-lg border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 shadow-sm hover:shadow-md transition-shadow overflow-hidden">
      {/* Header bar */}
      <div className="flex items-center gap-2 px-3 py-2 bg-gray-50 dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700">
        {/* Grip handle */}
        {!readOnly && (
          <GripVertical className="h-4 w-4 text-gray-300 dark:text-gray-600 shrink-0 cursor-grab" />
        )}

        {/* Type icon + label */}
        <div className="flex items-center gap-1.5 min-w-0">
          <Icon className="h-4 w-4 text-gray-500 dark:text-gray-400 shrink-0" />
          <span className="text-xs font-medium text-gray-600 dark:text-gray-300 truncate">{config.label}</span>
        </div>

        {/* Index badge */}
        <span className="text-[11px] font-mono text-gray-400 dark:text-gray-500 bg-gray-100 dark:bg-gray-700 px-1.5 py-0.5 rounded shrink-0">
          #{index + 1}
        </span>

        {/* Spacer */}
        <div className="flex-1" />

        {/* Action buttons */}
        {!readOnly && (
          <div className="flex items-center gap-0.5">
            {/* Edit button — always visible */}
            <button
              type="button"
              onClick={onEdit}
              className="p-1 rounded text-blue-500 hover:text-blue-700 hover:bg-blue-50 dark:text-blue-400 dark:hover:text-blue-300 dark:hover:bg-blue-950/40 transition-colors"
              title="Edit block"
            >
              <Pencil className="h-3.5 w-3.5" />
            </button>
            {/* Move/delete — visible on hover */}
            <div className="flex items-center gap-0.5 opacity-0 group-hover/card:opacity-100 transition-opacity">
              <button
                type="button"
                onClick={onMoveUp}
                disabled={index === 0}
                className="p-1 rounded text-gray-400 hover:text-gray-700 hover:bg-gray-200/60 dark:hover:text-gray-200 dark:hover:bg-gray-700/60 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
                title="Move up"
              >
                <ChevronUp className="h-3.5 w-3.5" />
              </button>
              <button
                type="button"
                onClick={onMoveDown}
                disabled={index === total - 1}
                className="p-1 rounded text-gray-400 hover:text-gray-700 hover:bg-gray-200/60 dark:hover:text-gray-200 dark:hover:bg-gray-700/60 disabled:opacity-30 disabled:cursor-not-allowed transition-colors"
                title="Move down"
              >
                <ChevronDown className="h-3.5 w-3.5" />
              </button>
              <button
                type="button"
                onClick={onRemove}
                className="p-1 rounded text-gray-400 hover:text-red-500 hover:bg-red-50 dark:hover:text-red-400 dark:hover:bg-red-950/40 transition-colors"
                title="Remove block"
              >
                <Trash2 className="h-3.5 w-3.5" />
              </button>
            </div>
          </div>
        )}
      </div>

      {/* Content area */}
      <div className="p-4">
        <div className="prose prose-sm dark:prose-invert max-w-none">
          <BlockContentRenderer block={block} />
        </div>
      </div>
    </div>
  )
}

// ============================================================================
// Block Array Editor — main component
// ============================================================================

interface BlockArrayEditorProps {
  blocks: BlockArray
  onChange: (blocks: BlockArray) => void
  readOnly?: boolean
}

export function BlockArrayEditor({ blocks, onChange, readOnly = false }: BlockArrayEditorProps) {
  const [pickerOpen, setPickerOpen] = useState(false)
  const [insertIndex, setInsertIndex] = useState<number | null>(null)

  // Editor modal state
  const [editorOpen, setEditorOpen] = useState(false)
  const [editingIndex, setEditingIndex] = useState<number | null>(null)
  const [editingBlock, setEditingBlock] = useState<Block | null>(null)
  const [editingBlockType, setEditingBlockType] = useState<BlockCellType | null>(null)

  // Delete confirmation state
  const [deleteIndex, setDeleteIndex] = useState<number | null>(null)
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)

  // Scroll-to-block after move
  const scrollToIndexRef = useRef<number | null>(null)
  const blockRefsMap = useRef<Map<number, HTMLDivElement>>(new Map())



  const handleAddBlock = useCallback((block: Block) => {
    const idx = insertIndex ?? blocks.length
    const next = [...blocks]
    next.splice(idx, 0, block)
    onChange(next)

    // Immediately open editor for the newly added block
    setEditingIndex(idx)
    setEditingBlock(block)
    setEditingBlockType(block.type)
    setEditorOpen(true)

    setInsertIndex(null)
  }, [blocks, onChange, insertIndex])

  const handleEditBlock = useCallback((index: number) => {
    const block = blocks[index]
    if (!block) return
    setEditingIndex(index)
    setEditingBlock(block)
    setEditingBlockType(block.type)
    setEditorOpen(true)
  }, [blocks])

  const handleEditorSave = useCallback((data: any) => {
    if (editingIndex === null || !editingBlock) return
    const updatedBlock: Block = { id: editingBlock.id, type: editingBlock.type, data }
    const next = [...blocks]
    next[editingIndex] = updatedBlock
    onChange(next)
    setEditorOpen(false)
    setEditingIndex(null)
    setEditingBlock(null)
    setEditingBlockType(null)
  }, [editingIndex, editingBlock, blocks, onChange])

  const handleRemoveBlock = useCallback((index: number) => {
    setDeleteIndex(index)
    setDeleteDialogOpen(true)
  }, [])

  const handleConfirmDelete = useCallback(() => {
    if (deleteIndex === null) return
    onChange(blocks.filter((_, i) => i !== deleteIndex))
    setDeleteIndex(null)
    setDeleteDialogOpen(false)
  }, [deleteIndex, blocks, onChange])

  const handleMoveUp = useCallback((index: number) => {
    if (index <= 0) return
    const next = [...blocks]
    ;[next[index - 1], next[index]] = [next[index]!, next[index - 1]!]
    onChange(next)
    scrollToIndexRef.current = index - 1
  }, [blocks, onChange])

  const handleMoveDown = useCallback((index: number) => {
    if (index >= blocks.length - 1) return
    const next = [...blocks]
    ;[next[index], next[index + 1]] = [next[index + 1]!, next[index]!]
    onChange(next)
    scrollToIndexRef.current = index + 1
  }, [blocks, onChange])

  // Scroll to moved block after render
  useEffect(() => {
    if (scrollToIndexRef.current !== null) {
      const idx = scrollToIndexRef.current
      scrollToIndexRef.current = null
      requestAnimationFrame(() => {
        const el = blockRefsMap.current.get(idx)
        el?.scrollIntoView({ behavior: "smooth", block: "center" })
      })
    }
  })

  const openPickerAt = (index: number) => {
    setInsertIndex(index)
    setPickerOpen(true)
  }

  return (
    <div className="space-y-0">
      {/* Empty state */}
      {blocks.length === 0 && !readOnly && (
        <div className="flex flex-col items-center justify-center py-20">
          <button
            type="button"
            onClick={() => openPickerAt(0)}
            className="flex items-center justify-center w-14 h-14 rounded-full border-2 border-dashed border-gray-300 dark:border-gray-600 text-gray-400 hover:border-blue-400 hover:text-blue-500 dark:hover:border-blue-500 dark:hover:text-blue-400 hover:scale-110 transition-all cursor-pointer mb-4"
          >
            <Plus className="h-7 w-7" />
          </button>
          <p className="text-sm font-medium text-gray-500 dark:text-gray-400">Add your first block</p>
          <p className="text-xs text-gray-400 dark:text-gray-500 mt-1">Choose from 20 different block types</p>
        </div>
      )}

      {/* Block list with insert lines */}
      {blocks.length > 0 && (
        <>
          {/* Insert line before first block */}
          {!readOnly && <InsertLine onInsert={() => openPickerAt(0)} />}

          {blocks.map((block, index) => (
            <div key={block.id} ref={(el) => { if (el) blockRefsMap.current.set(index, el); else blockRefsMap.current.delete(index) }}>
              <BlockCard
                block={block}
                index={index}
                total={blocks.length}
                onMoveUp={() => handleMoveUp(index)}
                onMoveDown={() => handleMoveDown(index)}
                onRemove={() => handleRemoveBlock(index)}
                onEdit={() => handleEditBlock(index)}
                readOnly={readOnly}
              />
              {/* Insert line after each block */}
              {!readOnly && <InsertLine onInsert={() => openPickerAt(index + 1)} />}
            </div>
          ))}
        </>
      )}

      <BlockTypePicker
        open={pickerOpen}
        onOpenChange={setPickerOpen}
        onSelect={handleAddBlock}
      />

      <BlockEditorModal
        open={editorOpen}
        onOpenChange={(v) => {
          setEditorOpen(v)
          if (!v) {
            setEditingIndex(null)
            setEditingBlock(null)
            setEditingBlockType(null)
          }
        }}
        block={editingBlock}
        blockType={editingBlockType}
        onSave={handleEditorSave}
      />

      <DeleteConfirmDialog
        open={deleteDialogOpen}
        onOpenChange={(v) => {
          setDeleteDialogOpen(v)
          if (!v) setDeleteIndex(null)
        }}
        title="Delete Block"
        itemName={deleteIndex !== null && blocks[deleteIndex] ? BLOCK_REGISTRY[blocks[deleteIndex]!.type]?.label ?? "Block" : "Block"}
        itemType="block"
        onConfirm={handleConfirmDelete}
      />
    </div>
  )
}
