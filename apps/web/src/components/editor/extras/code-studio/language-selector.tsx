"use client"

import { ChevronDown, Code } from "lucide-react"
import { Button } from "@/components/ui/button"
import type { CodeFile } from "./types"
import { cn } from "@/lib/utils"
import { useState, useRef, useEffect } from "react"

interface LanguageSelectorProps {
  files: CodeFile[]
  focusIndexPath?: string
  activeFileId?: string
  onSelectLanguage: (fileId: string) => void
}

export function LanguageSelector({
  files,
  focusIndexPath,
  activeFileId,
  onSelectLanguage,
}: LanguageSelectorProps) {
  const [isOpen, setIsOpen] = useState(false)
  const dropdownRef = useRef<HTMLDivElement>(null)

  // Filter files by focusIndexPath and group by basename
  const getAvailableFiles = () => {
    if (!focusIndexPath) return []
    
    // Filter files that are in the index folder
    const indexFiles = files.filter(file => {
      const filePath = file.path
      const folderPath = filePath.substring(0, filePath.lastIndexOf('/'))
      return folderPath === focusIndexPath
    })

    // Group by basename (name without extension)
    const grouped = new Map<string, CodeFile[]>()
    indexFiles.forEach(file => {
      const basename = file.name.substring(0, file.name.lastIndexOf('.'))
      if (!grouped.has(basename)) {
        grouped.set(basename, [])
      }
      grouped.get(basename)!.push(file)
    })

    // Return files from the first group (assuming all files have same basename)
    const firstGroup = Array.from(grouped.values())[0]
    return firstGroup || []
  }

  const availableFiles = getAvailableFiles()
  const activeFile = files.find(f => f.id === activeFileId)
  const activeLanguage = activeFile?.language || "javascript"
  const activeExtension = activeFile?.name.split('.').pop() || "js"

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

  if (availableFiles.length === 0) {
    return (
      <div className="flex items-center gap-2 px-3 py-2 bg-gray-100 dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800">
        <Code className="h-4 w-4 text-gray-400" />
        <span className="text-xs text-gray-500 dark:text-gray-400 italic">
          No index folder selected
        </span>
      </div>
    )
  }

  return (
    <div 
      ref={dropdownRef}
      className="relative flex items-center gap-2 px-3 py-2 bg-gray-100 dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800"
    >
      <Code className="h-4 w-4 text-blue-600 dark:text-blue-400 shrink-0" />
      <span className="text-xs text-gray-600 dark:text-gray-400 shrink-0">
        Language:
      </span>
      
      <Button
        variant="ghost"
        size="sm"
        onClick={() => setIsOpen(!isOpen)}
        className={cn(
          "h-7 px-3 text-xs font-medium flex items-center gap-2",
          "bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700",
          "hover:bg-gray-50 dark:hover:bg-gray-750"
        )}
      >
        <span className="uppercase">{activeExtension}</span>
        <ChevronDown className={cn(
          "h-3 w-3 transition-transform",
          isOpen && "rotate-180"
        )} />
      </Button>

      {isOpen && (
        <div className="absolute top-full left-0 mt-1 z-50 min-w-[150px] bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-md shadow-lg overflow-hidden">
          {availableFiles.map(file => {
            const extension = file.name.split('.').pop() || ''
            const isActive = file.id === activeFileId
            
            return (
              <button
                key={file.id}
                onClick={() => {
                  onSelectLanguage(file.id)
                  setIsOpen(false)
                }}
                className={cn(
                  "w-full px-4 py-2 text-left text-xs flex items-center gap-2",
                  "hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors",
                  isActive && "bg-blue-50 dark:bg-blue-900/20 text-blue-600 dark:text-blue-400"
                )}
              >
                <span className="uppercase font-medium">{extension}</span>
                <span className="text-gray-500 dark:text-gray-400">({file.language})</span>
              </button>
            )
          })}
        </div>
      )}
    </div>
  )
}
