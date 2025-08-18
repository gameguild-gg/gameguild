"use client"

import type React from "react"
import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog"
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
}: ExitConfirmDialogProps) {
  const defaultDescription = itemName
    ? `Você está prestes a sair de ${itemType} "${itemName}". Salve para nao perder seu trabalho.`
    : `Tem certeza que deseja sair de ${itemType}? Salve para nao perder seu trabalho.`

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-sm">
        <DialogHeader>
          <DialogTitle className="text-center">{title}</DialogTitle>
        </DialogHeader>
        <div className="space-y-4 py-3">
          <div className="flex flex-col items-center justify-center text-center">
            <div className={`p-3 rounded-full mb-3 ${isDestructive ? 'bg-red-100' : 'bg-yellow-100'}`}>
              <DoorOpen className={`w-6 h-6 ${isDestructive ? 'text-red-600' : 'text-yellow-600'}`} />
            </div>
            <h3 className="text-lg font-medium">
              {itemName ? `Exit ${itemType}?` : `Exit confirm?`}
            </h3>
            <p className="text-sm text-gray-500 mt-2 mb-3">
              {description || defaultDescription}
            </p>
          </div>
          <div className="flex justify-between gap-3 pt-2">
            <Button 
              variant="outline" 
              className="flex-1 bg-transparent" 
              onClick={() => onOpenChange(false)}
            >
              {cancelText}
            </Button>
            <Button
              variant={isDestructive ? "destructive" : "default"}
              className="flex-1"
              onClick={() => {
                onConfirm()
                onOpenChange(false)
              }}
            >
              {confirmText}
            </Button>
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
