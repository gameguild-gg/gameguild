"use client"

import { BaseConfirmDialog } from "./base-confirm-dialog"
import { DoorOpen } from 'lucide-react'

interface ExitConfirmDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title?: string
  description?: string
  itemName?: string
  itemType?: string
  onConfirm: () => void
  confirmText?: string
  cancelText?: string
  isDestructive?: boolean
  showSaveAndExit?: boolean
  onSaveAndExit?: () => void
  saveAndExitText?: string
}

export function ExitConfirmDialog({
  open,
  onOpenChange,
  title = "Exit Confirmation",
  description,
  itemName,
  itemType = "item",
  onConfirm,
  confirmText = "Exit",
  cancelText = "Cancel",
  isDestructive = true,
  showSaveAndExit = false,
  onSaveAndExit,
  saveAndExitText = "Save and Exit",
}: ExitConfirmDialogProps) {
  const defaultDescription = itemName
    ? `You are about to leave ${itemType} "${itemName}". Save to avoid losing your work.`
    : `Are you sure you want to leave this ${itemType}? Save to avoid losing your work.`

  return (
    <BaseConfirmDialog
      open={open}
      onOpenChange={onOpenChange}
      title={title}
      description={description || defaultDescription}
      onConfirm={onConfirm}
      confirmText={confirmText}
      cancelText={cancelText}
      confirmButtonClass={isDestructive ? "bg-destructive text-destructive-foreground hover:bg-destructive/90" : "bg-primary text-primary-foreground hover:bg-primary/90"}
      showSaveAndExit={showSaveAndExit}
      onSaveAndExit={onSaveAndExit}
      saveAndExitText={saveAndExitText}
      icon={
        <div className={`w-12 h-12 rounded-full flex items-center justify-center ${isDestructive ? 'bg-red-100 dark:bg-red-900/20' : 'bg-yellow-100 dark:bg-yellow-900/20'}`}>
          <DoorOpen className={`w-6 h-6 ${isDestructive ? 'text-red-600 dark:text-red-400' : 'text-yellow-600 dark:text-yellow-400'}`} />
        </div>
      }
    />
  )
}
