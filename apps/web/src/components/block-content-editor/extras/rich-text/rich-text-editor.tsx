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
import { Save, FileText, Eye } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { FloatingTextFormatToolbarPlugin } from "../../plugins/floating-text-format-toolbar-plugin"
import { InlineTextFormatToolbarPlugin } from "../../plugins/inline-text-format-toolbar-plugin"
import { BlockEmbedPlugin } from "../../plugins/block-embed-plugin"
import { BlockInsertMenuPlugin } from "../../plugins/block-insert-menu-plugin"
import { BlockInsertButtonPlugin } from "../../plugins/block-insert-button-plugin"
import { RichTextPreviewRenderer } from "./rich-text-preview-renderer"
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
  const [previewContent, setPreviewContent] = useState(initialData?.content || "")
  const editorStateRef = useRef<string>(initialData?.content || "")
  const settings = useEditorSettings("rich-text")

  const handleChange = useCallback((editorState: EditorState) => {
    const serialized = JSON.stringify(editorState.toJSON())
    editorStateRef.current = serialized
    setPreviewContent(serialized)
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
      {/* Main Content - Split Panels */}
      <div className="flex-1 overflow-hidden flex">
          {/* Left Panel — Lexical Editor */}
          <div className="w-1/2 border-r border-gray-200 dark:border-gray-800 flex flex-col min-h-0">
            <div className="p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
              <h3 className="text-sm font-medium text-gray-800 dark:text-gray-200 uppercase tracking-wide">Editor</h3>
              <p className="text-xs text-gray-500 dark:text-gray-500 mt-0.5">Select text to format (bold, italic, headings, lists...)</p>
            </div>
            <LexicalComposer initialConfig={initialConfig}>
              <InlineTextFormatToolbarPlugin />
              {/* Top insert toolbar — pinned above the scrollable content */}
              <div className="px-4 py-2 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900 flex items-center justify-between shrink-0">
                <p className="text-xs text-gray-500 dark:text-gray-500">
                  Tip: type <kbd className="px-1.5 py-0.5 rounded border bg-white dark:bg-gray-800 font-mono">/</kbd> to insert a block
                </p>
                <BlockInsertButtonPlugin />
              </div>
              <div className="flex-1 overflow-auto min-h-0">
                <div className="relative h-full">
                  <LexicalRichTextPlugin
                    contentEditable={
                      <ContentEditable
                        className="px-6 py-4 outline-none text-base text-gray-900 dark:text-gray-100 min-h-full"
                      />
                    }
                    placeholder={
                      <div className="pointer-events-none absolute left-6 top-4 select-none text-gray-400 dark:text-gray-600">
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
              </div>
            </LexicalComposer>
          </div>

          {/* Right Panel — Live Preview */}
          <div className="w-1/2 flex flex-col">
            <div className="p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900 flex items-center gap-2">
              <Eye className="h-4 w-4 text-gray-500 dark:text-gray-400" />
              <h3 className="text-sm font-medium text-gray-800 dark:text-gray-200 uppercase tracking-wide">Live Preview</h3>
            </div>
            <div className="flex-1 overflow-auto bg-white dark:bg-gray-950 p-6">
              {previewContent ? (
                <div className="prose prose-stone dark:prose-invert max-w-none">
                  <RichTextPreviewRenderer content={previewContent} />
                </div>
              ) : (
                <div className="flex items-center justify-center h-full">
                  <p className="text-gray-400 dark:text-gray-600 italic">
                    Your preview will appear here as you type...
                  </p>
                </div>
              )}
            </div>
          </div>
        </div>
    </BlockEditorShell>
  )
}
