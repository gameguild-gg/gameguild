"use client"

import { Play, TestTube, Eye, Terminal, Command } from "lucide-react"
import type { EditorMode } from "./types"
import { MODE_CONFIGS } from "./types"
import { Button } from "@/components/ui/button"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"

interface ModeSelectorProps {
  currentMode: EditorMode
  onModeChange: (mode: EditorMode) => void
  compact?: boolean
}

const ICON_MAP = {
  Play,
  TestTube,
  Eye,
  Terminal,
  Command,
}

export function ModeSelector({ currentMode, onModeChange, compact = false }: ModeSelectorProps) {
  const current = MODE_CONFIGS[currentMode]
  const CurrentIcon = ICON_MAP[current.icon as keyof typeof ICON_MAP] || Play

  if (compact) {
    return (
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="outline" size="sm" className="flex items-center gap-2">
            <CurrentIcon className="h-3 w-3" />
            <span className="text-xs">{current.label}</span>
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end">
          {Object.values(MODE_CONFIGS).map((mode) => {
            const Icon = ICON_MAP[mode.icon as keyof typeof ICON_MAP] || Play
            return (
              <DropdownMenuItem
                key={mode.id}
                onClick={() => onModeChange(mode.id)}
                className="flex items-start gap-3"
              >
                <Icon className="h-4 w-4 mt-0.5 text-blue-600 dark:text-blue-400" />
                <div>
                  <div className="font-medium text-sm">{mode.label}</div>
                  <div className="text-xs text-gray-500 dark:text-gray-400">{mode.description}</div>
                </div>
              </DropdownMenuItem>
            )
          })}
        </DropdownMenuContent>
      </DropdownMenu>
    )
  }

  return (
    <div className="flex items-center gap-2">
      <span className="text-sm font-medium text-gray-600 dark:text-gray-400">Mode:</span>
      <div className="flex items-center gap-1 bg-gray-100 dark:bg-gray-800 rounded-lg p-1">
        {Object.values(MODE_CONFIGS).map((mode) => {
          const Icon = ICON_MAP[mode.icon as keyof typeof ICON_MAP] || Play
          const isActive = mode.id === currentMode
          
          return (
            <button
              key={mode.id}
              onClick={() => onModeChange(mode.id)}
              className={`
                flex items-center gap-2 px-3 py-1.5 rounded-md text-sm font-medium transition-colors
                ${
                  isActive
                    ? "bg-white dark:bg-gray-700 text-blue-600 dark:text-blue-400 shadow-sm"
                    : "text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200"
                }
              `}
              title={mode.description}
            >
              <Icon className="h-4 w-4" />
              <span>{mode.label}</span>
            </button>
          )
        })}
      </div>
    </div>
  )
}
