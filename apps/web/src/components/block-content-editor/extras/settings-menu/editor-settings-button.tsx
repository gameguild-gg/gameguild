"use client"

import { Button } from "@/components/ui/button"
import { Menu } from "lucide-react"
import { BaseSettingsMenu } from "./base-settings-menu"
import { SystemSettings } from "./system-settings"
import { MonacoOptionsForm } from "./monaco-options-form"
import type { EditorSettings } from "./use-editor-settings"

interface EditorSettingsButtonProps {
  settings: EditorSettings
  /**
   * When `true` (default), the menu exposes the dedicated **Editor** and
   * **Preview** tabs that drive every Monaco-surface preference (theme,
   * font size, line numbers, word wrap, minimap, tab size, whitespace
   * rendering). Set `false` for non-Monaco editors (table, divider,
   * button, quiz, …) — those will only see the **System** tab.
   */
  includeMonacoTheme?: boolean
  /**
   * Which Monaco tab to open first. Defaults to `'editor'`; pass
   * `'preview'` when the user is currently looking at a read-only / base
   * Monaco surface so the relevant scope is preselected.
   */
  defaultMonacoTab?: 'editor' | 'preview'
}

/**
 * Single entry-point for any block editor's settings popover. The popover
 * is organized didactically into three tabs:
 *
 * - **Editor** — global Monaco options applied to every editable surface.
 * - **Preview** — global Monaco options applied to read-only renders and
 *   the code-studio "base" display.
 * - **System** — non-Monaco workspace ergonomics (modal sizing). The
 *   modal size still supports a per-nodeType override; all Monaco
 *   options are intentionally global to keep the experience uniform.
 */
export function EditorSettingsButton({ settings, includeMonacoTheme = true, defaultMonacoTab = 'editor' }: EditorSettingsButtonProps) {
  const {
    nodeType,
    showSettingsMenu,
    setShowSettingsMenu,
    setModalSize,
    editor,
    preview,
    setEditorOption,
    setPreviewOption,
  } = settings

  const monacoTabs = includeMonacoTheme
    ? [
        {
          id: 'editor',
          label: 'Editor',
          content: (
            <MonacoOptionsForm scope="editor" options={editor} onChange={setEditorOption} />
          ),
        },
        {
          id: 'preview',
          label: 'Preview',
          content: (
            <MonacoOptionsForm scope="preview" options={preview} onChange={setPreviewOption} />
          ),
        },
      ]
    : []

  return (
    <div className="relative settings-menu-container">
      <Button
        variant="outline"
        size="sm"
        onClick={() => setShowSettingsMenu(!showSettingsMenu)}
        className="h-8 w-8 p-0"
        title="Settings"
      >
        <Menu className="h-4 w-4" />
      </Button>
      {showSettingsMenu && (
        <BaseSettingsMenu
          tabs={[
            ...monacoTabs,
            {
              id: 'system',
              label: 'System',
              content: <SystemSettings nodeType={nodeType} onModalSizeChange={(size) => { void setModalSize(size) }} />,
            },
          ]}
          defaultTab={includeMonacoTheme ? defaultMonacoTab : 'system'}
          onClose={() => setShowSettingsMenu(false)}
        />
      )}
    </div>
  )
}
