"use client"

import type React from "react"

interface PreviewListProps {
  node: any
  children: React.ReactNode
}

export function PreviewList({ node, children }: PreviewListProps) {
  const ListTag = node.listType === "bullet" ? "ul" : "ol"
  const listClass = node.listType === "bullet" ? "list-disc list-inside" : "list-decimal list-inside"

  return <ListTag className={`${listClass} my-4`}>{children}</ListTag>
}
