"use client"

import { EditorProvider } from "@/components/editor/engines/editor-provider"
import { EditorToolbar } from "@/components/editor/engines/editor-toolbar"
import { EditorField } from "@/components/editor/engines/editor-field"
import { EditorDialogs } from "@/components/editor/engines/editor-dialogs"
import { StudioLayout } from "../studio/studio-layout"
import type { FieldConfig, ToolbarConfig } from "@/components/editor/engines/editor-config"

const fieldConfig: Partial<FieldConfig> = {
  engines: ["lexical"],
  layouts: ["type1"],
  defaultEngine: "lexical",
  defaultLayout: "type1",
  defaultMode: "free-page",
}

const toolbarConfig: Partial<ToolbarConfig> = {
  showPreviewModeSelector: false,
}

export default function DocEditorPage() {
  return (
    <EditorProvider fieldConfig={fieldConfig} toolbarConfig={toolbarConfig}>
      <StudioLayout>
        <EditorToolbar />
        <EditorField />
      </StudioLayout>
      <EditorDialogs />
    </EditorProvider>
  )
}
