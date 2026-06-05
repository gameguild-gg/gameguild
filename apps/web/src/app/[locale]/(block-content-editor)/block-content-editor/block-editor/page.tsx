"use client"

import { EditorProvider } from "@/components/block-content-editor/engines/editor-provider"
import { EditorToolbar } from "@/components/block-content-editor/engines/editor-toolbar"
import { EditorField } from "@/components/block-content-editor/engines/editor-field"
import { EditorDialogs } from "@/components/block-content-editor/engines/editor-dialogs"
import { StudioLayout } from "../studio/studio-layout"
import type { ToolbarConfig } from "@/components/block-content-editor/engines/editor-config"

const toolbarConfig: Partial<ToolbarConfig> = {
}

export default function DocEditorPage() {
  return (
    <EditorProvider toolbarConfig={toolbarConfig}>
      <StudioLayout header={<EditorToolbar />}>
        <EditorField />
      </StudioLayout>
      <EditorDialogs />
    </EditorProvider>
  )
}
