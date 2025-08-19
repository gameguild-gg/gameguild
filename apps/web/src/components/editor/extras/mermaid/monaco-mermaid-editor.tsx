"use client"

import { useRef, useCallback, useEffect } from "react"
import dynamic from "next/dynamic"
import type { editor } from "monaco-editor"
import type { OnMount } from "@monaco-editor/react"
import { mermaidLanguageConfig, mermaidTokensProvider, mermaidTheme } from "./mermaid-language"
import { MermaidValidator, type MermaidValidationResult } from "./mermaid-validator"
import { createMermaidCompletionProvider } from "./mermaid-completion-provider"

const MonacoEditor = dynamic(() => import("@monaco-editor/react"), {
  ssr: false,
  loading: () => <div className="flex items-center justify-center h-full">Loading editor...</div>,
})

interface MonacoMermaidEditorProps {
  value: string
  onChange: (value: string | undefined) => void
  onValidationChange?: (result: MermaidValidationResult) => void
  height?: string | number
  theme?: "light" | "dark"
  readOnly?: boolean
}

export function MonacoMermaidEditor({
  value,
  onChange,
  onValidationChange,
  height = "100%",
  theme = "light",
  readOnly = false,
}: MonacoMermaidEditorProps) {
  const editorRef = useRef<editor.IStandaloneCodeEditor | null>(null)
  const isLanguageRegistered = useRef(false)
  const validationTimeoutRef = useRef<NodeJS.Timeout | undefined>(undefined)

  const validateCode = useCallback(
    async (code: string) => {
      // Clear previous timeout
      if (validationTimeoutRef.current) {
        clearTimeout(validationTimeoutRef.current)
      }

      // Debounce validation to avoid excessive API calls
      validationTimeoutRef.current = setTimeout(async () => {
        try {
          // Quick validation first
          const quickResult = MermaidValidator.quickValidate(code)
          if (!quickResult.isValid) {
            onValidationChange?.(quickResult)
            return
          }

          // Full validation with mermaid parser
          const result = await MermaidValidator.validateCode(code)
          onValidationChange?.(result)

          // Update editor markers for errors
          if (editorRef.current && !result.isValid && result.line) {
            const monaco = await import("monaco-editor")
            const model = editorRef.current.getModel()
            if (model) {
              monaco.editor.setModelMarkers(model, "mermaid", [
                {
                  startLineNumber: result.line,
                  startColumn: 1,
                  endLineNumber: result.line,
                  endColumn: model.getLineMaxColumn(result.line),
                  message: result.error || "Syntax error",
                  severity: monaco.MarkerSeverity.Error,
                },
              ])
            }
          } else if (editorRef.current && result.isValid) {
            // Clear markers if validation passes
            const monaco = await import("monaco-editor")
            const model = editorRef.current.getModel()
            if (model) {
              monaco.editor.setModelMarkers(model, "mermaid", [])
            }
          }
        } catch (error) {
          console.error("Validation error:", error)
          onValidationChange?.({
            isValid: false,
            error: "Failed to validate code",
          })
        }
      }, 500) // 500ms debounce
    },
    [onValidationChange],
  )

  useEffect(() => {
    if (value && onValidationChange) {
      validateCode(value)
    }
  }, [value, validateCode, onValidationChange])
  const handleEditorDidMount: OnMount = (editor, monaco) => {
    editorRef.current = editor as unknown as editor.IStandaloneCodeEditor
    //editorRef.current = editor

    // Register Mermaid language only once
    if (!isLanguageRegistered.current) {
      // Register the language
      monaco.languages.register({ id: "mermaid" })

      // Set language configuration
      monaco.languages.setLanguageConfiguration("mermaid", mermaidLanguageConfig)

      // Set syntax highlighting
      monaco.languages.setMonarchTokensProvider("mermaid", mermaidTokensProvider)

      // Define custom theme
      monaco.editor.defineTheme("mermaid-light", mermaidTheme)

      // Define dark theme
      monaco.editor.defineTheme("mermaid-dark", {
        ...mermaidTheme,
        base: "vs-dark",
        rules: [
          { token: "comment", foreground: "6a737d", fontStyle: "italic" },
          { token: "keyword.diagram", foreground: "ff7b72", fontStyle: "bold" },
          { token: "keyword.direction", foreground: "79c0ff", fontStyle: "bold" },
          { token: "keyword", foreground: "ff7b72" },
          { token: "operator.arrow", foreground: "ffa657", fontStyle: "bold" },
          { token: "string", foreground: "a5d6ff" },
          { token: "string.escape", foreground: "ffa657" },
          { token: "identifier", foreground: "d2a8ff" },
          { token: "number", foreground: "79c0ff" },
          { token: "delimiter.bracket", foreground: "f0f6fc" },
          { token: "type.entity", foreground: "7ee787", fontStyle: "bold" },
          { token: "keyword.relationship", foreground: "f85149" },
          { token: "operator.er", foreground: "ffa657", fontStyle: "bold" },
          { token: "keyword.state.start", foreground: "ff7b72", fontStyle: "bold" },
          { token: "keyword.state.definition", foreground: "d2a8ff" },
          { token: "keyword.state", foreground: "79c0ff" },
          { token: "keyword.pie", foreground: "ff7b72", fontStyle: "bold" },
          { token: "string.pie.data", foreground: "7ee787" },
          { token: "keyword.pie.option", foreground: "d2a8ff" },
        ],
        colors: {
          "editor.background": "#0d1117",
          "editor.foreground": "#f0f6fc",
          "editorLineNumber.foreground": "#7d8590",
          "editorLineNumber.activeForeground": "#f0f6fc",
        },
      })

      monaco.languages.registerCompletionItemProvider("mermaid", createMermaidCompletionProvider(monaco))

      isLanguageRegistered.current = true
    }

    // Set the theme
    monaco.editor.setTheme(theme === "dark" ? "mermaid-dark" : "mermaid-light")

    editor.onDidChangeModelContent(() => {
      const currentValue = editor.getValue()
      if (onValidationChange) {
        validateCode(currentValue)
      }
    })
  }

  const handleChange = useCallback(
    (newValue: string | undefined) => {
      onChange(newValue)
      if (newValue && onValidationChange) {
        validateCode(newValue)
      }
    },
    [onChange, validateCode, onValidationChange],
  )

  return (
    <MonacoEditor
      height={height}
      language="mermaid"
      value={value}
      onChange={handleChange}
      onMount={handleEditorDidMount}
      theme={theme === "dark" ? "mermaid-dark" : "mermaid-light"}
      options={{
        minimap: { enabled: false },
        scrollBeyondLastLine: false,
        fontSize: 14,
        lineNumbers: "on",
        wordWrap: "on",
        automaticLayout: true,
        tabSize: 2,
        insertSpaces: true,
        folding: true,
        lineDecorationsWidth: 10,
        lineNumbersMinChars: 3,
        renderLineHighlight: "line",
        selectOnLineNumbers: true,
        roundedSelection: false,
        readOnly,
        cursorStyle: "line",
        fontFamily: "Monaco, Menlo, 'Ubuntu Mono', monospace",
        suggestOnTriggerCharacters: true,
        quickSuggestions: true,
        parameterHints: { enabled: true },
        autoIndent: "full",
        formatOnPaste: true,
        formatOnType: true,
        renderValidationDecorations: "on",
        showUnused: true,
        showDeprecated: true,
      }}
    />
  )
}
