"use client"

import { useState, useRef, useEffect } from "react"
import { cn } from "@/lib/utils"
import type { PanelType } from "./types"

interface GridDropZoneProps {
  isActive: boolean
  onDrop: (row: number, col: number, type: PanelType) => void
  children: React.ReactNode
  gridCols: number
  gridRows: number
}

export function GridDropZone({ isActive, onDrop, children, gridCols, gridRows }: GridDropZoneProps) {
  const [dropPreview, setDropPreview] = useState<{ row: number; col: number; rowSpan: number; colSpan: number } | null>(null)
  const [draggedType, setDraggedType] = useState<PanelType | null>(null)
  const gridRef = useRef<HTMLDivElement>(null)

  const getCellFromMousePosition = (clientX: number, clientY: number): { row: number; col: number } => {
    if (!gridRef.current) return { row: 0, col: 0 }
    
    const rect = gridRef.current.getBoundingClientRect()
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

  const handleDragOver = (e: React.DragEvent) => {
    if (!isActive) return
    e.preventDefault()
    e.dataTransfer.dropEffect = 'copy'
    
    const { row, col } = getCellFromMousePosition(e.clientX, e.clientY)
    
    // Preview padrão: 4 linhas x metade das colunas (ou 8, o que for menor)
    const defaultColSpan = Math.min(8, Math.floor(gridCols / 2))
    const rowSpan = Math.min(4, gridRows - row)
    const colSpan = Math.min(defaultColSpan, gridCols - col)
    
    setDropPreview({ row, col, rowSpan, colSpan })
  }

  const handleDragLeave = (e: React.DragEvent) => {
    // Só limpar se realmente saiu do grid
    if (e.currentTarget === e.target || !e.currentTarget.contains(e.relatedTarget as Node)) {
      setDropPreview(null)
    }
  }

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault()
    
    const type = e.dataTransfer.getData('panelType') as PanelType
    if (!type) return
    
    const { row, col } = getCellFromMousePosition(e.clientX, e.clientY)
    onDrop(row, col, type)
    
    setDropPreview(null)
    setDraggedType(null)
  }

  useEffect(() => {
    const handleGlobalDragStart = (e: DragEvent) => {
      const type = (e.target as HTMLElement).getAttribute('data-panel-type') as PanelType
      if (type) {
        setDraggedType(type)
      }
    }

    const handleGlobalDragEnd = () => {
      setDropPreview(null)
      setDraggedType(null)
    }

    document.addEventListener('dragstart', handleGlobalDragStart)
    document.addEventListener('dragend', handleGlobalDragEnd)

    return () => {
      document.removeEventListener('dragstart', handleGlobalDragStart)
      document.removeEventListener('dragend', handleGlobalDragEnd)
    }
  }, [])

  return (
    <div
      ref={gridRef}
      onDragOver={handleDragOver}
      onDragLeave={handleDragLeave}
      onDrop={handleDrop}
      className="relative h-full"
    >
      {children}
      
      {/* Drop Preview */}
      {dropPreview && isActive && (
        <div
          className="absolute pointer-events-none z-50 border-2 border-dashed border-blue-500 bg-blue-500/10 rounded-lg transition-all duration-150"
          style={{
            gridRow: `${dropPreview.row + 1} / span ${dropPreview.rowSpan}`,
            gridColumn: `${dropPreview.col + 1} / span ${dropPreview.colSpan}`,
          }}
        >
          <div className="h-full flex items-center justify-center">
            <span className="text-sm font-medium text-blue-600 dark:text-blue-400 bg-white/90 dark:bg-gray-900/90 px-3 py-1.5 rounded">
              {draggedType ? `Drop ${draggedType} here` : 'Drop here'}
            </span>
          </div>
        </div>
      )}
    </div>
  )
}
