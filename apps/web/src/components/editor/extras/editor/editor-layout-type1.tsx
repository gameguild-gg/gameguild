"use client"

import { Editor } from "@/components/editor/lexical-editor"
import type { LexicalEditor } from "lexical"
import type React from "react"
import type { ProjectMode } from "@/lib/storage/editor/project-modes"
import { type ProjectType} from "@/lib/storage/editor/project-types"

interface EditorLayoutType1Props {
  editorRef: React.MutableRefObject<LexicalEditor | null>
  editorState: string
  onEditorChange: (state: string) => void
  onLoadingChange?: (setLoading: (loading: boolean) => void) => void
  projectId: string
  mode?: ProjectMode
  currentProjectType?: ProjectType
  storageAdapter?: any
}

/**
 * Editor Layout Type 1: Single vertical editor
 * This is the traditional single-editor layout
 */
export function EditorLayoutType1({
  editorRef,
  editorState,
  onEditorChange,
  onLoadingChange,
  projectId,
  mode = "free-page",
  currentProjectType,
  storageAdapter,
}: EditorLayoutType1Props) {
  // For non-free modes in type1, use "single" panel to apply restrictions
  const panel = mode !== "free-page" ? "single" : undefined
  
  return (
    <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
      {/* Editor Content */}
      <div className="p-4 sm:p-6 md:p-8 lg:p-12">
        <Editor
          editorRef={editorRef}
          initialState={editorState}
          onChange={onEditorChange}
          onLoadingChange={onLoadingChange}
          projectId={projectId}
          mode={mode}
          blockId="b1"
          currentProjectType={currentProjectType}
          storageAdapter={storageAdapter}
        />
      </div>
    </div>
  )
}
