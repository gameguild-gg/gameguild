"use client"

import { useCallback, useEffect, useRef } from "react"
import type { editor, IDisposable, languages, IPosition } from "monaco-editor"
import type { Monaco, OnMount } from "@monaco-editor/react"
import type { VegaLiteValidationResult } from "./vega-lite-validator"
import { VegaLiteValidator } from "./vega-lite-validator"
import { BaseMonacoEditor } from "@/components/block-content-editor/lib/monaco"
import type { ShikiTheme } from "@/components/block-content-editor/lib/shiki/themes"
import type {
  MonacoOptionsPreferences,
  RenderWhitespace,
  RenderLineHighlight,
} from "@/components/block-content-editor/lib/storage/editor/editor-preferences"

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
  const effectiveOptions: MonacoOptionsPreferences = {
    shikiTheme: options?.shikiTheme ?? shikiThemeProp ?? "github",
    fontSize: options?.fontSize ?? fontSizeProp ?? 14,
    lineNumbers: options?.lineNumbers ?? lineNumbersProp ?? true,
    wordWrap: options?.wordWrap ?? wordWrapProp ?? true,
    minimap: options?.minimap ?? minimapProp ?? false,
    tabSize: options?.tabSize ?? tabSizeProp ?? 2,
    renderWhitespace: options?.renderWhitespace ?? renderWhitespaceProp ?? "none",
    renderLineHighlight: options?.renderLineHighlight ?? renderLineHighlightProp ?? "line",
  }

  const editorRef = useRef<editor.IStandaloneCodeEditor | null>(null)
  const completionDisposableRef = useRef<IDisposable | null>(null)
  const validationTimeoutRef = useRef<NodeJS.Timeout | null>(null)

  // Configure JSON diagnostics + fetch the Vega/Vega-Lite schemas before
  // the editor mounts so the schema bindings are in place when the
  // model is created.
  const configureJsonSchemas = useCallback(async (monaco: Monaco) => {
    async function fetchJson(url: string) {
      try {
        const res = await fetch(url)
        if (!res.ok) return undefined
        return await res.json()
      } catch {
        return undefined
      }
    }

    const vegaLiteSchema = await fetchJson(
      "https://cdn.jsdelivr.net/npm/vega-lite@5/build/vega-lite-schema.json",
    )
    const vegaSchema = await fetchJson(
      "https://cdn.jsdelivr.net/npm/vega@5/build/vega-schema.json",
    )

    const schemas: Array<{ uri: string; fileMatch?: string[]; schema?: unknown }> = []
    if (vegaLiteSchema) {
      schemas.push({
        uri: "https://vega.github.io/schema/vega-lite/v5.json",
        fileMatch: ["*"],
        schema: vegaLiteSchema,
      })
    }
    if (vegaSchema) {
      schemas.push({
        uri: "https://vega.github.io/schema/vega/v5.json",
        fileMatch: [],
        schema: vegaSchema,
      })
    }

    ;(monaco.languages as unknown as {
      json?: { jsonDefaults?: { setDiagnosticsOptions: (opts: unknown) => void } }
    }).json?.jsonDefaults?.setDiagnosticsOptions({
      validate: true,
      enableSchemaRequest: false,
      schemas,
    })
  }, [])

  const handleMount: OnMount = (ed, monaco) => {
    editorRef.current = ed

    // Format on Ctrl/Cmd+S.
    ed.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS, () => {
      ed.getAction("editor.action.formatDocument")?.run()
    })

    // Register Vega-Lite specific snippets + field/mark enum hints on
    // the JSON language. Stored on a ref so we can dispose on unmount.
    completionDisposableRef.current = monaco.languages.registerCompletionItemProvider("json", {
      provideCompletionItems: (model: editor.ITextModel, position: IPosition) => {
        const word = model.getWordUntilPosition(position)
        const range = {
          startLineNumber: position.lineNumber,
          endLineNumber: position.lineNumber,
          startColumn: word.startColumn,
          endColumn: word.endColumn,
        }

        const suggestions: languages.CompletionItem[] = []

        const snippets: languages.CompletionItem[] = [
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
            range,
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
            range,
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
            range,
          },
        ]

        const fieldTypes = ["quantitative", "temporal", "ordinal", "nominal", "geojson"]
        fieldTypes.forEach((type) => {
          suggestions.push({
            label: type,
            kind: monaco.languages.CompletionItemKind.Enum,
            insertText: `"${type}"`,
            documentation: `Vega-Lite field type: ${type}`,
            range,
          })
        })

        const markTypes = [
          "bar", "line", "circle", "square", "point", "area", "rect", "rule",
          "text", "tick", "boxplot", "errorband", "errorbar", "arc", "geoshape",
          "image", "trail",
        ]
        markTypes.forEach((mark) => {
          suggestions.push({
            label: mark,
            kind: monaco.languages.CompletionItemKind.Enum,
            insertText: `"${mark}"`,
            documentation: `Vega-Lite mark type: ${mark}`,
            range,
          })
        })

        return { suggestions: [...snippets, ...suggestions] }
      },
    })
  }

  // Cleanup the JSON completion provider (registered language-wide, so
  // it must be disposed explicitly when this editor unmounts).
  useEffect(
    () => () => {
      completionDisposableRef.current?.dispose()
      completionDisposableRef.current = null
      if (validationTimeoutRef.current) {
        clearTimeout(validationTimeoutRef.current)
      }
    },
    [],
  )

  const handleChange = useCallback(
    (next: string | undefined) => {
      onChange?.(next)
      if (validationTimeoutRef.current) {
        clearTimeout(validationTimeoutRef.current)
      }
      if (onValidationChange) {
        validationTimeoutRef.current = setTimeout(async () => {
          const result = await VegaLiteValidator.validateSpec(next ?? "")
          onValidationChange(result)
        }, 500)
      }
    },
    [onChange, onValidationChange],
  )

  return (
    <div
      style={{ height: typeof height === "number" ? `${height}px` : height }}
      className="border border-gray-200 dark:border-gray-600 rounded-lg overflow-hidden"
    >
      <BaseMonacoEditor
        language="json"
        height="100%"
        value={value}
        onChange={handleChange}
        readOnly={readOnly}
        isDark={theme === "dark"}
        options={effectiveOptions}
        beforeMount={configureJsonSchemas}
        onMount={handleMount}
        extraOptions={{
          folding: true,
          bracketPairColorization: { enabled: true },
          formatOnPaste: true,
          formatOnType: true,
          suggest: { showKeywords: true, showSnippets: true, showProperties: true },
          quickSuggestions: { other: true, comments: false, strings: true },
        }}
      />
    </div>
  )
}
