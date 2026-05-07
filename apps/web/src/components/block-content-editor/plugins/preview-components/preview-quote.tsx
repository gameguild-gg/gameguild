"use client"

import type React from "react"

interface PreviewQuoteProps {
  node: any
  children: React.ReactNode
}

export function PreviewQuote({ node, children }: PreviewQuoteProps) {
  return <blockquote className="border-l-4 border-muted pl-4 italic my-4">{children}</blockquote>
}
