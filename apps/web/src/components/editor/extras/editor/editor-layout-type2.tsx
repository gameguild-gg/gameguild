"use client"

import { Editor } from "@/components/editor/lexical-editor"
import type { LexicalEditor } from "lexical"
import type React from "react"
import { useEffect, useRef, useState } from "react"
import type { ProjectMode } from "@/lib/storage/editor/project-modes"
import { Button } from "@/components/ui/button"
import { Plus, Trash2 } from "lucide-react"
import { toast } from "sonner"
import { type ProjectType} from "@/lib/storage/editor/project-types"

interface EditorLayoutType2Props {
  blockRefs: React.MutableRefObject<Record<string, LexicalEditor | null>>
  blockStates: Record<string, string>
  onBlockChange: (blockId: string, state: string) => void
  onBlockAdd?: () => void
  onBlockRemove?: (blockId: string) => void
  onLoadingChange?: (setLoading: (loading: boolean) => void) => void
  projectId: string
  mode?: ProjectMode
  currentProjectType?: ProjectType
  storageAdapter?: any
}

/**
 * Editor Layout Type 2: Multiple horizontal panels
 * This layout displays multiple editors side by side (b1, b2, b3...bN)
 */
export function EditorLayoutType2({
  blockRefs,
  blockStates,
  onBlockChange,
  onBlockAdd,
  onBlockRemove,
  onLoadingChange,
  projectId,
  mode = "free-page",
  currentProjectType,
  storageAdapter,
}: EditorLayoutType2Props) {
  const [confirmDelete, setConfirmDelete] = useState<string | null>(null)
  
  const blocks = Object.keys(blockStates).sort((a, b) => {
    const numA = parseInt(a.slice(1))
    const numB = parseInt(b.slice(1))
    return numA - numB
  })
  
  const handleAddBlock = () => {
    if (onBlockAdd) {
      onBlockAdd()
      toast.success("Panel added", {
        description: `Panel ${blocks.length + 1} created`,
        duration: 2000,
      })
    }
  }
  
  const handleRemoveBlock = (blockId: string) => {
    if (blocks.length <= 1) {
      toast.error("Cannot remove", {
        description: "Must have at least 1 panel",
        duration: 2000,
      })
      return
    }
    
    if (confirmDelete === blockId) {
      if (onBlockRemove) {
        onBlockRemove(blockId)
        toast.success("Panel removed", {
          description: `Panel removed successfully`,
          duration: 2000,
        })
      }
      setConfirmDelete(null)
    } else {
      setConfirmDelete(blockId)
      setTimeout(() => setConfirmDelete(null), 3000)
    }
  }

  // Create individual refs for each block
  const localRefs = useRef<Record<string, React.RefObject<LexicalEditor | null>>>({})
  
  // Initialize refs for each block
  useEffect(() => {
    blocks.forEach(blockId => {
      if (!localRefs.current[blockId]) {
        localRefs.current[blockId] = { current: null }
      }
    })
  }, [blocks])
  
  // Sync local refs to parent blockRefs
  useEffect(() => {
    blocks.forEach(blockId => {
      const localRef = localRefs.current[blockId]
      if (localRef && blockRefs.current) {
        blockRefs.current[blockId] = localRef.current
      }
    })
  })

  return (
    <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
      {/* Panel Management Header */}
      <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900">
        <div className="flex items-center gap-2">
          <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
            {blocks.length} {blocks.length === 1 ? "Panel" : "Panels"}
          </span>
        </div>
        <Button
          size="sm"
          variant="outline"
          onClick={handleAddBlock}
          className="gap-2 h-8"
        >
          <Plus className="h-4 w-4" />
          Add Panel
        </Button>
      </div>
      
      {/* Multi Panel Content - Side by side */}
      <div className="flex">
        {blocks.map((blockId, index) => {
          // Get or create ref for this block
          if (!localRefs.current[blockId]) {
            localRefs.current[blockId] = { current: null }
          }
          
          return (
            <div
              key={blockId}
              className={`flex-1 ${index < blocks.length - 1 ? "border-r border-gray-200 dark:border-gray-700" : ""}`}
            >
              <div className="p-2 flex items-center justify-between border-b border-gray-200 dark:border-gray-700">
                <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
                  Panel {index + 1}
                </span>
                {blocks.length > 1 && (
                  <Button
                    size="sm"
                    variant="ghost"
                    onClick={() => handleRemoveBlock(blockId)}
                    className={`h-6 w-6 p-0 ${
                      confirmDelete === blockId
                        ? "bg-red-100 dark:bg-red-900 text-red-600 dark:text-red-400"
                        : "hover:bg-red-50 dark:hover:bg-red-950 hover:text-red-600 dark:hover:text-red-400"
                    }`}
                    title={confirmDelete === blockId ? "Click again to confirm" : "Remove panel"}
                  >
                    <Trash2 className="h-3 w-3" />
                  </Button>
                )}
              </div>
              <div className="p-4 sm:p-6 md:p-8 lg:p-12">
                <Editor
                  editorRef={localRefs.current[blockId]}
                  initialState={blockStates[blockId]}
                  onChange={(state) => onBlockChange(blockId, state)}
                  onLoadingChange={onLoadingChange}
                  projectId={projectId}
                  mode={mode}
                  blockId={blockId}
                  currentProjectType={currentProjectType}
                  storageAdapter={storageAdapter}
                />
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
