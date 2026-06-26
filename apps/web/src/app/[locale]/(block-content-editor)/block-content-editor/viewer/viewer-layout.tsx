"use client"

import type { ReactNode } from "react"
import { useViewer } from "@/components/block-content-editor/engines/viewer-provider"

export function ViewerLayout({ children, header }: { children: ReactNode, header?: ReactNode }) {
  const { viewer } = useViewer()

  const hasContent = !!viewer.currentProject && viewer.blocks.length > 0

  return (
    <div className="flex flex-col min-h-screen bg-gray-50 dark:bg-gray-950">
      {header}
      <div className="flex-1 container mx-auto py-10">
        <div
          className={`mx-auto space-y-4 px-4 sm:px-6 lg:px-8 ${
            hasContent ? "max-w-full" : "max-w-4xl"
          }`}
        >
          {children}
        </div>
      </div>
    </div>
  )
}
