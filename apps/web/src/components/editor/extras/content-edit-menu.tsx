"use client"

import type { ReactNode } from "react"
import { Edit } from "lucide-react"
import { Button } from "@/components/ui/button"
import { cn } from "@/lib/utils"

export interface EditMenuOption {
  id: string
  icon: ReactNode
  label: string
  action: () => void
}

interface ContentEditMenuProps {
  options: EditMenuOption[]
  className?: string
  isPreviewMode?: boolean
}

export function ContentEditMenu({ options, className, isPreviewMode = false }: ContentEditMenuProps) {
  // Don't render the button in preview mode
  if (isPreviewMode) {
    return null
  }

  // Get the first option (usually the edit action)
  const primaryOption = options.length > 0 ? options[0] : null

  return (
    <div className={cn("absolute top-1/2 -translate-y-1/2 -right-12 z-10", className)}>
      <Button
        variant="ghost"
        size="icon"
        className="h-8 w-8 rounded-full bg-gray-500/80 hover:bg-gray-600/90 backdrop-blur text-white"
        onClick={(e) => {
          e.stopPropagation()
          // Execute the primary action directly
          if (primaryOption) {
            primaryOption.action()
          }
        }}
        title={primaryOption?.label || "Edit"}
      >
        {primaryOption?.icon || <Edit className="h-4 w-4" />}
      </Button>
    </div>
  )
}
