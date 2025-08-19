"use client"

import type { SerializedSourceCodeNode } from "../../nodes/source-code-node"
import { SourceCodeCore } from "../../nodes/source-code-core"

export function PreviewSourceCode({ node }: { node: SerializedSourceCodeNode }) {
  if (!node?.data) {
    console.error("Invalid source code node structure:", node)
    return null
  }

  return (
    <div className="my-4">
      <SourceCodeCore
        data={node.data}
        isPreview={true}
        onUpdateSourceCode={(newData) => {
          console.log("Source code updated in preview:", newData)
        }}
        onSave={() => {
          console.log("Save triggered in preview mode")
        }}
      />
    </div>
  )
}
