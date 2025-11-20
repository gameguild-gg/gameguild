"use client"

import { useEffect, useRef, useCallback } from "react"
import { Terminal } from "@xterm/xterm"
import { FitAddon } from "@xterm/addon-fit"
import "@xterm/xterm/css/xterm.css"
import { useTheme } from "next-themes"

interface XTermTerminalProps {
  output: string
  isExecuting: boolean
  onClear?: () => void
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

export function XTermTerminal({ output, isExecuting }: XTermTerminalProps) {
  const terminalRef = useRef<HTMLDivElement>(null)
  const xtermRef = useRef<Terminal | null>(null)
  const fitAddonRef = useRef<FitAddon | null>(null)
  const { resolvedTheme } = useTheme()
  const isDarkMode = resolvedTheme === "dark"

  const fitTerminal = useCallback(() => {
    fitAddonRef.current?.fit()
  }, [])

  useEffect(() => {
    if (!terminalRef.current || xtermRef.current) return

    const terminal = new Terminal({
      cursorBlink: false,
      fontSize: 13,
      fontFamily: 'Menlo, Monaco, "Courier New", monospace',
      theme: isDarkMode ? darkTheme : lightTheme,
      rows: 20,
      scrollback: 1000,
      disableStdin: true, // Read-only terminal
    })
    xtermRef.current = terminal

    const fitAddon = new FitAddon()
    fitAddonRef.current = fitAddon
    terminal.loadAddon(fitAddon)

    terminal.open(terminalRef.current)
    fitTerminal()

    window.addEventListener("resize", fitTerminal)

    return () => {
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

    xtermRef.current.clear()
    if (isExecuting) {
      xtermRef.current.writeln("\x1b[33m⟳ Executing...\x1b[0m")
    } else if (output) {
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
    <div
      ref={terminalRef}
      className="h-full w-full overflow-hidden p-2"
    />
  )
}
