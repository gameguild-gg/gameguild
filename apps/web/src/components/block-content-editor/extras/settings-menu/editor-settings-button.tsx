"use client"

import { Button } from "@/components/ui/button"
import { Menu } from "lucide-react"
import { BaseSettingsMenu } from "./base-settings-menu"
import { SystemSettings } from "./system-settings"
import { MonacoStyleSettings } from "./monaco-style-settings"
import { MonacoThemeSettings } from "./monaco-theme-settings"
import type { EditorSettings } from "./use-editor-settings"

interface EditorSettingsButtonProps {
  settings: EditorSettings
  /**
   * When `true`, the Style tab includes the shared Monaco/Shiki theme
   * controls. Default `true`; set `false` for non-Monaco editors (e.g.
   * table, divider, button, quiz) where syntax-theme picking makes no
   * sense.
   */
  includeMonacoTheme?: boolean
}

export function EditorSettingsButton({ settings, includeMonacoTheme = true }: EditorSettingsButtonProps) {
  const {
    nodeType,
    showSettingsMenu,
    setShowSettingsMenu,
    setModalSize,
    editorFontSize,
    setEditorFontSize,
    editorLineNumbers,
    setEditorLineNumbers,
  } = settings

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
            {
              id: 'style',
              label: 'Style',
              content: (
                <div className="space-y-4">
                  <MonacoStyleSettings
                    fontSize={editorFontSize}
                    showLineNumbers={editorLineNumbers}
                    onFontSizeChange={setEditorFontSize}
                    onLineNumbersChange={setEditorLineNumbers}
                  />
                  {includeMonacoTheme && <MonacoThemeSettings settings={settings} />}
                </div>
              ),
            },
            {
              id: 'system',
              label: 'System',
              content: <SystemSettings nodeType={nodeType} onModalSizeChange={(size) => { void setModalSize(size) }} />,
            },
          ]}
          defaultTab="style"
          onClose={() => setShowSettingsMenu(false)}
        />
      )}
    </div>
  )
}
