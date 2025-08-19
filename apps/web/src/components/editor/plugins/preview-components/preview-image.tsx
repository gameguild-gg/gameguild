"use client"

import type { SerializedImageNode } from "../../nodes/image-node"

export function PreviewImage({ node }: { node: SerializedImageNode }) {
  if (!node?.data) {
    console.error("Invalid image node structure:", node)
    return null
  }

  const { src, alt, caption, size = 100 } = node.data

  return (
    <div className="my-4 relative">
      <div className="relative flex justify-center">
        <img src={src || "/placeholder.svg"} alt={alt} style={{ width: `${size}%` }} className="h-auto rounded-lg" />
      </div>
      {caption && <div className="mt-2 text-sm text-muted-foreground text-center">{caption}</div>}
    </div>
  )
}
