"use client"

import { useEffect, useRef, useCallback, useImperativeHandle, forwardRef, useState } from "react"
import { Terminal } from "@xterm/xterm"
import { FitAddon } from "@xterm/addon-fit"
import { WebglAddon } from "@xterm/addon-webgl"
import { SearchAddon } from "@xterm/addon-search"
import { WebLinksAddon } from "@xterm/addon-web-links"
import { ClipboardAddon } from "@xterm/addon-clipboard"
import { Unicode11Addon } from "@xterm/addon-unicode11"
import "@xterm/xterm/css/xterm.css"
import { useTheme } from "next-themes"
import { X, ChevronUp, ChevronDown } from "lucide-react"
import { LinkConfirmDialog } from "../dialogs/link-confirm-dialog"

interface XTermTerminalProps {
  output: string
  isExecuting: boolean
  onClear?: () => void
}

export interface XTermTerminalHandle {
  requestInput: () => Promise<string>
  write: (text: string) => void
  clear: () => void
  search: (term: string, searchOptions?: { incremental?: boolean }) => boolean
  searchNext: () => boolean
  searchPrevious: () => boolean
  clearSearch: () => void
}

const darkTheme = {
  background: "#0a0a0a",
  foreground: "#e5e7eb",
  cursor: "#60a5fa",
  black: "#1f2937",
  red: "#ef4444",
  green: "#10b981",
  yellow: "#f59e0b",
  blue: "#3b82f6",
  magenta: "#a855f7",
  cyan: "#06b6d4",
  white: "#e5e7eb",
  brightBlack: "#4b5563",
  brightRed: "#f87171",
  brightGreen: "#34d399",
  brightYellow: "#fbbf24",
  brightBlue: "#60a5fa",
  brightMagenta: "#c084fc",
  brightCyan: "#22d3ee",
  brightWhite: "#f9fafb",
}

const lightTheme = {
  background: "#ffffff",
  foreground: "#1f2937",
  cursor: "#3b82f6",
  black: "#374151",
  red: "#ef4444",
  green: "#10b981",
  yellow: "#f59e0b",
  blue: "#3b82f6",
  magenta: "#a855f7",
  cyan: "#06b6d4",
  white: "#6b7280",
  brightBlack: "#9ca3af",
  brightRed: "#f87171",
  brightGreen: "#34d399",
  brightYellow: "#fbbf24",
  brightBlue: "#60a5fa",
  brightMagenta: "#c084fc",
  brightCyan: "#22d3ee",
  brightWhite: "#1f2937",
}

export const XTermTerminal = forwardRef<XTermTerminalHandle, XTermTerminalProps>(
  function XTermTerminal({ output, isExecuting }, ref) {
    const terminalRef = useRef<HTMLDivElement>(null)
    const xtermRef = useRef<Terminal | null>(null)
    const fitAddonRef = useRef<FitAddon | null>(null)
    const searchAddonRef = useRef<SearchAddon | null>(null)
    const inputBufferRef = useRef<string>("")
    const inputResolverRef = useRef<((value: string) => void) | null>(null)
    const [showSearch, setShowSearch] = useState(false)
    const [searchTerm, setSearchTerm] = useState("")
    const [linkConfirmDialog, setLinkConfirmDialog] = useState<{ open: boolean; url: string }>({
      open: false,
      url: "",
    })
    const { resolvedTheme } = useTheme()
    const isDarkMode = resolvedTheme === "dark"

    // Expose methods via ref
    useImperativeHandle(ref, () => ({
      requestInput: async (): Promise<string> => {
        return new Promise((resolve) => {
          if (!xtermRef.current) {
            resolve("")
            return
          }
          
          // Set up the resolver
          inputResolverRef.current = resolve
          inputBufferRef.current = ""
        })
      },
      write: (text: string) => {
        if (xtermRef.current) {
          xtermRef.current.write(text)
        }
      },
      clear: () => {
        if (xtermRef.current) {
          xtermRef.current.clear()
        }
      },
      search: (term: string, searchOptions?: { incremental?: boolean }) => {
        if (searchAddonRef.current) {
          return searchAddonRef.current.findNext(term, searchOptions)
        }
        return false
      },
      searchNext: () => {
        if (searchAddonRef.current) {
          return searchAddonRef.current.findNext('')
        }
        return false
      },
      searchPrevious: () => {
        if (searchAddonRef.current) {
          return searchAddonRef.current.findPrevious('')
        }
        return false
      },
      clearSearch: () => {
        if (searchAddonRef.current) {
          searchAddonRef.current.clearDecorations()
        }
      },
    }))

  const fitTerminal = useCallback(() => {
    fitAddonRef.current?.fit()
  }, [])

  useEffect(() => {
    if (!terminalRef.current || xtermRef.current) return

    const terminal = new Terminal({
      cursorBlink: true,
      fontSize: 13,
      fontFamily: 'Menlo, Monaco, "Courier New", monospace',
      theme: isDarkMode ? darkTheme : lightTheme,
      rows: 20,
      scrollback: 1000,
      disableStdin: false, // Enable input for interactive mode
      allowProposedApi: true, // Required for clipboard addon
    })
    xtermRef.current = terminal

    const fitAddon = new FitAddon()
    fitAddonRef.current = fitAddon
    terminal.loadAddon(fitAddon)

    // Load search addon for Ctrl+F functionality
    const searchAddon = new SearchAddon()
    searchAddonRef.current = searchAddon
    terminal.loadAddon(searchAddon)

    // Load web links addon for clickable URLs with confirmation
    const webLinksAddon = new WebLinksAddon((event, uri) => {
      event.preventDefault()
      setLinkConfirmDialog({ open: true, url: uri })
    })
    terminal.loadAddon(webLinksAddon)

    // Load clipboard addon for better copy/paste support
    const clipboardAddon = new ClipboardAddon()
    terminal.loadAddon(clipboardAddon)

    // Load unicode11 addon for better emoji and special character support
    const unicode11Addon = new Unicode11Addon()
    terminal.loadAddon(unicode11Addon)
    terminal.unicode.activeVersion = '11' // Activate Unicode 11

    // Defer open to next frame so container is visible and has dimensions.
    // xterm's renderer crashes with "dimensions is undefined" if the
    // container has 0 width/height when open() is called.
    const openFrame = requestAnimationFrame(() => {
      if (!terminalRef.current) return
      try {
        terminal.open(terminalRef.current)
      } catch (e) {
        console.warn('[XTermTerminal] Failed to open terminal:', e)
        return
      }

      // Try to load WebGL addon for better performance
      try {
        const webglAddon = new WebglAddon()
        terminal.loadAddon(webglAddon)
      } catch (e) {
        // WebGL not supported, fallback to canvas renderer
        console.warn('WebGL addon could not be loaded, using canvas renderer')
      }

      // Delay fit to ensure container has dimensions
      requestAnimationFrame(() => {
        fitTerminal()
      })
    })

    // Handle keyboard shortcuts for copy/paste
    terminal.attachCustomKeyEventHandler((event) => {
      // Ctrl+F / Cmd+F - Open search
      if ((event.ctrlKey || event.metaKey) && event.key === 'f' && event.type === 'keydown') {
        event.preventDefault()
        setShowSearch(true)
        return false
      }
      // Escape - Close search
      if (event.key === 'Escape' && showSearch && event.type === 'keydown') {
        setShowSearch(false)
        searchAddonRef.current?.clearDecorations()
        return false
      }
      // Allow default browser behavior for copy (Ctrl+C / Cmd+C) when text is selected
      if ((event.ctrlKey || event.metaKey) && event.key === 'c' && terminal.hasSelection()) {
        return false // Let browser handle copy
      }
      // Allow default browser behavior for paste (Ctrl+V / Cmd+V)
      if ((event.ctrlKey || event.metaKey) && event.key === 'v') {
        return false // Let browser handle paste
      }
      return true
    })

    // Handle input from user
    terminal.onData((data) => {
      if (!inputResolverRef.current) return
      
      const code = data.charCodeAt(0)
      
      // Enter key
      if (code === 13) {
        terminal.write('\r\n')
        const input = inputBufferRef.current
        inputBufferRef.current = ""
        inputResolverRef.current(input)
        inputResolverRef.current = null
      }
      // Backspace
      else if (code === 127 || code === 8) {
        if (inputBufferRef.current.length > 0) {
          inputBufferRef.current = inputBufferRef.current.slice(0, -1)
          terminal.write('\b \b')
        }
      }
      // Regular character
      else if (code >= 32) {
        inputBufferRef.current += data
        terminal.write(data)
      }
    })

    window.addEventListener("resize", fitTerminal)

    return () => {
      cancelAnimationFrame(openFrame)
      window.removeEventListener("resize", fitTerminal)
      terminal.dispose()
      xtermRef.current = null
    }
  }, [fitTerminal]) // A dependência `isDarkMode` foi removida para evitar recriação

  useEffect(() => {
    if (xtermRef.current) {
      xtermRef.current.options.theme = isDarkMode ? darkTheme : lightTheme
    }
  }, [isDarkMode])

  useEffect(() => {
    if (!xtermRef.current) return

    // Quando começar uma execução, limpar o terminal
    if (isExecuting) {
      xtermRef.current.clear()
      return
    }
    
    // Só mostrar output quando não está executando (já foi limpo quando isExecuting=true)
    if (output) {
      xtermRef.current.writeln(output.replace(/\n/g, "\r\n"))
      xtermRef.current.scrollToBottom()
    }
  }, [output, isExecuting])

  // Capturar scroll para evitar que propague para a página principal
  useEffect(() => {
    const terminalElement = terminalRef.current
    if (!terminalElement) return

    const handleWheel = (e: WheelEvent) => {
      // Sempre prevenir propagação do scroll para o elemento pai
      e.stopPropagation()
      e.preventDefault()
      
      // Scrollar manualmente o viewport do xterm
      const viewport = terminalElement.querySelector('.xterm-viewport') as HTMLElement
      if (viewport) {
        viewport.scrollTop += e.deltaY
      }
    }

    // Adicionar listener com capture: true para capturar em toda a área
    terminalElement.addEventListener('wheel', handleWheel, { passive: false, capture: true })

    return () => {
      terminalElement.removeEventListener('wheel', handleWheel, { capture: true })
    }
  }, [])

  return (
    <div className="h-full w-full overflow-hidden p-2 relative">
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

      {/* Search Bar */}
      {showSearch && (
        <div className="absolute top-2 right-2 z-10 bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-md shadow-lg p-2 flex items-center gap-2">
          <input
            type="text"
            value={searchTerm}
            onChange={(e) => {
              setSearchTerm(e.target.value)
              if (searchAddonRef.current && e.target.value) {
                searchAddonRef.current.findNext(e.target.value, { incremental: true })
              } else {
                searchAddonRef.current?.clearDecorations()
              }
            }}
            onKeyDown={(e) => {
              if (e.key === 'Enter') {
                if (e.shiftKey) {
                  searchAddonRef.current?.findPrevious(searchTerm)
                } else {
                  searchAddonRef.current?.findNext(searchTerm)
                }
              } else if (e.key === 'Escape') {
                setShowSearch(false)
                searchAddonRef.current?.clearDecorations()
              }
            }}
            placeholder="Find..."
            className="px-2 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded bg-white dark:bg-gray-900 text-gray-900 dark:text-gray-100 outline-none focus:ring-1 focus:ring-blue-500 w-48"
            autoFocus
          />
          <button
            onClick={() => searchAddonRef.current?.findPrevious(searchTerm)}
            className="p-1 hover:bg-gray-100 dark:hover:bg-gray-700 rounded"
            title="Previous (Shift+Enter)"
          >
            <ChevronUp className="h-4 w-4" />
          </button>
          <button
            onClick={() => searchAddonRef.current?.findNext(searchTerm)}
            className="p-1 hover:bg-gray-100 dark:hover:bg-gray-700 rounded"
            title="Next (Enter)"
          >
            <ChevronDown className="h-4 w-4" />
          </button>
          <button
            onClick={() => {
              setShowSearch(false)
              setSearchTerm("")
              searchAddonRef.current?.clearDecorations()
            }}
            className="p-1 hover:bg-gray-100 dark:hover:bg-gray-700 rounded"
            title="Close (Esc)"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
      )}
      
      <div
        ref={terminalRef}
        className="h-full w-full"
      />
    </div>
  )
})