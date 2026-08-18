"use client"

import { EditorProvider } from "@/components/block-content-editor/engines/editor-provider"
import { EditorToolbar } from "@/components/block-content-editor/engines/editor-toolbar"
import { EditorField } from "@/components/block-content-editor/engines/editor-field"
import { EditorDialogs } from "@/components/block-content-editor/engines/editor-dialogs"
import { StudioLayout } from "../studio/studio-layout"
import type { FieldConfig, ToolbarConfig } from "@/components/block-content-editor/engines/editor-config"

const fieldConfig: Partial<FieldConfig> = {
  allowedBlockTypes: ["quiz"],
  // Project identity: this page creates and opens "quiz" projects.
  projectType: "quiz",
  allowedProjectTypes: ["quiz"],
}

const toolbarConfig: Partial<ToolbarConfig> = {
}

export default function QuizEditorPage() {
  return (
    <EditorProvider fieldConfig={fieldConfig} toolbarConfig={toolbarConfig}>
      <StudioLayout header={<EditorToolbar />} mode="wide" className="max-w-none w-full">
        <EditorField
          contentContainer={{
            className: "flex-1 h-full max-h-[calc(100dvh-16px)]",
            blocksClassName:
              "w-full max-w-6xl mx-auto border border-gray-200 dark:border-gray-700 rounded-lg bg-white dark:bg-gray-900 p-4 transition-transform duration-300 ease-in-out",
          }}
        />
      </StudioLayout>
      <EditorDialogs />
    </EditorProvider>
  )
}
