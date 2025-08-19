"use client"

import type { SerializedDividerNode } from "../../nodes/divider-node"

export function PreviewDivider({ node }: { node: SerializedDividerNode }) {
  if (!node?.data) {
    console.error("Invalid divider node structure:", node)
    return null
  }

  const { style } = node.data

  switch (style) {
    case "simple":
      return <hr className="my-6 border-t border-gray-300 dark:border-gray-700" />
    case "double":
      return <hr className="my-6 border-t-2 border-double border-gray-300 dark:border-gray-700" />
    case "dashed":
      return <hr className="my-6 border-t-2 border-dashed border-gray-300 dark:border-gray-700" />
    case "dotted":
      return <hr className="my-6 border-t-2 border-dotted border-gray-300 dark:border-gray-700" />
    case "gradient":
      return (
        <div className="my-6 h-px bg-gradient-to-r from-transparent via-primary to-transparent" aria-hidden="true" />
      )
    case "icon":
      return (
        <div className="my-6 flex items-center justify-center">
          <div className="flex-1 border-t border-gray-300 dark:border-gray-700" />
          <div className="mx-4 text-gray-500 dark:text-gray-400">●</div>
          <div className="flex-1 border-t border-gray-300 dark:border-gray-700" />
        </div>
      )
    default:
      return <hr className="my-6 border-t border-gray-300 dark:border-gray-700" />
  }
}
