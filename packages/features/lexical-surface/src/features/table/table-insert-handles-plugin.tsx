/**
 * TableInsertHandlesPlugin — hover affordances for the table:
 *
 *  - "+" button at the top-right corner of the hovered cell:
 *    inserts a new column to the right of that cell.
 *  - "+" button at the bottom-left corner of the hovered cell:
 *    inserts a new row below that cell.
 *  - Grip in the middle of the top edge: drag to reorder the column.
 *
 * Column reorder moves the underlying `TableCellNode`s within each
 * row of the Lexical tree, so the change persists via serialization.
 */
"use client";

import * as React from "react";
import { useCallback, useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import {
  $getTableColumnIndexFromTableCellNode,
  $getTableNodeFromLexicalNodeOrThrow,
  $insertTableColumnAtSelection,
  $insertTableRowAtSelection,
  $isTableCellNode,
  $isTableRowNode,
} from "@lexical/table";
import { $getNearestNodeFromDOMNode } from "lexical";
import { GripHorizontal, Plus } from "lucide-react";
import { cn } from "@game-guild/ui/lib/utils";

type CellHover = {
  el: HTMLTableCellElement;
  // "+" affordances (corners).
  plusCol: { top: number; left: number };
  plusRow: { top: number; left: number };
  // Grip affordance (mid-top edge — column move).
  gripCol: { top: number; left: number; width: number };
};

type DragState = { kind: "col"; sourceEl: HTMLTableCellElement } | null;

export function TableInsertHandlesPlugin({
  anchorElem,
}: {
  anchorElem: HTMLElement;
}): React.ReactNode {
  const [editor] = useLexicalComposerContext();
  const [hover, setHover] = useState<CellHover | null>(null);
  const [drag, setDrag] = useState<DragState>(null);
  const dragRef = useRef<DragState>(null);
  dragRef.current = drag;

  // ---- hover tracking -----------------------------------------------------
  useEffect(() => {
    const rootEl = editor.getRootElement();
    if (!rootEl) return;
    let raf = 0;
    const HANDLE_MARGIN = 18;

    const onMove = (e: MouseEvent) => {
      cancelAnimationFrame(raf);
      raf = requestAnimationFrame(() => {
        const target = e.target as HTMLElement | null;
        if (target?.closest("[data-table-handle]")) return;

        let cell = target?.closest("td, th") as HTMLTableCellElement | null;
        if (!cell && hover) {
          const r = hover.el.getBoundingClientRect();
          if (
            e.clientX >= r.left - HANDLE_MARGIN &&
            e.clientX <= r.right + HANDLE_MARGIN &&
            e.clientY >= r.top - HANDLE_MARGIN &&
            e.clientY <= r.bottom + HANDLE_MARGIN
          ) {
            cell = hover.el;
          }
        }
        if (!cell || !rootEl.contains(cell)) {
          setHover(null);
          return;
        }
        const cellRect = cell.getBoundingClientRect();
        const anchorRect = anchorElem.getBoundingClientRect();
        setHover({
          el: cell,
          plusCol: {
            top: cellRect.top - anchorRect.top - 12,
            left: cellRect.right - anchorRect.left - 10,
          },
          plusRow: {
            top: cellRect.bottom - anchorRect.top - 10,
            left: cellRect.left - anchorRect.left - 12,
          },
          gripCol: {
            top: cellRect.top - anchorRect.top - 10,
            left: cellRect.left - anchorRect.left + cellRect.width / 2 - 12,
            width: 24,
          },
        });
      });
    };
    window.addEventListener("mousemove", onMove);
    return () => {
      cancelAnimationFrame(raf);
      window.removeEventListener("mousemove", onMove);
    };
  }, [editor, anchorElem, hover]);

  // ---- insert column / row -----------------------------------------------
  const insertColumn = useCallback(
    (cellEl: HTMLTableCellElement) => {
      editor.update(() => {
        const node = $getNearestNodeFromDOMNode(cellEl);
        if (!node) return;
        const cellNode = $isTableCellNode(node) ? node : null;
        if (!cellNode) return;
        const sourceWidth = cellNode.getWidth() ?? cellEl.offsetWidth ?? 120;
        cellNode.selectStart();
        const newCell = $insertTableColumnAtSelection(true);
        if (!newCell) return;
        // `table-fixed` uses the first row's cell widths. Split the
        // source column's width between source and new column so the
        // new column is visible without overflowing the table.
        const half = Math.max(60, Math.floor(sourceWidth / 2));
        const tableNode = $getTableNodeFromLexicalNodeOrThrow(newCell);
        const newColIdx = $getTableColumnIndexFromTableCellNode(newCell);
        const srcColIdx = $getTableColumnIndexFromTableCellNode(cellNode);
        tableNode.getChildren().forEach((row) => {
          if (!$isTableRowNode(row)) return;
          const rowCells = row.getChildren();
          const nc = rowCells[newColIdx];
          const sc = rowCells[srcColIdx];
          if (nc && $isTableCellNode(nc)) nc.setWidth(half);
          if (sc && $isTableCellNode(sc)) sc.setWidth(half);
        });
      });
    },
    [editor],
  );

  const insertRow = useCallback(
    (cellEl: HTMLTableCellElement) => {
      editor.update(() => {
        const node = $getNearestNodeFromDOMNode(cellEl);
        if (!node) return;
        const cellNode = $isTableCellNode(node) ? node : null;
        if (!cellNode) return;
        cellNode.selectStart();
        $insertTableRowAtSelection(true);
      });
    },
    [editor],
  );

  // ---- reorder column / row via drag --------------------------------------
  const moveColumn = useCallback(
    (sourceEl: HTMLTableCellElement, targetEl: HTMLTableCellElement) => {
      editor.update(() => {
        const sNode = $getNearestNodeFromDOMNode(sourceEl);
        const tNode = $getNearestNodeFromDOMNode(targetEl);
        if (!sNode || !tNode) return;
        const sCell = $isTableCellNode(sNode) ? sNode : null;
        const tCell = $isTableCellNode(tNode) ? tNode : null;
        if (!sCell || !tCell) return;
        const sIdx = $getTableColumnIndexFromTableCellNode(sCell);
        const tIdx = $getTableColumnIndexFromTableCellNode(tCell);
        if (sIdx === tIdx) return;
        const tableNode = $getTableNodeFromLexicalNodeOrThrow(sCell);
        const after = tIdx > sIdx;
        tableNode.getChildren().forEach((row) => {
          if (!$isTableRowNode(row)) return;
          const cells = row.getChildren();
          const s = cells[sIdx];
          const t = cells[tIdx];
          if (!s || !t) return;
          if (after) t.insertAfter(s);
          else t.insertBefore(s);
        });
      });
    },
    [editor],
  );

  // ---- drag lifecycle -----------------------------------------------------
  useEffect(() => {
    if (!drag) return;
    const onUp = (e: PointerEvent) => {
      const el = document.elementFromPoint(
        e.clientX,
        e.clientY,
      ) as HTMLElement | null;
      const cell = el?.closest("td, th") as HTMLTableCellElement | null;
      const cur = dragRef.current;
      if (cell && cur) {
        moveColumn(cur.sourceEl, cell);
      }
      setDrag(null);
    };
    window.addEventListener("pointerup", onUp);
    return () => window.removeEventListener("pointerup", onUp);
  }, [drag, moveColumn]);

  if (!hover) return null;

  const plusClass = cn(
    "absolute z-30 inline-flex items-center justify-center",
    "h-5 w-5 rounded-full bg-blue-600 text-white shadow",
    "hover:bg-blue-700 cursor-pointer",
  );
  const gripClass = cn(
    "absolute z-30 inline-flex items-center justify-center",
    "rounded bg-gray-400/90 text-white shadow",
    "hover:bg-gray-600 cursor-grab active:cursor-grabbing",
  );

  return createPortal(
    <>
      {/* "+" insert column (top-right corner) */}
      <button
        type="button"
        aria-label="Insert column right"
        data-table-handle="plus-col"
        className={plusClass}
        style={{ top: hover.plusCol.top, left: hover.plusCol.left }}
        onMouseDown={(e) => e.preventDefault()}
        onClick={(e) => {
          e.preventDefault();
          e.stopPropagation();
          insertColumn(hover.el);
        }}
      >
        <Plus className="w-3 h-3" />
      </button>

      {/* "+" insert row (bottom-left corner) */}
      <button
        type="button"
        aria-label="Insert row below"
        data-table-handle="plus-row"
        className={plusClass}
        style={{ top: hover.plusRow.top, left: hover.plusRow.left }}
        onMouseDown={(e) => e.preventDefault()}
        onClick={(e) => {
          e.preventDefault();
          e.stopPropagation();
          insertRow(hover.el);
        }}
      >
        <Plus className="w-3 h-3" />
      </button>

      {/* Grip — drag to move column (top-middle of cell) */}
      <button
        type="button"
        aria-label="Move column"
        data-table-handle="grip-col"
        className={gripClass}
        style={{
          top: hover.gripCol.top,
          left: hover.gripCol.left,
          width: hover.gripCol.width,
          height: 12,
        }}
        onPointerDown={(e) => {
          e.preventDefault();
          e.stopPropagation();
          setDrag({ kind: "col", sourceEl: hover.el });
        }}
      >
        <GripHorizontal className="w-3 h-3" />
      </button>
    </>,
    anchorElem,
  );
}
