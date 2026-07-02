"use client"

import type { ReactNode } from "react"
import { cn } from "@/lib/utils"

export function StudioLayout({ children, header, className }: { children: ReactNode, header?: ReactNode, className?: string }) {
  return (
    <div className="flex flex-col min-h-screen bg-gray-50 dark:bg-gray-950">
      {header}
      <div className="flex-1 container mx-auto pt-3 pb-3">
        <div className={cn("mx-auto space-y-6 px-4 sm:px-4 lg:px-4 max-w-4xl", className)}>
          {children}
        </div>
      </div>
    </div>
  )
}
