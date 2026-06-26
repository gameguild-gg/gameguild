"use client"

import { EditorProvider } from "@/components/block-content-editor/engines/editor-provider"
import { EditorToolbar } from "@/components/block-content-editor/engines/editor-toolbar"
import { EditorField } from "@/components/block-content-editor/engines/editor-field"
import { EditorDialogs } from "@/components/block-content-editor/engines/editor-dialogs"
import { StudioLayout } from "../studio/studio-layout"
import type { FieldConfig, ToolbarConfig } from "@/components/block-content-editor/engines/editor-config"

const fieldConfig: Partial<FieldConfig> = {
  allowedBlockTypes: [],
  // Project identity: this page creates and opens "quiz" projects.
  projectType: "quiz",
  allowedProjectTypes: ["quiz"],
}

const toolbarConfig: Partial<ToolbarConfig> = {
}

export default function QuizEditorPage() {
  return (
    <EditorProvider fieldConfig={fieldConfig} toolbarConfig={toolbarConfig}>
      <StudioLayout header={<EditorToolbar />}>
        <EditorField />
      </StudioLayout>
      <EditorDialogs />
    </EditorProvider>
  )
}
