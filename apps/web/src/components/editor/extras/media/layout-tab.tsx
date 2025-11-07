"use client"

import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import type { BaseMediaData } from "@/components/editor/nodes/base/media-node-base"

interface LayoutTabProps {
  items: BaseMediaData[]
  onItemsChange: (items: BaseMediaData[]) => void
  columns: number
  onColumnsChange: (columns: number) => void
}

export function LayoutTab({ items, onItemsChange, columns, onColumnsChange }: LayoutTabProps) {
  const handleDragStart = (e: React.DragEvent, index: number) => {
    e.dataTransfer.setData("draggedIndex", index.toString())
  }

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault()
  }

  const handleDrop = (e: React.DragEvent, targetIndex: number) => {
    e.preventDefault()
    const draggedIndex = parseInt(e.dataTransfer.getData("draggedIndex"))
    
    if (draggedIndex === targetIndex) return

    const newItems = [...items]
    const [draggedItem] = newItems.splice(draggedIndex, 1)
    if (draggedItem) {
      newItems.splice(targetIndex, 0, draggedItem)
      onItemsChange(newItems)
    }
  }

  const handleSizeChange = (index: number, newSize: number) => {
    const newItems = [...items]
    const item = newItems[index]
    if (item) {
      newItems[index] = { ...item, size: newSize }
      onItemsChange(newItems)
    }
  }

  return (
    <div className="space-y-6">
      {/* Column Selection */}
      <div className="space-y-2">
        <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
          Number of Columns
        </Label>
        <Select value={columns.toString()} onValueChange={(value) => onColumnsChange(parseInt(value))}>
          <SelectTrigger className="w-full">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="1">1 Column</SelectItem>
            <SelectItem value="2">2 Columns</SelectItem>
            <SelectItem value="3">3 Columns</SelectItem>
            <SelectItem value="4">4 Columns</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {/* Layout Preview with Drag & Drop */}
      <div className="space-y-2">
        <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
          Arrange Items (Drag to reorder)
        </Label>
        
        <div 
          className="grid gap-3"
          style={{ gridTemplateColumns: `repeat(${columns}, 1fr)` }}
        >
          {items.map((item, index) => (
            <div
              key={index}
              draggable
              onDragStart={(e) => handleDragStart(e, index)}
              onDragOver={handleDragOver}
              onDrop={(e) => handleDrop(e, index)}
              className="relative group cursor-move bg-gray-100 dark:bg-gray-800 rounded-lg p-3 border-2 border-gray-200 dark:border-gray-700 hover:border-blue-500 dark:hover:border-blue-400 transition-colors"
            >
              {/* Item Preview */}
              <div className="aspect-video bg-gray-200 dark:bg-gray-700 rounded overflow-hidden mb-2">
                {item.type === "image" && item.src && (
                  <img src={item.src} alt="" className="w-full h-full object-cover" />
                )}
                {item.type !== "image" && (
                  <div className="w-full h-full flex items-center justify-center text-xs text-gray-500">
                    {item.type}
                  </div>
                )}
              </div>

              {/* Size Control */}
              <div className="space-y-1">
                <div className="flex items-center justify-between text-xs text-gray-600 dark:text-gray-400">
                  <span>Width</span>
                  <span>{item.size || 100}%</span>
                </div>
                <input
                  type="range"
                  min="25"
                  max="200"
                  value={item.size || 100}
                  onChange={(e) => handleSizeChange(index, parseInt(e.target.value))}
                  className="w-full h-2 bg-gray-200 rounded-lg appearance-none cursor-pointer dark:bg-gray-700"
                  onClick={(e) => e.stopPropagation()}
                />
              </div>

              {/* Position Badge */}
              <div className="absolute top-1 right-1 bg-blue-500 text-white text-xs px-2 py-1 rounded">
                #{index + 1}
              </div>
            </div>
          ))}
        </div>

        {items.length === 0 && (
          <div className="text-center py-8 text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-800/50 rounded-lg border-2 border-dashed">
            <p>No items to arrange</p>
            <p className="text-sm">Add media in the Media tab first</p>
          </div>
        )}
      </div>
    </div>
  )
}
