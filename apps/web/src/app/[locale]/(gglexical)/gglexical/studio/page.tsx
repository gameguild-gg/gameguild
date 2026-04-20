"use client"

import { EditorProvider } from "@/components/editor/engines/editor-provider"
import { EditorToolbar } from "@/components/editor/engines/editor-toolbar"
import { EditorField } from "@/components/editor/engines/editor-field"
import { EditorDialogs } from "@/components/editor/engines/editor-dialogs"
import { StudioLayout } from "./studio-layout"

export default function Page() {
  return (
    <EditorProvider>
      <StudioLayout>
        <EditorToolbar />
        <EditorField />
      </StudioLayout>
      <EditorDialogs />
    </EditorProvider>
  )
}
