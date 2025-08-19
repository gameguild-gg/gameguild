"use client"

import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog"
import type { ReactNode } from "react"

interface BaseConfirmDialogProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  description?: string
  children?: ReactNode
  onConfirm: () => void
  confirmText?: string
  confirmContent?: ReactNode
  cancelText?: string
  confirmButtonClass?: string
  icon?: ReactNode
}

export function BaseConfirmDialog({
  open,
  onOpenChange,
  title,
  description,
  children,
  onConfirm,
  confirmText = "Confirm",
  cancelText = "Cancel",
  confirmButtonClass = "bg-primary text-primary-foreground hover:bg-primary/90",
  icon,
}: BaseConfirmDialogProps) {
  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent>
        <AlertDialogHeader>
          {icon && <div className="flex justify-center mb-4">{icon}</div>}
          <AlertDialogTitle className="text-center">{title}</AlertDialogTitle>
          {description && <AlertDialogDescription className="text-center">{description}</AlertDialogDescription>}
          {children && <div className="mt-4">{children}</div>}
        </AlertDialogHeader>
        <AlertDialogFooter className="flex gap-2 justify-center">
          <AlertDialogCancel className="flex-1">{cancelText}</AlertDialogCancel>
          <AlertDialogAction onClick={onConfirm} className={`flex-1 ${confirmButtonClass}`}>
            {confirmText}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
