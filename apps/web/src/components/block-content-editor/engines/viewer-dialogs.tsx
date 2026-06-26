"use client"

import { useRouter } from "next/navigation"
import { ExitConfirmDialog } from "@/components/block-content-editor/extras/dialogs/exit-confirm-dialog"
import { useViewer } from "./viewer-provider"

export function ViewerDialogs() {
  const router = useRouter()
  const { viewer, ui } = useViewer()

  return (
    <ExitConfirmDialog
      open={ui.exitDialogOpen}
      onOpenChange={ui.setExitDialogOpen}
      onConfirm={() => {
        if (ui.nextUrl) router.push(ui.nextUrl)
      }}
      itemName={viewer.currentProject?.name}
      itemType="project"
    />
  )
}
