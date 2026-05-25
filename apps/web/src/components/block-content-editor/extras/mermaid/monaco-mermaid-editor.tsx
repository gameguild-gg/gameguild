"use client"

import { useRef, useCallback, useEffect } from "react"
import type { editor, IDisposable } from "monaco-editor"
import type { Monaco, OnMount } from "@monaco-editor/react"
import { mermaidLanguageConfig, mermaidTokensProvider, mermaidTheme } from "./mermaid-language"
import { MermaidValidator, type MermaidValidationResult } from "./mermaid-validator"
import { createMermaidCompletionProvider } from "./mermaid-completion-provider"
import { isShikiActive } from "@/components/block-content-editor/lib/shiki/highlighter"
import { BaseMonacoEditor } from "@/components/block-content-editor/lib/monaco"
import type { ShikiTheme } from "@/components/block-content-editor/lib/shiki/themes"
import type {
  MonacoOptionsPreferences,
  RenderWhitespace,
  RenderLineHighlight,
} from "@/components/block-content-editor/lib/storage/editor/editor-preferences"

interface MonacoMermaidEditorProps {
  value: string
  onChange: (value: string | undefined) => void
  onValidationChange?: (result: MermaidValidationResult) => void
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

export function MonacoMermaidEditor({
  value,
  onChange,
  onValidationChange,
  height = "100%",
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
}: MonacoMermaidEditorProps) {
  // The `options` bag wins when provided; the individual props remain
  // as a defensive fallback for tests / call sites that haven't yet
  // migrated to the preferences hook.
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
  const isLanguageRegistered = useRef(false)
  const completionProviderDisposable = useRef<IDisposable | null>(null)
  const validationTimeoutRef = useRef<NodeJS.Timeout | undefined>(undefined)

  const validateCode = useCallback(
    async (code: string) => {
      if (validationTimeoutRef.current) {
        clearTimeout(validationTimeoutRef.current)
      }

      validationTimeoutRef.current = setTimeout(async () => {
        try {
          const quickResult = MermaidValidator.quickValidate(code)
          if (!quickResult.isValid) {
            onValidationChange?.(quickResult)
            return
          }

          const result = await MermaidValidator.validateCode(code)
          onValidationChange?.(result)

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
      }, 500)
    },
    [onValidationChange],
  )

  // Cleanup on unmount.
  useEffect(
    () => () => {
      completionProviderDisposable.current?.dispose()
      completionProviderDisposable.current = null
      if (validationTimeoutRef.current) {
        clearTimeout(validationTimeoutRef.current)
      }
    },
    [],
  )

  useEffect(() => {
    if (value && onValidationChange) {
      validateCode(value)
    }
  }, [value, validateCode, onValidationChange])

  const registerMermaidLanguage = useCallback((monaco: Monaco) => {
    if (isLanguageRegistered.current) return
    monaco.languages.register({ id: "mermaid" })
    monaco.languages.setLanguageConfiguration("mermaid", mermaidLanguageConfig)
    monaco.languages.setMonarchTokensProvider("mermaid", mermaidTokensProvider)

    // Define the custom mermaid-light/mermaid-dark themes only when
    // Shiki hasn't taken over — otherwise Shiki's theme registration
    // wins and these would be unused.
    if (!isShikiActive()) {
      monaco.editor.defineTheme("mermaid-light", mermaidTheme)
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
    }

    completionProviderDisposable.current = monaco.languages.registerCompletionItemProvider(
      "mermaid",
      createMermaidCompletionProvider(monaco),
    )
    isLanguageRegistered.current = true
  }, [])

  const handleMount: OnMount = (ed) => {
    editorRef.current = ed
    ed.onDidChangeModelContent(() => {
      if (onValidationChange) {
        validateCode(ed.getValue())
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
    <BaseMonacoEditor
      language="mermaid"
      height={height}
      value={value}
      onChange={handleChange}
      readOnly={readOnly}
      isDark={theme === "dark"}
      fallbackLight="mermaid-light"
      fallbackDark="mermaid-dark"
      options={effectiveOptions}
      beforeMount={registerMermaidLanguage}
      onMount={handleMount}
      extraOptions={{
        folding: true,
        lineDecorationsWidth: 10,
        lineNumbersMinChars: 3,
        selectOnLineNumbers: true,
        roundedSelection: false,
        cursorStyle: "line",
        fontFamily: "Monaco, Menlo, 'Ubuntu Mono', monospace",
        suggestOnTriggerCharacters: true,
        quickSuggestions: true,
        wordBasedSuggestions: "off",
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
