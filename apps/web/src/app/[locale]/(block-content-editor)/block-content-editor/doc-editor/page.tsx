"use client"

import { EditorProvider } from "@/components/block-content-editor/engines/editor-provider"
import { EditorToolbar } from "@/components/block-content-editor/engines/editor-toolbar"
import { EditorField } from "@/components/block-content-editor/engines/editor-field"
import { EditorDialogs } from "@/components/block-content-editor/engines/editor-dialogs"
import { StudioLayout } from "../studio/studio-layout"
import type { FieldConfig, ToolbarConfig } from "@/components/block-content-editor/engines/editor-config"

const fieldConfig: Partial<FieldConfig> = {
  allowedBlockTypes: ["rich-text"],
  // Starts with a single rich-text block; no option to add or
  // remove other blocks. Saving uses the standard project flow.
  singleBlockMode: true,
  // Project identity: this page creates and opens "document" projects.
  projectType: "document",
  allowedProjectTypes: ["document"],
}

const toolbarConfig: Partial<ToolbarConfig> = {
}

export default function DocEditorPage() {
  return (
    <EditorProvider fieldConfig={fieldConfig} toolbarConfig={toolbarConfig}>
      <StudioLayout header={<EditorToolbar />} className="max-w-none w-full">
        <EditorField maxHeight="calc(100dvh - 16px)" />
      </StudioLayout>
      <EditorDialogs />
    </EditorProvider>
  )
}
