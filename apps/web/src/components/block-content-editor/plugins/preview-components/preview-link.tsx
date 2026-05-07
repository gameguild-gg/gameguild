"use client"

import type React from "react"

interface PreviewLinkProps {
  node: any
  children: React.ReactNode
}

export function PreviewLink({ node, children }: PreviewLinkProps) {
  return (
    <a href={node.url} className="text-primary hover:underline" target="_blank" rel="noopener noreferrer">
      {children}
    </a>
  )
}
