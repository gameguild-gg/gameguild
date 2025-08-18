"use client"

import type React from "react"

interface PreviewParagraphProps {
  node: any
  children: React.ReactNode
}

export function PreviewParagraph({ node, children }: PreviewParagraphProps) {
  const paragraphClasses = ["my-4"]
  if (node.format === "left") paragraphClasses.push("text-left")
  else if (node.format === "center") paragraphClasses.push("text-center")
  else if (node.format === "right") paragraphClasses.push("text-right")
  else if (node.format === "justify") paragraphClasses.push("text-justify")

  return <p className={paragraphClasses.join(" ")}>{children}</p>
}
