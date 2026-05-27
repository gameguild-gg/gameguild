/**
 * Isolated Lexical Editor for Essay Quiz Questions
 *
 * Uses the project's shared Lexical configuration (`SHARED_LEXICAL_NODES`
 * + `SHARED_LEXICAL_THEME`) so the essay answer field behaves and looks
 * exactly like every other inline Lexical editor in the block content
 * editor (notably the rich-text block). The Lexical context itself is
 * still scoped to this component so it does not collide with the parent
 * page's Lexical state.
 */

"use client"

import { useCallback, useMemo, useRef, useEffect } from "react"
import { LexicalComposer } from "@lexical/react/LexicalComposer"
import { RichTextPlugin } from "@lexical/react/LexicalRichTextPlugin"
import { ContentEditable } from "@lexical/react/LexicalContentEditable"
import { HistoryPlugin } from "@lexical/react/LexicalHistoryPlugin"
import { OnChangePlugin } from "@lexical/react/LexicalOnChangePlugin"
import { ListPlugin } from "@lexical/react/LexicalListPlugin"
import { LinkPlugin } from "@lexical/react/LexicalLinkPlugin"
import { LexicalErrorBoundary } from "@lexical/react/LexicalErrorBoundary"
import { $getRoot, type EditorState } from "lexical"
import { FloatingTextFormatToolbarPlugin } from "../../../plugins/floating-text-format-toolbar-plugin"
import { InlineTextFormatToolbarPlugin } from "../../../plugins/inline-text-format-toolbar-plugin"
import { BlockEmbedPlugin } from "../../../plugins/block-embed-plugin"
import { BlockInsertMenuPlugin } from "../../../plugins/block-insert-menu-plugin"
import {
  SHARED_LEXICAL_NODES,
  SHARED_LEXICAL_THEME,
} from "../../../lib/lexical"

interface EssayLexicalEditorProps {
  initialState?: string
  onChange: (serialized: string, plainText: string) => void
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

  const handleChange = useCallback((editorState: EditorState) => {
    const serialized = JSON.stringify(editorState.toJSON())
    const plainText = editorState.read(() => $getRoot().getTextContent())
    onChangeRef.current(serialized, plainText)
  }, [])

  const initialConfig = useMemo(
    () => ({
      namespace: "EssayQuizEditor",
      nodes: SHARED_LEXICAL_NODES,
      theme: SHARED_LEXICAL_THEME,
      editable: !disabled,
      editorState: initialState || undefined,
      onError: (error: Error) => {
        console.error("[EssayLexicalEditor]", error)
      },
    }),
    // Only use initial values — don't recreate on every disabled change
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [],
  )

  return (
    <div className="essay-lexical-editor">
      <LexicalComposer initialConfig={initialConfig}>
        <div className="relative rounded-lg border-2 border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus-within:border-blue-500 transition-colors overflow-hidden">
          {!disabled && <InlineTextFormatToolbarPlugin />}
          <RichTextPlugin
            contentEditable={
              <ContentEditable
                className="px-4 py-3 outline-none text-base text-gray-900 dark:text-gray-100 resize-y overflow-auto"
                style={{ minHeight }}
              />
            }
            placeholder={
              <div className="pointer-events-none absolute left-4 top-3 select-none text-gray-400 dark:text-gray-500">
                {placeholder}
              </div>
            }
            ErrorBoundary={LexicalErrorBoundary}
          />
          {!disabled && <FloatingTextFormatToolbarPlugin />}
          <HistoryPlugin />
          <ListPlugin />
          <LinkPlugin />
          <BlockEmbedPlugin />
          {!disabled && <BlockInsertMenuPlugin />}
          <OnChangePlugin onChange={handleChange} ignoreSelectionChange />
          {disabled && <ReadOnlyPlugin />}
        </div>
      </LexicalComposer>
    </div>
  )
}

/** Plugin to toggle readOnly after mount */
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"

function ReadOnlyPlugin() {
  const [editor] = useLexicalComposerContext()
  useEffect(() => {
    editor.setEditable(false)
  }, [editor])
  return null
}
