"use client"

import type { editor } from "monaco-editor"
import { useEffect } from "react"

interface XMLCompletionProviderProps {
  monaco: typeof import("monaco-editor") | null
  editor: editor.IStandaloneCodeEditor | null
}

export function XMLCompletionProvider({ monaco, editor }: XMLCompletionProviderProps) {
  useEffect(() => {
    if (!monaco || !editor) return

    // Register XML completion provider
    const disposable = monaco.languages.registerCompletionItemProvider("xml", {
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
            label: "XML-tag",
            kind: monaco.languages.CompletionItemKind.Snippet,
            insertText: "<${1:tag}>$0</${1:tag}>",
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
