"use client"

import { ChevronLeft, ChevronRight } from "lucide-react"

interface CollapsedPanelProps {
  panelId: string
  blockIds: string[]
  isFirstPanel: boolean
  onTabClick: (panelId: string, blockId: string) => void
  onToggleCollapse: (panelId: string) => void
}

export function CollapsedPanel({ 
  panelId, 
  blockIds, 
  isFirstPanel,
  onTabClick, 
  onToggleCollapse 
}: CollapsedPanelProps) {
  return (
    <div className="w-12 bg-gray-100 dark:bg-gray-800 border-r border-gray-200 dark:border-gray-700 flex flex-col h-full">
      <div className="flex-1 overflow-y-auto py-2">
        {blockIds.map(blockId => (
          <button
            key={blockId}
            onClick={() => onTabClick(panelId, blockId)}
            className="w-full px-2 py-3 text-xs font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors writing-mode-vertical transform rotate-180"
            style={{ writingMode: 'vertical-rl' }}
          >
            Block {parseInt(blockId.slice(1))}
          </button>
        ))}
      </div>
      <button
        onClick={() => onToggleCollapse(panelId)}
        className="w-full p-2 text-gray-500 hover:text-gray-700 dark:hover:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors border-t border-gray-200 dark:border-gray-700"
      >
        {isFirstPanel ? <ChevronRight className="h-4 w-4 mx-auto" /> : <ChevronLeft className="h-4 w-4 mx-auto" />}
      </button>
    </div>
  )
}
