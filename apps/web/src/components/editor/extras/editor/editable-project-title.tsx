"use client"

import { useState } from "react"

interface EditableProjectTitleProps {
  projectName: string
  isEditing: boolean
  editingName: string
  onEditStart: () => void
  onEditEnd: () => void
  onNameChange: (name: string) => void
  onSave: () => void
}

export function EditableProjectTitle({
  projectName,
  isEditing,
  editingName,
  onEditStart,
  onEditEnd,
  onNameChange,
  onSave,
}: EditableProjectTitleProps) {
  return (
    <>
      {isEditing ? (
        <input
          type="text"
          value={editingName}
          onChange={(e) => onNameChange(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              onSave()
            } else if (e.key === "Escape") {
              onEditEnd()
            }
          }}
          onBlur={onSave}
          className="w-full max-w-md bg-transparent px-2 py-1 text-center text-xl font-semibold text-gray-900 outline-none dark:text-gray-100"
          autoFocus
        />
      ) : (
        <h2
          className="cursor-pointer px-2 py-1 text-xl font-semibold text-gray-900 transition-colors hover:bg-gray-100 dark:text-gray-100 dark:hover:bg-gray-800"
          onClick={onEditStart}
          title="Click to edit project name"
        >
          {projectName || "Untitled Project"}
        </h2>
      )}
    </>
  )
}
