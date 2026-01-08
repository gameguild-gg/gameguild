"use client"

import { ChevronDown, Code, FileCode, RotateCcw } from "lucide-react"
import { Button } from "@/components/ui/button"
import type { CodeFile, FileTreeFolder } from "./types"
import { cn } from "@/lib/utils"
import { useState, useRef, useEffect } from "react"
import { ResetConfirmDialog } from "../dialogs/reset-confirm-dialog"

interface LanguageSelectorProps {
  files: CodeFile[]
  folders: FileTreeFolder[]
  activeFileId?: string
  onSelectLanguage: (fileId: string) => void
  isPreview?: boolean
  onResetFile?: (fileId: string) => void
  onResetAllFiles?: () => void
}

export function LanguageSelector({
  files,
  folders,
  activeFileId,
  onSelectLanguage,
  isPreview = false,
  onResetFile,
  onResetAllFiles,
}: LanguageSelectorProps) {
  const [openDropdown, setOpenDropdown] = useState<string | null>(null)
  const [showResetMenu, setShowResetMenu] = useState(false)
  const [resetDialogOpen, setResetDialogOpen] = useState(false)
  const [resetType, setResetType] = useState<"current" | "all">("current")
  const dropdownRef = useRef<HTMLDivElement>(null)
  const resetMenuRef = useRef<HTMLDivElement>(null)
  // Track last selected file ID for each basename
  const lastSelectedPerBasenameRef = useRef<Record<string, string>>({})

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

  // Update last selected file when active file changes
  useEffect(() => {
    if (activeFile && currentBasename) {
      lastSelectedPerBasenameRef.current[currentBasename] = activeFile.id
    }
  }, [activeFile, currentBasename])

  // Reset handlers
  const handleResetClick = (type: "current" | "all") => {
    setResetType(type)
    setResetDialogOpen(true)
    setShowResetMenu(false)
  }

  const handleConfirmReset = () => {
    if (resetType === "current" && activeFileId && onResetFile) {
      onResetFile(activeFileId)
    } else if (resetType === "all" && onResetAllFiles) {
      onResetAllFiles()
    }
    setResetDialogOpen(false)
  }

  // Close dropdown when clicking outside
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setOpenDropdown(null)
      }
      if (resetMenuRef.current && !resetMenuRef.current.contains(event.target as Node)) {
        setShowResetMenu(false)
      }
    }

    if (openDropdown || showResetMenu) {
      document.addEventListener('mousedown', handleClickOutside)
      return () => document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [openDropdown, showResetMenu])

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
            const isCurrentBasename = basename === currentBasename
            
            // Determine which file to show in the dropdown button
            let displayFile: CodeFile | undefined
            if (isCurrentBasename) {
              // Current basename: show active file
              displayFile = basenameFiles.find(f => f.id === activeFileId)
            } else {
              // Other basename: show last selected or first
              const lastSelectedId = lastSelectedPerBasenameRef.current[basename]
              displayFile = lastSelectedId 
                ? basenameFiles.find(f => f.id === lastSelectedId)
                : undefined
            }
            
            const activeExtension = displayFile?.name.split('.').pop() || basenameFiles[0]?.name.split('.').pop() || "js"
            const isOpen = openDropdown === basename

            return (
              <div key={basename} className="flex items-center gap-2">
                <FileCode className="h-3 w-3 text-gray-500 dark:text-gray-400" />
                <button
                  onClick={() => {
                    // Select last selected file of this basename, or first if none selected before
                    if (!isCurrentBasename) {
                      const lastSelectedId = lastSelectedPerBasenameRef.current[basename]
                      const fileToSelect = lastSelectedId 
                        ? basenameFiles.find(f => f.id === lastSelectedId) || basenameFiles[0]
                        : basenameFiles[0]
                      
                      if (fileToSelect) {
                        onSelectLanguage(fileToSelect.id)
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
      
      {/* Reset Button - Only in preview mode */}
      {isPreview && (onResetFile || onResetAllFiles) && (
        <div ref={resetMenuRef} className="relative ml-auto">
          <Button
            variant="ghost"
            size="sm"
            onClick={() => setShowResetMenu(!showResetMenu)}
            className={cn(
              "h-7 px-2 text-xs flex items-center gap-1.5",
              "hover:bg-orange-100 dark:hover:bg-orange-900/20",
              "text-orange-600 dark:text-orange-400"
            )}
            title="Reset files"
          >
            <RotateCcw className="h-3.5 w-3.5" />
            <span>Reset</span>
          </Button>

          {showResetMenu && (
            <div className="absolute top-full right-0 mt-1 z-50 min-w-[180px] bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-md shadow-lg overflow-hidden">
              <button
                onClick={() => handleResetClick("current")}
                disabled={!activeFileId}
                className={cn(
                  "w-full px-4 py-2 text-left text-xs flex items-center gap-2",
                  "hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors",
                  !activeFileId && "opacity-50 cursor-not-allowed"
                )}
              >
                <RotateCcw className="h-3.5 w-3.5 text-orange-600 dark:text-orange-400" />
                <span>Reset Current File</span>
              </button>
              <button
                onClick={() => handleResetClick("all")}
                className="w-full px-4 py-2 text-left text-xs flex items-center gap-2 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
              >
                <RotateCcw className="h-3.5 w-3.5 text-orange-600 dark:text-orange-400" />
                <span>Reset All Files</span>
              </button>
            </div>
          )}
        </div>
      )}

      {/* Reset Confirmation Dialog */}
      <ResetConfirmDialog
        open={resetDialogOpen}
        onOpenChange={setResetDialogOpen}
        resetType={resetType}
        fileName={activeFile?.name}
        onConfirm={handleConfirmReset}
      />
    </div>
  )
}
