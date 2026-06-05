"use client"

import { EditorProvider } from "@/components/block-content-editor/engines/editor-provider"
import { EditorToolbar } from "@/components/block-content-editor/engines/editor-toolbar"
import { EditorField } from "@/components/block-content-editor/engines/editor-field"
import { EditorDialogs } from "@/components/block-content-editor/engines/editor-dialogs"
import { StudioLayout } from "../studio/studio-layout"

export default function FullEditorPage() {
  return (
    <EditorProvider>
      <StudioLayout header={<EditorToolbar />}>
        <EditorField />
      </StudioLayout>
      <EditorDialogs />
    </EditorProvider>
  )
}
