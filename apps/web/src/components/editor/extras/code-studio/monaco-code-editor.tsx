"use client"

import { useEffect, useRef } from "react"
import Editor from "@monaco-editor/react"
import type { editor } from "monaco-editor"
import type { SupportedLanguage } from "./types"

interface MonacoCodeEditorProps {
  value: string
  language: SupportedLanguage
  onChange?: (value: string) => void
  readonly?: boolean
  theme?: "vs-light" | "vs-dark"
  fontSize?: number
  showLineNumbers?: boolean
  height?: string
}

export function MonacoCodeEditor({
  value,
  language,
  onChange,
  readonly = false,
  theme = "vs-light",
  fontSize = 14,
  showLineNumbers = true,
  height = "100%",
}: MonacoCodeEditorProps) {
  const editorRef = useRef<editor.IStandaloneCodeEditor | null>(null)

  const handleEditorDidMount = (editor: editor.IStandaloneCodeEditor) => {
    editorRef.current = editor

    // Configurações adicionais do editor
    editor.updateOptions({
      readOnly: readonly,
      fontSize,
      lineNumbers: showLineNumbers ? "on" : "off",
      minimap: { enabled: false },
      scrollBeyondLastLine: false,
      wordWrap: "on",
      automaticLayout: true,
    })
  }

  const handleChange = (value: string | undefined) => {
    if (onChange && !readonly) {
      onChange(value || "")
    }
  }

  // Atualizar opções quando props mudarem
  useEffect(() => {
    if (editorRef.current) {
      editorRef.current.updateOptions({
        readOnly: readonly,
        fontSize,
        lineNumbers: showLineNumbers ? "on" : "off",
      })
    }
  }, [readonly, fontSize, showLineNumbers])

  return (
    <Editor
      height={height}
      language={language}
      value={value}
      onChange={handleChange}
      onMount={handleEditorDidMount}
      theme={theme}
      options={{
        readOnly: readonly,
        fontSize,
        lineNumbers: showLineNumbers ? "on" : "off",
        minimap: { enabled: false },
        scrollBeyondLastLine: false,
        wordWrap: "on",
        automaticLayout: true,
        padding: { top: 8, bottom: 8 },
        suggest: {
          showKeywords: true,
          showSnippets: true,
        },
        quickSuggestions: {
          other: true,
          comments: false,
          strings: false,
        },
      }}
    />
  )
}
