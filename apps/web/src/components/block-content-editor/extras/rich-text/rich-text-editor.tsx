"use client"

import { useCallback, useMemo, useRef, useEffect, useState } from "react"
import { LexicalComposer } from "@lexical/react/LexicalComposer"
import { RichTextPlugin as LexicalRichTextPlugin } from "@lexical/react/LexicalRichTextPlugin"
import { ContentEditable } from "@lexical/react/LexicalContentEditable"
import { HistoryPlugin } from "@lexical/react/LexicalHistoryPlugin"
import { OnChangePlugin } from "@lexical/react/LexicalOnChangePlugin"
import { ListPlugin } from "@lexical/react/LexicalListPlugin"
import { LinkPlugin } from "@lexical/react/LexicalLinkPlugin"
import { LexicalErrorBoundary } from "@lexical/react/LexicalErrorBoundary"
import { type EditorState } from "lexical"
import { Save, FileText } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { FloatingTextFormatToolbarPlugin } from "../../plugins/floating-text-format-toolbar-plugin"
import { InlineTextFormatToolbarPlugin } from "../../plugins/inline-text-format-toolbar-plugin"
import { BlockEmbedPlugin } from "../../plugins/block-embed-plugin"
import { BlockInsertMenuPlugin } from "../../plugins/block-insert-menu-plugin"
import { BlockInsertButtonPlugin } from "../../plugins/block-insert-button-plugin"
import { useEditorSettings } from "@/components/block-content-editor/extras/settings-menu"
import { BlockEditorShell } from "@/components/block-content-editor/extras/block-editor-shell"
import { SHARED_LEXICAL_NODES, SHARED_LEXICAL_THEME } from "../../lib/lexical"
import type { RichTextData } from "../../nodes/rich-text-node"

const RT_THEME = SHARED_LEXICAL_THEME

const RT_NODES = SHARED_LEXICAL_NODES

interface RichTextEditorProps {
  initialData?: RichTextData
  onSave: (data: RichTextData) => void
  onCancel: () => void
}

export function RichTextEditor({ initialData, onSave, onCancel }: RichTextEditorProps) {
  const [title, setTitle] = useState(initialData?.title || "")
  const editorStateRef = useRef<string>(initialData?.content || "")
  const settings = useEditorSettings("rich-text")

  const handleChange = useCallback((editorState: EditorState) => {
    const serialized = JSON.stringify(editorState.toJSON())
    editorStateRef.current = serialized
  }, [])

  const handleSave = useCallback(() => {
    onSave({
      content: editorStateRef.current,
      title: title || undefined,
    })
  }, [onSave, title])

  const initialConfig = useMemo(
    () => ({
      namespace: "RichTextEditor",
      nodes: RT_NODES,
      theme: RT_THEME,
      editable: true,
      editorState: initialData?.content || undefined,
      onError: (error: Error) => {
        console.error("[RichTextEditor]", error)
      },
    }),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [],
  )

  return (
    <BlockEditorShell
      settings={settings}
      includeMonacoTheme={false}
      onClose={onCancel}
      icon={<FileText className="h-5 w-5 text-blue-600 dark:text-blue-400" />}
      title="Rich Text Editor"
      headerActions={
        <div className="flex items-center gap-2">
          <Label htmlFor="rt-title" className="text-sm text-gray-600 dark:text-gray-400 whitespace-nowrap">
            Title
          </Label>
          <Input
            id="rt-title"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="Section title (optional)"
            className="h-8 w-48 text-sm"
          />
        </div>
      }
      footer={
        <div className="flex gap-2 justify-end">
          <Button
            variant="outline"
            onClick={onCancel}
            className="border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent"
          >
            Cancel
          </Button>
          <Button
            onClick={handleSave}
            className="flex items-center gap-2 bg-blue-600 hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600"
          >
            <Save className="h-4 w-4" />
            Save
          </Button>
        </div>
      }
    >
      {/* Main Content — single focused editor, centered.
          The OUTER container scrolls so the wheel works anywhere inside the
          modal body (including the side gutters), not only over the column.
          Block layout (not flex) lets the inner column grow with content so
          the scroll content height matches the column, and the column's
          background covers the full scrolled area. */}
      <div className="flex-1 overflow-auto bg-gray-50 dark:bg-gray-950">
        <div className="w-full max-w-3xl mx-auto flex flex-col bg-white dark:bg-gray-900 border-x border-gray-200 dark:border-gray-800 shadow-sm min-h-full">
          <LexicalComposer initialConfig={initialConfig}>
            {/* Sticky toolbars so they stay pinned while the outer area scrolls */}
            <div className="sticky top-0 z-10 bg-white dark:bg-gray-900">
              <InlineTextFormatToolbarPlugin />
              {/* Top insert toolbar — pinned above the scrollable content */}
              <div className="px-4 py-2 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900 flex items-center justify-between">
                <p className="text-xs text-gray-500 dark:text-gray-500">
                  Tip: type <kbd className="px-1.5 py-0.5 rounded border bg-white dark:bg-gray-800 font-mono">/</kbd> to insert a block
                </p>
                <BlockInsertButtonPlugin />
              </div>
            </div>
            <div className="relative flex-1 flex flex-col">
              <LexicalRichTextPlugin
                contentEditable={
                  <ContentEditable
                    className="flex-1 px-8 py-6 outline-none text-base text-gray-900 dark:text-gray-100 max-w-none"
                  />
                }
                placeholder={
                  <div className="pointer-events-none absolute left-8 top-6 select-none text-gray-400 dark:text-gray-600">
                    Start writing your rich text content...
                  </div>
                }
                ErrorBoundary={LexicalErrorBoundary}
              />
              <FloatingTextFormatToolbarPlugin />
              <HistoryPlugin />
              <ListPlugin />
              <LinkPlugin />
              <BlockEmbedPlugin />
              <BlockInsertMenuPlugin />
              <OnChangePlugin onChange={handleChange} ignoreSelectionChange />
            </div>
          </LexicalComposer>
        </div>
      </div>
    </BlockEditorShell>
  )
}
