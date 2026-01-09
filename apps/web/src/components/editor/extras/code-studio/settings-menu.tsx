"use client"

import type { CodeStudioData } from "./types"
import { ModalSize } from "@/lib/storage/editor/editor-preferences"
import { BaseSettingsMenu, SystemSettings, CodeStudioStyleSettings, type SettingsTab } from "../settings-menu"

interface SettingsMenuProps {
  data: CodeStudioData
  onDataChange: (newData: Partial<CodeStudioData>) => void
  onClose: () => void
  nodeType?: string
  onModalSizeChange?: (size: ModalSize) => void
}

export function SettingsMenu({ data, onDataChange, onClose, nodeType = 'code-studio', onModalSizeChange }: SettingsMenuProps) {
  const tabs: SettingsTab[] = [
    {
      id: 'style',
      label: 'Style',
      content: <CodeStudioStyleSettings data={data} onDataChange={onDataChange} />
    },
    {
      id: 'system',
      label: 'System',
      content: <SystemSettings nodeType={nodeType} onModalSizeChange={onModalSizeChange} />
    }
  ]
  
  return <BaseSettingsMenu tabs={tabs} defaultTab="style" onClose={onClose} />
}
