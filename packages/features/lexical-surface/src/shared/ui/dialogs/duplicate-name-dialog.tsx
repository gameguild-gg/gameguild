"use client";

import { useState } from "react";
import { BaseConfirmDialog } from "./base-confirm-dialog";
import { AlertTriangle } from "lucide-react";
import { Input } from "@game-guild/ui/components/input";
import { Label } from "@game-guild/ui/components/label";

interface DuplicateNameDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  itemType: "file" | "folder";
  originalName: string;
  onConfirm: (newName: string) => void;
  onCancel?: () => void;
}

export function DuplicateNameDialog({
  open,
  onOpenChange,
  itemType,
  originalName,
  onConfirm,
  onCancel,
}: DuplicateNameDialogProps) {
  const [newName, setNewName] = useState(originalName);

  const handleConfirm = () => {
    if (newName.trim() && newName !== originalName) {
      onConfirm(newName.trim());
      onOpenChange(false);
    }
  };

  const handleCancel = () => {
    setNewName(originalName);
    onOpenChange(false);
    onCancel?.();
  };

  return (
    <BaseConfirmDialog
      open={open}
      onOpenChange={(open) => {
        if (!open) handleCancel();
      }}
      title={`${itemType === "file" ? "File" : "Folder"} Already Exists`}
      description={`A ${itemType} named "${originalName}" already exists in this location. Please choose a different name.`}
      onConfirm={handleConfirm}
      confirmText="Create with New Name"
      cancelText="Cancel"
      confirmButtonClass="bg-blue-600 text-white hover:bg-blue-700"
      icon={
        <div className="w-12 h-12 rounded-full bg-yellow-100 dark:bg-yellow-900/20 flex items-center justify-center">
          <AlertTriangle className="w-6 h-6 text-yellow-600 dark:text-yellow-400" />
        </div>
      }
    >
      <div className="space-y-2">
        <Label htmlFor="newName" className="text-sm font-medium">
          New {itemType === "file" ? "File" : "Folder"} Name
        </Label>
        <Input
          id="newName"
          value={newName}
          onChange={(e) => setNewName(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") handleConfirm();
            if (e.key === "Escape") handleCancel();
          }}
          placeholder={`Enter ${itemType} name`}
          autoFocus
          className="w-full"
        />
        {newName.trim() === originalName && (
          <p className="text-xs text-red-600 dark:text-red-400">
            Name must be different from "{originalName}"
          </p>
        )}
      </div>
    </BaseConfirmDialog>
  );
}
