"use client"

import type { ReactNode } from "react"

export function StudioLayout({ children }: { children: ReactNode }) {
  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
      <div className="container mx-auto py-8">
        <div className="mx-auto space-y-6 px-4 sm:px-4 lg:px-4 max-w-4xl">
          {children}
          <footer aria-hidden="true" className="h-24 sm:h-32" />
        </div>
      </div>
    </div>
  )
}
