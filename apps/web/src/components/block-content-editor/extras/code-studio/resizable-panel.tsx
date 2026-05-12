"use client"

import { useState, useRef, useEffect } from "react"
import { GripVertical, X } from "lucide-react"
import { cn } from "@/lib/utils"
import type { PanelConfig } from "./types"

interface ResizablePanelProps {
  panel: PanelConfig
  allPanels: PanelConfig[]
  isEditMode: boolean
  gridContainerRef?: React.RefObject<HTMLDivElement | null>
  gridCols: number
  gridRows: number
  onResize?: (panelId: string, row: number, col: number, rowSpan: number, colSpan: number) => void
  onMove?: (panelId: string, row: number, col: number) => void
  onRemove?: (panelId: string) => void
  onDragStart?: (panelId: string) => void
  onDragEnd?: () => void
  children: React.ReactNode
}

export function ResizablePanel({
  panel,
  allPanels,
  isEditMode,
  gridContainerRef,
  gridCols,
  gridRows,
  onResize,
  onMove,
  onRemove,
  onDragStart,
  onDragEnd,
  children,
}: ResizablePanelProps) {
  const [isResizing, setIsResizing] = useState<'n' | 's' | 'e' | 'w' | 'ne' | 'nw' | 'se' | 'sw' | null>(null)
  const [isDragging, setIsDragging] = useState(false)
  const panelRef = useRef<HTMLDivElement>(null)
  const startPosRef = useRef({ x: 0, y: 0, row: 0, col: 0, rowSpan: 0, colSpan: 0 })

  // Verificar se há colisão com outros painéis
  const hasCollision = (row: number, col: number, rowSpan: number, colSpan: number): boolean => {
    const endRow = row + rowSpan
    const endCol = col + colSpan

    return allPanels.some(otherPanel => {
      // Não verificar colisão com o próprio painel
      if (otherPanel.id === panel.id) return false

      const otherEndRow = otherPanel.row + otherPanel.rowSpan
      const otherEndCol = otherPanel.col + otherPanel.colSpan

      // Verificar se há sobreposição
      const rowOverlap = row < otherEndRow && endRow > otherPanel.row
      const colOverlap = col < otherEndCol && endCol > otherPanel.col

      return rowOverlap && colOverlap
    })
  }

  // Calcular célula do grid baseado na posição do mouse
  const getCellFromMousePosition = (clientX: number, clientY: number): { row: number; col: number } => {
    if (!gridContainerRef?.current) return { row: 0, col: 0 }
    
    const rect = gridContainerRef.current.getBoundingClientRect()
    const x = clientX - rect.left
    const y = clientY - rect.top
    
    const cellWidth = rect.width / gridCols
    const cellHeight = rect.height / gridRows
    
    const col = Math.floor(x / cellWidth)
    const row = Math.floor(y / cellHeight)
    
    return {
      row: Math.max(0, Math.min(gridRows - 1, row)),
      col: Math.max(0, Math.min(gridCols - 1, col)),
    }
  }

  // Drag para mover painel
  const handleDragStart = (e: React.MouseEvent) => {
    if (!isEditMode || isResizing) return
    
    // Não iniciar drag se o clique foi em um elemento com data-no-drag
    const target = e.target as HTMLElement
    if (target.closest('[data-no-drag="true"]')) {
      return
    }
    
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
  const handleResizeStart = (e: React.MouseEvent, direction: 'n' | 's' | 'e' | 'w' | 'ne' | 'nw' | 'se' | 'sw') => {
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
        const maxRow = Math.min(row, gridRows - panel.rowSpan)
        const maxCol = Math.min(col, gridCols - panel.colSpan)
        
        if (maxRow !== panel.row || maxCol !== panel.col) {
          onMove?.(panel.id, maxRow, maxCol)
        }
      } else if (isResizing && panelRef.current) {
        const gridRect = panelRef.current.parentElement?.getBoundingClientRect()
        if (!gridRect) return

        const cellWidth = gridRect.width / gridCols
        const cellHeight = gridRect.height / gridRows

        const deltaX = e.clientX - startPosRef.current.x
        const deltaY = e.clientY - startPosRef.current.y

        let newRow = startPosRef.current.row
        let newCol = startPosRef.current.col
        let newRowSpan = startPosRef.current.rowSpan
        let newColSpan = startPosRef.current.colSpan

        // Redimensionar pela direita (east)
        if (isResizing === 'e' || isResizing === 'ne' || isResizing === 'se') {
          const colDelta = Math.round(deltaX / cellWidth)
          newColSpan = Math.max(1, Math.min(gridCols - startPosRef.current.col, startPosRef.current.colSpan + colDelta))
        }

        // Redimensionar pela esquerda (west)
        if (isResizing === 'w' || isResizing === 'nw' || isResizing === 'sw') {
          const colDelta = Math.round(deltaX / cellWidth)
          const potentialNewCol = Math.max(0, startPosRef.current.col + colDelta)
          const potentialNewColSpan = startPosRef.current.colSpan - colDelta
          
          if (potentialNewColSpan >= 1 && potentialNewCol + potentialNewColSpan <= gridCols) {
            newCol = potentialNewCol
            newColSpan = potentialNewColSpan
          }
        }

        // Redimensionar por baixo (south)
        if (isResizing === 's' || isResizing === 'se' || isResizing === 'sw') {
          const rowDelta = Math.round(deltaY / cellHeight)
          newRowSpan = Math.max(1, Math.min(gridRows - startPosRef.current.row, startPosRef.current.rowSpan + rowDelta))
        }

        // Redimensionar por cima (north)
        if (isResizing === 'n' || isResizing === 'ne' || isResizing === 'nw') {
          const rowDelta = Math.round(deltaY / cellHeight)
          const potentialNewRow = Math.max(0, startPosRef.current.row + rowDelta)
          const potentialNewRowSpan = startPosRef.current.rowSpan - rowDelta
          
          if (potentialNewRowSpan >= 1 && potentialNewRow + potentialNewRowSpan <= gridRows) {
            newRow = potentialNewRow
            newRowSpan = potentialNewRowSpan
          }
        }

        // Apenas aplicar resize se não houver colisão
        if ((newRow !== panel.row || newCol !== panel.col || newColSpan !== panel.colSpan || newRowSpan !== panel.rowSpan) &&
            !hasCollision(newRow, newCol, newRowSpan, newColSpan)) {
          onResize?.(panel.id, newRow, newCol, newRowSpan, newColSpan)
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
      <div className={cn("flex-1 min-h-0 w-full overflow-hidden", isEditMode && "pt-6")}>
        {children}
      </div>

      {/* Resize handles */}
      {isEditMode && (
        <>
          {/* North handle */}
          <div
            className="absolute top-0 left-0 right-0 h-1 cursor-ns-resize bg-blue-500/0 hover:bg-blue-500/50 transition-colors z-20"
            onMouseDown={(e) => handleResizeStart(e, 'n')}
          />

          {/* South handle */}
          <div
            className="absolute bottom-0 left-0 right-0 h-1 cursor-ns-resize bg-blue-500/0 hover:bg-blue-500/50 transition-colors z-20"
            onMouseDown={(e) => handleResizeStart(e, 's')}
          />

          {/* East handle */}
          <div
            className="absolute top-0 right-0 bottom-0 w-1 cursor-ew-resize bg-blue-500/0 hover:bg-blue-500/50 transition-colors z-20"
            onMouseDown={(e) => handleResizeStart(e, 'e')}
          />

          {/* West handle */}
          <div
            className="absolute top-0 left-0 bottom-0 w-1 cursor-ew-resize bg-blue-500/0 hover:bg-blue-500/50 transition-colors z-20"
            onMouseDown={(e) => handleResizeStart(e, 'w')}
          />

          {/* Northwest corner handle */}
          <div
            className="absolute top-0 left-0 w-3 h-3 cursor-nwse-resize bg-blue-500/0 hover:bg-blue-500/70 transition-colors z-30"
            onMouseDown={(e) => handleResizeStart(e, 'nw')}
          >
            <div className="absolute top-0 left-0 w-2 h-2 border-l-2 border-t-2 border-blue-500" />
          </div>

          {/* Northeast corner handle */}
          <div
            className="absolute top-0 right-0 w-3 h-3 cursor-nesw-resize bg-blue-500/0 hover:bg-blue-500/70 transition-colors z-30"
            onMouseDown={(e) => handleResizeStart(e, 'ne')}
          >
            <div className="absolute top-0 right-0 w-2 h-2 border-r-2 border-t-2 border-blue-500" />
          </div>

          {/* Southwest corner handle */}
          <div
            className="absolute bottom-0 left-0 w-3 h-3 cursor-nesw-resize bg-blue-500/0 hover:bg-blue-500/70 transition-colors z-30"
            onMouseDown={(e) => handleResizeStart(e, 'sw')}
          >
            <div className="absolute bottom-0 left-0 w-2 h-2 border-l-2 border-b-2 border-blue-500" />
          </div>

          {/* Southeast corner handle */}
          <div
            className="absolute bottom-0 right-0 w-3 h-3 cursor-nwse-resize bg-blue-500/0 hover:bg-blue-500/70 transition-colors z-30"
            onMouseDown={(e) => handleResizeStart(e, 'se')}
          >
            <div className="absolute bottom-0 right-0 w-2 h-2 border-r-2 border-b-2 border-blue-500" />
          </div>
        </>
      )}
    </div>
  )
}
