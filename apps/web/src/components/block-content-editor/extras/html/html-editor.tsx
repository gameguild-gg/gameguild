"use client"

import { useState, useEffect, useRef } from "react"
import { Button } from "@/components/ui/button"
import { Save, Code2, Plus } from "lucide-react"
import { Switch } from "@/components/ui/switch"
import { Label } from "@/components/ui/label"
import { useTheme } from "next-themes"
import { BaseMonacoEditor } from "@/components/block-content-editor/lib/monaco"
import DOMPurify from "dompurify"
import type { HTMLData } from "@/components/block-content-editor/nodes/html-node"
import { useEditorSettings } from "../settings-menu"
import { BlockEditorShell } from "@/components/block-content-editor/extras/block-editor-shell"
import { TemplateBar } from "./components/template-bar"
import type { HTMLTemplate } from "./templates"

interface HTMLEditorProps {
  initialData?: HTMLData
  onSave: (data: HTMLData) => void
  onCancel: () => void
}

export function HTMLEditor({ initialData, onSave, onCancel }: HTMLEditorProps) {
  const { resolvedTheme } = useTheme()
  const isDarkMode = resolvedTheme === "dark"

  const [content, setContent] = useState(initialData?.content || "")
  const [sandboxScripts, setSandboxScripts] = useState(false)
  const [showTemplates, setShowTemplates] = useState(!initialData?.content)
  const [selectedTemplate, setSelectedTemplate] = useState<HTMLTemplate | null>(null)
  const settings = useEditorSettings("html")
  const iframeRef = useRef<HTMLIFrameElement>(null)
  const editorRef = useRef<any>(null)

  // Auto-resize iframe to content height
  useEffect(() => {
    const iframe = iframeRef.current
    if (!iframe) return
    const onLoad = () => {
      try {
        const doc = iframe.contentDocument || iframe.contentWindow?.document
        if (doc?.body) {
          iframe.style.height = doc.body.scrollHeight + 32 + "px"
        }
      } catch {
        // cross-origin, ignore
      }
    }
    iframe.addEventListener("load", onLoad)
    return () => iframe.removeEventListener("load", onLoad)
  }, [content, sandboxScripts])

  const handleSave = () => {
    onSave({ content })
  }

  const handleEditorMount = (editor: any) => {
    editorRef.current = editor
  }

  const insertAtCursor = (code: string) => {
    const editor = editorRef.current
    if (!editor) {
      setContent((prev) => prev + "\n" + code)
      return
    }
    const position = editor.getPosition()
    const model = editor.getModel()
    if (model && position) {
      editor.executeEdits("insert-template", [
        {
          range: {
            startLineNumber: position.lineNumber,
            startColumn: position.column,
            endLineNumber: position.lineNumber,
            endColumn: position.column,
          },
          text: code,
          forceMoveMarkers: true,
        },
      ])
      setContent(model.getValue())
      setSelectedTemplate(null)
      editor.focus()
    }
  }

  const getPreviewDoc = () => {
    const sanitized = sandboxScripts ? content : DOMPurify.sanitize(content)
    return `<!DOCTYPE html>
<html>
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <base target="_blank">
  <style>
    *, *::before, *::after { box-sizing: border-box; }
    body { margin: 0; padding: 16px; font-family: system-ui, -apple-system, sans-serif; color: ${isDarkMode ? "#e5e7eb" : "#1f2937"}; background: ${isDarkMode ? "#0a0a0a" : "#ffffff"}; }
    img, video, iframe { max-width: 100%; }
  </style>
</head>
<body>${sanitized}</body>
</html>`
  }

  const placeholder = `<div style="max-width:600px;margin:0 auto;padding:20px;text-align:center;font-family:system-ui,sans-serif">
  <h1>Hello, World!</h1>
  <p>Write your HTML here. The live preview updates as you type.</p>
  <button style="padding:10px 20px;background:#0070f3;color:#fff;border:none;border-radius:5px;cursor:pointer">
    Click Me!
  </button>
</div>`

  return (
    <BlockEditorShell
      settings={settings}
      onClose={onCancel}
      icon={<Code2 className="h-5 w-5 text-orange-600 dark:text-orange-400" />}
      title="HTML Editor"
      headerActions={
        <>
          <div className="flex items-center gap-2">
            <Switch
              id="sandbox-scripts"
              checked={sandboxScripts}
              onCheckedChange={setSandboxScripts}
            />
            <Label htmlFor="sandbox-scripts" className="text-sm text-gray-600 dark:text-gray-400 cursor-pointer">
              Allow scripts
            </Label>
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setShowTemplates(!showTemplates)}
            className="border-gray-300 dark:border-gray-600 hover:bg-gray-100 dark:hover:bg-gray-800"
          >
            <Plus className="h-4 w-4 mr-1" />
            Templates
          </Button>
        </>
      }
      secondaryHeader={
        showTemplates ? (
          <TemplateBar
            onInsert={insertAtCursor}
            onClose={() => setShowTemplates(false)}
            selectedTemplate={selectedTemplate}
            onSelectTemplate={setSelectedTemplate}
          />
        ) : undefined
      }
      footer={
        <div className="flex gap-2 justify-end">
          <Button
            variant="outline"
            onClick={onCancel}
            disabled={showTemplates && selectedTemplate !== null}
            className="border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800 bg-transparent disabled:opacity-50 disabled:cursor-not-allowed"
          >
            Cancel
          </Button>
          <Button
            onClick={handleSave}
            disabled={showTemplates && selectedTemplate !== null}
            className="flex items-center gap-2 bg-orange-600 hover:bg-orange-700 dark:bg-orange-500 dark:hover:bg-orange-600 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <Save className="h-4 w-4" />
            Save HTML
          </Button>
        </div>
      }
    >
      {/* Main Content */}
      <div className="flex-1 overflow-hidden flex">
          {/* Left Panel - Monaco Editor */}
          <div className="w-1/2 border-r border-gray-200 dark:border-gray-800 flex flex-col">
            <div className="p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900">
              <h3 className="text-sm font-medium text-gray-800 dark:text-gray-200 uppercase tracking-wide">Editor</h3>
            </div>
            <div className="flex-1 overflow-hidden">
              <BaseMonacoEditor
                language="html"
                value={content}
                onChange={(value) => setContent(value || "")}
                onMount={handleEditorMount}
                isDark={isDarkMode}
                options={settings.editor}
                extraOptions={{
                  roundedSelection: true,
                  autoClosingBrackets: "always",
                  autoClosingQuotes: "always",
                  formatOnPaste: true,
                  suggest: { showWords: true },
                }}
              />
            </div>
          </div>

          {/* Right Panel - Live Preview */}
          <div className="w-1/2 flex flex-col">
            <div className="p-4 border-b border-gray-200 dark:border-gray-800 bg-gray-50 dark:bg-gray-900 flex items-center justify-between">
              <h3 className="text-sm font-medium text-gray-800 dark:text-gray-200 uppercase tracking-wide">Live Preview</h3>
              <div className="flex items-center gap-2">
                {selectedTemplate && (
                  <span className="text-xs px-2 py-1 bg-orange-100 dark:bg-orange-900/30 text-orange-700 dark:text-orange-300 rounded">
                    Preview: {selectedTemplate.title}
                  </span>
                )}
                {!sandboxScripts && (
                  <span className="text-[10px] px-2 py-0.5 rounded bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-300">
                    Scripts disabled (sanitized)
                  </span>
                )}
              </div>
            </div>
            <div className="flex-1 overflow-auto bg-white dark:bg-gray-950">
              {selectedTemplate ? (
                <div className="p-6 space-y-4">
                  <div className="p-3 bg-orange-50 dark:bg-orange-900/10 border border-orange-200 dark:border-orange-800 rounded-lg">
                    <p className="text-sm text-orange-800 dark:text-orange-200 font-medium">
                      Template Preview: {selectedTemplate.title}
                    </p>
                    <p className="text-xs text-orange-600 dark:text-orange-300 mt-1">
                      {selectedTemplate.description}
                    </p>
                  </div>
                  <iframe
                    srcDoc={(() => {
                      const code = DOMPurify.sanitize(selectedTemplate.code)
                      return `<!DOCTYPE html><html><head><meta charset="utf-8"><style>*,*::before,*::after{box-sizing:border-box}body{margin:0;padding:16px;font-family:system-ui,-apple-system,sans-serif;color:${isDarkMode ? "#e5e7eb" : "#1f2937"};background:${isDarkMode ? "#0a0a0a" : "#ffffff"}}</style></head><body>${code}</body></html>`
                    })()}
                    className="w-full border-0 rounded-md"
                    style={{ minHeight: 200 }}
                    sandbox=""
                    title="Template Preview"
                  />
                </div>
              ) : content ? (
                <iframe
                  ref={iframeRef}
                  srcDoc={getPreviewDoc()}
                  className="w-full min-h-full border-0"
                  sandbox={sandboxScripts ? "allow-scripts allow-popups" : ""}
                  title="HTML Preview"
                />
              ) : (
                <div className="flex items-center justify-center h-full">
                  <p className="text-gray-400 dark:text-gray-600 italic">
                    Your HTML preview will appear here...
                  </p>
                </div>
              )}
            </div>
          </div>
        </div>
      </BlockEditorShell>
  )
}
