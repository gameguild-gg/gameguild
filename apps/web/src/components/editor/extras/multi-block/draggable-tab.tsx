"use client"

import { useSortable } from '@dnd-kit/sortable'
import { CSS } from '@dnd-kit/utilities'
import { GripVertical } from "lucide-react"

interface DraggableTabProps {
  blockId: string
  isDragging: boolean
}

export function DraggableTab({ blockId, isDragging }: DraggableTabProps) {
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
    <div 
      ref={setNodeRef}
      style={style}
      className={`flex items-center gap-2 text-sm font-medium text-gray-700 dark:text-gray-300 cursor-grab active:cursor-grabbing ${
        isDragging ? 'opacity-50' : ''
      }`}
      {...attributes}
      {...listeners}
    >
      <GripVertical className="h-4 w-4 text-gray-400" />
      <span>Block {parseInt(blockId.slice(1))}</span>
    </div>
  )
}
