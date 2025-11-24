"use client"

import { useRef, useEffect } from "react"
import * as monaco from "monaco-editor"
import type { VegaLiteValidationResult } from "./vega-lite-validator"
import { VegaLiteValidator } from "./vega-lite-validator"

interface MonacoVegaLiteEditorProps {
  value: string
  onChange?: (value: string | undefined) => void
  onValidationChange?: (result: VegaLiteValidationResult) => void
  height?: string | number
  theme?: "light" | "dark"
  readOnly?: boolean
}

export function MonacoVegaLiteEditor({
  value,
  onChange,
  onValidationChange,
  height = "400px",
  theme = "light",
  readOnly = false,
}: MonacoVegaLiteEditorProps) {
  const containerRef = useRef<HTMLDivElement>(null)
  const editorRef = useRef<monaco.editor.IStandaloneCodeEditor | null>(null)
  const validationTimeoutRef = useRef<NodeJS.Timeout | null>(null)

  useEffect(() => {
    if (!containerRef.current) return

    // Configure Monaco editor for JSON with offline Vega/Vega-Lite schemas when available
    (async () => {
      let vegaLiteSchema: any | undefined
      let vegaSchema: any | undefined

      // Try to get schemas from the vega-schema package first (offline)
      try {
        // Common path in vega-schema package
        const mod = await import("vega-schema/vega-lite/v5.json")
        vegaLiteSchema = (mod as any).default || mod
      } catch {
        // Fallbacks commonly used by other packages
        try {
          const mod = await import("vega-lite/build/vega-lite-schema.json")
          vegaLiteSchema = (mod as any).default || mod
        } catch {}
      }

      try {
        const mod = await import("vega-schema/vega/v5.json")
        vegaSchema = (mod as any).default || mod
      } catch {
        try {
          const mod = await import("vega/build/vega-schema.json")
          vegaSchema = (mod as any).default || mod
        } catch {}
      }

      // Build the schemas array; keep the official URIs so $ref targets resolve locally
      const schemas: monaco.languages.json.DiagnosticsOptions["schemas"] = []
      if (vegaLiteSchema) {
        schemas.push({
          uri: "https://vega.github.io/schema/vega-lite/v5.json",
          fileMatch: ["*"],
          schema: vegaLiteSchema as any,
        })
      } else {
        // Last resort: still register the URI so Monaco knows the base, but without content it would try network
        schemas.push({
          uri: "https://vega.github.io/schema/vega-lite/v5.json",
          fileMatch: ["*"],
        } as any)
      }

      if (vegaSchema) {
        schemas.push({
          uri: "https://vega.github.io/schema/vega/v5.json",
          fileMatch: [],
          schema: vegaSchema as any,
        })
      }

      monaco.languages.json.jsonDefaults.setDiagnosticsOptions({
        validate: true,
        enableSchemaRequest: false, // avoid network fetch; rely on provided schemas
        schemas,
      })
    })()

    // Create the editor
    const editor = monaco.editor.create(containerRef.current, {
      value: value,
      language: "json",
      theme: theme === "dark" ? "vs-dark" : "vs",
      automaticLayout: true,
      readOnly: readOnly,
      minimap: { enabled: false },
      scrollBeyondLastLine: false,
      wordWrap: "on",
      fontSize: 14,
      lineNumbers: "on",
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

    // Theme change handler
    const updateTheme = () => {
      monaco.editor.setTheme(theme === "dark" ? "vs-dark" : "vs")
    }
    updateTheme()

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

  // Update theme when prop changes
  useEffect(() => {
    monaco.editor.setTheme(theme === "dark" ? "vs-dark" : "vs")
  }, [theme])

  return (
    <div 
      ref={containerRef} 
      style={{ height: typeof height === "number" ? `${height}px` : height }}
      className="border border-gray-200 dark:border-gray-600 rounded-lg overflow-hidden"
    />
  )
}