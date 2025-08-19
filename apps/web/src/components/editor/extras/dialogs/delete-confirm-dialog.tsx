"use client"

import { BaseConfirmDialog } from "./base-confirm-dialog"
import { Trash2 } from "lucide-react"

interface DeleteConfirmDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  itemName: string | undefined
  itemType: string
  onConfirm: () => void
  confirmText?: string
  cancelText?: string
}

export function DeleteConfirmDialog({
  open,
  onOpenChange,
  title,
  itemName,
  itemType,
  onConfirm,
  confirmText = "Delete",
  cancelText = "Cancel",
}: DeleteConfirmDialogProps) {
  return (
    <BaseConfirmDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Delete File"
      description={`Are you sure you want to delete "${itemName}"? \t This ${itemType} will be permanently removed and cannot be recovered.`}
      onConfirm={onConfirm}
      confirmText={confirmText}
      cancelText={cancelText}
      confirmButtonClass="bg-destructive text-destructive-foreground hover:bg-destructive/90"
      icon={
        <div className="w-12 h-12 rounded-full bg-red-100 dark:bg-red-900/20 flex items-center justify-center">
          <Trash2 className="w-6 h-6 text-red-600 dark:text-red-400" />
        </div>
      }
    />
  )
}
