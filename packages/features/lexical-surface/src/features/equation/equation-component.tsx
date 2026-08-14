/**
 * EquationComponent — decorator rendered by `EquationNode`. Shows the
 * KaTeX-rendered equation. Single click selects (with mini-toolbar for
 * font-size + alignment); double click opens the full editor dialog.
 */
"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import { useLexicalEditable } from "@lexical/react/useLexicalEditable";
import { mergeRegister } from "@lexical/utils";
import {
  $createNodeSelection,
  $getNodeByKey,
  $getSelection,
  $isNodeSelection,
  $setSelection,
  type NodeKey,
} from "lexical";
import { AlignCenter, AlignLeft, AlignRight, Trash2 } from "lucide-react";
import { cn } from "@game-guild/ui/lib/utils";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@game-guild/ui/components/dialog";
import { DeleteConfirmDialog } from "../../shared/ui/dialogs/delete-confirm-dialog";
import { useNodeDeleteProtection } from "../../shared/lexical/node-delete-protection";
import { KatexRenderer } from "./katex-renderer";
import {
  $createEquationNode,
  $isEquationNode,
  type EquationAlign,
} from "./equation-node";
import { EquationDialogBody } from "./equations-plugin";

type Props = {
  equation: string;
  inline: boolean;
  fontSize: number;
  align: EquationAlign;
  nodeKey: NodeKey;
};

export default function EquationComponent({
  equation,
  inline,
  fontSize,
  align,
  nodeKey,
}: Props) {
  const [editor] = useLexicalComposerContext();
  const isEditable = useLexicalEditable();
  const [open, setOpen] = useState(false);
  const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false);
  const [isSelected, setIsSelected] = useState(false);
  const wrapperRef = useRef<HTMLSpanElement>(null);

  // Acompanha o estado de seleção do nó.
  useEffect(() => {
    return mergeRegister(
      editor.registerUpdateListener(({ editorState }) => {
        const selected = editorState.read(() => {
          const sel = $getSelection();
          return $isNodeSelection(sel) && sel.has(nodeKey);
        });
        setIsSelected(selected);
      }),
    );
  }, [editor, nodeKey]);

  // Intercept Backspace / Delete que afetariam esta equação — abre o
  // dialog de confirmação em vez de remover silenciosamente.
  useNodeDeleteProtection({
    nodeKey,
    enabled: isEditable,
    onRequestDelete: () => setConfirmDeleteOpen(true),
  });

  const onClick = useCallback(
    (e: React.MouseEvent) => {
      if (!isEditable) return;
      e.stopPropagation();
      editor.update(() => {
        const ns = $createNodeSelection();
        ns.add(nodeKey);
        $setSelection(ns);
      });
    },
    [editor, nodeKey, isEditable],
  );

  const onSubmit = useCallback(
    ({
      equation: nextEquation,
      inline: nextInline,
    }: {
      equation: string;
      inline: boolean;
    }) => {
      editor.update(() => {
        const node = $getNodeByKey(nodeKey);
        if (!$isEquationNode(node)) return;
        if (node.__inline === nextInline) {
          node.setEquation(nextEquation);
        } else {
          // Trocar inline/block exige recriar o nó (createDOM usa o flag).
          const replacement = $createEquationNode(
            nextEquation,
            nextInline,
            node.getFontSize(),
            node.getAlign(),
          );
          node.replace(replacement);
        }
      });
    },
    [editor, nodeKey],
  );

  const updateAlign = useCallback(
    (next: EquationAlign) => {
      editor.update(() => {
        const node = $getNodeByKey(nodeKey);
        if ($isEquationNode(node)) node.setAlign(next);
      });
    },
    [editor, nodeKey],
  );

  const removeNode = useCallback(() => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey);
      if ($isEquationNode(node)) node.remove();
    });
    setConfirmDeleteOpen(false);
  }, [editor, nodeKey]);

  return (
    <span ref={wrapperRef} className="relative inline-block">
      {isSelected && isEditable && (
        <EquationMiniToolbar
          align={align}
          inline={inline}
          onAlignChange={updateAlign}
          onEdit={() => setOpen(true)}
          onDelete={() => setConfirmDeleteOpen(true)}
        />
      )}
      <KatexRenderer
        equation={equation}
        inline={inline}
        fontSize={fontSize}
        align={align}
        selected={isSelected}
        onClick={onClick}
        onDoubleClick={() => {
          if (isEditable) setOpen(true);
        }}
      />
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent
          className="sm:max-w-[720px]"
          onPointerDownOutside={(e) => {
            const target = e.target as HTMLElement | null;
            if (document.body.hasAttribute("data-math-keyboard-open"))
              e.preventDefault();
            else if (
              target?.closest(
                ".ML__keyboard, .ML__virtual-keyboard, math-field",
              )
            )
              e.preventDefault();
          }}
          onInteractOutside={(e) => {
            const target = e.target as HTMLElement | null;
            if (document.body.hasAttribute("data-math-keyboard-open"))
              e.preventDefault();
            else if (
              target?.closest(
                ".ML__keyboard, .ML__virtual-keyboard, math-field",
              )
            )
              e.preventDefault();
          }}
          onFocusOutside={(e) => {
            const target = e.target as HTMLElement | null;
            if (document.body.hasAttribute("data-math-keyboard-open"))
              e.preventDefault();
            else if (
              target?.closest(
                ".ML__keyboard, .ML__virtual-keyboard, math-field",
              )
            )
              e.preventDefault();
          }}
          onEscapeKeyDown={(e) => {
            if (document.body.hasAttribute("data-math-keyboard-open"))
              e.preventDefault();
          }}
        >
          <DialogHeader>
            <DialogTitle>Edit Equation</DialogTitle>
          </DialogHeader>
          <EquationDialogBody
            initialEquation={equation}
            initialInline={inline}
            initialFontSize={Math.max(fontSize, 2)}
            submitLabel="Save"
            onClose={() => setOpen(false)}
            onSubmit={onSubmit}
          />
        </DialogContent>
      </Dialog>
      <DeleteConfirmDialog
        open={confirmDeleteOpen}
        onOpenChange={setConfirmDeleteOpen}
        title="Remove equation?"
        itemName="this equation"
        itemType="equation"
        onConfirm={removeNode}
        confirmText="Remove"
      />
    </span>
  );
}

function EquationMiniToolbar({
  align,
  inline,
  onAlignChange,
  onEdit,
  onDelete,
}: {
  align: EquationAlign;
  inline: boolean;
  onAlignChange: (next: EquationAlign) => void;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <div
      className="absolute -top-10 left-1/2 -translate-x-1/2 z-30 flex items-center gap-1 rounded-md border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 shadow-md px-2 py-1 text-xs whitespace-nowrap"
      // Não propagar clicks para o nó (evita re-seleção / deseleção).
      onMouseDown={(e) => e.stopPropagation()}
      onClick={(e) => e.stopPropagation()}
    >
      {!inline && (
        <>
          <button
            type="button"
            onClick={() => onAlignChange("left")}
            className={cn(
              "h-6 w-6 inline-flex items-center justify-center rounded hover:bg-gray-100 dark:hover:bg-gray-800",
              align === "left" && "bg-gray-100 dark:bg-gray-800 text-blue-600",
            )}
            aria-label="Align left"
          >
            <AlignLeft className="w-3.5 h-3.5" />
          </button>
          <button
            type="button"
            onClick={() => onAlignChange("center")}
            className={cn(
              "h-6 w-6 inline-flex items-center justify-center rounded hover:bg-gray-100 dark:hover:bg-gray-800",
              align === "center" &&
                "bg-gray-100 dark:bg-gray-800 text-blue-600",
            )}
            aria-label="Align center"
          >
            <AlignCenter className="w-3.5 h-3.5" />
          </button>
          <button
            type="button"
            onClick={() => onAlignChange("right")}
            className={cn(
              "h-6 w-6 inline-flex items-center justify-center rounded hover:bg-gray-100 dark:hover:bg-gray-800",
              align === "right" && "bg-gray-100 dark:bg-gray-800 text-blue-600",
            )}
            aria-label="Align right"
          >
            <AlignRight className="w-3.5 h-3.5" />
          </button>
          <span className="mx-1 h-4 w-px bg-gray-300 dark:bg-gray-700" />
        </>
      )}
      <button
        type="button"
        onClick={onEdit}
        className="h-6 px-2 rounded text-xs hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300"
      >
        Edit
      </button>
      <span className="mx-1 h-4 w-px bg-gray-300 dark:bg-gray-700" />
      <button
        type="button"
        onClick={onDelete}
        className="h-6 w-6 inline-flex items-center justify-center rounded hover:bg-red-50 dark:hover:bg-red-950 text-red-600"
        aria-label="Delete equation"
        title="Delete equation"
      >
        <Trash2 className="w-3.5 h-3.5" />
      </button>
    </div>
  );
}
