"use client"

import { Editor } from "@/components/editor/lexical-editor"
import { EditableProjectTitle } from "./editable-project-title"
import type { LexicalEditor } from "lexical"
import type React from "react"

interface EditorLayoutType2Props {
  projectName: string
  isEditing: boolean
  editingName: string
  onEditStart: () => void
  onEditEnd: () => void
  onNameChange: (name: string) => void
  onSave: () => void
  leftEditorRef: React.MutableRefObject<LexicalEditor | null>
  rightEditorRef: React.MutableRefObject<LexicalEditor | null>
  leftEditorState: string
  rightEditorState: string
  onLeftEditorChange: (state: string) => void
  onRightEditorChange: (state: string) => void
  onLoadingChange?: (setLoading: (loading: boolean) => void) => void
  projectId: string
}

/**
 * Editor Layout Type 2: Dual horizontal editors
 * This layout displays two editors side by side (left and right)
 */
export function EditorLayoutType2({
  projectName,
  isEditing,
  editingName,
  onEditStart,
  onEditEnd,
  onNameChange,
  onSave,
  leftEditorRef,
  rightEditorRef,
  leftEditorState,
  rightEditorState,
  onLeftEditorChange,
  onRightEditorChange,
  onLoadingChange,
  projectId,
}: EditorLayoutType2Props) {
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
      
      {/* Dual Editor Content - Horizontal Layout */}
      <div className="grid grid-cols-2 gap-4 p-4">
        {/* Left Editor */}
        <div className="border-r border-gray-200 dark:border-gray-700 pr-4">
          <div className="mb-2 flex items-center justify-center">
            <span className="text-sm font-medium text-gray-600 dark:text-gray-400">Left Panel</span>
          </div>
          <Editor
            editorRef={leftEditorRef}
            initialState={leftEditorState}
            onChange={onLeftEditorChange}
            onLoadingChange={onLoadingChange}
            projectId={projectId}
          />
        </div>

        {/* Right Editor */}
        <div className="pl-4">
          <div className="mb-2 flex items-center justify-center">
            <span className="text-sm font-medium text-gray-600 dark:text-gray-400">Right Panel</span>
          </div>
          <Editor
            editorRef={rightEditorRef}
            initialState={rightEditorState}
            onChange={onRightEditorChange}
            projectId={projectId}
          />
        </div>
      </div>
    </div>
  )
}
