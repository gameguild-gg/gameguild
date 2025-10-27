"use client"

import type { MermaidData } from "@/components/editor/nodes/mermaid-node"
import { ControlledMermaidViewer } from "@/components/editor/extras/mermaid/controlled-mermaid-viewer"

interface PreviewMermaidProps {
  data: MermaidData
}

export function PreviewMermaid({ data }: PreviewMermaidProps) {
  return (
    <div className="my-4" style={{ width: `${data.size || 100}%` }}>
      <div className="border rounded-lg bg-white dark:bg-gray-800 p-4 shadow-sm">
        {data.title && <h3 className="text-lg font-semibold mb-2 text-center dark:text-white">{data.title}</h3>}

        <ControlledMermaidViewer 
          data={data}
          className="min-h-[200px]"
          showError={true}
          showLoading={true}
        />

        {data.caption && (
          <p className="text-sm text-gray-600 dark:text-gray-300 mt-2 text-center italic">{data.caption}</p>
        )}
      </div>
    </div>
  )
}
