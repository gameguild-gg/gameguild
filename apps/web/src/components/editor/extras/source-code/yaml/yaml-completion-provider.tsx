"use client"

import type { editor } from "monaco-editor"
import { useEffect } from "react"

interface YAMLCompletionProviderProps {
  monaco: typeof import("monaco-editor") | null
  editor: editor.IStandaloneCodeEditor | null
}

export function YAMLCompletionProvider({ monaco, editor }: YAMLCompletionProviderProps) {
  useEffect(() => {
    if (!monaco || !editor) return

    // Register YAML completion provider
    const disposable = monaco.languages.registerCompletionItemProvider("yaml", {
      provideCompletionItems: (model, position) => {
        const word = model.getWordUntilPosition(position)
        const range = {
          startLineNumber: position.lineNumber,
          endLineNumber: position.lineNumber,
          startColumn: word.startColumn,
          endColumn: word.endColumn,
        }
        var suggestions = [
          {
            label: "YAML-key-value",
            kind: monaco.languages.CompletionItemKind.Snippet,
            insertText: "${1:key}: ${2:value}",
            insertTextRules: monaco.languages.CompletionItemInsertTextRule.InsertAsSnippet,
            range,
          },
        ]
        return { suggestions: suggestions }
      },
    })

    return () => {
      disposable.dispose()
    }
  }, [monaco, editor])

  return null
}
