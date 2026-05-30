/**
 * Isolated Lexical Editor for Essay Quiz Questions.
 *
 * Thin wrapper around `<LexicalSurface />` with toolbars/draggable/embed
 * disabled (essay answers are inline only).
 */

"use client"

import { useCallback, useRef } from "react"
import { $getRoot, type LexicalEditor, type SerializedEditorState } from "lexical"

import { LexicalSurface } from "../../../lexical-surface"

interface EssayLexicalEditorProps {
  initialState?: SerializedEditorState | null
  onChange: (state: SerializedEditorState, plainText: string) => void
  disabled?: boolean
  placeholder?: string
  minHeight?: string
}

export function EssayLexicalEditor({
  initialState,
  onChange,
  disabled = false,
  placeholder = "Write your answer...",
  minHeight = "150px",
}: EssayLexicalEditorProps) {
  const onChangeRef = useRef(onChange)
  onChangeRef.current = onChange

  const handleChange = useCallback((serialized: SerializedEditorState, editor: LexicalEditor) => {
    const plainText = editor.getEditorState().read(() => $getRoot().getTextContent())
    onChangeRef.current(serialized, plainText)
  }, [])

  return (
    <div className="essay-lexical-editor">
      <div className="relative rounded-lg border-2 border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus-within:border-blue-500 transition-colors overflow-hidden">
        <LexicalSurface
          namespace="EssayQuizEditor"
          initialState={initialState ?? null}
          onChange={handleChange}
          readOnly={disabled}
          placeholder={placeholder}
          contentStyle={{ minHeight }}
          contentClassName="resize-y overflow-auto"
          features={{
            toolbar: true,
            draggable: false,
            blockEmbed: false,
            blockInsertMenu: false,
            picker: false,
            pageLayout: false,
          }}
        />
      </div>
    </div>
  )
}
