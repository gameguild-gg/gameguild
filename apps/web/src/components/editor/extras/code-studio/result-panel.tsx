"use client"

import { Play, Square } from "lucide-react"
import { Button } from "@/components/ui/button"
import type { EditorMode, TestCase, CodeFile } from "./types"
import { XTermTerminal, type XTermTerminalHandle } from "./xterm-terminal"
import { forwardRef } from "react"

interface ResultPanelProps {
  mode: EditorMode
  output: string
  isExecuting: boolean
  onExecute: () => void
  onStop?: () => void
  testCases: TestCase[]
  activeFile?: CodeFile
}

export const ResultPanel = forwardRef<XTermTerminalHandle, ResultPanelProps>(
  function ResultPanel({
    mode,
    output,
    isExecuting,
    onExecute,
    onStop,
    testCases,
    activeFile,
  }, ref) {
  // Unified output panel for both execution and test modes
  if (mode === "execution" || mode === "test") {
    const isTestMode = mode === "test"
    const headerLabel = isTestMode ? "Test Output" : "Console Output"
    const buttonLabel = isTestMode ? "Run Tests" : "Run"
    
    return (
      <div className="h-full flex flex-col bg-gray-50 dark:bg-gray-950">
        {/* Output Header */}
        <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-800 bg-gray-100 dark:bg-gray-900">
          <span className="text-xs font-mono text-gray-600 dark:text-gray-400">
            {headerLabel}
            {isTestMode && testCases.length > 0 && (
              <span className="ml-2 text-gray-500">({testCases.length} tests)</span>
            )}
          </span>
          <div className="flex items-center gap-2">
            {isExecuting ? (
              <Button variant="ghost" size="sm" onClick={onStop} className="h-7 text-xs">
                <Square className="h-3 w-3 mr-1" />
                Stop
              </Button>
            ) : (
              <Button variant="ghost" size="sm" onClick={onExecute} className="h-7 text-xs">
                <Play className="h-3 w-3 mr-1" />
                {buttonLabel}
              </Button>
            )}
          </div>
        </div>

        {/* Terminal Output - XTerm Terminal */}
        <div className="flex-1 overflow-hidden">
          <XTermTerminal 
            ref={ref}
            output={output} 
            isExecuting={isExecuting}
          />
        </div>
      </div>
    )
  }

  // VIEW MODE: Não mostra nada (modo apenas visualização)
  return null
})
