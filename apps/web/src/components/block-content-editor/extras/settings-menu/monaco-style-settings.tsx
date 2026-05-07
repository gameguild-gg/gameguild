"use client"

import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { Hash, Type } from "lucide-react"

interface MonacoStyleSettingsProps {
  fontSize: number
  showLineNumbers: boolean
  onFontSizeChange: (size: number) => void
  onLineNumbersChange: (show: boolean) => void
}

export function MonacoStyleSettings({ fontSize, showLineNumbers, onFontSizeChange, onLineNumbersChange }: MonacoStyleSettingsProps) {
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
          checked={showLineNumbers}
          onCheckedChange={onLineNumbersChange}
        />
      </div>
      
      {/* Font Size */}
      <div className="space-y-2">
        <div className="flex items-center gap-2">
          <Type className="h-4 w-4 text-purple-500" />
          <Label htmlFor="fontSize" className="text-sm font-medium">
            Font Size: {fontSize}px
          </Label>
        </div>
        <input
          id="fontSize"
          type="range"
          min="10"
          max="24"
          value={fontSize}
          onChange={(e) => onFontSizeChange(parseInt(e.target.value))}
          className="w-full"
        />
      </div>
    </>
  )
}
