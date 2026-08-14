"use client";

import type { LexicalEditor } from "lexical";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@game-guild/ui/components/dialog";
import type { InsertionDialogDefinition } from "./insertion-types";

function preserveMathKeyboardInteraction(event: Event): void {
  const target = event.target as HTMLElement | null;
  if (
    document.body.hasAttribute("data-math-keyboard-open") ||
    target?.closest(".ML__keyboard, .ML__virtual-keyboard, math-field")
  ) {
    event.preventDefault();
  }
}

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
      <DialogContent
        className={definition?.contentClassName}
        onPointerDownOutside={preserveMathKeyboardInteraction}
        onInteractOutside={preserveMathKeyboardInteraction}
        onFocusOutside={preserveMathKeyboardInteraction}
        onEscapeKeyDown={(event) => {
          if (document.body.hasAttribute("data-math-keyboard-open")) {
            event.preventDefault();
          }
        }}
      >
        <DialogHeader>
          <DialogTitle>{definition?.title}</DialogTitle>
        </DialogHeader>
        {definition?.render({ activeEditor, onClose })}
      </DialogContent>
    </Dialog>
  );
}
