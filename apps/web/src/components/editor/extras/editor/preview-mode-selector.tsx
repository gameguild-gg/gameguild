"use client"

import { Button } from "@/components/ui/button"
import { Monitor, Presentation } from "lucide-react"
import { toast } from "sonner"
import type { PreviewMode } from "@/lib/storage/editor/slideshow-structure"

interface PreviewModeSelectorProps {
  previewMode: PreviewMode
  onPreviewModeChange: (mode: PreviewMode) => void
}

export function PreviewModeSelector({
  previewMode,
  onPreviewModeChange,
}: PreviewModeSelectorProps) {
  return (
    <div className="flex items-center gap-2 ml-2 pl-2 border-l border-gray-300 dark:border-gray-600">
      <span className="text-xs text-gray-500 dark:text-gray-400">
        Preview Mode:
      </span>
      <Button
        variant={previewMode === "continuous" ? "default" : "outline"}
        size="sm"
        onClick={() => {
          onPreviewModeChange("continuous")
          toast.success("Preview mode changed", {
            description: "Preview will show all slides in continuous scroll",
            duration: 2000
          })
        }}
        className="gap-2 h-8"
        title="Show all slides in continuous scroll"
      >
        <Monitor className="h-3.5 w-3.5" />
        Continuous
      </Button>
      <Button
        variant={previewMode === "slide" ? "default" : "outline"}
        size="sm"
        onClick={() => {
          onPreviewModeChange("slide")
          toast.success("Preview mode changed", {
            description: "Preview will show one slide at a time",
            duration: 2000
          })
        }}
        className="gap-2 h-8"
        title="Show one slide at a time (presentation mode)"
      >
        <Presentation className="h-3.5 w-3.5" />
        Slide
      </Button>
    </div>
  )
}
