"use client"

import { Button } from "@/components/ui/button"
import { Plus, Trash2, GripVertical, Layers } from "lucide-react"
import { useState } from "react"
import { cn } from "@/lib/utils"
import type { SlideData } from "@/lib/storage/editor/slideshow-structure"
import { DeleteConfirmDialog } from "@/components/editor/extras/dialogs/delete-confirm-dialog"

interface SlideNavigationSidebarProps {
  slides: SlideData[]
  currentSlideIndex: number
  onSlideSelect: (index: number) => void
  onSlideAdd: () => void
  onSlideRemove: (slideId: string) => void
  onSlideReorder: (fromIndex: number, toIndex: number) => void
  onSlideNameChange: (slideId: string, name: string) => void
}

export function SlideNavigationSidebar({
  slides,
  currentSlideIndex,
  onSlideSelect,
  onSlideAdd,
  onSlideRemove,
  onSlideReorder,
  onSlideNameChange,
}: SlideNavigationSidebarProps) {
  const [draggedIndex, setDraggedIndex] = useState<number | null>(null)
  const [editingSlideId, setEditingSlideId] = useState<string | null>(null)
  const [editingName, setEditingName] = useState("")
  const [deleteConfirm, setDeleteConfirm] = useState<{ open: boolean; slideId: string | null; slideName: string }>({
    open: false,
    slideId: null,
    slideName: ""
  })

  const handleDragStart = (index: number) => {
    setDraggedIndex(index)
  }

  const handleDragOver = (e: React.DragEvent, index: number) => {
    e.preventDefault()
    if (draggedIndex === null || draggedIndex === index) return
    
    onSlideReorder(draggedIndex, index)
    setDraggedIndex(index)
  }

  const handleDragEnd = () => {
    setDraggedIndex(null)
  }

  const handleNameEdit = (slide: SlideData, index: number) => {
    setEditingSlideId(slide.id)
    setEditingName(slide.name || `Slide ${index + 1}`)
  }

  const handleNameSave = (slideId: string) => {
    if (editingName.trim()) {
      onSlideNameChange(slideId, editingName.trim())
    }
    setEditingSlideId(null)
  }

  const handleNameCancel = () => {
    setEditingSlideId(null)
    setEditingName("")
  }

  const handleDeleteClick = (slide: SlideData, index: number) => {
    setDeleteConfirm({
      open: true,
      slideId: slide.id,
      slideName: slide.name || `Slide ${index + 1}`
    })
  }

  const confirmDelete = () => {
    if (deleteConfirm.slideId) {
      onSlideRemove(deleteConfirm.slideId)
    }
    setDeleteConfirm({ open: false, slideId: null, slideName: "" })
  }

  return (
    <div className="w-64 h-full bg-white dark:bg-gray-900 border-r border-gray-200 dark:border-gray-800 flex flex-col">
      {/* Header */}
      <div className="p-4 border-b border-gray-200 dark:border-gray-800">
        <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
          Slides ({slides.length})
        </h3>
        
        {/* Add Slide Button */}
        <Button
          onClick={() => onSlideAdd()}
          variant="outline"
          size="sm"
          className="w-full"
          title="Add New Slide"
        >
          <Plus className="h-3 w-3 mr-1" />
          Add Slide
        </Button>
      </div>

      {/* Slides List */}
      <div className="flex-1 overflow-y-auto p-2">
        {slides.length === 0 ? (
          <div className="text-center py-8 text-sm text-gray-500">
            No slides yet. Add one to get started.
          </div>
        ) : (
          <div className="space-y-2">
            {slides.map((slide, index) => (
              <div
                key={slide.id}
                draggable
                onDragStart={() => handleDragStart(index)}
                onDragOver={(e) => handleDragOver(e, index)}
                onDragEnd={handleDragEnd}
                onClick={() => onSlideSelect(index)}
                className={cn(
                  "group relative p-3 rounded-lg border-2 cursor-pointer transition-all",
                  currentSlideIndex === index
                    ? "border-blue-500 bg-blue-50 dark:bg-blue-900/20"
                    : "border-gray-200 dark:border-gray-700 hover:border-gray-300 dark:hover:border-gray-600",
                  draggedIndex === index && "opacity-50"
                )}
              >
                {/* Drag Handle */}
                <div className="absolute left-1 top-1/2 -translate-y-1/2 opacity-0 group-hover:opacity-100 transition-opacity cursor-grab active:cursor-grabbing">
                  <GripVertical className="h-4 w-4 text-gray-400" />
                </div>

                <div className="pl-6">
                  {/* Slide Number */}
                  <div className="flex items-center justify-between mb-1">
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-semibold text-gray-500 dark:text-gray-400">
                        #{index + 1}
                      </span>
                      <Layers className="h-3 w-3 text-blue-500" />
                    </div>
                    
                    {/* Delete Button */}
                    {slides.length > 1 && (
                      <Button
                        onClick={(e) => {
                          e.stopPropagation()
                          handleDeleteClick(slide, index)
                        }}
                        variant="ghost"
                        size="sm"
                        className="h-6 w-6 p-0 opacity-0 group-hover:opacity-100 transition-opacity"
                        title="Remove Slide"
                      >
                        <Trash2 className="h-3 w-3 text-red-500" />
                      </Button>
                    )}
                  </div>

                  {/* Slide Name (Editable) */}
                  {editingSlideId === slide.id ? (
                    <input
                      type="text"
                      value={editingName}
                      onChange={(e) => setEditingName(e.target.value)}
                      onBlur={() => handleNameSave(slide.id)}
                      onKeyDown={(e) => {
                        if (e.key === "Enter") handleNameSave(slide.id)
                        if (e.key === "Escape") handleNameCancel()
                      }}
                      onClick={(e) => e.stopPropagation()}
                      className="w-full px-2 py-1 text-xs border border-blue-500 rounded bg-white dark:bg-gray-800 focus:outline-none"
                      autoFocus
                    />
                  ) : (
                    <div
                      onDoubleClick={(e) => {
                        e.stopPropagation()
                        handleNameEdit(slide, index)
                      }}
                      className="text-sm font-medium text-gray-700 dark:text-gray-300 truncate"
                      title={slide.name || `Slide ${index + 1}`}
                    >
                      {slide.name || `Slide ${index + 1}`}
                    </div>
                  )}

                  {/* Block Count Badge */}
                  <div className="mt-1">
                    <span className="inline-block px-2 py-0.5 text-xs rounded bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400">
                      {Object.keys(slide.blocks || {}).length} block{Object.keys(slide.blocks || {}).length !== 1 ? 's' : ''}
                    </span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Footer with Add Button */}
      <div className="p-3 border-t border-gray-200 dark:border-gray-800">
        <Button
          onClick={() => onSlideAdd()}
          variant="outline"
          size="sm"
          className="w-full"
        >
          <Plus className="h-4 w-4 mr-2" />
          Add Slide
        </Button>
      </div>

      {/* Delete Confirmation Dialog */}
      <DeleteConfirmDialog
        open={deleteConfirm.open}
        onOpenChange={(open) => setDeleteConfirm({ open, slideId: null, slideName: "" })}
        title="Remove Slide"
        itemName={deleteConfirm.slideName}
        itemType="slide"
        onConfirm={confirmDelete}
      />
    </div>
  )
}
