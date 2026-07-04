"use client"

import type { ReactNode } from "react"
import { cn } from "@/lib/utils"

type StudioLayoutMode = "content" | "wide"

interface StudioLayoutProps {
  children: ReactNode
  header?: ReactNode
  className?: string
  /**
   * `content` keeps the classic centered container.
   * `wide` removes the outer `container` cap for edge-to-edge layouts.
   */
  mode?: StudioLayoutMode
}

export function StudioLayout({ children, header, className, mode = "content" }: StudioLayoutProps) {
  const hasHeader = header != null

  const shellClass =
    mode === "wide"
      ? cn(
          "flex-1 min-h-0 w-full px-3 pb-3 box-border overflow-hidden",
          hasHeader ? "pt-[var(--editor-toolbar-offset,68px)]" : "pt-3",
        )
      : cn(
          "flex-1 min-h-0 container mx-auto px-3 pb-3 box-border overflow-hidden",
          hasHeader ? "pt-[var(--editor-toolbar-offset,68px)]" : "pt-3",
        )

  const contentClass =
    mode === "wide"
      ? "w-full h-full min-h-0 space-y-6"
      : "mx-auto w-full h-full min-h-0 space-y-6 px-4 sm:px-4 lg:px-4 max-w-4xl"

  return (
    <div className="flex h-dvh min-h-0 flex-col overflow-hidden bg-gray-50 dark:bg-gray-950">
      {header}
      <div className={shellClass}>
        <div className={cn(contentClass, className)}>
          {children}
        </div>
      </div>
    </div>
  )
}
