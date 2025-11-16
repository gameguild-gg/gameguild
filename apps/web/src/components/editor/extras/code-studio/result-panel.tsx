"use client"

import { Play, Square } from "lucide-react"
import { Button } from "@/components/ui/button"
import type { EditorMode, TestCase, CodeFile } from "./types"

interface ResultPanelProps {
  mode: EditorMode
  output: string
  isExecuting: boolean
  onExecute: () => void
  testCases: TestCase[]
  activeFile?: CodeFile
}

export function ResultPanel({
  mode,
  output,
  isExecuting,
  onExecute,
  testCases,
  activeFile,
}: ResultPanelProps) {
  // EXECUTION MODE: Console com output
  if (mode === "execution") {
    return (
      <div className="h-full flex flex-col bg-gray-50 dark:bg-gray-950">
        {/* Console Header */}
        <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-800 bg-gray-100 dark:bg-gray-900">
          <span className="text-xs font-mono text-gray-600 dark:text-gray-400">Console Output</span>
          <div className="flex items-center gap-2">
            {isExecuting ? (
              <Button variant="ghost" size="sm" className="h-7 text-xs">
                <Square className="h-3 w-3 mr-1" />
                Stop
              </Button>
            ) : (
              <Button variant="ghost" size="sm" onClick={onExecute} className="h-7 text-xs">
                <Play className="h-3 w-3 mr-1" />
                Run
              </Button>
            )}
          </div>
        </div>

        {/* Console Content */}
        <div className="flex-1 p-4 overflow-auto font-mono text-sm">
          {isExecuting ? (
            <div className="text-gray-500 dark:text-gray-400 flex items-center gap-2">
              <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-600"></div>
              <span>Executing...</span>
            </div>
          ) : output ? (
            <pre className="text-gray-800 dark:text-gray-200 whitespace-pre-wrap">{output}</pre>
          ) : (
            <div className="text-gray-400 dark:text-gray-600 italic">
              Click "Run" to execute the code
            </div>
          )}
        </div>
      </div>
    )
  }

  // TEST MODE: Casos de teste
  if (mode === "test") {
    return (
      <div className="h-full flex flex-col bg-gray-50 dark:bg-gray-950">
        {/* Test Header */}
        <div className="flex items-center justify-between p-3 border-b border-gray-200 dark:border-gray-800 bg-gray-100 dark:bg-gray-900">
          <span className="text-xs font-medium text-gray-600 dark:text-gray-400">
            Test Results ({testCases.length} tests)
          </span>
          <Button variant="ghost" size="sm" onClick={onExecute} className="h-7 text-xs">
            <Play className="h-3 w-3 mr-1" />
            Run Tests
          </Button>
        </div>

        {/* Test Content */}
        <div className="flex-1 p-4 overflow-auto">
          {testCases.length === 0 ? (
            <div className="text-center text-gray-400 dark:text-gray-600 italic py-8">
              No test cases defined
            </div>
          ) : (
            <div className="space-y-2">
              {testCases.map((test) => (
                <div
                  key={test.id}
                  className="p-3 border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900"
                >
                  <div className="font-medium text-sm">{test.name}</div>
                  <div className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                    Type: {test.type}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    )
  }

  // VIEW MODE: Não mostra nada (modo apenas visualização)
  return null
}
