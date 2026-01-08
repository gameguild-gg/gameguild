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
  onExecuteFile: () => void
  onExecuteProject: () => void
  onExecuteTest: () => void
  onStop?: () => void
  testCases: TestCase[]
  activeFile?: CodeFile
  hasMainFile: boolean
  hasTestFile: boolean
}

export const ResultPanel = forwardRef<XTermTerminalHandle, ResultPanelProps>(
  function ResultPanel({
    mode,
    output,
    isExecuting,
    onExecuteFile,
    onExecuteProject,
    onExecuteTest,
    onStop,
    testCases,
    activeFile,
    hasMainFile,
    hasTestFile,
  }, ref) {
  // Unified output panel for both execution and test modes
  if (mode === "execution" || mode === "test") {
    const isTestMode = mode === "test"
    const headerLabel = isTestMode ? "Test Output" : "Console Output"
    
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
          <div className="flex items-center gap-1">
            {isExecuting ? (
              <Button variant="ghost" size="sm" onClick={onStop} className="h-7 text-xs px-2">
                <Square className="h-3 w-3 mr-1" />
                Stop
              </Button>
            ) : (
              <>
                <Button 
                  variant="ghost" 
                  size="sm" 
                  onClick={onExecuteFile} 
                  className="h-7 text-xs px-2"
                  disabled={!activeFile}
                  title="Run current file"
                >
                  <Play className="h-3 w-3 mr-1" />
                  File
                </Button>
                {hasMainFile && (
                  <Button 
                    variant="ghost" 
                    size="sm" 
                    onClick={onExecuteProject} 
                    className="h-7 text-xs px-2"
                    title="Run main file (marked as 'm')"
                  >
                    <Play className="h-3 w-3 mr-1" />
                    Project
                  </Button>
                )}
                {hasTestFile && (
                  <Button 
                    variant="ghost" 
                    size="sm" 
                    onClick={onExecuteTest} 
                    className="h-7 text-xs px-2"
                    title="Run test file (marked as 't')"
                  >
                    <Play className="h-3 w-3 mr-1" />
                    Test
                  </Button>
                )}
              </>
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
