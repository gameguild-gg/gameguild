"use client"

import type { ReactNode } from "react"

export function StudioLayout({ children, header }: { children: ReactNode, header?: ReactNode }) {
  return (
    <div className="flex flex-col min-h-screen bg-gray-50 dark:bg-gray-950">
      {header}
      <div className="flex-1 container mx-auto py-8">
        <div className="mx-auto space-y-6 px-4 sm:px-4 lg:px-4 max-w-4xl">
          {children}
          <footer aria-hidden="true" className="h-24 sm:h-32" />
        </div>
      </div>
    </div>
  )
}
