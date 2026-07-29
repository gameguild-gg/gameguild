"use client"

/**
 * BlockArrayEditor
 *
 * The only field implementation. Renders an ordered list of `Block`s with
 * insert seams (`InsertLine`) between them, drag-and-drop reordering, and
 * per-block toolbars (edit / delete / move up / move down).
 *
 * Insertion flow:
 *   1. User clicks a "+" insert line.
 *   2. `BlockTypePicker` opens, filtered by `FieldConfig.allowedBlockTypes`.
 *   3. On select, `BLOCK_REGISTRY[type].createEmpty()` produces a new `Block`.
 *   4. `onChange(nextBlocks)` is called \u2014 the parent (typically
 *      `useProjectStorage.setBlocks`) updates state and the debounced auto-save
 *      kicks in.
 *
 * Editing a block opens `BlockEditorModal`, which dispatches to the
 * type-specific editor in `extras/<area>/`.
 *
 * See `docs/ARCHITECTURE.md` (\"The Block Engine\").
 */

import { useState, useCallback, useRef, useEffect } from "react"
import { Plus, GripVertical, Trash2, ChevronUp, ChevronDown, Pencil } from "lucide-react"
import { BlockTypePicker } from "./block-type-picker"
import { BlockEditorModal } from "./block-editor-modal"
import { BLOCK_REGISTRY, type BlockCellType } from "./block-component-registry"
import { DeleteConfirmDialog } from "@/components/block-content-editor/extras/dialogs/delete-confirm-dialog"
import type { Block, BlockArray } from "@/components/block-content-editor/lib/storage/editor/block-structure"
import { nextBlockId } from "@/components/block-content-editor/lib/storage/editor/block-structure"
import { getProjectTypeStructure, type ProjectType } from "@/components/block-content-editor/lib/storage/editor/project-types"
import { BlockContentRenderer } from "./block-array-viewer"
import { InlineRichTextEditor } from "../../extras/rich-text/inline-rich-text-editor"
import type { RichTextData } from "../../nodes/rich-text-node"
import { DragPreview, useBlockDragDrop } from "./block-drag-drop"

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
  /** Called with new block data when the inline editor (rich-text) updates. */
  onUpdate?: (data: unknown) => void
  readOnly: boolean
  /**
   * When true, the block header hides the move/remove buttons
   * (single-block document mode). The edit button remains.
   */
  hideRemove?: boolean
  onDragStart?: () => void
  onDragEnd?: () => void
  isDragSource?: boolean
  /** Called during drag with the insertion index (before=index, after=index+1) */
  onDragHover?: (insertIndex: number) => void
  onDropHere?: () => void
}

function BlockCard({ block, index, total, onMoveUp, onMoveDown, onRemove, onEdit, onUpdate, readOnly, hideRemove, onDragStart, onDragEnd, isDragSource, onDragHover, onDropHere }: BlockCardProps) {
  const config = BLOCK_REGISTRY[block.type]

  if (!config) return null

  const Icon = config.icon

  return (
    <div
      data-block-card
      onDragOver={onDragHover ? (e) => {
        e.preventDefault()
        e.dataTransfer.dropEffect = "move"
        const rect = e.currentTarget.getBoundingClientRect()
        const midY = rect.top + rect.height / 2
        onDragHover(e.clientY < midY ? index : index + 1)
      } : undefined}
      onDrop={onDropHere ? (e) => { e.preventDefault(); onDropHere() } : undefined}
      className={`group/card rounded-lg border bg-white dark:bg-gray-900 overflow-hidden transition-all duration-300 ${
        isDragSource
          ? "border-dashed border-blue-300 dark:border-blue-600 opacity-30"
          : "border-gray-200 dark:border-gray-700 shadow-sm hover:shadow-md"
      }`}
    >
      {/* Header bar */}
      <div className="flex items-center gap-2 px-3 py-2 bg-gray-50 dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700">
        {/* Grip handle */}
        {!readOnly && !hideRemove && (
          <div
            draggable
            onDragStart={(e) => {
              const card = e.currentTarget.closest("[data-block-card]") as HTMLElement
              if (card) e.dataTransfer.setDragImage(card, card.offsetWidth / 2, 20)
              e.dataTransfer.effectAllowed = "move"
              onDragStart?.()
            }}
            onDragEnd={() => onDragEnd?.()}
            className="shrink-0 cursor-grab active:cursor-grabbing"
          >
            <GripVertical className="h-4 w-4 text-gray-300 dark:text-gray-600" />
          </div>
        )}

        {/* Type icon + label */}
        <div className="flex items-center gap-1.5 min-w-0">
          <Icon className="h-4 w-4 text-gray-500 dark:text-gray-400 shrink-0" />
          <span className="text-xs font-medium text-gray-600 dark:text-gray-300 truncate">{config.label}</span>
        </div>

        {/* Index badge (hidden in single-block mode) */}
        {!hideRemove && (
          <span className="text-[11px] font-mono text-gray-400 dark:text-gray-500 bg-gray-100 dark:bg-gray-700 px-1.5 py-0.5 rounded shrink-0">
            #{index + 1}
          </span>
        )}

        {/* Spacer */}
        <div className="flex-1" />

        {/* Action buttons */}
        {!readOnly && (
          <div className="flex items-center gap-0.5">
            {/* Edit button — opens the focused modal editor */}
            <button
              type="button"
              onClick={onEdit}
              className="p-1 rounded text-blue-500 hover:text-blue-700 hover:bg-blue-50 dark:text-blue-400 dark:hover:text-blue-300 dark:hover:bg-blue-950/40 transition-colors"
              title="Open focused editor"
            >
              <Pencil className="h-3.5 w-3.5" />
            </button>
            {/* Move/delete — visible on hover; hidden in single-block mode */}
            {!hideRemove && (
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
            )}
          </div>
        )}
      </div>

      {/* Content area */}
      {block.type === "rich-text" ? (
        <InlineRichTextEditor
          data={block.data as RichTextData}
          readOnly={readOnly}
          onChange={(data) => onUpdate?.(data)}
        />
      ) : (
        <div className="p-4">
          <div className="prose prose-sm dark:prose-invert max-w-none">
            <BlockContentRenderer block={block} />
          </div>
        </div>
      )}
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
  allowedBlockTypes?: import("@/components/block-content-editor/lib/storage/editor/block-structure").BlockCellType[]
  /** High-level editor structure, used by direct BlockArrayEditor consumers. */
  projectType?: ProjectType
  /** Which tab to show by default in the picker */
  defaultPickerTab?: "blocks" | "templates"
  /** Hide the Block Types tab in the picker */
  hideBlockTypesTab?: boolean
  /**
   * Mode "single block document": hides the insertion lines, the
   * empty state, and the move/remove buttons of the single block.
   * The auto-create of the block is done in `EditorField`.
   */
  singleBlockMode?: boolean
  /** Called when drag state changes (for parent zoom) */
  onDragStateChange?: (dragging: boolean) => void
}

export function BlockArrayEditor({ blocks, onChange, readOnly = false, allowedBlockTypes, projectType, defaultPickerTab, hideBlockTypesTab, singleBlockMode, onDragStateChange }: BlockArrayEditorProps) {
  const [pickerOpen, setPickerOpen] = useState(false)
  const [insertIndex, setInsertIndex] = useState<number | null>(null)
  const projectTypeStructure = projectType ? getProjectTypeStructure(projectType) : {}
  const effectiveAllowedBlockTypes = allowedBlockTypes ?? projectTypeStructure.allowedBlockTypes
  const effectiveSingleBlockMode = singleBlockMode ?? projectTypeStructure.singleBlockMode
  const isQuizMode = projectType === "quiz"
  const hasRestrictedBlockTypes = !!effectiveAllowedBlockTypes && effectiveAllowedBlockTypes.length <= 1
  const effectiveHideBlockTypesTab = hideBlockTypesTab ?? (hasRestrictedBlockTypes || (isQuizMode && !effectiveAllowedBlockTypes?.length))
  const effectiveDefaultPickerTab = defaultPickerTab ?? (effectiveHideBlockTypesTab || isQuizMode ? "templates" : "blocks")

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

  // Drag-and-drop (extracted to useBlockDragDrop)
  const drag = useBlockDragDrop({ blocks, onChange, onDragStateChange, scrollToIndexRef })



  const handleAddBlock = useCallback((factory: (id: string) => Block) => {
    const idx = insertIndex ?? blocks.length
    const block = factory(nextBlockId(blocks))
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

  const handleInlineBlockUpdate = useCallback((index: number, data: unknown) => {
    const current = blocks[index]
    if (!current) return
    const next = [...blocks]
    next[index] = { id: current.id, type: current.type, data } as Block
    onChange(next)
  }, [blocks, onChange])

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
      // Delay scroll to let any scale transition finish (300ms in editor-field)
      setTimeout(() => {
        requestAnimationFrame(() => {
          const el = blockRefsMap.current.get(idx)
          el?.scrollIntoView({ behavior: "smooth", block: "center" })
        })
      }, 150)
    }
  })

  const openPickerAt = (index: number) => {
    setInsertIndex(index)
    setPickerOpen(true)
  }

  return (
    <div className="space-y-0">
      {/* Empty state (oculto em modo single-block; EditorField auto-cria o bloco) */}
      {blocks.length === 0 && !readOnly && !effectiveSingleBlockMode && (
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

      {/* Block list with insert lines / drag preview */}
      {blocks.length > 0 && (
        <div
          ref={drag.containerRef}
          onDragOver={drag.isDragging ? drag.handleContainerDragOver : undefined}
          onDragLeave={drag.isDragging ? drag.handleContainerDragLeave : undefined}
        >
          {/* Insert line before first block (normal mode) */}
          {!readOnly && !drag.isDragging && !effectiveSingleBlockMode && <InsertLine onInsert={() => openPickerAt(0)} />}

          {/* Drag preview before first block */}
          {drag.isDragging && drag.dropTargetIndex === 0 && drag.dragIndex !== null && (
            <DragPreview onDragOver={drag.handleContainerDragOver} onDrop={() => drag.handleDragEnd()} />
          )}

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
                onUpdate={(data) => handleInlineBlockUpdate(index, data)}
                readOnly={readOnly}
                hideRemove={effectiveSingleBlockMode}
                onDragStart={() => drag.handleDragStart(index)}
                onDragEnd={drag.handleDragEnd}
                isDragSource={drag.dragIndex === index}
                onDragHover={drag.isDragging ? (targetIdx) => drag.setDropTargetIndex(targetIdx) : undefined}
                onDropHere={drag.isDragging ? () => drag.handleDragEnd() : undefined}
              />

              {/* Drag preview after this block */}
              {drag.isDragging && drag.dropTargetIndex === index + 1 && drag.dragIndex !== null && (
                <DragPreview onDragOver={drag.handleContainerDragOver} onDrop={() => drag.handleDragEnd()} />
              )}

              {/* Insert line after each block (normal mode; oculto em single-block) */}
              {!readOnly && !drag.isDragging && !effectiveSingleBlockMode && <InsertLine onInsert={() => openPickerAt(index + 1)} />}
            </div>
          ))}
        </div>
      )}

      <BlockTypePicker
        open={pickerOpen}
        onOpenChange={setPickerOpen}
        onSelect={handleAddBlock}
        allowedBlockTypes={effectiveAllowedBlockTypes}
        defaultTab={effectiveDefaultPickerTab}
        hideBlockTypesTab={effectiveHideBlockTypesTab}
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
