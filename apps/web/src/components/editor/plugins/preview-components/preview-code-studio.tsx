"use client"

import type { CodeStudioData } from "@/components/editor/extras/code-studio/types"
import { CodeStudioEditor } from "@/components/editor/extras/code-studio/code-studio-editor"

interface PreviewCodeStudioProps {
  data: CodeStudioData
}

export function PreviewCodeStudio({ data }: PreviewCodeStudioProps) {
  return (
    <CodeStudioEditor
      data={data}
      isPreview={true}
      onUpdate={() => {}}
    />
  )
}
