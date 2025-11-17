"use client"

import { X, File } from "lucide-react"
import { Button } from "@/components/ui/button"
import type { CodeFile } from "./types"
import { cn } from "@/lib/utils"

interface FileTabsProps {
  files: CodeFile[]
  openTabs: string[]
  activeFileId?: string
  onSelectTab: (fileId: string) => void
  onCloseTab: (fileId: string) => void
}

export function FileTabs({
  files,
  openTabs,
  activeFileId,
  onSelectTab,
  onCloseTab,
}: FileTabsProps) {
  const openFiles = openTabs
    .map(id => files.find(f => f.id === id))
    .filter((f): f is CodeFile => f !== undefined)

  if (openFiles.length === 0) {
    return null
  }

  return (
    <div className="flex items-center gap-0.5 bg-gray-100 dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800 overflow-x-auto">
      {openFiles.map(file => {
        const isActive = file.id === activeFileId
        
        return (
          <div
            key={file.id}
            className={cn(
              "flex items-center gap-2 px-3 py-1.5 cursor-pointer border-r border-gray-200 dark:border-gray-800 group min-w-0",
              isActive
                ? "bg-white dark:bg-gray-950 text-blue-600 dark:text-blue-400"
                : "hover:bg-gray-200 dark:hover:bg-gray-800 text-gray-600 dark:text-gray-400"
            )}
            onClick={() => onSelectTab(file.id)}
          >
            <File className="h-3 w-3 shrink-0" />
            <span className="text-xs truncate max-w-[120px]">
              {file.name}
            </span>
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
          </div>
        )
      })}
    </div>
  )
}
