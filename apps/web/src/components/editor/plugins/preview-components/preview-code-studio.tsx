"use client"

import dynamic from "next/dynamic"
import type { CodeStudioData } from "@/components/editor/extras/code-studio/types"

const CodeStudioEditor = dynamic(
  () => import("@/components/editor/extras/code-studio/code-studio-editor").then(mod => ({ default: mod.CodeStudioEditor })),
  { ssr: false }
)

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
