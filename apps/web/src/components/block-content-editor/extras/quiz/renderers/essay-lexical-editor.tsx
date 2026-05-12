/**
 * Isolated Lexical Editor for Essay Quiz Questions
 * Provides rich text editing with its own LexicalComposer context,
 * ensuring no interaction with the parent page's Lexical editor.
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
import { HeadingNode, QuoteNode } from "@lexical/rich-text"
import { ListNode, ListItemNode } from "@lexical/list"
import { LinkNode, AutoLinkNode } from "@lexical/link"
import { CodeNode } from "@lexical/code"
import { $getRoot, type EditorState } from "lexical"
import { FloatingTextFormatToolbarPlugin } from "../../../plugins/floating-text-format-toolbar-plugin"

const ESSAY_THEME = {
  text: {
    bold: "font-bold",
    italic: "italic",
    underline: "underline",
    strikethrough: "line-through",
    code: "bg-gray-100 dark:bg-gray-800 px-1 py-0.5 rounded font-mono text-sm",
  },
  paragraph: "my-1",
  heading: {
    h1: "text-2xl font-bold my-2",
    h2: "text-xl font-bold my-2",
    h3: "text-lg font-bold my-1",
  },
  list: {
    ul: "list-disc list-inside ml-4",
    ol: "list-decimal list-inside ml-4",
    listitem: "my-0.5",
  },
  quote: "border-l-4 border-gray-300 dark:border-gray-600 pl-3 italic text-gray-600 dark:text-gray-400 my-2",
  code: "bg-gray-100 dark:bg-gray-800 p-2 rounded font-mono text-sm",
  link: "text-blue-600 underline hover:text-blue-800 cursor-pointer",
}

const ESSAY_NODES = [
  HeadingNode,
  QuoteNode,
  ListNode,
  ListItemNode,
  CodeNode,
  LinkNode,
  AutoLinkNode,
]

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
      nodes: ESSAY_NODES,
      theme: ESSAY_THEME,
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
        <div className="relative rounded-lg border-2 border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 focus-within:border-blue-500 transition-colors">
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
