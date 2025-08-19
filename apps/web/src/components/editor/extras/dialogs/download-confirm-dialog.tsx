"use client"

import { BaseConfirmDialog } from "./base-confirm-dialog"
import { Download, FileText, Calendar, HardDrive, Archive, FileJson } from "lucide-react"

interface ProjectData {
  id: string
  name: string
  data: string
  tags: string[]
  size: number
  createdAt: string
  updatedAt: string
}

interface DownloadConfirmDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  fileName: string
  fileSize: string
  lastModified: string
  onConfirm: () => void
  confirmText?: string
  cancelText?: string
  project?: ProjectData | null
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
  project,
}: DownloadConfirmDialogProps) {
  return (
    <BaseConfirmDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Download Project Folder"
      description="You are about to download a project folder containing:"
      onConfirm={onConfirm}
      confirmContent={
        <span className="flex items-center gap-2">
          <Download className="h-4 w-4" />
          {confirmText}
        </span>
      }
      cancelText={cancelText}
      confirmButtonClass="bg-green-600 text-white hover:bg-green-700"
      icon={
        <div className="w-12 h-12 rounded-full bg-green-100 dark:bg-green-900/20 flex items-center justify-center">
          <Archive className="w-6 h-6 text-green-600 dark:text-green-400" />
        </div>
      }
    >
      <div className="bg-muted/50 rounded-lg p-4 space-y-3">
        <div className="flex items-center gap-2">
          <Archive className="h-4 w-4 text-muted-foreground" />
          <span className="font-medium">{fileName}</span>
        </div>

        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <HardDrive className="h-4 w-4" />
          <span>Estimated size: {fileSize}</span>
        </div>

        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Calendar className="h-4 w-4" />
          <span>Last modified: {lastModified}</span>
        </div>

        <div className="mt-4 pt-3 border-t border-muted">
          <p className="text-sm font-medium text-muted-foreground mb-2">Folder contents:</p>
          <div className="space-y-2 text-sm">
            <div className="flex items-center gap-2 text-muted-foreground">
              <FileText className="h-3 w-3" />
              <span>{project?.name || "project"}.gglexical</span>
              <span className="text-xs text-muted-foreground/70">(Lexical editor data)</span>
            </div>
            <div className="flex items-center gap-2 text-muted-foreground">
              <FileJson className="h-3 w-3" />
              <span>index.json</span>
              <span className="text-xs text-muted-foreground/70">(Project metadata)</span>
            </div>
          </div>
        </div>
      </div>

      <p className="text-sm text-muted-foreground mt-4">
        The folder will be saved to your default downloads directory.
      </p>
    </BaseConfirmDialog>
  )
}
