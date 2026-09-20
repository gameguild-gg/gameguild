"use client";

import type { LexicalEditor } from "lexical";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@game-guild/ui/components/dialog";
import type { InsertionDialogDefinition } from "./insertion-types";

export function InsertionDialog({
  definition,
  activeEditor,
  onClose,
}: {
  definition: InsertionDialogDefinition | null;
  activeEditor: LexicalEditor;
  onClose: () => void;
}) {
  return (
    <Dialog
      open={definition !== null}
      onOpenChange={(open) => !open && onClose()}
    >
      <DialogContent className={definition?.contentClassName}>
        <DialogHeader>
          <DialogTitle>{definition?.title}</DialogTitle>
        </DialogHeader>
        {definition?.render({ activeEditor, onClose })}
      </DialogContent>
    </Dialog>
  );
}
