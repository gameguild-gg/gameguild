"use client";

import { BaseConfirmDialog } from "./base-confirm-dialog";
import { RotateCcw } from "lucide-react";

interface ResetConfirmDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  resetType: "current" | "all";
  fileName?: string;
  onConfirm: () => void;
  confirmText?: string;
  cancelText?: string;
}

export function ResetConfirmDialog({
  open,
  onOpenChange,
  resetType,
  fileName,
  onConfirm,
  confirmText = "Reset",
  cancelText = "Cancel",
}: ResetConfirmDialogProps) {
  const title =
    resetType === "current" ? "Reset Current File?" : "Reset All Files?";
  const description =
    resetType === "current"
      ? `Are you sure you want to reset "${fileName}" to its original state? All changes you made to this file will be lost.`
      : "Are you sure you want to reset all files to their original state? All changes you made will be lost.";

  return (
    <BaseConfirmDialog
      open={open}
      onOpenChange={onOpenChange}
      title={title}
      description={description}
      onConfirm={onConfirm}
      confirmText={confirmText}
      cancelText={cancelText}
      confirmButtonClass="bg-orange-600 text-white hover:bg-orange-700 dark:bg-orange-700 dark:hover:bg-orange-800"
      icon={
        <div className="w-12 h-12 rounded-full bg-orange-100 dark:bg-orange-900/20 flex items-center justify-center">
          <RotateCcw className="w-6 h-6 text-orange-600 dark:text-orange-400" />
        </div>
      }
    />
  );
}
