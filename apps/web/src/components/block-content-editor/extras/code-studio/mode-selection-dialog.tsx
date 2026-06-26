"use client"

import { Play, TestTube } from "lucide-react"
import { Button } from "@/components/ui/button"
import type { CodeStudioMode } from "./types"
import { MODE_CONFIGS } from "./types"

interface ModeSelectionDialogProps {
  onSelect: (mode: CodeStudioMode) => void
  onCancel: () => void
}

const ICON_MAP = {
  Play,
  TestTube,
}

export function ModeSelectionDialog({ onSelect, onCancel }: ModeSelectionDialogProps) {
  return (
    <div 
      className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4"
      onClick={onCancel}
    >
      <div 
        className="bg-white dark:bg-gray-900 border dark:border-gray-700 shadow-2xl rounded-lg max-w-2xl w-full p-6"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="mb-6">
          <h2 className="text-2xl font-bold text-gray-900 dark:text-gray-100">
            Choose Code Studio Mode
          </h2>
          <p className="text-sm text-gray-600 dark:text-gray-400 mt-2">
            Select how you want to work with your code. This cannot be changed later.
          </p>
        </div>

        <div className="grid gap-4">
          {Object.values(MODE_CONFIGS).map((mode) => {
            const Icon = ICON_MAP[mode.icon as keyof typeof ICON_MAP]
            
            return (
              <button
                key={mode.id}
                onClick={() => onSelect(mode.id)}
                className="flex items-start gap-4 p-4 border-2 border-gray-200 dark:border-gray-700 rounded-lg hover:border-blue-500 dark:hover:border-blue-400 hover:bg-blue-50 dark:hover:bg-blue-950/30 transition-all group"
              >
                <div className="shrink-0 p-3 bg-blue-100 dark:bg-blue-900/30 rounded-lg group-hover:bg-blue-500 dark:group-hover:bg-blue-500 transition-colors">
                  <Icon className="h-6 w-6 text-blue-600 dark:text-blue-400 group-hover:text-white transition-colors" />
                </div>
                
                <div className="flex-1 text-left">
                  <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-1">
                    {mode.label}
                  </h3>
                  <p className="text-sm text-gray-600 dark:text-gray-400">
                    {mode.description}
                  </p>
                  
                  <div className="mt-2 flex flex-wrap gap-1">
                    {mode.supportedLanguages.slice(0, 6).map((lang) => (
                      <span
                        key={lang}
                        className="text-xs px-2 py-0.5 bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 rounded"
                      >
                        {lang}
                      </span>
                    ))}
                    {mode.supportedLanguages.length > 6 && (
                      <span className="text-xs px-2 py-0.5 text-gray-500 dark:text-gray-500">
                        +{mode.supportedLanguages.length - 6} more
                      </span>
                    )}
                  </div>
                </div>

                <div className="shrink-0 self-center">
                  <div className="w-6 h-6 border-2 border-gray-300 dark:border-gray-600 rounded-full group-hover:border-blue-500 dark:group-hover:border-blue-400 flex items-center justify-center">
                    <div className="w-3 h-3 bg-blue-500 rounded-full opacity-0 group-hover:opacity-100 transition-opacity" />
                  </div>
                </div>
              </button>
            )
          })}
        </div>

        <div className="mt-6 flex justify-end">
          <Button variant="outline" onClick={onCancel}>
            Cancel
          </Button>
        </div>
      </div>
    </div>
  )
}
