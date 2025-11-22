"use client"

import { useEffect, useRef, useState } from "react"
import Editor from "@monaco-editor/react"
import type { editor } from "monaco-editor"
import type { Monaco } from "@monaco-editor/react"
import type { SupportedLanguage, ShikiTheme } from "./types"
import { getShikiThemeName, SHIKI_THEME_CONFIGS } from "./types"
import { shikiToMonaco } from "@shikijs/monaco"
import { useTheme } from "next-themes"
import { createHighlighter, type Highlighter } from "shiki"

// Singleton para o highlighter do Shiki
let shikiHighlighter: Highlighter | null = null
let shikiPromise: Promise<Highlighter> | null = null
let shikiAppliedToMonaco = false

async function getShikiHighlighter(): Promise<Highlighter> {
  if (shikiHighlighter) {
    return shikiHighlighter
  }
  
  if (!shikiPromise) {
    shikiPromise = createHighlighter({
      themes: [
        'github-dark',
        'github-light',
        'github-dark-default',
        'github-light-default',
        'github-dark-dimmed',
        'dark-plus',
        'light-plus',
      ],
      langs: ['javascript', 'typescript', 'python', 'lua', 'c', 'cpp', 'html', 'css', 'markdown'],
    }).then((highlighter) => {
      shikiHighlighter = highlighter
      return highlighter
    })
  }
  
  return shikiPromise
}

interface MonacoCodeEditorProps {
  value: string
  language: SupportedLanguage
  onChange?: (value: string) => void
  readonly?: boolean
  theme?: "vs-light" | "vs-dark"
  shikiTheme?: ShikiTheme
  fontSize?: number
  showLineNumbers?: boolean
  height?: string
  fileId?: string // ID único do arquivo para garantir instâncias separadas
}

export function MonacoCodeEditor({
  value,
  language,
  onChange,
  readonly = false,
  theme = "vs-light",
  shikiTheme = "github",
  fontSize = 14,
  showLineNumbers = true,
  height = "100%",
  fileId,
}: MonacoCodeEditorProps) {
  const editorRef = useRef<editor.IStandaloneCodeEditor | null>(null)
  const monacoRef = useRef<Monaco | null>(null)
  const [isShikiReady, setIsShikiReady] = useState(false)
  const { resolvedTheme, theme: themeState } = useTheme()
  
  // Determinar o tema atual (dark ou light) - usa theme como fallback
  const effectiveTheme = resolvedTheme || themeState
  const isDarkMode = effectiveTheme === "dark"
  const currentTheme = getShikiThemeName(shikiTheme, isDarkMode)

  const handleEditorWillMount = async (monaco: Monaco) => {
    monacoRef.current = monaco
    
    // Carregar Shiki ANTES de montar o editor (apenas uma vez globalmente)
    if (!shikiAppliedToMonaco) {
      try {
        const highlighter = await getShikiHighlighter()
        
        // Apply Shiki to Monaco (apenas uma vez globalmente)
        shikiToMonaco(highlighter, monaco)
        shikiAppliedToMonaco = true
        setIsShikiReady(true)
      } catch (error) {
        console.error('Failed to load Shiki:', error)
      }
    } else {
      // Shiki já foi aplicado em outro editor
      setIsShikiReady(true)
    }
  }

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

    // Centralizar a command palette no container do Monaco
    const container = editor.getDomNode()
    if (container) {
      // Adicionar estilo para centralizar widgets do Monaco
      const style = document.createElement('style')
      style.textContent = `
        .monaco-editor .quick-input-widget {
          position: fixed !important;
          left: 50% !important;
          top: 50% !important;
          transform: translate(-50%, -50%) !important;
          z-index: 9999 !important;
          max-width: 600px !important;
        }
        .monaco-editor .monaco-workbench .part.editor > .content .monaco-editor .quick-input-widget {
          position: absolute !important;
        }
      `
      container.appendChild(style)
    }
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

  // Atualizar tema do Monaco quando mudar
  useEffect(() => {
    if (monacoRef.current && isShikiReady) {
      monacoRef.current.editor.setTheme(currentTheme)
    }
  }, [currentTheme, isShikiReady])

  return (
    <Editor
      key={fileId} // Força nova instância do Monaco para cada arquivo
      height={height}
      language={language}
      value={value}
      onChange={handleChange}
      beforeMount={handleEditorWillMount}
      onMount={handleEditorDidMount}
      theme={currentTheme}
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
