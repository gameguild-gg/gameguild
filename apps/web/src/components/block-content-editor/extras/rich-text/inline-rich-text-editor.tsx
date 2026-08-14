"use client"

/**
 * Inline editable rich-text editor used directly inside a block card —
 * no modal. Thin wrapper around the unified `<LexicalSurface />`.
 *
 * Top toolbar is hidden because this editor lives inside a constrained
 * block card; bubble toolbars cover the formatting needs.
 */

import { useCallback, useEffect, useRef, useState } from "react"
import type { LexicalEditor, SerializedEditorState } from "lexical"

import { LexicalSurface } from "@game-guild/lexical-surface"
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

  const lastEmittedRef = useRef<SerializedEditorState | null | undefined>(data?.content)
  const [mountKey, setMountKey] = useState(0)
  const seededContentRef = useRef<SerializedEditorState | null | undefined>(data?.content)

  useEffect(() => {
    const incoming = data?.content
    if (incoming !== lastEmittedRef.current) {
      seededContentRef.current = incoming
      lastEmittedRef.current = incoming
      setMountKey((k) => k + 1)
    }
  }, [data?.content])

  const handleChange = useCallback((serialized: SerializedEditorState, _editor: LexicalEditor) => {
    lastEmittedRef.current = serialized
    onChangeRef.current({
      content: serialized,
      title: titleRef.current,
    })
  }, [])

  return (
    <LexicalSurface
      namespace="InlineRichTextEditor"
      mountKey={mountKey}
      readOnly={readOnly}
      initialState={seededContentRef.current ?? null}
      onChange={handleChange}
      placeholder="Start writing…"
      contentClassName="min-h-[80px]"
      features={{ toolbar: false, pageLayout: false }}
    />
  )
}
