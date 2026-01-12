"use client"

import { Editor } from "@/components/editor/lexical-editor"
import { EditableProjectTitle } from "./editable-project-title"
import type { LexicalEditor } from "lexical"
import type React from "react"

interface EditorLayoutType1Props {
  projectName: string
  isEditing: boolean
  editingName: string
  onEditStart: () => void
  onEditEnd: () => void
  onNameChange: (name: string) => void
  onSave: () => void
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
  projectName,
  isEditing,
  editingName,
  onEditStart,
  onEditEnd,
  onNameChange,
  onSave,
  editorRef,
  editorState,
  onEditorChange,
  onLoadingChange,
  projectId,
}: EditorLayoutType1Props) {
  return (
    <div className="border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900">
      {/* Title Bar */}
      <div className="flex items-center justify-center border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900 px-4 py-3">
        <EditableProjectTitle
          projectName={projectName}
          isEditing={isEditing}
          editingName={editingName}
          onEditStart={onEditStart}
          onEditEnd={onEditEnd}
          onNameChange={onNameChange}
          onSave={onSave}
        />
      </div>
      
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
