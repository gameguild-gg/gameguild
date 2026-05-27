"use client"

/**
 * Inline editable rich-text editor used directly inside a block card —
 * no modal. Mirrors the plugin set of `RichTextEditor` (the modal version)
 * so authors get the same toolbar, slash menu, embed support, etc.
 */

import { useCallback, useMemo, useRef, useState, useEffect } from "react"
import { LexicalComposer } from "@lexical/react/LexicalComposer"
import { RichTextPlugin as LexicalRichTextPlugin } from "@lexical/react/LexicalRichTextPlugin"
import { ContentEditable } from "@lexical/react/LexicalContentEditable"
import { HistoryPlugin } from "@lexical/react/LexicalHistoryPlugin"
import { OnChangePlugin } from "@lexical/react/LexicalOnChangePlugin"
import { ListPlugin } from "@lexical/react/LexicalListPlugin"
import { LinkPlugin } from "@lexical/react/LexicalLinkPlugin"
import { LexicalErrorBoundary } from "@lexical/react/LexicalErrorBoundary"
import type { EditorState } from "lexical"

import { FloatingTextFormatToolbarPlugin } from "../../plugins/floating-text-format-toolbar-plugin"
import { InlineTextFormatToolbarPlugin } from "../../plugins/inline-text-format-toolbar-plugin"
import { BlockEmbedPlugin } from "../../plugins/block-embed-plugin"
import { BlockInsertMenuPlugin } from "../../plugins/block-insert-menu-plugin"
import { BlockInsertButtonPlugin } from "../../plugins/block-insert-button-plugin"
import { SHARED_LEXICAL_NODES, SHARED_LEXICAL_THEME } from "../../lib/lexical"
import type { RichTextData } from "../../nodes/rich-text-node"

interface InlineRichTextEditorProps {
  data: RichTextData | undefined
  onChange: (data: RichTextData) => void
  readOnly?: boolean
}

export function InlineRichTextEditor({ data, onChange, readOnly = false }: InlineRichTextEditorProps) {
  const onChangeRef = useRef(onChange)
  onChangeRef.current = onChange
  const titleRef = useRef<string | undefined>(data?.title)
  titleRef.current = data?.title

  // Track the content we last emitted so we can distinguish external updates
  // (e.g. modal save) from our own OnChangePlugin emissions. When external
  // content differs, bump the mount key to re-seed Lexical's editor state.
  const lastEmittedRef = useRef<string | undefined>(data?.content)
  const [mountKey, setMountKey] = useState(0)
  const seededContentRef = useRef<string | undefined>(data?.content)

  useEffect(() => {
    const incoming = data?.content
    if (incoming !== undefined && incoming !== lastEmittedRef.current) {
      // External change — remount with the new content as initial state.
      seededContentRef.current = incoming
      lastEmittedRef.current = incoming
      setMountKey((k) => k + 1)
    }
  }, [data?.content])

  const handleChange = useCallback((editorState: EditorState) => {
    const serialized = JSON.stringify(editorState.toJSON())
    lastEmittedRef.current = serialized
    onChangeRef.current({
      content: serialized,
      title: titleRef.current,
    })
  }, [])

  const initialConfig = useMemo(
    () => ({
      namespace: "InlineRichTextEditor",
      nodes: SHARED_LEXICAL_NODES,
      theme: SHARED_LEXICAL_THEME,
      editable: !readOnly,
      editorState: seededContentRef.current || undefined,
      onError: (error: Error) => {
        console.error("[InlineRichTextEditor]", error)
      },
    }),
    // Re-seed only on external remount; readOnly toggles also re-seed.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [mountKey, readOnly],
  )

  return (
    <LexicalComposer key={mountKey} initialConfig={initialConfig}>
      {!readOnly && <InlineTextFormatToolbarPlugin />}
      {!readOnly && (
        <div className="px-3 py-1.5 border-b border-gray-200 dark:border-gray-800 bg-gray-50/60 dark:bg-gray-900/40 flex items-center justify-between shrink-0">
          <p className="text-[11px] text-gray-500 dark:text-gray-500">
            Tip: type <kbd className="px-1 py-0.5 rounded border bg-white dark:bg-gray-800 font-mono text-[10px]">/</kbd> to insert a block
          </p>
          <BlockInsertButtonPlugin />
        </div>
      )}
      <div className="relative">
        <LexicalRichTextPlugin
          contentEditable={
            <ContentEditable
              className="px-4 py-3 outline-none text-base text-gray-900 dark:text-gray-100 min-h-[80px]"
              readOnly={readOnly}
              tabIndex={readOnly ? -1 : 0}
            />
          }
          placeholder={
            <div className="pointer-events-none absolute left-4 top-3 select-none text-gray-400 dark:text-gray-600">
              Start writing… press / to insert a block
            </div>
          }
          ErrorBoundary={LexicalErrorBoundary}
        />
        {!readOnly && <FloatingTextFormatToolbarPlugin />}
        <HistoryPlugin />
        <ListPlugin />
        <LinkPlugin />
        <BlockEmbedPlugin />
        {!readOnly && <BlockInsertMenuPlugin />}
        {!readOnly && <OnChangePlugin onChange={handleChange} ignoreSelectionChange />}
      </div>
    </LexicalComposer>
  )
}
