"use client"

import { X, File } from "lucide-react"
import { Button } from "@/components/ui/button"
import type { CodeFile } from "./types"
import { cn } from "@/lib/utils"
import { useRef, useEffect } from "react"

interface FileTabsProps {
  files: CodeFile[]
  openTabs: string[]
  activeFileId?: string
  onSelectTab: (fileId: string) => void
  onCloseTab?: (fileId: string) => void
}

export function FileTabs({
  files,
  openTabs,
  activeFileId,
  onSelectTab,
  onCloseTab,
}: FileTabsProps) {
  const scrollContainerRef = useRef<HTMLDivElement>(null)
  const activeTabRef = useRef<HTMLDivElement>(null)
  
  const openFiles = openTabs
    .map(id => files.find(f => f.id === id))
    .filter((f): f is CodeFile => f !== undefined)

  // Auto-scroll para a aba ativa quando mudar
  useEffect(() => {
    if (activeTabRef.current && scrollContainerRef.current) {
      activeTabRef.current.scrollIntoView({
        behavior: 'smooth',
        block: 'nearest',
        inline: 'center'
      })
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

  return (
    <div 
      ref={scrollContainerRef}
      className="flex items-center gap-0.5 bg-gray-100 dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800 overflow-x-auto min-h-[32px] scrollbar-thin scrollbar-thumb-gray-400 dark:scrollbar-thumb-gray-600 scrollbar-track-transparent"
    >
      {openFiles.length === 0 ? (
        <div className="px-3 py-1.5 text-xs text-gray-400 dark:text-gray-600 italic">
          No files open
        </div>
      ) : (
        openFiles.map(file => {
          const isActive = file.id === activeFileId
          
          return (
            <div
              key={file.id}
              ref={isActive ? activeTabRef : null}
              className={cn(
                "flex items-center gap-2 px-3 py-1.5 cursor-pointer border-r border-gray-200 dark:border-gray-800 group whitespace-nowrap shrink-0",
                isActive
                  ? "bg-white dark:bg-gray-950 text-blue-600 dark:text-blue-400"
                  : "hover:bg-gray-200 dark:hover:bg-gray-800 text-gray-600 dark:text-gray-400"
              )}
              onClick={() => onSelectTab(file.id)}
            >
              <File className="h-3 w-3 shrink-0" />
              <span className="text-xs whitespace-nowrap">
                {file.name}
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
