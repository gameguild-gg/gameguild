"use client"

import { Button } from "@/components/ui/button"
import { Menu } from "lucide-react"
import { BaseSettingsMenu } from "./base-settings-menu"
import { SystemSettings } from "./system-settings"
import { MonacoStyleSettings } from "./monaco-style-settings"
import type { EditorSettings } from "./use-editor-settings"

interface EditorSettingsButtonProps {
  settings: EditorSettings
}

export function EditorSettingsButton({ settings }: EditorSettingsButtonProps) {
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
                <MonacoStyleSettings
                  fontSize={editorFontSize}
                  showLineNumbers={editorLineNumbers}
                  onFontSizeChange={setEditorFontSize}
                  onLineNumbersChange={setEditorLineNumbers}
                />
              ),
            },
            {
              id: 'system',
              label: 'System',
              content: <SystemSettings nodeType={nodeType} onModalSizeChange={setModalSize} />,
            },
          ]}
          defaultTab="style"
          onClose={() => setShowSettingsMenu(false)}
        />
      )}
    </div>
  )
}
