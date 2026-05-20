"use client"

import { X, File } from "lucide-react"
import { Button } from "@/components/ui/button"
import type { CodeFile, EditorInstance } from "./types"
import { cn } from "@/lib/utils"
import { useRef, useEffect, useState } from "react"

interface FileTabsProps {
  files: CodeFile[]
  openTabs: string[]
  activeFileId?: string
  editorInstance?: EditorInstance
  panelId?: string
  onSelectTab: (fileId: string) => void
  onCloseTab?: (fileId: string) => void
  onReorderTabs: (newOrder: string[], panelId?: string) => void
}

export function FileTabs({
  files,
  openTabs,
  activeFileId,
  editorInstance,
  panelId,
  onSelectTab,
  onCloseTab,
  onReorderTabs,
}: FileTabsProps) {
  const scrollContainerRef = useRef<HTMLDivElement>(null)
  const activeTabRef = useRef<HTMLDivElement>(null)
  const [draggedIndex, setDraggedIndex] = useState<number | null>(null)
  const [dragOverIndex, setDragOverIndex] = useState<number | null>(null)
  
  const openFiles = openTabs
    .map(id => files.find(f => f.id === id))
    .filter((f): f is CodeFile => f !== undefined)

  // Auto-scroll para a aba ativa quando mudar.
  // IMPORTANTE: scrollar apenas o container horizontal de tabs, sem usar
  // `Element.scrollIntoView` (que propaga o scroll para todos os ancestrais,
  // inclusive o window — isso fazia a página inteira pular para o meio do
  // documento sempre que um bloco code-studio era hidratado fora da viewport,
  // tanto no studio quanto em cada DirectSection do static-viewer).
  useEffect(() => {
    const container = scrollContainerRef.current
    const tab = activeTabRef.current
    if (!container || !tab) return

    const containerRect = container.getBoundingClientRect()
    const tabRect = tab.getBoundingClientRect()

    // Posição da aba relativa ao conteúdo rolável do container.
    const tabLeftInContainer = tab.offsetLeft
    const tabCenter = tabLeftInContainer + tab.offsetWidth / 2
    const targetScrollLeft = tabCenter - containerRect.width / 2

    const maxScrollLeft = Math.max(0, container.scrollWidth - container.clientWidth)
    const nextScrollLeft = Math.max(0, Math.min(targetScrollLeft, maxScrollLeft))

    // Só rolar se a aba está parcialmente fora horizontalmente.
    const tabFullyVisible =
      tabRect.left >= containerRect.left && tabRect.right <= containerRect.right

    if (!tabFullyVisible && Math.abs(container.scrollLeft - nextScrollLeft) > 1) {
      container.scrollTo({ left: nextScrollLeft, behavior: 'smooth' })
    }
  }, [activeFileId])

  // Scroll horizontal com roda do mouse
  useEffect(() => {
    const container = scrollContainerRef.current
    if (!container) return

    const handleWheel = (e: WheelEvent) => {
      if (e.deltaY !== 0) {
        e.preventDefault()
        container.scrollLeft += e.deltaY
      }
    }

    container.addEventListener('wheel', handleWheel, { passive: false })
    return () => container.removeEventListener('wheel', handleWheel)
  }, [])

  const handleDragStart = (e: React.DragEvent, index: number) => {
    setDraggedIndex(index)
    e.dataTransfer.effectAllowed = 'move'
  }

  const handleDragOver = (e: React.DragEvent, index: number) => {
    e.preventDefault()
    if (draggedIndex === null || draggedIndex === index) return
    setDragOverIndex(index)
  }

  const handleDragLeave = () => {
    setDragOverIndex(null)
  }

  const handleDrop = (e: React.DragEvent, dropIndex: number) => {
    e.preventDefault()
    
    if (draggedIndex === null || draggedIndex === dropIndex) {
      setDraggedIndex(null)
      setDragOverIndex(null)
      return
    }

    const newOrder = [...openTabs]
    const [movedItem] = newOrder.splice(draggedIndex, 1)
    if (movedItem) {
      newOrder.splice(dropIndex, 0, movedItem)
    }
    
    onReorderTabs(newOrder, panelId)
    setDraggedIndex(null)
    setDragOverIndex(null)
  }

  const handleDragEnd = () => {
    setDraggedIndex(null)
    setDragOverIndex(null)
  }

  return (
    <div 
      ref={scrollContainerRef}
      className="flex items-center gap-0.5 bg-gray-100 dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800 overflow-x-auto min-h-[40px] scrollbar-thin scrollbar-thumb-gray-400 dark:scrollbar-thumb-gray-600 scrollbar-track-transparent"
    >
      {/* Editor Instance Indicator */}
      {editorInstance && (
        <div className="shrink-0 px-2 py-1.5 flex items-center gap-1.5 border-r border-gray-300 dark:border-gray-700">
          <span
            className="text-xs font-bold px-1.5 py-0.5 rounded"
            style={{
              backgroundColor: editorInstance === "multiple" ? "#3b82f6" : "#6b7280",
              color: "#ffffff"
            }}
            title={editorInstance === "multiple" ? "Multiple: Shared tabs across displays" : "Unique: Independent tabs for this display"}
          >
            {editorInstance === "multiple" ? "M" : "U"}
          </span>
        </div>
      )}
      
      {openFiles.length === 0 ? (
        <div className="px-3 py-1.5 text-xs text-gray-400 dark:text-gray-600 italic">
          No files open
        </div>
      ) : (
        openFiles.map((file, index) => {
          const isActive = file.id === activeFileId
          const isDragging = draggedIndex === index
          const isDragOver = dragOverIndex === index
          
          return (
            <div
              key={file.id}
              ref={isActive ? activeTabRef : null}
              draggable
              onDragStart={(e) => handleDragStart(e, index)}
              onDragOver={(e) => handleDragOver(e, index)}
              onDragLeave={handleDragLeave}
              onDrop={(e) => handleDrop(e, index)}
              onDragEnd={handleDragEnd}
              className={cn(
                "flex items-center gap-2 px-3 py-1.5 cursor-pointer border-r border-gray-200 dark:border-gray-800 group whitespace-nowrap shrink-0 select-none transition-opacity",
                isActive
                  ? "bg-white dark:bg-gray-950 text-blue-600 dark:text-blue-400"
                  : "hover:bg-gray-200 dark:hover:bg-gray-700 text-gray-600 dark:text-gray-300",
                isDragging && "opacity-50",
                isDragOver && "border-l-2 border-l-blue-500"
              )}
              onClick={() => onSelectTab(file.id)}
            >
              <File className="h-3 w-3 shrink-0" />
              <span className="text-xs whitespace-nowrap flex items-center gap-1">
                {file.name}
                {file.assetId && (
                  <span className="text-[8px] px-1 py-0.5 rounded bg-blue-100 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400" title="From assets">
                    A
                  </span>
                )}
                {file.isModified && (
                  <span className="text-[8px] px-1 py-0.5 rounded bg-yellow-100 dark:bg-yellow-900/30 text-yellow-600 dark:text-yellow-400" title="Modified">
                    M
                  </span>
                )}
              </span>
              {onCloseTab && (
                <Button
                  variant="ghost"
                  size="sm"
                  className={cn(
                    "h-4 w-4 p-0 shrink-0 hover:bg-gray-300 dark:hover:bg-gray-700",
                    isActive ? "opacity-100" : "opacity-0 group-hover:opacity-100"
                  )}
                  onClick={(e) => {
                    e.stopPropagation()
                    onCloseTab(file.id)
                  }}
                >
                  <X className="h-3 w-3" />
                </Button>
              )}
            </div>
          )
        })
      )}
    </div>
  )
}
