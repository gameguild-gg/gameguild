"use client"

import { useSortable } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { GripVertical } from "lucide-react"

interface DraggableTabButtonProps {
  blockId: string
  isActive: boolean
  isDragging: boolean
  onClick: () => void
}

export function DraggableTabButton({ 
  blockId, 
  isActive, 
  isDragging, 
  onClick 
}: DraggableTabButtonProps) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
  } = useSortable({ id: blockId })

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  }

  return (
    <button
      ref={setNodeRef}
      style={style}
      onClick={onClick}
      className={`flex items-center gap-2 px-3 py-1.5 text-sm font-medium rounded transition-colors whitespace-nowrap cursor-grab active:cursor-grabbing ${
        isActive
          ? "bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 shadow-sm"
          : "text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800"
      } ${isDragging ? 'opacity-50' : ''}`}
      {...attributes}
      {...listeners}
    >
      <GripVertical className="h-3.5 w-3.5 text-gray-400" />
      Block {parseInt(blockId.slice(1))}
    </button>
  )
}
