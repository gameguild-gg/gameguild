"use client"

import { useRef, useCallback, useEffect, useState } from "react"
import dynamic from "next/dynamic"
import type { editor, IDisposable } from "monaco-editor"
import type { OnMount } from "@monaco-editor/react"
import { mermaidLanguageConfig, mermaidTokensProvider, mermaidTheme } from "./mermaid-language"
import { MermaidValidator, type MermaidValidationResult } from "./mermaid-validator"
import { createMermaidCompletionProvider } from "./mermaid-completion-provider"
import { MonacoErrorBoundary } from "@/components/block-content-editor/extras/code-studio/monaco-error-boundary"
import { ensureShikiLoaded, isShikiActive, useShikiReady } from "@/components/block-content-editor/lib/shiki/highlighter"
import { registerMonacoSurface, type MonacoThemeHandle } from "@/components/block-content-editor/lib/shiki/theme-coordinator"
import {
  applyLineHighlightDecoration,
  toMonacoRenderLineHighlight,
} from "@/components/block-content-editor/lib/monaco/line-highlight"
import { getShikiThemeName, type ShikiTheme } from "@/components/block-content-editor/lib/shiki/themes"
import type { MonacoOptionsPreferences, RenderWhitespace, RenderLineHighlight } from "@/components/block-content-editor/lib/storage/editor/editor-preferences"

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
  // The `options` bag wins when provided; the individual props remain as
  // a defensive fallback (e.g. for tests or call sites that haven't yet
  // been migrated to the preferences hook).
  const shikiTheme: ShikiTheme = options?.shikiTheme ?? shikiThemeProp ?? "github"
  const fontSize = options?.fontSize ?? fontSizeProp ?? 14
  const lineNumbers = options?.lineNumbers ?? lineNumbersProp ?? true
  const wordWrap = options?.wordWrap ?? wordWrapProp ?? true
  const minimap = options?.minimap ?? minimapProp ?? false
  const tabSize = options?.tabSize ?? tabSizeProp ?? 2
  const renderWhitespace: RenderWhitespace = options?.renderWhitespace ?? renderWhitespaceProp ?? "none"
  const renderLineHighlight: RenderLineHighlight = options?.renderLineHighlight ?? renderLineHighlightProp ?? "line"
  const editorRef = useRef<editor.IStandaloneCodeEditor | null>(null)
  const monacoRef = useRef<typeof import("monaco-editor") | null>(null)
  const themeHandleRef = useRef<MonacoThemeHandle | null>(null)
  const isLanguageRegistered = useRef(false)
  const completionProviderDisposable = useRef<IDisposable | null>(null)
  const validationTimeoutRef = useRef<NodeJS.Timeout | undefined>(undefined)
  const shikiReady = useShikiReady()
  const [monacoReady, setMonacoReady] = useState(false)

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

  // Cleanup effect
  useEffect(() => {
    return () => {
      // Dispose completion provider on unmount
      if (completionProviderDisposable.current) {
        completionProviderDisposable.current.dispose()
        completionProviderDisposable.current = null
      }
      // Clear validation timeout on unmount
      if (validationTimeoutRef.current) {
        clearTimeout(validationTimeoutRef.current)
      }
    }
  }, [])

  useEffect(() => {
    if (value && onValidationChange) {
      validateCode(value)
    }
  }, [value, validateCode, onValidationChange])

  // Register with the global Monaco theme coordinator so this editor
  // owns the theme while mounted and gracefully yields it back when the
  // modal closes. Runs once Monaco is mounted; the `getTheme` closure
  // always reads the latest resolved theme name through refs.
  const themeStateRef = useRef({ shikiTheme, theme, shikiReady })
  themeStateRef.current = { shikiTheme, theme, shikiReady }
  useEffect(() => {
    if (!monacoRef.current) return
    const handle = registerMonacoSurface(monacoRef.current, () => {
      const { shikiTheme: t, theme: themeMode, shikiReady: ready } = themeStateRef.current
      if (ready && isShikiActive()) {
        return getShikiThemeName(t, themeMode === "dark")
      }
      return themeMode === "dark" ? "mermaid-dark" : "mermaid-light"
    })
    themeHandleRef.current = handle
    return () => {
      handle.unregister()
      themeHandleRef.current = null
    }
  }, [monacoReady])

  useEffect(() => {
    themeHandleRef.current?.refresh()
  }, [shikiTheme, theme, shikiReady])

  // Keep the rectangle-mode CSS hook in sync with the resolved value.
  useEffect(() => {
    applyLineHighlightDecoration(editorRef.current, renderLineHighlight)
  }, [renderLineHighlight])

  const handleEditorDidMount: OnMount = async (editor, monaco) => {
    editorRef.current = editor as unknown as editor.IStandaloneCodeEditor
    monacoRef.current = monaco as unknown as typeof import("monaco-editor")
    setMonacoReady(true)

    // Ensure Shiki is loaded so the user's selected theme actually takes
    // effect even on pages that have no code-studio block.
    await ensureShikiLoaded(monaco)

    // Register Mermaid language only once
    if (!isLanguageRegistered.current) {
      // Register the language
      monaco.languages.register({ id: "mermaid" })

      // Set language configuration
      monaco.languages.setLanguageConfiguration("mermaid", mermaidLanguageConfig)

      // Set syntax highlighting
      monaco.languages.setMonarchTokensProvider("mermaid", mermaidTokensProvider)

      // Define custom themes only if Shiki hasn't taken over
      if (!isShikiActive()) {
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
      }

      // Register completion provider and store disposable
      completionProviderDisposable.current = monaco.languages.registerCompletionItemProvider(
        "mermaid",
        createMermaidCompletionProvider(monaco),
      )

      isLanguageRegistered.current = true
    }

    // Theme application is delegated to the global Monaco theme
    // coordinator (see below) so that closing this editor restores the
    // theme of any underlying surface (e.g. code-studio preview).

    editor.onDidChangeModelContent(() => {
      const currentValue = editor.getValue()
      if (onValidationChange) {
        validateCode(currentValue)
      }
    })

    applyLineHighlightDecoration(editor as unknown as import("monaco-editor").editor.IStandaloneCodeEditor, renderLineHighlight)
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
    <MonacoErrorBoundary>
    <MonacoEditor
      height={height}
      language="mermaid"
      value={value}
      onChange={handleChange}
      beforeMount={(monaco) => { void ensureShikiLoaded(monaco) }}
      onMount={handleEditorDidMount}
      theme={
        shikiReady && isShikiActive()
          ? getShikiThemeName(shikiTheme, theme === "dark")
          : theme === "dark"
            ? "mermaid-dark"
            : "mermaid-light"
      }
      options={{
        minimap: { enabled: minimap },
        scrollBeyondLastLine: false,
        fontSize: fontSize,
        lineNumbers: lineNumbers ? "on" : "off",
        wordWrap: wordWrap ? "on" : "off",
        automaticLayout: true,
        tabSize,
        renderWhitespace,
        insertSpaces: true,
        folding: true,
        lineDecorationsWidth: 10,
        lineNumbersMinChars: 3,
        renderLineHighlight: toMonacoRenderLineHighlight(renderLineHighlight),
        selectOnLineNumbers: true,
        roundedSelection: false,
        readOnly,
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
    </MonacoErrorBoundary>
  )
}
