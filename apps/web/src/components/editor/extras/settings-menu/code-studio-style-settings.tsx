"use client"

import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { Hash, Type, Palette } from "lucide-react"
import type { CodeStudioData, ShikiTheme } from "../code-studio/types"
import { SHIKI_THEME_CONFIGS } from "../code-studio/types"

interface CodeStudioStyleSettingsProps {
  data: CodeStudioData
  onDataChange: (newData: Partial<CodeStudioData>) => void
}

export function CodeStudioStyleSettings({ data, onDataChange }: CodeStudioStyleSettingsProps) {
  return (
    <>
      {/* Show Line Numbers */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Hash className="h-4 w-4 text-blue-500" />
          <Label htmlFor="lineNumbers" className="text-sm font-medium cursor-pointer">
            Line Numbers
          </Label>
        </div>
        <Switch
          id="lineNumbers"
          checked={data.showLineNumbers ?? true}
          onCheckedChange={(checked) => onDataChange({ showLineNumbers: checked })}
        />
      </div>
      
      {/* Font Size */}
      <div className="space-y-2">
        <div className="flex items-center gap-2">
          <Type className="h-4 w-4 text-purple-500" />
          <Label htmlFor="fontSize" className="text-sm font-medium">
            Font Size: {data.fontSize || 14}px
          </Label>
        </div>
        <input
          id="fontSize"
          type="range"
          min="10"
          max="24"
          value={data.fontSize || 14}
          onChange={(e) => onDataChange({ fontSize: parseInt(e.target.value) })}
          className="w-full"
        />
      </div>
      
      {/* Shiki Theme */}
      <div className="space-y-2">
        <div className="flex items-center gap-2">
          <Palette className="h-4 w-4 text-indigo-500" />
          <Label htmlFor="shikiTheme" className="text-sm font-medium">
            Syntax Theme
          </Label>
        </div>
        <select
          id="shikiTheme"
          value={data.shikiTheme || "github"}
          onChange={(e) => onDataChange({ shikiTheme: e.target.value as ShikiTheme })}
          className="w-full px-2 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
        >
          {Object.entries(SHIKI_THEME_CONFIGS).map(([key, config]) => (
            <option key={key} value={key}>
              {config.label}
            </option>
          ))}
        </select>
      </div>
    </>
  )
}
