"use client"

import { Editor } from "@/components/editor/lexical-editor"
import type { LexicalEditor } from "lexical"
import type React from "react"

interface EditorLayoutType1Props {
  editorRef: React.MutableRefObject<LexicalEditor | null>
  editorState: string
  onEditorChange: (state: string) => void
  onLoadingChange?: (setLoading: (loading: boolean) => void) => void
  projectId: string
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
}: EditorLayoutType1Props) {
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
        />
      </div>
    </div>
  )
}
