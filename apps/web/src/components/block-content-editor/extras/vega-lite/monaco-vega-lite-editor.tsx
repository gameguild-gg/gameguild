"use client"

import * as monaco from "monaco-editor"
import { useEffect, useRef } from "react"
import type { VegaLiteValidationResult } from "./vega-lite-validator"
import { VegaLiteValidator } from "./vega-lite-validator"
import { ensureShikiLoaded, isShikiActive, useShikiReady } from "@/components/block-content-editor/lib/shiki/highlighter"
import { registerMonacoSurface, type MonacoThemeHandle } from "@/components/block-content-editor/lib/shiki/theme-coordinator"
import {
  applyLineHighlightDecoration,
  toMonacoRenderLineHighlight,
} from "@/components/block-content-editor/lib/monaco/line-highlight"
import { getShikiThemeName, type ShikiTheme } from "@/components/block-content-editor/lib/shiki/themes"
import type { MonacoOptionsPreferences, RenderWhitespace, RenderLineHighlight } from "@/components/block-content-editor/lib/storage/editor/editor-preferences"

function resolveMonacoThemeName(isDark: boolean, shikiTheme: ShikiTheme): string {
  if (isShikiActive()) {
    return getShikiThemeName(shikiTheme, isDark)
  }
  return isDark ? "vs-dark" : "vs"
}

interface MonacoVegaLiteEditorProps {
  value: string
  onChange?: (value: string | undefined) => void
  onValidationChange?: (result: VegaLiteValidationResult) => void
  height?: string | number
  theme?: "light" | "dark"
  /**
   * Resolved global Monaco options. When supplied, overrides the
   * individual fallback props below. Pass `settings.editor` from
   * `useEditorSettings`.
   */
  options?: MonacoOptionsPreferences | null
  shikiTheme?: ShikiTheme
  readOnly?: boolean
  fontSize?: number
  lineNumbers?: boolean
  wordWrap?: boolean
  minimap?: boolean
  tabSize?: number
  renderWhitespace?: RenderWhitespace
  renderLineHighlight?: RenderLineHighlight
}

export function MonacoVegaLiteEditor({
  value,
  onChange,
  onValidationChange,
  height = "400px",
  theme = "light",
  options,
  shikiTheme: shikiThemeProp,
  readOnly = false,
  fontSize: fontSizeProp,
  lineNumbers: lineNumbersProp,
  wordWrap: wordWrapProp,
  minimap: minimapProp,
  tabSize: tabSizeProp,
  renderWhitespace: renderWhitespaceProp,
  renderLineHighlight: renderLineHighlightProp,
}: MonacoVegaLiteEditorProps) {
  const shikiTheme: ShikiTheme = options?.shikiTheme ?? shikiThemeProp ?? "github"
  const fontSize = options?.fontSize ?? fontSizeProp ?? 14
  const lineNumbers = options?.lineNumbers ?? lineNumbersProp ?? true
  const wordWrap = options?.wordWrap ?? wordWrapProp ?? true
  const minimap = options?.minimap ?? minimapProp ?? false
  const tabSize = options?.tabSize ?? tabSizeProp ?? 2
  const renderWhitespace: RenderWhitespace = options?.renderWhitespace ?? renderWhitespaceProp ?? "none"
  const renderLineHighlight: RenderLineHighlight = options?.renderLineHighlight ?? renderLineHighlightProp ?? "line"
  const containerRef = useRef<HTMLDivElement>(null)
  const editorRef = useRef<monaco.editor.IStandaloneCodeEditor | null>(null)
  const themeHandleRef = useRef<MonacoThemeHandle | null>(null)
  const validationTimeoutRef = useRef<NodeJS.Timeout | null>(null)
  const shikiReady = useShikiReady()

  useEffect(() => {
    if (!containerRef.current) return

    // Kick off Shiki initialization for this Monaco namespace; the
    // dedicated theme-update effect below will swap to the Shiki theme
    // as soon as it resolves.
    void ensureShikiLoaded(monaco);

    // Configure Monaco editor for JSON; fetch Vega/Vega-Lite schemas at runtime to avoid bundler export issues
    (async () => {
      async function fetchJson(url: string) {
        try {
          const res = await fetch(url)
          if (!res.ok) return undefined
          return await res.json()
        } catch {
          return undefined
        }
      }

      const vegaLiteSchema = await fetchJson('https://cdn.jsdelivr.net/npm/vega-lite@5/build/vega-lite-schema.json')
      const vegaSchema = await fetchJson('https://cdn.jsdelivr.net/npm/vega@5/build/vega-schema.json')

      const schemas: Array<{ uri: string; fileMatch?: string[]; schema?: unknown }> = []
      if (vegaLiteSchema) {
        schemas.push({
          uri: "https://vega.github.io/schema/vega-lite/v5.json",
          fileMatch: ["*"],
          schema: vegaLiteSchema as any,
        })
      }

      if (vegaSchema) {
        schemas.push({
          uri: "https://vega.github.io/schema/vega/v5.json",
          fileMatch: [],
          schema: vegaSchema as any,
        })
      }

      ;(monaco.languages as any).json?.jsonDefaults?.setDiagnosticsOptions({
        validate: true,
        enableSchemaRequest: false,
        schemas,
      })
    })()

    // Create the editor
    const editor = monaco.editor.create(containerRef.current, {
      value: value,
      language: "json",
      theme: resolveMonacoThemeName(theme === "dark", shikiTheme),
      automaticLayout: true,
      readOnly: readOnly,
      minimap: { enabled: minimap },
      scrollBeyondLastLine: false,
      wordWrap: wordWrap ? "on" : "off",
      fontSize: fontSize,
      lineNumbers: lineNumbers ? "on" : "off",
      tabSize,
      renderWhitespace,
      renderLineHighlight: toMonacoRenderLineHighlight(renderLineHighlight),
      folding: true,
      bracketPairColorization: { enabled: true },
      formatOnPaste: true,
      formatOnType: true,
      suggest: {
        showKeywords: true,
        showSnippets: true,
        showProperties: true,
      },
      quickSuggestions: {
        other: true,
        comments: false,
        strings: true,
      },
    })

    editorRef.current = editor

    // Set up change listener
    const changeListener = editor.onDidChangeModelContent(() => {
      const currentValue = editor.getValue()
      onChange?.(currentValue)

      // Debounced validation
      if (validationTimeoutRef.current) {
        clearTimeout(validationTimeoutRef.current)
      }
      validationTimeoutRef.current = setTimeout(async () => {
        if (onValidationChange) {
          const result = await VegaLiteValidator.validateSpec(currentValue)
          onValidationChange(result)
        }
      }, 500)
    })

    // Add custom key bindings
    editor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS, () => {
      // Format document on Ctrl+S
      editor.getAction("editor.action.formatDocument")?.run()
    })

    // Add Vega-Lite specific snippets and completions
    const completionProvider = monaco.languages.registerCompletionItemProvider("json", {
      provideCompletionItems: (model, position) => {
        const word = model.getWordUntilPosition(position)
        const range = {
          startLineNumber: position.lineNumber,
          endLineNumber: position.lineNumber,
          startColumn: word.startColumn,
          endColumn: word.endColumn,
        }

        const suggestions: monaco.languages.CompletionItem[] = []

        // Common Vega-Lite snippets
        const snippets = [
          {
            label: "Basic Bar Chart",
            kind: monaco.languages.CompletionItemKind.Snippet,
            insertText: `{
  "\\$schema": "https://vega.github.io/schema/vega-lite/v5.json",
  "data": {
    "values": [
      {"category": "A", "value": 28},
      {"category": "B", "value": 55},
      {"category": "C", "value": 43}
    ]
  },
  "mark": "bar",
  "encoding": {
    "x": {"field": "category", "type": "nominal"},
    "y": {"field": "value", "type": "quantitative"}
  }
}`,
            insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
            documentation: "Creates a basic bar chart with sample data",
            range: range
          },
          {
            label: "Line Chart",
            kind: monaco.languages.CompletionItemKind.Snippet,
            insertText: `{
  "\\$schema": "https://vega.github.io/schema/vega-lite/v5.json",
  "data": {
    "values": [
      {"date": "2023-01", "value": 100},
      {"date": "2023-02", "value": 120},
      {"date": "2023-03", "value": 110}
    ]
  },
  "mark": "line",
  "encoding": {
    "x": {"field": "date", "type": "temporal"},
    "y": {"field": "value", "type": "quantitative"}
  }
}`,
            insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
            documentation: "Creates a line chart with temporal data",
            range: range
          },
          {
            label: "Scatter Plot",
            kind: monaco.languages.CompletionItemKind.Snippet,
            insertText: `{
  "\\$schema": "https://vega.github.io/schema/vega-lite/v5.json",
  "data": {
    "values": [
      {"x": 10, "y": 20},
      {"x": 30, "y": 45},
      {"x": 50, "y": 80}
    ]
  },
  "mark": "circle",
  "encoding": {
    "x": {"field": "x", "type": "quantitative"},
    "y": {"field": "y", "type": "quantitative"}
  }
}`,
            insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
            documentation: "Creates a scatter plot",
            range: range
          }
        ]

        // Add field type suggestions
        const fieldTypes = ["quantitative", "temporal", "ordinal", "nominal", "geojson"]
        fieldTypes.forEach(type => {
          suggestions.push({
            label: type,
            kind: monaco.languages.CompletionItemKind.Enum,
            insertText: `"${type}"`,
            documentation: `Vega-Lite field type: ${type}`,
            range: range
          })
        })

        // Add mark type suggestions
        const markTypes = ["bar", "line", "circle", "square", "point", "area", "rect", "rule", "text", "tick", "boxplot", "errorband", "errorbar", "arc", "geoshape", "image", "trail"]
        markTypes.forEach(mark => {
          suggestions.push({
            label: mark,
            kind: monaco.languages.CompletionItemKind.Enum,
            insertText: `"${mark}"`,
            documentation: `Vega-Lite mark type: ${mark}`,
            range: range
          })
        })

        return { suggestions: [...snippets, ...suggestions] }
      }
    })

    // Theme change handler — delegated to the global Monaco theme
    // coordinator. The dedicated effect below registers this surface and
    // refreshes on theme changes / Shiki readiness.

    // Cleanup
    return () => {
      changeListener.dispose()
      completionProvider.dispose()
      if (validationTimeoutRef.current) {
        clearTimeout(validationTimeoutRef.current)
      }
      editor.dispose()
    }
  }, [])

  // Update value when prop changes
  useEffect(() => {
    if (editorRef.current && editorRef.current.getValue() !== value) {
      editorRef.current.setValue(value)
    }
  }, [value])

  // Register with the global Monaco theme coordinator so closing this
  // editor restores the theme of any underlying surface (e.g. the
  // code-studio preview rendered behind it in the document).
  const themeStateRef = useRef({ shikiTheme, theme, shikiReady })
  themeStateRef.current = { shikiTheme, theme, shikiReady }
  useEffect(() => {
    const handle = registerMonacoSurface(monaco, () => {
      const { shikiTheme: t, theme: themeMode } = themeStateRef.current
      return resolveMonacoThemeName(themeMode === "dark", t)
    })
    themeHandleRef.current = handle
    return () => {
      handle.unregister()
      themeHandleRef.current = null
    }
  }, [])

  // Refresh the coordinator when theme inputs change (only takes effect
  // if this surface is currently dominant).
  useEffect(() => {
    themeHandleRef.current?.refresh()
  }, [theme, shikiTheme, shikiReady])

  // Update editor options when any Monaco preference changes
  useEffect(() => {
    editorRef.current?.updateOptions({
      fontSize,
      lineNumbers: lineNumbers ? "on" : "off",
      wordWrap: wordWrap ? "on" : "off",
      minimap: { enabled: minimap },
      tabSize,
      renderWhitespace,
      renderLineHighlight: toMonacoRenderLineHighlight(renderLineHighlight),
    })
    applyLineHighlightDecoration(editorRef.current, renderLineHighlight)
  }, [fontSize, lineNumbers, wordWrap, minimap, tabSize, renderWhitespace, renderLineHighlight])

  return (
    <div
      ref={containerRef}
      style={{ height: typeof height === "number" ? `${height}px` : height }}
      className="border border-gray-200 dark:border-gray-600 rounded-lg overflow-hidden"
    />
  )
}