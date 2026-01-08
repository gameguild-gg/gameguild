"use client"

import { ChevronDown, Code, FileCode } from "lucide-react"
import { Button } from "@/components/ui/button"
import type { CodeFile, FileTreeFolder } from "./types"
import { cn } from "@/lib/utils"
import { useState, useRef, useEffect } from "react"

interface LanguageSelectorProps {
  files: CodeFile[]
  folders: FileTreeFolder[]
  activeFileId?: string
  onSelectLanguage: (fileId: string) => void
}

export function LanguageSelector({
  files,
  folders,
  activeFileId,
  onSelectLanguage,
}: LanguageSelectorProps) {
  const [openDropdown, setOpenDropdown] = useState<string | null>(null)
  const dropdownRef = useRef<HTMLDivElement>(null)

  // Filter files by focus folder and group by basename
  const getFileGroups = () => {
    // Find folder marked as focus
    const focusFolder = folders.find(f => f.isFocusFolder)
    if (!focusFolder) return new Map<string, CodeFile[]>()
    
    // Filter files that are in the focus folder
    const indexFiles = files.filter(file => {
      const filePath = file.path
      const folderPath = filePath.substring(0, filePath.lastIndexOf('/'))
      return folderPath === focusFolder.path
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

    return grouped
  }

  const fileGroups = getFileGroups()
  const basenames = Array.from(fileGroups.keys())
  
  // Determine current basename from active file
  const activeFile = files.find(f => f.id === activeFileId)
  const currentBasename = activeFile ? activeFile.name.substring(0, activeFile.name.lastIndexOf('.')) : basenames[0] || null

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setOpenDropdown(null)
      }
    }

    if (openDropdown) {
      document.addEventListener('mousedown', handleClickOutside)
      return () => document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [openDropdown])

  if (basenames.length === 0) {
    return (
      <div className="flex items-center gap-2 px-3 py-2 bg-gray-100 dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800">
        <Code className="h-4 w-4 text-gray-400" />
        <span className="text-xs text-gray-500 dark:text-gray-400 italic">
          No focus folder selected (mark a folder as focus 🎯)
        </span>
      </div>
    )
  }

  return (
    <div 
      ref={dropdownRef}
      className="flex items-center gap-3 px-3 py-2 bg-gray-100 dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800 flex-wrap"
    >
      {basenames.length === 1 ? (
        // Single basename: show "Language: [dropdown]"
        <>
          <Code className="h-4 w-4 text-blue-600 dark:text-blue-400 shrink-0" />
          <span className="text-xs text-gray-600 dark:text-gray-400 shrink-0">
            Language:
          </span>
          {(() => {
            const basename = basenames[0]!
            const basenameFiles = fileGroups.get(basename) || []
            const activeBasenameFile = basenameFiles.find(f => f.id === activeFileId)
            const activeExtension = activeBasenameFile?.name.split('.').pop() || basenameFiles[0]?.name.split('.').pop() || "js"
            const isOpen = openDropdown === basename

            return (
              <div className="relative">
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => setOpenDropdown(isOpen ? null : basename)}
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
                  <div className={cn(
                    "absolute top-full left-0 mt-1 z-50 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-md shadow-lg overflow-hidden",
                    basenameFiles.length > 8 ? "grid grid-cols-2 min-w-[320px]" : "min-w-[180px]"
                  )}>
                    {basenameFiles.map(file => {
                      const extension = file.name.split('.').pop() || ''
                      const isActive = file.id === activeFileId
                      
                      return (
                        <button
                          key={file.id}
                          onClick={() => {
                            onSelectLanguage(file.id)
                            setOpenDropdown(null)
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
          })()}
        </>
      ) : (
        // Multiple basenames: show "basename: [dropdown]" for each
        <>
          <Code className="h-4 w-4 text-blue-600 dark:text-blue-400 shrink-0" />
          {basenames.map((basename) => {
            const basenameFiles = fileGroups.get(basename) || []
            const activeBasenameFile = basenameFiles.find(f => f.id === activeFileId)
            const isCurrentBasename = basename === currentBasename
            const activeExtension = activeBasenameFile?.name.split('.').pop() || basenameFiles[0]?.name.split('.').pop() || "js"
            const isOpen = openDropdown === basename

            return (
              <div key={basename} className="flex items-center gap-2">
                <FileCode className="h-3 w-3 text-gray-500 dark:text-gray-400" />
                <button
                  onClick={() => {
                    // Select first file of this basename if not already on this basename
                    if (!isCurrentBasename) {
                      const firstFile = basenameFiles[0]
                      if (firstFile) {
                        onSelectLanguage(firstFile.id)
                      }
                    }
                  }}
                  className={cn(
                    "text-xs font-medium shrink-0 hover:underline cursor-pointer transition-colors",
                    isCurrentBasename ? "text-blue-600 dark:text-blue-400" : "text-gray-600 dark:text-gray-400 hover:text-blue-500 dark:hover:text-blue-300"
                  )}
                >
                  {basename}:
                </button>
                
                <div className="relative">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setOpenDropdown(isOpen ? null : basename)}
                    className={cn(
                      "h-7 px-3 text-xs font-medium flex items-center gap-2",
                      "bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-700",
                      "hover:bg-gray-50 dark:hover:bg-gray-750",
                      isCurrentBasename && "border-blue-500 dark:border-blue-500"
                    )}
                  >
                    <span className="uppercase">{activeExtension}</span>
                    <ChevronDown className={cn(
                      "h-3 w-3 transition-transform",
                      isOpen && "rotate-180"
                    )} />
                  </Button>

                  {isOpen && (
                    <div className={cn(
                      "absolute top-full left-0 mt-1 z-50 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-md shadow-lg overflow-hidden",
                      basenameFiles.length > 8 ? "grid grid-cols-2 min-w-[320px]" : "min-w-[180px]"
                    )}>
                      {basenameFiles.map(file => {
                        const extension = file.name.split('.').pop() || ''
                        const isActive = file.id === activeFileId
                        
                        return (
                          <button
                            key={file.id}
                            onClick={() => {
                              onSelectLanguage(file.id)
                              setOpenDropdown(null)
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
              </div>
            )
          })}
        </>
      )}
    </div>
  )
}
