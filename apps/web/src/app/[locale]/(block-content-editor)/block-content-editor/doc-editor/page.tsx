"use client"

import { EditorProvider } from "@/components/block-content-editor/engines/editor-provider"
import { EditorToolbar } from "@/components/block-content-editor/engines/editor-toolbar"
import { EditorField } from "@/components/block-content-editor/engines/editor-field"
import { EditorDialogs } from "@/components/block-content-editor/engines/editor-dialogs"
import { StudioLayout } from "../studio/studio-layout"
import type { FieldConfig, ToolbarConfig } from "@/components/block-content-editor/engines/editor-config"

const fieldConfig: Partial<FieldConfig> = {
  allowedBlockTypes: ["rich-text"],
  defaultMode: "free-page",
  // Starts with a single rich-text block; no option to add or
  // remove other blocks. Saving uses the standard project flow.
  singleBlockMode: true,
}

const toolbarConfig: Partial<ToolbarConfig> = {
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
