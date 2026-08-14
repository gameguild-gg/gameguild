/**
 * ExcalidrawComponent — rendered by `ExcalidrawNode.decorate()`.
 *
 * Click-to-select via Lexical `CLICK_COMMAND`, double-click to re-open
 * the editor modal. Resizer/caption from the playground is intentionally
 * omitted to keep this surface lean.
 */
"use client";

import * as React from "react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import { useLexicalEditable } from "@lexical/react/useLexicalEditable";
import { useLexicalNodeSelection } from "@lexical/react/useLexicalNodeSelection";
import { mergeRegister } from "@lexical/utils";
import {
  $getNodeByKey,
  CLICK_COMMAND,
  COMMAND_PRIORITY_LOW,
  isDOMNode,
  type NodeKey,
} from "lexical";
import { Pencil, Trash2 } from "lucide-react";
import { cn } from "@game-guild/ui/lib/utils";
import type { AppState, BinaryFiles } from "@excalidraw/excalidraw/types";
import { DeleteConfirmDialog } from "../../shared/ui/dialogs/delete-confirm-dialog";
import { useDarkMode } from "../../shared/ui/use-dark-mode";
import { useNodeDeleteProtection } from "../../shared/lexical/node-delete-protection";
import ExcalidrawImage from "./excalidraw-image";
import ExcalidrawModal, {
  type ExcalidrawInitialElements,
} from "./excalidraw-modal";
import { $isExcalidrawNode } from "./excalidraw-node";

export default function ExcalidrawComponent({
  nodeKey,
  data,
  width,
  height,
}: {
  nodeKey: NodeKey;
  data: string;
  width: "inherit" | number;
  height: "inherit" | number;
}): React.JSX.Element {
  const [editor] = useLexicalComposerContext();
  const isEditable = useLexicalEditable();
  const [isModalOpen, setModalOpen] = useState<boolean>(
    data === "[]" && editor.isEditable(),
  );
  const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false);
  const imageContainerRef = useRef<HTMLDivElement | null>(null);
  const buttonRef = useRef<HTMLButtonElement | null>(null);
  const [isSelected, setSelected, clearSelection] =
    useLexicalNodeSelection(nodeKey);

  // Proteção contra delete acidental (Backspace/Delete) — abre dialog
  // de confirmação em vez de remover silenciosamente.
  useNodeDeleteProtection({
    nodeKey,
    enabled: isEditable,
    onRequestDelete: () => setConfirmDeleteOpen(true),
  });

  useEffect(() => {
    if (!isEditable) {
      if (isSelected) clearSelection();
      return;
    }
    return mergeRegister(
      editor.registerCommand(
        CLICK_COMMAND,
        (event: MouseEvent) => {
          const buttonElem = buttonRef.current;
          const eventTarget = event.target;
          if (
            buttonElem !== null &&
            isDOMNode(eventTarget) &&
            buttonElem.contains(eventTarget)
          ) {
            if (!event.shiftKey) clearSelection();
            setSelected(!isSelected);
            if (event.detail > 1) setModalOpen(true);
            return true;
          }
          return false;
        },
        COMMAND_PRIORITY_LOW,
      ),
    );
  }, [clearSelection, editor, isSelected, setSelected, isEditable]);

  const deleteNode = useCallback(() => {
    setModalOpen(false);
    setConfirmDeleteOpen(false);
    editor.update(() => {
      const node = $getNodeByKey(nodeKey);
      if (node) node.remove();
    });
  }, [editor, nodeKey]);

  const setData = (
    els: ExcalidrawInitialElements,
    aps: Partial<AppState>,
    fls: BinaryFiles,
  ) => {
    editor.update(() => {
      const node = $getNodeByKey(nodeKey);
      if ($isExcalidrawNode(node)) {
        if ((els && els.length > 0) || Object.keys(fls).length > 0) {
          node.setData(
            JSON.stringify({ appState: aps, elements: els, files: fls }),
          );
        } else {
          node.remove();
        }
      }
    });
  };

  const {
    elements = [],
    files = {},
    appState = {},
  } = useMemo(() => {
    try {
      return JSON.parse(data);
    } catch {
      return { elements: [], files: {}, appState: {} };
    }
  }, [data]);

  // Mantém a pré-visualização SVG do desenho coerente com o tema
  // atual da página: sobrescreve `appState.theme` / `exportWithDarkMode`
  // sem alterar o JSON salvo no nó.
  const isDarkMode = useDarkMode();
  const themedAppState = useMemo<AppState>(() => {
    return {
      ...(appState as AppState),
      theme: isDarkMode ? "dark" : "light",
      exportWithDarkMode: isDarkMode,
    } as AppState;
  }, [appState, isDarkMode]);

  const closeModal = useCallback(() => {
    setModalOpen(false);
    if (elements.length === 0) {
      editor.update(() => {
        const node = $getNodeByKey(nodeKey);
        if (node) node.remove();
      });
    }
  }, [editor, nodeKey, elements.length]);

  return (
    <>
      {isEditable && isModalOpen && (
        <ExcalidrawModal
          initialElements={elements}
          initialFiles={files}
          initialAppState={appState as AppState}
          isShown={isModalOpen}
          onDelete={deleteNode}
          onClose={closeModal}
          onSave={(els, aps, fls) => {
            setData(els, aps, fls);
            setModalOpen(false);
          }}
        />
      )}
      {elements.length > 0 && (
        <span className="relative inline-block">
          {isSelected && isEditable && (
            <ExcalidrawMiniToolbar
              onEdit={() => setModalOpen(true)}
              onDelete={() => setConfirmDeleteOpen(true)}
            />
          )}
          <button
            ref={buttonRef}
            type="button"
            onClick={(e) => {
              if (!isEditable) return;
              e.preventDefault();
              e.stopPropagation();
              if (!e.shiftKey) clearSelection();
              setSelected(!isSelected);
            }}
            onDoubleClick={(e) => {
              if (!isEditable) return;
              e.preventDefault();
              e.stopPropagation();
              setModalOpen(true);
            }}
            className={cn(
              "inline-block align-baseline border-2 rounded cursor-pointer",
              isSelected
                ? "border-blue-500"
                : "border-transparent hover:border-gray-300 dark:hover:border-gray-700",
            )}
          >
            <ExcalidrawImage
              imageContainerRef={imageContainerRef}
              elements={elements}
              files={files}
              appState={themedAppState}
              width={width}
              height={height}
            />
          </button>
        </span>
      )}
      <DeleteConfirmDialog
        open={confirmDeleteOpen}
        onOpenChange={setConfirmDeleteOpen}
        title="Remove drawing?"
        itemName="this drawing"
        itemType="drawing"
        onConfirm={deleteNode}
        confirmText="Remove"
      />
    </>
  );
}

function ExcalidrawMiniToolbar({
  onEdit,
  onDelete,
}: {
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <div
      className="absolute -top-10 left-1/2 -translate-x-1/2 z-30 flex items-center gap-1 rounded-md border border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 shadow-md px-2 py-1 text-xs whitespace-nowrap"
      onMouseDown={(e) => e.stopPropagation()}
      onClick={(e) => e.stopPropagation()}
    >
      <button
        type="button"
        onClick={onEdit}
        className="h-6 px-2 inline-flex items-center gap-1 rounded text-xs hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-700 dark:text-gray-300"
        aria-label="Edit drawing"
        title="Edit drawing"
      >
        <Pencil className="w-3.5 h-3.5" /> Edit
      </button>
      <span className="mx-1 h-4 w-px bg-gray-300 dark:bg-gray-700" />
      <button
        type="button"
        onClick={onDelete}
        className="h-6 w-6 inline-flex items-center justify-center rounded hover:bg-red-50 dark:hover:bg-red-950 text-red-600"
        aria-label="Delete drawing"
        title="Delete drawing"
      >
        <Trash2 className="w-3.5 h-3.5" />
      </button>
    </div>
  );
}
