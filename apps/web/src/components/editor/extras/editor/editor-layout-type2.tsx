"use client"

import { Editor } from "@/components/editor/lexical-editor"
import type { LexicalEditor } from "lexical"
import type React from "react"
import type { ProjectMode } from "@/lib/storage/editor/project-modes"

interface EditorLayoutType2Props {
  leftEditorRef: React.MutableRefObject<LexicalEditor | null>
  rightEditorRef: React.MutableRefObject<LexicalEditor | null>
  leftEditorState: string
  rightEditorState: string
  onLeftEditorChange: (state: string) => void
  onRightEditorChange: (state: string) => void
  onLoadingChange?: (setLoading: (loading: boolean) => void) => void
  projectId: string
  mode?: ProjectMode
  currentProjectType?: "type1" | "type2" | "type3"
  storageAdapter?: any
}

/**
 * Editor Layout Type 2: Dual horizontal editors
 * This layout displays two editors side by side (left and right)
 */
export function EditorLayoutType2({
  leftEditorRef,
  rightEditorRef,
  leftEditorState,
  rightEditorState,
  onLeftEditorChange,
  onRightEditorChange,
  onLoadingChange,
  projectId,
  mode = "free-page",
  currentProjectType,
  storageAdapter,
}: EditorLayoutType2Props) {
  return (
    <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
      {/* Dual Editor Content - Side by side */}
      <div className="flex">
        {/* Left Editor */}
        <div className="flex-1 border-r border-gray-200 dark:border-gray-700">
          <div className="p-2 flex items-center justify-center border-b border-gray-200 dark:border-gray-700">
            <span className="text-sm font-medium text-gray-600 dark:text-gray-400">Left Panel</span>
          </div>
          <div className="p-4 sm:p-6 md:p-8 lg:p-12">
            <Editor
              editorRef={leftEditorRef}
              initialState={leftEditorState}
              onChange={onLeftEditorChange}
              onLoadingChange={onLoadingChange}
              projectId={projectId}
              mode={mode}
              panel="left"
              currentProjectType={currentProjectType}
              storageAdapter={storageAdapter}
            />
          </div>
        </div>

        {/* Right Editor */}
        <div className="flex-1">
          <div className="p-2 flex items-center justify-center border-b border-gray-200 dark:border-gray-700">
            <span className="text-sm font-medium text-gray-600 dark:text-gray-400">Right Panel</span>
          </div>
          <div className="p-4 sm:p-6 md:p-8 lg:p-12">
            <Editor
              editorRef={rightEditorRef}
              initialState={rightEditorState}
              onChange={onRightEditorChange}
              projectId={projectId}
              mode={mode}
              panel="right"
              currentProjectType={currentProjectType}
              storageAdapter={storageAdapter}
            />
          </div>
        </div>
      </div>
    </div>
  )
}
