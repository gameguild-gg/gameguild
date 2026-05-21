"use client"

import { Label } from "@/components/ui/label"
import { Palette, Eye } from "lucide-react"
import { SHIKI_THEME_CONFIGS, type ShikiTheme } from "@/components/block-content-editor/lib/shiki/themes"
import type { EditorSettings } from "./use-editor-settings"

interface MonacoThemeSettingsProps {
  settings: EditorSettings
}

/**
 * Style-tab fragment that controls the two global Shiki/Monaco themes
 * shared by every Monaco-backed block editor:
 *
 * - **Editor Theme**: applied to all editor surfaces (code-studio
 *   secondary displays, html, markdown, mermaid, vega-lite, …).
 * - **Preview Theme**: applied to read-only / document renders of those
 *   blocks, including the code-studio "base" display (which is itself a
 *   what-students-see preview).
 *
 * Both preferences are global — no per-nodeType overrides — so the mental
 * model stays simple: one theme for editing, one theme for viewing.
 */
export function MonacoThemeSettings({ settings }: MonacoThemeSettingsProps) {
  const { shikiTheme, setShikiTheme, previewShikiTheme, setPreviewShikiTheme } = settings

  // Render nothing while preferences are still hydrating from IndexedDB
  // to avoid flashing the global default before the resolved value loads.
  if (shikiTheme === null || previewShikiTheme === null) {
    return null
  }

  return (
    <>
      {/* Editor syntax theme (global) */}
      <div className="space-y-2">
        <div className="flex items-center gap-2">
          <Palette className="h-4 w-4 text-indigo-500" />
          <Label htmlFor="shikiTheme" className="text-sm font-medium">
            Editor Theme
          </Label>
        </div>
        <select
          id="shikiTheme"
          value={shikiTheme}
          onChange={(e) => void setShikiTheme(e.target.value as ShikiTheme)}
          className="w-full px-2 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
        >
          {Object.entries(SHIKI_THEME_CONFIGS).map(([key, config]) => (
            <option key={key} value={key}>
              {config.label}
            </option>
          ))}
        </select>
        <p className="text-xs text-gray-500 dark:text-gray-400">
          Used in every Monaco editor surface.
        </p>
      </div>

      {/* Preview syntax theme (global) */}
      <div className="space-y-2 pt-2 border-t border-gray-200 dark:border-gray-700">
        <div className="flex items-center gap-2">
          <Eye className="h-4 w-4 text-teal-500" />
          <Label htmlFor="previewShikiTheme" className="text-sm font-medium">
            Preview Theme
          </Label>
        </div>
        <select
          id="previewShikiTheme"
          value={previewShikiTheme}
          onChange={(e) => void setPreviewShikiTheme(e.target.value as ShikiTheme)}
          className="w-full px-2 py-1 text-sm border border-gray-300 dark:border-gray-600 rounded-md bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100"
        >
          {Object.entries(SHIKI_THEME_CONFIGS).map(([key, config]) => (
            <option key={key} value={key}>
              {config.label}
            </option>
          ))}
        </select>
        <p className="text-xs text-gray-500 dark:text-gray-400">
          Used in read-only views and in the code-studio base display.
        </p>
      </div>
    </>
  )
}
