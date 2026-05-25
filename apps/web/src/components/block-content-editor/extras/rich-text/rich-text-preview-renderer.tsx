"use client"

import { useMemo } from "react"
import { LexicalComposer } from "@lexical/react/LexicalComposer"
import { RichTextPlugin } from "@lexical/react/LexicalRichTextPlugin"
import { ContentEditable } from "@lexical/react/LexicalContentEditable"
import { LexicalErrorBoundary } from "@lexical/react/LexicalErrorBoundary"
import { SHARED_LEXICAL_NODES } from "../../lib/lexical"

const PREVIEW_THEME = {
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
    h4: "text-base font-bold my-1",
    h5: "text-sm font-bold my-1",
  },
  list: {
    ul: "list-disc list-inside ml-4 my-1",
    ol: "list-decimal list-inside ml-4 my-1",
    listitem: "my-0.5",
    nested: {
      listitem: "ml-4",
    },
  },
  quote: "border-l-4 border-gray-300 dark:border-gray-600 pl-3 italic text-gray-600 dark:text-gray-400 my-1",
  code: "bg-gray-100 dark:bg-gray-800 p-2 rounded font-mono text-sm my-1",
  link: "text-blue-600 dark:text-blue-400 underline",
}

const PREVIEW_NODES = SHARED_LEXICAL_NODES

interface RichTextPreviewRendererProps {
  content: string
  className?: string
}

export function RichTextPreviewRenderer({ content, className }: RichTextPreviewRendererProps) {
  // Strip any persisted `selection` from the serialized state. Lexical, even
  // with `editable: false`, will restore that selection on mount, which makes
  // the browser auto-scroll the page so the selection becomes visible — this
  // causes the scrollbar to jump every time a rich-text block hydrates (e.g.
  // when opening a project in the studio or when a static direct section
  // finishes loading).
  const sanitizedContent = useMemo(() => {
    if (!content) return undefined
    try {
      const parsed = JSON.parse(content)
      if (parsed && typeof parsed === "object") {
        delete (parsed as { selection?: unknown }).selection
        return JSON.stringify(parsed)
      }
      return content
    } catch {
      return content
    }
  }, [content])

  const initialConfig = useMemo(
    () => ({
      namespace: "RichTextPreview",
      nodes: PREVIEW_NODES,
      theme: PREVIEW_THEME,
      editable: false,
      editorState: sanitizedContent,
      onError: (error: Error) => {
        console.error("[RichTextPreview]", error)
      },
    }),
    [sanitizedContent],
  )

  if (!content) return null

  return (
    <LexicalComposer key={sanitizedContent} initialConfig={initialConfig}>
      <RichTextPlugin
        contentEditable={
          <ContentEditable
            className={className || "text-sm text-foreground"}
            readOnly
            tabIndex={-1}
          />
        }
        placeholder={null}
        ErrorBoundary={LexicalErrorBoundary}
      />
    </LexicalComposer>
  )
}
