"use client"

import { useState } from "react"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Button } from "@/components/ui/button"
import { Plus, Lock, Unlock, X, MoreVertical } from "lucide-react"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import type { BaseMediaData } from "@/components/block-content-editor/nodes/base/media-node-base"
import { AssetImage } from "./asset-image"

interface LayoutTabProps {
  items: BaseMediaData[]
  onItemsChange: (items: BaseMediaData[]) => void
  columns: number
  onColumnsChange: (columns: number) => void
}

export function LayoutTab({ items, onItemsChange, columns, onColumnsChange }: LayoutTabProps) {
  const [draggedIndex, setDraggedIndex] = useState<number | null>(null)
  const [dragOverIndex, setDragOverIndex] = useState<number | null>(null)
  const [hasDropped, setHasDropped] = useState(false)

  // Calculate total grid slots - always show at least one empty row below content
  const minSlots = columns * 2
  const currentRows = Math.ceil(items.length / columns)
  const totalSlots = Math.max(minSlots, (currentRows + 1) * columns) // +1 row for empty space
  
  // ARRAY 1: Static items map (position -> item)
  const staticItemsMap = new Map<number, BaseMediaData>()
  items.forEach(item => {
    if (item.isStatic && item.gridPosition !== undefined) {
      staticItemsMap.set(item.gridPosition, item)
    }
  })
  
  // ARRAY 2: Dynamic items (ordered list of non-static items)
  const dynamicItems = items.filter(item => !item.isStatic || item.gridPosition === undefined)
  
  // ARRAY 3: Final grid (combines static at fixed positions + dynamic in remaining slots)
  const gridSlots: (BaseMediaData | null)[] = Array(totalSlots).fill(null)
  
  // Place static items at their fixed positions
  staticItemsMap.forEach((item, position) => {
    if (position < totalSlots) {
      gridSlots[position] = item
    }
  })
  
  // Fill remaining slots with dynamic items in order
  let dynamicIndex = 0
  for (let i = 0; i < totalSlots && dynamicIndex < dynamicItems.length; i++) {
    if (gridSlots[i] === null) {
      const item = dynamicItems[dynamicIndex]
      if (item) {
        gridSlots[i] = item
      }
      dynamicIndex++
    }
  }
  
  // Helper: Get list of static items for display
  const staticItems = Array.from(staticItemsMap.values())

  const handleDragStart = (e: React.DragEvent, index: number) => {
    const item = gridSlots[index]
    if (!item || item.isStatic) {
      e.preventDefault()
      return
    }
    e.dataTransfer.effectAllowed = "move"
    setDraggedIndex(index)
    setHasDropped(false) // Reset drop flag
  }

  const handleDragEnd = () => {
    // If drag ended without a successful drop, just clear states (cancel)
    setDraggedIndex(null)
    setDragOverIndex(null)
    setHasDropped(false)
  }

  const handleDragOver = (e: React.DragEvent, index: number) => {
    e.preventDefault()
    
    const targetItem = gridSlots[index]
    // Don't allow dropping on static items
    if (targetItem?.isStatic) {
      e.dataTransfer.dropEffect = "none"
      return
    }
    
    e.dataTransfer.dropEffect = "move"
    
    if (draggedIndex !== null && draggedIndex !== index) {
      setDragOverIndex(index)
    }
  }

  const handleDragLeave = () => {
    setDragOverIndex(null)
  }

  const handleDrop = (e: React.DragEvent, targetIndex: number) => {
    e.preventDefault()
    e.stopPropagation() // Prevent event bubbling
    
    if (draggedIndex === null || draggedIndex === targetIndex) {
      setDragOverIndex(null)
      setHasDropped(true) // Mark as dropped (even if same position)
      return
    }

    const targetItem = gridSlots[targetIndex]
    if (targetItem?.isStatic) {
      setDragOverIndex(null)
      setHasDropped(true)
      return
    }

    const draggedItem = gridSlots[draggedIndex]
    
    if (!draggedItem) {
      setDragOverIndex(null)
      setHasDropped(true)
      return
    }

    // Reconstruct items array using the 3-array approach
    const newStaticMap = new Map(staticItemsMap) // Copy static map
    const newDynamicItems = [...dynamicItems] // Copy dynamic list
    
    // Create temp grid to perform swap
    const tempGrid: (BaseMediaData | null)[] = Array(totalSlots).fill(null)
    
    // Place static items at their fixed positions
    newStaticMap.forEach((item, position) => {
      if (position < totalSlots) {
        tempGrid[position] = item
      }
    })
    
    // Fill remaining slots with dynamic items
    let dynIdx = 0
    for (let i = 0; i < totalSlots; i++) {
      if (tempGrid[i] === null && dynIdx < newDynamicItems.length) {
        const item = newDynamicItems[dynIdx]
        if (item) {
          tempGrid[i] = item
        }
        dynIdx++
      }
    }
    
    // Perform swap in tempGrid
    const temp = tempGrid[draggedIndex] ?? null
    tempGrid[draggedIndex] = tempGrid[targetIndex] ?? null
    tempGrid[targetIndex] = temp
    
    // Rebuild final items array from tempGrid, preserving static positions
    const newItems: BaseMediaData[] = []
    
    // For static items, preserve their gridPosition property
    // For dynamic items, add them in the order they appear in tempGrid
    tempGrid.forEach((item, index) => {
      if (item) {
        if (newStaticMap.has(index)) {
          // This is a static item - ensure gridPosition is set
          newItems.push({
            ...item,
            isStatic: true,
            gridPosition: index
          })
        } else {
          // This is a dynamic item - no gridPosition
          newItems.push({
            ...item,
            isStatic: false,
            gridPosition: undefined
          })
        }
      }
    })
    
    onItemsChange(newItems)
    setHasDropped(true) // Mark as successfully dropped
    
    setDraggedIndex(null)
    setDragOverIndex(null)
  }
  
  const handleCreatePlaceholder = (index: number, isStatic: boolean) => {
    const newPlaceholder: BaseMediaData = {
      type: "image",
      src: "",
      isPlaceholder: true,
      isStatic: isStatic,
      gridPosition: isStatic ? index : undefined
    }
    
    const newItems = [...items, newPlaceholder]
    onItemsChange(newItems)
  }
  
  const handleRemoveSlot = (index: number) => {
    const itemToRemove = gridSlots[index]
    if (!itemToRemove) return
    
    const newItems = items.filter(item => item !== itemToRemove)
    onItemsChange(newItems)
  }
  
  const handleToggleStatic = (index: number) => {
    const item = gridSlots[index]
    if (!item) return
    
    const newIsStatic = !item.isStatic
    
    if (newIsStatic) {
      // Making static - just update the item with its current position
      const newItems = items.map(i => {
        if (i === item) {
          return {
            ...i,
            isStatic: true,
            gridPosition: index
          }
        }
        return i
      })
      onItemsChange(newItems)
    } else {
      // Making movable - need to rebuild array maintaining visual order
      // Create a new items array based on current grid visual order
      const newItems: BaseMediaData[] = []
      
      gridSlots.forEach((slot, slotIndex) => {
        if (slot) {
          if (slot === item) {
            // This is the item being made movable - remove static props
            newItems.push({
              ...slot,
              isStatic: false,
              gridPosition: undefined
            })
          } else {
            // Keep other items as they are
            newItems.push(slot)
          }
        }
      })
      
      onItemsChange(newItems)
    }
  }
  
  // Get visual representation during drag
  const getVisualSlots = () => {
    if (draggedIndex === null || dragOverIndex === null) {
      return gridSlots
    }
    
    const draggedItem = gridSlots[draggedIndex]
    const targetItem = gridSlots[dragOverIndex]
    
    // Don't show swap if target is static or dragged item doesn't exist
    if (targetItem?.isStatic || !draggedItem || draggedItem.isStatic) {
      return gridSlots
    }
    
    // Create visual grid using the 3-array approach
    const visual: (BaseMediaData | null)[] = Array(totalSlots).fill(null)
    
    // Place static items at their fixed positions (unchanged during drag)
    staticItemsMap.forEach((item, position) => {
      if (position < totalSlots) {
        visual[position] = item
      }
    })
    
    // Create a copy of dynamic items for swap preview
    const dynamicWithSwap = [...dynamicItems]
    
    // Find indices in dynamic array
    const draggedDynamicIndex = dynamicItems.findIndex(item => item === draggedItem)
    const targetDynamicIndex = dynamicItems.findIndex(item => item === targetItem)
    
    if (draggedDynamicIndex !== -1 && targetDynamicIndex !== -1) {
      // Swap in dynamic array
      const temp = dynamicWithSwap[draggedDynamicIndex]
      const target = dynamicWithSwap[targetDynamicIndex]
      if (temp && target) {
        dynamicWithSwap[draggedDynamicIndex] = target
        dynamicWithSwap[targetDynamicIndex] = temp
      }
    } else if (draggedDynamicIndex !== -1 && targetItem === null) {
      // Dragging to empty slot - reorder in dynamic array
      dynamicWithSwap.splice(draggedDynamicIndex, 1)
      
      // Calculate where to insert in dynamic array based on target position
      let insertIndex = 0
      for (let i = 0; i < dragOverIndex; i++) {
        if (!staticItemsMap.has(i)) {
          insertIndex++
        }
      }
      dynamicWithSwap.splice(Math.min(insertIndex, dynamicWithSwap.length), 0, draggedItem)
    }
    
    // Fill remaining slots with reordered dynamic items
    let dynIdx = 0
    for (let i = 0; i < totalSlots && dynIdx < dynamicWithSwap.length; i++) {
      if (visual[i] === null) {
        const item = dynamicWithSwap[dynIdx]
        if (item) {
          visual[i] = item
        }
        dynIdx++
      }
    }
    
    return visual
  }
  
  const visualSlots = getVisualSlots()

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
        <div className="flex items-center justify-between">
          <Label className="text-sm font-medium text-gray-700 dark:text-gray-300">
            Arrange Items (Drag to reorder)
          </Label>
          {staticItems.length > 0 && (
            <div className="flex items-center gap-1 text-xs text-amber-600 dark:text-amber-400 bg-amber-50 dark:bg-amber-900/20 px-2 py-1 rounded">
              <Lock className="h-3 w-3" />
              {staticItems.length} static {staticItems.length === 1 ? 'item' : 'items'}
            </div>
          )}
        </div>
        
        <div 
          className="grid gap-3"
          style={{ gridTemplateColumns: `repeat(${columns}, 1fr)` }}
          onDragOver={(e) => {
            // Allow drop on the grid container itself
            e.preventDefault()
          }}
          onDrop={(e) => {
            // If dropped on grid container (not on a specific slot), cancel
            e.preventDefault()
            e.stopPropagation()
            setDraggedIndex(null)
            setDragOverIndex(null)
            setHasDropped(true)
          }}
        >
          {visualSlots.map((slot, slotIndex) => {
            const isDragging = draggedIndex === slotIndex
            const isDragOver = dragOverIndex === slotIndex
            const isStatic = slot?.isStatic ?? false
            const isPlaceholder = slot?.isPlaceholder ?? false
            const hasSlot = slot !== null
            const hasContent = slot && !isPlaceholder // Real content (not placeholder)
            
            return (
              <div
                key={slotIndex}
                draggable={!!(hasSlot && !isStatic)}
                onDragStart={(e) => handleDragStart(e, slotIndex)}
                onDragEnd={handleDragEnd}
                onDragOver={(e) => handleDragOver(e, slotIndex)}
                onDragLeave={handleDragLeave}
                onDrop={(e) => handleDrop(e, slotIndex)}
                className={`
                  relative group rounded-lg border-2 transition-all duration-200
                  ${hasSlot ? (hasContent ? 'bg-gray-100 dark:bg-gray-800 p-3' : 'bg-purple-50 dark:bg-purple-900/10 p-3') : 'border-dashed bg-gray-50 dark:bg-gray-800/30'}
                  ${isDragging ? 'opacity-40 scale-95 border-blue-400 dark:border-blue-500' : 'opacity-100 scale-100'}
                  ${isDragOver && !isStatic ? 'border-blue-500 dark:border-blue-400 ring-2 ring-blue-500/50 dark:ring-blue-400/50' : 'border-gray-200 dark:border-gray-700'}
                  ${hasSlot && !isStatic && !isDragging && !isDragOver ? 'cursor-move hover:border-blue-500 dark:hover:border-blue-400' : ''}
                  ${isStatic ? 'border-amber-500 dark:border-amber-400' : ''}
                  ${isPlaceholder && !isStatic ? 'border-purple-500 dark:border-purple-400' : ''}
                  ${!hasSlot ? 'border-gray-300 dark:border-gray-600' : ''}
                `}
              >
                {/* Slot Controls Menu */}
                <div className="absolute top-1 right-1 z-10 opacity-0 group-hover:opacity-100 transition-opacity">
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button 
                        variant="ghost" 
                        size="sm" 
                        className="h-6 w-6 p-0 bg-white/90 dark:bg-gray-800/90 hover:bg-white dark:hover:bg-gray-800"
                        onClick={(e) => e.stopPropagation()}
                      >
                        <MoreVertical className="h-3 w-3" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end" className="w-48">
                      {!slot && (
                        <>
                          <DropdownMenuItem onClick={() => handleCreatePlaceholder(slotIndex, false)}>
                            <Plus className="mr-2 h-4 w-4" />
                            Create Placeholder
                          </DropdownMenuItem>
                          <DropdownMenuItem onClick={() => handleCreatePlaceholder(slotIndex, true)}>
                            <Lock className="mr-2 h-4 w-4" />
                            Create Static Placeholder
                          </DropdownMenuItem>
                        </>
                      )}
                      {slot && (
                        <>
                          <DropdownMenuItem onClick={() => handleToggleStatic(slotIndex)}>
                            {isStatic ? (
                              <>
                                <Unlock className="mr-2 h-4 w-4" />
                                Make Movable
                              </>
                            ) : (
                              <>
                                <Lock className="mr-2 h-4 w-4" />
                                Make Static
                              </>
                            )}
                          </DropdownMenuItem>
                          <DropdownMenuSeparator />
                          <DropdownMenuItem 
                            onClick={() => handleRemoveSlot(slotIndex)}
                            className="text-red-600 dark:text-red-400"
                          >
                            <X className="mr-2 h-4 w-4" />
                            Remove {isPlaceholder ? 'Placeholder' : 'Item'}
                          </DropdownMenuItem>
                        </>
                      )}
                    </DropdownMenuContent>
                  </DropdownMenu>
                </div>

                {/* Content or Empty State */}
                {hasContent ? (
                  <>
                    {/* Item Preview */}
                    <div className="aspect-video bg-gray-200 dark:bg-gray-700 rounded overflow-hidden">
                      {slot.type === "image" && slot.src && (
                        <AssetImage src={slot.src} alt="" className="w-full h-full object-cover" />
                      )}
                      {slot.type !== "image" && (
                        <div className="w-full h-full flex items-center justify-center text-xs text-gray-500">
                          {slot.type}
                        </div>
                      )}
                    </div>

                    {/* Static Badge */}
                    {isStatic && (
                      <div className="absolute top-1 left-1 bg-amber-500 text-white text-xs px-2 py-1 rounded flex items-center gap-1">
                        <Lock className="h-3 w-3" />
                        Static #{slot.gridPosition !== undefined ? slot.gridPosition + 1 : slotIndex + 1}
                      </div>
                    )}
                    
                    {/* Drag Handle Indicator */}
                    {!isDragging && !isStatic && (
                      <div className="absolute top-1 left-1 opacity-0 group-hover:opacity-100 transition-opacity">
                        <div className="bg-gray-700/80 dark:bg-gray-300/80 text-white dark:text-gray-900 text-xs px-2 py-1 rounded">
                          ⋮⋮
                        </div>
                      </div>
                    )}
                  </>
                ) : isPlaceholder ? (
                  <>
                    {/* Placeholder State */}
                    <div className="aspect-video flex items-center justify-center">
                      <div className="text-center">
                        <div className="w-12 h-12 mx-auto mb-2 rounded-lg bg-purple-100 dark:bg-purple-900/30 flex items-center justify-center">
                          {isStatic ? (
                            <Lock className="h-6 w-6 text-purple-500 dark:text-purple-400" />
                          ) : (
                            <Plus className="h-6 w-6 text-purple-500 dark:text-purple-400" />
                          )}
                        </div>
                        <p className="text-xs text-purple-600 dark:text-purple-400 font-medium">
                          {isStatic ? 'Static' : 'Movable'} Placeholder
                        </p>
                      </div>
                    </div>
                    
                    {/* Static Badge for Placeholder */}
                    {isStatic && slot && (
                      <div className="absolute top-1 left-1 bg-amber-500 text-white text-xs px-2 py-1 rounded flex items-center gap-1">
                        <Lock className="h-3 w-3" />
                        Static #{slot.gridPosition !== undefined ? slot.gridPosition + 1 : slotIndex + 1}
                      </div>
                    )}
                    
                    {/* Drag Handle Indicator for Placeholder */}
                    {!isDragging && !isStatic && (
                      <div className="absolute top-1 left-1 opacity-0 group-hover:opacity-100 transition-opacity">
                        <div className="bg-purple-700/80 dark:bg-purple-300/80 text-white dark:text-gray-900 text-xs px-2 py-1 rounded">
                          ⋮⋮
                        </div>
                      </div>
                    )}
                  </>
                ) : (
                  <>
                    {/* Empty State */}
                    <div className="aspect-video flex items-center justify-center">
                      <Plus className={`h-8 w-8 transition-colors ${
                        isDragOver 
                          ? 'text-blue-500 dark:text-blue-400' 
                          : 'text-gray-400 dark:text-gray-600'
                      }`} />
                    </div>
                  </>
                )}

                {/* Position Badge */}
                <div className="absolute bottom-1 right-1 bg-gray-700/80 dark:bg-gray-300/80 text-white dark:text-gray-900 text-xs px-2 py-0.5 rounded">
                  {slotIndex + 1}
                </div>
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}
