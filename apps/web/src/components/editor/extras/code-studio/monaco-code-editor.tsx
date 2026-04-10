"use client"

import { useEffect, useRef, useState } from "react"
import Editor from "@monaco-editor/react"
import type { editor } from "monaco-editor"
import type { Monaco } from "@monaco-editor/react"
import type { SupportedLanguage, ShikiTheme } from "./types"
import { getShikiThemeName, SHIKI_LANGS } from "./types"
import { shikiToMonaco } from "@shikijs/monaco"
import { useTheme } from "next-themes"
import { createHighlighter, type Highlighter } from "shiki"
import { registerPathCompletionProvider } from "./monaco-file-system"
import { LinkConfirmDialog } from "../dialogs/link-confirm-dialog"

// Singleton para o highlighter do Shiki
let shikiHighlighter: Highlighter | null = null
let shikiPromise: Promise<Highlighter> | null = null
let shikiAppliedToMonaco = false
let pathCompletionRegistered = false

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
        'catppuccin-mocha',
        'catppuccin-latte',
        'vitesse-dark',
        'vitesse-light',
        'monokai',
        'solarized-dark',
        'solarized-light',
        'dracula',
        'nord',
      ],
      langs: SHIKI_LANGS,
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
  filePath?: string // Caminho do arquivo para o sistema de arquivos virtual
  instanceId?: string // ID da instância do Code Studio para isolamento completo
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
  filePath,
  instanceId,
}: MonacoCodeEditorProps) {
  const editorRef = useRef<editor.IStandaloneCodeEditor | null>(null)
  const monacoRef = useRef<Monaco | null>(null)
  const [isShikiReady, setIsShikiReady] = useState(false)
  const [linkConfirmDialog, setLinkConfirmDialog] = useState<{ open: boolean; url: string }>({
    open: false,
    url: "",
  })
  const { resolvedTheme, theme: themeState } = useTheme()
  const isUserTypingRef = useRef(false)
  const lastValueRef = useRef(value)
  
  // Determinar o tema atual (dark ou light) - usa theme como fallback
  const effectiveTheme = resolvedTheme || themeState
  const isDarkMode = effectiveTheme === "dark"
  const currentTheme = getShikiThemeName(shikiTheme, isDarkMode)

  const handleEditorWillMount = async (monaco: Monaco) => {
    monacoRef.current = monaco
    
    // Configurar TypeScript/JavaScript compiler options
    monaco.languages.typescript.typescriptDefaults.setCompilerOptions({
      target: monaco.languages.typescript.ScriptTarget.ES2020,
      allowNonTsExtensions: true,
      moduleResolution: monaco.languages.typescript.ModuleResolutionKind.NodeJs,
      module: monaco.languages.typescript.ModuleKind.ESNext,
      noEmit: true,
      esModuleInterop: true,
      jsx: monaco.languages.typescript.JsxEmit.React,
      allowJs: true,
      typeRoots: [],
      allowSyntheticDefaultImports: true,
      skipLibCheck: true,
      skipDefaultLibCheck: true,
    })

    monaco.languages.typescript.javascriptDefaults.setCompilerOptions({
      target: monaco.languages.typescript.ScriptTarget.ES2020,
      allowNonTsExtensions: true,
      moduleResolution: monaco.languages.typescript.ModuleResolutionKind.NodeJs,
      module: monaco.languages.typescript.ModuleKind.ESNext,
      noEmit: true,
      esModuleInterop: true,
      jsx: monaco.languages.typescript.JsxEmit.React,
      allowJs: true,
      allowSyntheticDefaultImports: true,
      skipLibCheck: true,
      skipDefaultLibCheck: true,
    })

    // Configurar diagnósticos - desabilitar validação semântica que causa erros
    monaco.languages.typescript.typescriptDefaults.setDiagnosticsOptions({
      noSemanticValidation: true, // Desabilita erros de módulos não encontrados
      noSyntaxValidation: false,
      diagnosticCodesToIgnore: [],
    })

    monaco.languages.typescript.javascriptDefaults.setDiagnosticsOptions({
      noSemanticValidation: true, // Desabilita erros de módulos não encontrados
      noSyntaxValidation: false,
      diagnosticCodesToIgnore: [],
    })

    // Registrar path completion provider (apenas uma vez)
    if (!pathCompletionRegistered) {
      registerPathCompletionProvider(monaco)
      pathCompletionRegistered = true
    }
    
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

    // Interceptar cliques em links para mostrar dialog de confirmação (apenas com Ctrl pressionado)
    editor.onMouseDown((e) => {
      // Só processa se Ctrl/Cmd estiver pressionado
      if (!e.event.ctrlKey && !e.event.metaKey) return
      if (!e.target.position) return

      const model = editor.getModel()
      if (!model) return

      const lineContent = model.getLineContent(e.target.position.lineNumber)
      const urlRegex = /(https?:\/\/[^\s"')\]]+)/g
      let match

      while ((match = urlRegex.exec(lineContent)) !== null) {
        const startColumn = match.index + 1
        const endColumn = startColumn + match[0].length

        if (
          e.target.position.column >= startColumn &&
          e.target.position.column <= endColumn
        ) {
          e.event.preventDefault()
          e.event.stopPropagation()
          setLinkConfirmDialog({ open: true, url: match[0] })
          return false
        }
      }
    })

    // Adicionar decorações para destacar links
    let decorationIds: string[] = []
    
    const updateLinkDecorations = () => {
      const model = editor.getModel()
      if (!model) return

      const decorations: editor.IModelDeltaDecoration[] = []
      const urlRegex = /(https?:\/\/[^\s"')\]]+)/g

      for (let lineNumber = 1; lineNumber <= model.getLineCount(); lineNumber++) {
        const lineContent = model.getLineContent(lineNumber)
        let match

        while ((match = urlRegex.exec(lineContent)) !== null) {
          decorations.push({
            range: {
              startLineNumber: lineNumber,
              startColumn: match.index + 1,
              endLineNumber: lineNumber,
              endColumn: match.index + match[0].length + 1,
            },
            options: {
              inlineClassName: 'monaco-link-decoration',
              hoverMessage: { value: `Ctrl+Clique para abrir: ${match[0]}` },
            },
          })
        }
      }

      decorationIds = editor.deltaDecorations(decorationIds, decorations)
    }

    // Atualizar decorações quando o conteúdo mudar
    updateLinkDecorations()
    editor.onDidChangeModelContent(() => {
      updateLinkDecorations()
    })

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
      // Adicionar estilo para centralizar widgets do Monaco e decoração de links
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
        .monaco-link-decoration {
          text-decoration: underline !important;
          color: #0066cc !important;
          cursor: pointer !important;
        }
        .monaco-editor.vs-dark .monaco-link-decoration {
          color: #3794ff !important;
        }
      `
      container.appendChild(style)
    }
  }

  const handleChange = (value: string | undefined) => {
    isUserTypingRef.current = true
    if (onChange && !readonly) {
      onChange(value || "")
    }
  }

  // Atualizar conteúdo do editor quando value mudar externamente (reset)
  useEffect(() => {
    if (editorRef.current && !isUserTypingRef.current && value !== lastValueRef.current) {
      const editor = editorRef.current
      const model = editor.getModel()
      if (model) {
        const currentValue = model.getValue()
        if (currentValue !== value) {
          // Salvar posição do cursor
          const position = editor.getPosition()
          
          // Atualizar conteúdo
          model.setValue(value)
          
          // Restaurar posição do cursor se ainda válida
          if (position) {
            editor.setPosition(position)
          }
        }
      }
      lastValueRef.current = value
    }
    
    // Resetar flag após um breve delay
    if (isUserTypingRef.current) {
      const timeout = setTimeout(() => {
        isUserTypingRef.current = false
      }, 100)
      return () => clearTimeout(timeout)
    }
  }, [value])

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
    <>
      {/* Link Confirmation Dialog */}
      <LinkConfirmDialog
        open={linkConfirmDialog.open}
        onOpenChange={(open) => setLinkConfirmDialog({ open, url: "" })}
        url={linkConfirmDialog.url}
        onConfirm={() => {
          window.open(linkConfirmDialog.url, '_blank', 'noopener,noreferrer')
          setLinkConfirmDialog({ open: false, url: "" })
        }}
      />
      
      <Editor
        key={fileId} // Força nova instância do Monaco para cada arquivo
        height={height}
        language={language}
        defaultValue={value} // Usar defaultValue ao invés de value para modo não-controlado
        path={filePath && instanceId ? `file:///${instanceId}/${filePath}` : filePath ? `file:///${filePath}` : undefined} // URI único com instanceId
        keepCurrentModel={true} // Não destruir o modelo ao desmontar (evita quebrar preview quando modal fecha)
        onChange={handleChange}
        beforeMount={handleEditorWillMount}
        onMount={handleEditorDidMount}
        theme={currentTheme}
        loading="" // Remove mensagem "Loading..."
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
          links: false, 
        }}
      />
    </>
  )
}
