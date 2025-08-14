"use client"

import { BaseConfirmDialog } from "./base-confirm-dialog"
import { Download, FileText, Calendar, HardDrive } from "lucide-react"

interface DownloadConfirmDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  fileName: string
  fileSize: string
  lastModified: string
  onConfirm: () => void
  confirmText?: string
  cancelText?: string
}

export function DownloadConfirmDialog({
  open,
  onOpenChange,
  fileName,
  fileSize,
  lastModified,
  onConfirm,
  confirmText = "Download",
  cancelText = "Cancel",
}: DownloadConfirmDialogProps) {
  return (
    <BaseConfirmDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Download File"
      description="You are about to download the following file:"
      onConfirm={onConfirm}
      confirmContent={
        <span className="flex items-center gap-2">
          <Download className="h-4 w-4" />
          {confirmText}
        </span>
      }
      confirmText={confirmText}
      cancelText={cancelText}
      confirmButtonClass="bg-green-600 text-white hover:bg-green-700"
      icon={
        <div className="w-12 h-12 rounded-full bg-green-100 dark:bg-green-900/20 flex items-center justify-center">
          <Download className="w-6 h-6 text-green-600 dark:text-green-400" />
        </div>
      }
    >
      <div className="bg-muted/50 rounded-lg p-4 space-y-3">
        <div className="flex items-center gap-2">
          <FileText className="h-4 w-4 text-muted-foreground" />
          <span className="font-medium">{fileName}</span>
        </div>

        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <HardDrive className="h-4 w-4" />
          <span>Size: {fileSize}</span>
        </div>

        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Calendar className="h-4 w-4" />
          <span>Last modified: {lastModified}</span>
        </div>
      </div>

      <p className="text-sm text-muted-foreground mt-4">The file will be saved to your default downloads folder.</p>
    </BaseConfirmDialog>
  )
}
