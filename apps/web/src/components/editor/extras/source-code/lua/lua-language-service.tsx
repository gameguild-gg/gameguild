"use client"

import { useState, useEffect } from "react"
import type { editor } from "monaco-editor"
import { LuaSyntaxHighlighter } from "./lua-syntax-highlighter"
import { LuaTypeChecker } from "./lua-type-checker"
import { LuaCompletionProvider } from "./lua-completion-provider"

interface LuaLanguageServiceProps {
  monaco: typeof import("monaco-editor") | null
  editor: editor.IStandaloneCodeEditor | null
  code: string
  enabled: boolean
}

export function LuaLanguageService({ monaco, editor, code, enabled }: LuaLanguageServiceProps) {
  const [editorInstance, setEditorInstance] = useState<editor.IStandaloneCodeEditor | null>(null)
  const [monacoInstance, setMonacoInstance] = useState<typeof import("monaco-editor") | null>(null)

  useEffect(() => {
    if (monaco && editor && enabled) {
      setEditorInstance(editor)
      setMonacoInstance(monaco)
    }
  }, [monaco, editor, enabled])

  if (!enabled || !editorInstance || !monacoInstance) {
    return null
  }

  return (
    <>
      <LuaSyntaxHighlighter monaco={monacoInstance} editor={editorInstance} />
      <LuaTypeChecker monaco={monacoInstance} editor={editorInstance} code={code} />
      <LuaCompletionProvider monaco={monacoInstance} editor={editorInstance} />
    </>
  )
}
