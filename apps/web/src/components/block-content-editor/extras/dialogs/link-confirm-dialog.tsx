"use client"

import { BaseConfirmDialog } from "./base-confirm-dialog"
import { ExternalLink } from "lucide-react"

interface LinkConfirmDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  url: string
  onConfirm: () => void
}

export function LinkConfirmDialog({
  open,
  onOpenChange,
  url,
  onConfirm,
}: LinkConfirmDialogProps) {
  return (
    <BaseConfirmDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Open External Link"
      description="Do you want to open this URL in a new tab?"
      confirmText="Open Link"
      cancelText="Cancel"
      confirmButtonClass="bg-blue-600 text-white hover:bg-blue-700"
      icon={
        <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-blue-100 dark:bg-blue-900">
          <ExternalLink className="h-6 w-6 text-blue-600 dark:text-blue-400" />
        </div>
      }
      onConfirm={onConfirm}
    >
      <div className="bg-gray-100 dark:bg-gray-800 rounded-md p-3 break-all">
        <code className="text-sm text-gray-900 dark:text-gray-100">
          {url}
        </code>
      </div>
    </BaseConfirmDialog>
  )
}
