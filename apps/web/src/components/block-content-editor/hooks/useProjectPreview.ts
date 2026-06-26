"use client"

import { useState } from "react"
import { toast } from "sonner"
import type { UseProjectStorageReturn } from "./useProjectStorage"

export interface UseProjectPreviewReturn {
  previewOpen: boolean
  setPreviewOpen: (open: boolean) => void
  openPreview(): void
}

export function useProjectPreview(project: UseProjectStorageReturn): UseProjectPreviewReturn {
  const [previewOpen, setPreviewOpen] = useState(false)

  const openPreview = () => {
    if (!project.projectId) {
      toast.error("No project loaded", {
        description: "Please load or create a project first",
        duration: 3000,
      })
      return
    }
    setPreviewOpen(true)
  }

  return {
    previewOpen,
    setPreviewOpen,
    openPreview,
  }
}
