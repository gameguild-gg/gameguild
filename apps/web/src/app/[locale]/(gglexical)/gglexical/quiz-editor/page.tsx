"use client"

import { EditorProvider } from "@/components/editor/engines/editor-provider"
import { EditorToolbar } from "@/components/editor/engines/editor-toolbar"
import { EditorField } from "@/components/editor/engines/editor-field"
import { EditorDialogs } from "@/components/editor/engines/editor-dialogs"
import { StudioLayout } from "../studio/studio-layout"
import type { FieldConfig, ToolbarConfig } from "@/components/editor/engines/editor-config"

const fieldConfig: Partial<FieldConfig> = {
  engines: ["blocks"],
  layouts: ["type1"],
  allowedBlockTypes: [],
  allowedModes: ["quiz-page"],
  defaultEngine: "blocks",
  defaultLayout: "type1",
}

const toolbarConfig: Partial<ToolbarConfig> = {
  showPreviewModeSelector: false,
}

export default function QuizEditorPage() {
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
