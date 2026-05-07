"use client"

import type { ReactNode } from "react"
import { useEditor } from "@/components/block-content-editor/engines/editor-provider"

export function StudioLayout({ children }: { children: ReactNode }) {
  const { project } = useEditor()

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
      <div className="container mx-auto py-8">
        <div className="mx-auto space-y-6 px-4 sm:px-4 lg:px-4 max-w-4xl">
          {children}
        </div>
      </div>
    </div>
  )
}
