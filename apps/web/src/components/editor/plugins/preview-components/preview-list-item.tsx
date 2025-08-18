"use client"

import type React from "react"

interface PreviewListItemProps {
  node: any
  children: React.ReactNode
}

export function PreviewListItem({ node, children }: PreviewListItemProps) {
  const listItemClasses = ["my-1"]
  if (node.format === "left") listItemClasses.push("text-left")
  else if (node.format === "center") listItemClasses.push("text-center")
  else if (node.format === "right") listItemClasses.push("text-right")
  else if (node.format === "justify") listItemClasses.push("text-justify")

  return <li className={listItemClasses.join(" ")}>{children}</li>
}
