"use client"

import { useSortable } from '@dnd-kit/sortable'

interface EmptyPanelProps {
  panelId: string
  isOver: boolean
  showAddButton?: boolean
  onAddBlock?: () => void
}

export function EmptyPanel({ panelId, isOver, showAddButton, onAddBlock }: EmptyPanelProps) {
  const { setNodeRef } = useSortable({
    id: panelId,
    data: { type: 'panel' },
  })

  return (
    <div 
      ref={setNodeRef}
      className={`flex flex-col items-center justify-center h-full p-8 text-center bg-gray-50 dark:bg-gray-900 border-2 border-dashed border-gray-300 dark:border-gray-700 transition-colors m-4 rounded-lg ${
        isOver ? 'border-blue-500 bg-blue-50 dark:bg-blue-900/20' : ''
      }`}
    >
      <p className="text-sm text-gray-500 dark:text-gray-400 mb-2">Empty Panel</p>
      {showAddButton && onAddBlock && (
        <p className="text-xs text-gray-400 dark:text-gray-500">
          Drag blocks here or click Add Block to get started
        </p>
      )}
    </div>
  )
}
