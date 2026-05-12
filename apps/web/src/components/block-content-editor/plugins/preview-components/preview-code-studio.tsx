"use client"

import type { CodeStudioData } from "@/components/block-content-editor/extras/code-studio/types"
import { CodeStudioEditor } from "@/components/block-content-editor/lazy-client-components"

interface PreviewCodeStudioProps {
  data: CodeStudioData
  projectId?: string
}

export function PreviewCodeStudio({ data, projectId }: PreviewCodeStudioProps) {
  return (
    <CodeStudioEditor
      data={data}
      isPreview={true}
      onUpdate={() => {}}
      projectId={projectId}
    />
  )
}
