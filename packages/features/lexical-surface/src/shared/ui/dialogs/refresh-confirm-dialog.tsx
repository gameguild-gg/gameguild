"use client";

import { BaseConfirmDialog } from "./base-confirm-dialog";
import { RefreshCw } from "lucide-react";

interface RefreshConfirmDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  projectName: string;
  onConfirm: () => void;
  confirmText?: string;
  cancelText?: string;
}

export function RefreshConfirmDialog({
  open,
  onOpenChange,
  projectName,
  onConfirm,
  confirmText = "Refresh",
  cancelText = "Cancel",
}: RefreshConfirmDialogProps) {
  return (
    <BaseConfirmDialog
      open={open}
      onOpenChange={onOpenChange}
      title="Refresh from Original Project?"
      description={`Are you sure you want to refresh "${projectName}"? All local changes will be lost and the project will be reset to its original state.`}
      onConfirm={onConfirm}
      confirmText={confirmText}
      cancelText={cancelText}
      confirmButtonClass="bg-blue-600 text-white hover:bg-blue-700 dark:bg-blue-700 dark:hover:bg-blue-800"
      icon={
        <div className="w-12 h-12 rounded-full bg-blue-100 dark:bg-blue-900/20 flex items-center justify-center">
          <RefreshCw className="w-6 h-6 text-blue-600 dark:text-blue-400" />
        </div>
      }
    />
  );
}
