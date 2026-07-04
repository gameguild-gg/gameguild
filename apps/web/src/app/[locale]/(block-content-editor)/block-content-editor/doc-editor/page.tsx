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
      <StudioLayout header={<EditorToolbar />} mode="wide" className="max-w-none w-full">
        <EditorField
          toolbarContainer={{
            className: "w-full shrink-0 h-auto",
            innerClassName: "w-full max-w-4xl mx-auto",
            mergeWithContent: false,
          }}
          contentContainer={{
            className: "flex-1 h-full max-h-[calc(100dvh-16px)]",
            documentClassName: "flex-1 min-h-0 w-full max-w-full bg-transparent border-none shadow-none rounded-none",
            pageSettings: {
              size: "a4",
              orientation: "portrait",
              margin: "normal",
            },
          }}
        />
      </StudioLayout>
      <EditorDialogs />
    </EditorProvider>
  )
}
