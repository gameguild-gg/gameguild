"use client"

import { useState, useRef, useEffect } from "react"
import { GripVertical, X } from "lucide-react"
import { cn } from "@/lib/utils"
import type { PanelConfig } from "./types"

interface ResizablePanelProps {
  panel: PanelConfig
  isEditMode: boolean
  gridContainerRef?: React.RefObject<HTMLDivElement | null>
  onResize?: (panelId: string, row: number, col: number, rowSpan: number, colSpan: number) => void
  onMove?: (panelId: string, row: number, col: number) => void
  onRemove?: (panelId: string) => void
  onDragStart?: (panelId: string) => void
  onDragEnd?: () => void
  children: React.ReactNode
}

export function ResizablePanel({
  panel,
  isEditMode,
  gridContainerRef,
  onResize,
  onMove,
  onRemove,
  onDragStart,
  onDragEnd,
  children,
}: ResizablePanelProps) {
  const [isResizing, setIsResizing] = useState<'se' | 's' | 'e' | null>(null)
  const [isDragging, setIsDragging] = useState(false)
  const panelRef = useRef<HTMLDivElement>(null)
  const startPosRef = useRef({ x: 0, y: 0, row: 0, col: 0, rowSpan: 0, colSpan: 0 })

  // Calcular célula do grid baseado na posição do mouse
  const getCellFromMousePosition = (clientX: number, clientY: number): { row: number; col: number } => {
    if (!gridContainerRef?.current) return { row: 0, col: 0 }
    
    const rect = gridContainerRef.current.getBoundingClientRect()
    const x = clientX - rect.left
    const y = clientY - rect.top
    
    // Grid 12x12
    const cellWidth = rect.width / 12
    const cellHeight = rect.height / 12
    
    const col = Math.floor(x / cellWidth)
    const row = Math.floor(y / cellHeight)
    
    return {
      row: Math.max(0, Math.min(11, row)),
      col: Math.max(0, Math.min(11, col)),
    }
  }

  // Drag para mover painel
  const handleDragStart = (e: React.MouseEvent) => {
    if (!isEditMode || isResizing) return
    e.stopPropagation()
    
    setIsDragging(true)
    startPosRef.current = {
      x: e.clientX,
      y: e.clientY,
      row: panel.row,
      col: panel.col,
      rowSpan: panel.rowSpan,
      colSpan: panel.colSpan,
    }
    onDragStart?.(panel.id)
  }

  // Resize handlers
  const handleResizeStart = (e: React.MouseEvent, direction: 'se' | 's' | 'e') => {
    if (!isEditMode) return
    e.preventDefault()
    e.stopPropagation()
    
    setIsResizing(direction)
    startPosRef.current = {
      x: e.clientX,
      y: e.clientY,
      row: panel.row,
      col: panel.col,
      rowSpan: panel.rowSpan,
      colSpan: panel.colSpan,
    }
  }

  useEffect(() => {
    if (!isResizing && !isDragging) return

    const handleMouseMove = (e: MouseEvent) => {
      if (isDragging && gridContainerRef?.current) {
        // Calcular nova posição baseada no mouse
        const { row, col } = getCellFromMousePosition(e.clientX, e.clientY)
        
        // Garantir que o painel não saia do grid
        const maxRow = Math.min(row, 12 - panel.rowSpan)
        const maxCol = Math.min(col, 12 - panel.colSpan)
        
        if (maxRow !== panel.row || maxCol !== panel.col) {
          onMove?.(panel.id, maxRow, maxCol)
        }
      } else if (isResizing && panelRef.current) {
        const gridRect = panelRef.current.parentElement?.getBoundingClientRect()
        if (!gridRect) return

        const cellWidth = gridRect.width / 12
        const cellHeight = gridRect.height / 12

        const deltaX = e.clientX - startPosRef.current.x
        const deltaY = e.clientY - startPosRef.current.y

        let newColSpan = startPosRef.current.colSpan
        let newRowSpan = startPosRef.current.rowSpan

        if (isResizing === 'e' || isResizing === 'se') {
          const colDelta = Math.round(deltaX / cellWidth)
          newColSpan = Math.max(1, Math.min(12 - panel.col, startPosRef.current.colSpan + colDelta))
        }

        if (isResizing === 's' || isResizing === 'se') {
          const rowDelta = Math.round(deltaY / cellHeight)
          newRowSpan = Math.max(1, Math.min(12 - panel.row, startPosRef.current.rowSpan + rowDelta))
        }

        if (newColSpan !== panel.colSpan || newRowSpan !== panel.rowSpan) {
          onResize?.(panel.id, panel.row, panel.col, newRowSpan, newColSpan)
        }
      }
    }

    const handleMouseUp = () => {
      if (isDragging) {
        setIsDragging(false)
        onDragEnd?.()
      }
      setIsResizing(null)
    }

    document.addEventListener('mousemove', handleMouseMove)
    document.addEventListener('mouseup', handleMouseUp)

    return () => {
      document.removeEventListener('mousemove', handleMouseMove)
      document.removeEventListener('mouseup', handleMouseUp)
    }
  }, [isResizing, isDragging, panel, onResize, onDragEnd])

  return (
    <div
      ref={panelRef}
      className={cn(
        "relative border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 overflow-hidden transition-all flex flex-col",
        isEditMode && "ring-2 ring-blue-500/30",
        isDragging && "opacity-50 z-50 cursor-grabbing"
      )}
      style={{
        gridRow: `${panel.row + 1} / span ${panel.rowSpan}`,
        gridColumn: `${panel.col + 1} / span ${panel.colSpan}`,
        maxHeight: '100%',
      }}
      onMouseDown={handleDragStart}
    >
      {/* Header de controle quando em modo de edição */}
      {isEditMode && (
        <div className="absolute top-0 left-0 right-0 h-6 bg-blue-500/10 border-b border-blue-500/30 flex items-center justify-between px-2 z-10 cursor-grab active:cursor-grabbing">
          <div className="flex items-center gap-1 text-xs text-blue-600 dark:text-blue-400 font-medium pointer-events-none">
            <GripVertical className="h-3 w-3" />
            <span className="capitalize">{panel.type}</span>
          </div>
          
          <div className="flex items-center gap-2">
            <span className="text-[10px] text-gray-500 pointer-events-none">
              {panel.row},{panel.col} • {panel.rowSpan}×{panel.colSpan}
            </span>

            <button
              onClick={(e) => {
                e.stopPropagation()
                onRemove?.(panel.id)
              }}
              onMouseDown={(e) => e.stopPropagation()}
              className="text-red-500 hover:bg-red-500/20 rounded p-0.5"
              title="Remove panel"
            >
              <X className="h-3 w-3" />
            </button>
          </div>
        </div>
      )}

      {/* Conteúdo */}
      <div className={cn("flex-1 min-h-0 w-full overflow-hidden", isEditMode && "pt-6 pointer-events-none")}>
        {children}
      </div>

      {/* Resize handles */}
      {isEditMode && (
        <>
          {/* East handle */}
          <div
            className="absolute top-0 right-0 bottom-0 w-1 cursor-ew-resize bg-blue-500/0 hover:bg-blue-500/50 transition-colors"
            onMouseDown={(e) => handleResizeStart(e, 'e')}
          />

          {/* South handle */}
          <div
            className="absolute bottom-0 left-0 right-0 h-1 cursor-ns-resize bg-blue-500/0 hover:bg-blue-500/50 transition-colors"
            onMouseDown={(e) => handleResizeStart(e, 's')}
          />

          {/* Southeast corner handle */}
          <div
            className="absolute bottom-0 right-0 w-3 h-3 cursor-nwse-resize bg-blue-500/0 hover:bg-blue-500/70 transition-colors"
            onMouseDown={(e) => handleResizeStart(e, 'se')}
          >
            <div className="absolute bottom-0 right-0 w-2 h-2 border-r-2 border-b-2 border-blue-500" />
          </div>
        </>
      )}
    </div>
  )
}
