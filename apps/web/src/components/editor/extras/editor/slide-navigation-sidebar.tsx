"use client"

import { Button } from "@/components/ui/button"
import { Plus, Trash2, GripVertical, Layout, LayoutGrid } from "lucide-react"
import { useState } from "react"
import { cn } from "@/lib/utils"
import type { SlideData, SlideLayoutType } from "@/lib/storage/editor/slideshow-structure"

interface SlideNavigationSidebarProps {
  slides: SlideData[]
  currentSlideIndex: number
  onSlideSelect: (index: number) => void
  onSlideAdd: (type: SlideLayoutType, position?: number) => void
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

  return (
    <div className="w-64 h-full bg-white dark:bg-gray-900 border-r border-gray-200 dark:border-gray-800 flex flex-col">
      {/* Header */}
      <div className="p-4 border-b border-gray-200 dark:border-gray-800">
        <h3 className="text-sm font-semibold text-gray-700 dark:text-gray-300 mb-3">
          Slides ({slides.length})
        </h3>
        
        {/* Add Slide Buttons */}
        <div className="flex gap-2">
          <Button
            onClick={() => onSlideAdd("single")}
            variant="outline"
            size="sm"
            className="flex-1 text-xs"
            title="Add Simple Slide"
          >
            <Layout className="h-3 w-3 mr-1" />
            Simple
          </Button>
          <Button
            onClick={() => onSlideAdd("multiple")}
            variant="outline"
            size="sm"
            className="flex-1 text-xs"
            title="Add Multi-Panel Slide"
          >
            <LayoutGrid className="h-3 w-3 mr-1" />
            Multi-Panel
          </Button>
        </div>
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
                  {/* Slide Number and Type */}
                  <div className="flex items-center justify-between mb-1">
                    <div className="flex items-center gap-2">
                      <span className="text-xs font-semibold text-gray-500 dark:text-gray-400">
                        #{index + 1}
                      </span>
                      {slide.type === "multiple" ? (
                        <LayoutGrid className="h-3 w-3 text-blue-500" />
                      ) : (
                        <Layout className="h-3 w-3 text-green-500" />
                      )}
                    </div>
                    
                    {/* Delete Button */}
                    {slides.length > 1 && (
                      <Button
                        onClick={(e) => {
                          e.stopPropagation()
                          onSlideRemove(slide.id)
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

                  {/* Slide Type Badge */}
                  <div className="mt-1">
                    <span className={cn(
                      "inline-block px-2 py-0.5 text-xs rounded",
                      slide.type === "multiple"
                        ? "bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300"
                        : "bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-300"
                    )}>
                      {slide.type === "multiple" ? "Multi-Panel" : "Simple"}
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
          onClick={() => onSlideAdd("multiple")}
          variant="outline"
          size="sm"
          className="w-full"
        >
          <Plus className="h-4 w-4 mr-2" />
          Add Slide
        </Button>
      </div>
    </div>
  )
}
