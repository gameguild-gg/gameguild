"use client"

import { Editor } from "@/components/editor/lexical-editor"
import type { LexicalEditor } from "lexical"
import type React from "react"
import { useEffect, useRef } from "react"
import type { ProjectMode } from "@/lib/storage/editor/project-modes"

interface EditorLayoutType2Props {
  blockRefs: React.MutableRefObject<Record<string, LexicalEditor | null>>
  blockStates: Record<string, string>
  onBlockChange: (blockId: string, state: string) => void
  onLoadingChange?: (setLoading: (loading: boolean) => void) => void
  projectId: string
  mode?: ProjectMode
  currentProjectType?: "type1" | "type2" | "type3"
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
  onLoadingChange,
  projectId,
  mode = "free-page",
  currentProjectType,
  storageAdapter,
}: EditorLayoutType2Props) {
  const blocks = Object.keys(blockStates).sort((a, b) => {
    const numA = parseInt(a.slice(1))
    const numB = parseInt(b.slice(1))
    return numA - numB
  })

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
              <div className="p-2 flex items-center justify-center border-b border-gray-200 dark:border-gray-700">
                <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
                  Panel {index + 1}
                </span>
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
