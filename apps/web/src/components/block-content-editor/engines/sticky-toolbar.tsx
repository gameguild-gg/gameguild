"use client"

import * as React from "react"
import { cn } from "@/lib/utils"

interface StickyToolbarProps {
  children: React.ReactNode
  offset?: string | number
  className?: string
}

export function StickyToolbar({ children, offset, className }: StickyToolbarProps) {
  return (
    <div
      className={cn("sticky z-10", className)}
      style={{
        top: offset ?? "var(--lexical-toolbar-offset, 0px)",
      }}
    >
      {children}
    </div>
  )
}
