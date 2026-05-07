"use client"

import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { Hash, Type, Palette } from "lucide-react"
import type { CodeStudioData, ShikiTheme } from "../code-studio/types"
import { SHIKI_THEME_CONFIGS } from "../code-studio/types"
import { useState, useEffect } from "react"
import { 
  getProjectPreference, 
  setNodeProjectPreference,
  type ProjectPreferences 
} from "@/lib/storage/editor/project-preferences"
import { EnhancedStorageAdapter } from "@/lib/storage/editor/enhanced-storage-adapter"

interface CodeStudioStyleSettingsProps {
  data: CodeStudioData
  onDataChange: (newData: Partial<CodeStudioData>) => void
  projectId?: string // Project ID to save preferences
  onShikiThemePreview?: (theme: ShikiTheme) => void // Callback for immediate preview
}

export function CodeStudioStyleSettings({ data, onDataChange, projectId, onShikiThemePreview }: CodeStudioStyleSettingsProps) {
  const [projectPreferences, setProjectPreferences] = useState<ProjectPreferences | undefined>()
  const [isLoading, setIsLoading] = useState(true)
  
  useEffect(() => {
    // Load project preferences
    const loadPreferences = async () => {
      if (!projectId) {
        setIsLoading(false)
        return
      }
      
      try {
        const adapter = new EnhancedStorageAdapter()
        await adapter.init()
        const prefs = await adapter.getProjectPreferences(projectId)
        setProjectPreferences(prefs)
      } catch (error) {
        console.error("Failed to load project preferences:", error)
      } finally {
        setIsLoading(false)
      }
    }
    
    loadPreferences()
  }, [projectId])
  
  const handleShikiThemeChange = async (theme: ShikiTheme) => {
    // Apply preview immediately
    if (onShikiThemePreview) {
      onShikiThemePreview(theme)
    }
    
    // Save to project preferences (code-studio node-specific)
    if (projectId) {
      try {
        const adapter = new EnhancedStorageAdapter()
        await adapter.init()
        
        const newPreferences = setNodeProjectPreference(
          projectPreferences,
          'code-studio',
          'shikiTheme',
          theme
        )
        
        await adapter.updateProjectPreferences(projectId, newPreferences)
        setProjectPreferences(newPreferences)
      } catch (error) {
        console.error("Failed to save project preferences:", error)
      }
    } else {
      // If no projectId, fallback to saving in node data
      onDataChange({ shikiTheme: theme })
    }
  }
  
  // Get current theme from project preferences (fallback to data)
  const currentTheme = projectId && projectPreferences
    ? getProjectPreference(projectPreferences, 'code-studio', 'shikiTheme') || data.shikiTheme || "github"
    : data.shikiTheme || "github"
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
          value={currentTheme}
          onChange={(e) => handleShikiThemeChange(e.target.value as ShikiTheme)}
          disabled={isLoading}
          className="w-full px-2 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 disabled:opacity-50"
        >
          {Object.entries(SHIKI_THEME_CONFIGS).map(([key, config]) => (
            <option key={key} value={key}>
              {config.label}
            </option>
          ))}
        </select>
        {projectId && (
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Theme saved at project level for Code Studio
          </p>
        )}
      </div>
    </>
  )
}
