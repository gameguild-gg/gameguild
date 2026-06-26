"use client"

import { Folder, ChevronDown } from "lucide-react"
import { Button } from "@/components/ui/button"
import type { FileTreeFolder } from "./types"
import { cn } from "@/lib/utils"
import { useState, useRef, useEffect } from "react"

interface FocusEditorConfigProps {
  focusIndexPath?: string
  folders: FileTreeFolder[]
  editorInstance?: "multiple" | "unique"
  onSetIndexPath: (path: string) => void
}

export function FocusEditorConfig({
  focusIndexPath,
  folders,
  editorInstance,
  onSetIndexPath,
}: FocusEditorConfigProps) {
  const [isOpen, setIsOpen] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)

  // Only show config for unique editor instances
  // Multiple instances use the folder marked as focus
  if (editorInstance === "multiple") {
    return null
  }

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside)
      return () => document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [isOpen])

  // Get all folders as dropdown options
  const availableFolders = folders.filter(f => !f.readonly).sort((a, b) => a.path.localeCompare(b.path))

  const selectedFolder = focusIndexPath 
    ? folders.find(f => f.path === focusIndexPath)
    : null

  return (
    <div 
      ref={dropdownRef}
      className="relative flex items-center gap-2 px-3 py-2 bg-amber-50 dark:bg-amber-950/20 border-b border-amber-200 dark:border-amber-800"
      data-no-drag="true"
    >
      <Folder className="h-4 w-4 text-amber-600 dark:text-amber-400 shrink-0" />
      <span className="text-xs text-amber-700 dark:text-amber-400 shrink-0 font-medium">
        Index Folder:
      </span>
      
      <Button
        variant="ghost"
        size="sm"
        onClick={() => setIsOpen(!isOpen)}
        className={cn(
          "h-7 px-3 text-xs flex items-center gap-2 flex-1 justify-between",
          "bg-white dark:bg-gray-800 border border-amber-300 dark:border-amber-700",
          "hover:bg-amber-50 dark:hover:bg-gray-750 text-amber-900 dark:text-amber-100"
        )}
      >
        <span className="truncate">
          {selectedFolder ? selectedFolder.path : "Select folder..."}
        </span>
        <ChevronDown className={cn(
          "h-3 w-3 shrink-0 transition-transform",
          isOpen && "rotate-180"
        )} />
      </Button>

      {isOpen && (
        <div className="absolute top-full left-0 right-0 mt-1 z-50 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-md shadow-lg max-h-48 overflow-y-auto">
          {availableFolders.length === 0 ? (
            <div className="px-4 py-2 text-xs text-gray-500 dark:text-gray-400 italic">
              No folders available
            </div>
          ) : (
            availableFolders.map(folder => {
              const isSelected = folder.path === focusIndexPath
              
              return (
                <button
                  key={folder.path}
                  onClick={() => {
                    onSetIndexPath(folder.path)
                    setIsOpen(false)
                  }}
                  className={cn(
                    "w-full px-4 py-2 text-left text-xs flex items-center gap-2",
                    "hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors",
                    isSelected && "bg-amber-50 dark:bg-amber-900/20 text-amber-600 dark:text-amber-400 font-medium"
                  )}
                >
                  <Folder className="h-3 w-3 shrink-0" />
                  <span className="truncate">{folder.path}</span>
                </button>
              )
            })
          )}
        </div>
      )}
    </div>
  )
}
