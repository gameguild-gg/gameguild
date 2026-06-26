/**
 * TableCellResizerPlugin — drag the right edge of a `<th>`/`<td>` to
 * resize the column, or the bottom edge to resize the row height.
 *
 * Minimal port of facebook/lexical playground `TableCellResizer`:
 * we look at the currently-focused cell, render two thin handle bars
 * (right + bottom), and on `mousedown` capture mousemove deltas. On
 * release we commit the new width via `TableCellNode.setWidth()` for
 * every cell in the dragged column (and rows via inline height for the
 * dragged row).
 */
"use client"

import * as React from "react"
import { useCallback, useEffect, useRef, useState } from "react"
import { createPortal } from "react-dom"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import {
  $getTableCellNodeFromLexicalNode,
  $getTableNodeFromLexicalNodeOrThrow,
  $isTableCellNode,
  TableCellNode,
} from "@lexical/table"
import { $getNearestNodeFromDOMNode } from "lexical"
import { cn } from "@/lib/utils"

const MIN_WIDTH = 60
const MIN_HEIGHT = 30

/** Maximum table width = the editor content width (parent of the
 *  scrollable wrapper that Lexical inserts around every table). We use
 *  `clientWidth` so overflow doesn't inflate the measurement. */
function getMaxTableWidth(tableEl: HTMLTableElement): number {
  // Walk up until we find an element whose clientWidth is not affected
  // by the table's own overflow (i.e. an actual layout container).
  let el: HTMLElement | null = tableEl.parentElement
  while (el) {
    const cs = window.getComputedStyle(el)
    if (cs.overflowX !== "visible" || el.getAttribute("contenteditable") === "true") {
      return el.clientWidth
    }
    if (el.clientWidth > 0 && cs.display !== "inline") {
      // Fallback: first sized block ancestor.
      return el.clientWidth
    }
    el = el.parentElement
  }
  return tableEl.getBoundingClientRect().width
}

type DragKind = "col" | "row"
type DragState = {
  kind: DragKind
  startX: number
  startY: number
  startWidth: number
  startHeight: number
  cellEl: HTMLTableCellElement
}

export function TableCellResizerPlugin({
  anchorElem,
}: {
  anchorElem: HTMLElement
}): React.ReactNode {
  const [editor] = useLexicalComposerContext()
  const [hoverCell, setHoverCell] = useState<HTMLTableCellElement | null>(null)
  const [pos, setPos] = useState<{
    top: number
    left: number
    width: number
    height: number
  } | null>(null)
  const dragRef = useRef<DragState | null>(null)
  const [dragging, setDragging] = useState<DragKind | null>(null)
  const [previewDelta, setPreviewDelta] = useState(0)

  // Track hovered table cell.
  useEffect(() => {
    const rootEl = editor.getRootElement()
    if (!rootEl) return

    const onMove = (e: MouseEvent) => {
      if (dragRef.current) return
      const target = e.target as HTMLElement | null
      const cell = target?.closest("td, th") as HTMLTableCellElement | null
      setHoverCell(cell)
    }
    rootEl.addEventListener("mousemove", onMove)
    return () => rootEl.removeEventListener("mousemove", onMove)
  }, [editor])

  // Recompute handle position when hovered cell changes.
  useEffect(() => {
    if (!hoverCell) {
      if (!dragRef.current) setPos(null)
      return
    }
    const compute = () => {
      const cellRect = hoverCell.getBoundingClientRect()
      const anchorRect = anchorElem.getBoundingClientRect()
      setPos({
        top: cellRect.top - anchorRect.top,
        left: cellRect.left - anchorRect.left,
        width: cellRect.width,
        height: cellRect.height,
      })
    }
    compute()
    const ro = new ResizeObserver(compute)
    ro.observe(hoverCell)
    window.addEventListener("scroll", compute, true)
    window.addEventListener("resize", compute)
    return () => {
      ro.disconnect()
      window.removeEventListener("scroll", compute, true)
      window.removeEventListener("resize", compute)
    }
  }, [hoverCell, anchorElem])

  const commitColumn = useCallback(
    (cellEl: HTMLTableCellElement, newWidth: number) => {
      const tableEl = cellEl.closest("table") as HTMLTableElement | null
      if (!tableEl) return
      const firstRow = tableEl.querySelector("tr")
      if (!firstRow) return
      const headerCells = Array.from(
        firstRow.querySelectorAll("th, td"),
      ) as HTMLTableCellElement[]
      const widths = headerCells.map((c) => c.getBoundingClientRect().width)
      const maxTableWidth = getMaxTableWidth(tableEl)
      const rowEl = cellEl.parentElement as HTMLTableRowElement | null
      if (!rowEl) return
      const draggedIndex = Array.from(rowEl.children).indexOf(cellEl)
      if (draggedIndex < 0 || draggedIndex >= widths.length) return

      const colCount = widths.length
      const otherCount = colCount - 1
      // The total budget is hard-capped at maxTableWidth (container).
      const budget = maxTableWidth
      // Dragged column constraints.
      const minDragged = MIN_WIDTH
      const maxDragged = Math.max(MIN_WIDTH, budget - otherCount * MIN_WIDTH)
      const clampedNew = Math.min(Math.max(minDragged, newWidth), maxDragged)
      // Remaining for the other columns.
      const remaining = budget - clampedNew
      // Iterative redistribution: start proportional to previous widths,
      // bump anyone below MIN_WIDTH up to MIN_WIDTH, then redistribute
      // the deficit from the columns that are above MIN_WIDTH.
      const otherIdx = widths
        .map((_, i) => i)
        .filter((i) => i !== draggedIndex)
      const prevOthersTotal =
        otherIdx.reduce((sum, i) => sum + (widths[i] ?? 0), 0) || 1
      const next: number[] = widths.slice()
      next[draggedIndex] = clampedNew
      for (const i of otherIdx) {
        next[i] = ((widths[i] ?? 0) / prevOthersTotal) * remaining
      }
      // Floor pass.
      for (let pass = 0; pass < 5; pass++) {
        let belowFloorDeficit = 0
        const aboveFloor: number[] = []
        for (const i of otherIdx) {
          const v = next[i] ?? 0
          if (v < MIN_WIDTH) {
            belowFloorDeficit += MIN_WIDTH - v
            next[i] = MIN_WIDTH
          } else if (v > MIN_WIDTH) {
            aboveFloor.push(i)
          }
        }
        if (belowFloorDeficit <= 0 || aboveFloor.length === 0) break
        const aboveTotal =
          aboveFloor.reduce((sum, i) => sum + ((next[i] ?? 0) - MIN_WIDTH), 0) || 1
        for (const i of aboveFloor) {
          const slack = (next[i] ?? 0) - MIN_WIDTH
          const take = (slack / aboveTotal) * belowFloorDeficit
          next[i] = Math.max(MIN_WIDTH, (next[i] ?? 0) - take)
        }
      }

      editor.update(() => {
        const cellNode = $getNearestNodeFromDOMNode(cellEl)
        if (!cellNode) return
        const target = $isTableCellNode(cellNode)
          ? cellNode
          : $getTableCellNodeFromLexicalNode(cellNode)
        if (!target || !$isTableCellNode(target)) return
        const tableNode = $getTableNodeFromLexicalNodeOrThrow(target)
        for (const row of tableNode.getChildren()) {
          const rowChildren = (row as unknown as {
            getChildren: () => TableCellNode[]
          }).getChildren()
          for (let i = 0; i < next.length; i++) {
            const candidate = rowChildren[i]
            const w = next[i]
            if (candidate && $isTableCellNode(candidate) && typeof w === "number") {
              candidate.setWidth(Math.floor(w))
            }
          }
        }
      })
    },
    [editor],
  )

  const commitRow = useCallback((cellEl: HTMLTableCellElement, newHeight: number) => {
    // Row height isn't part of Lexical's table model; set inline on the <tr>
    // so it survives within the DOM render. (Refresh on update would reset
    // it, so this is a visual-only convenience for now.)
    const rowEl = cellEl.parentElement as HTMLTableRowElement | null
    if (rowEl) rowEl.style.height = `${newHeight}px`
  }, [])

  // Global mousemove/mouseup while dragging.
  useEffect(() => {
    if (!dragging) return
    const onMove = (e: MouseEvent) => {
      const state = dragRef.current
      if (!state) return
      if (state.kind === "col") {
        // Clamp delta so column doesn't shrink below MIN_WIDTH or push
        // the table past its container width.
        const tableEl = state.cellEl.closest("table") as HTMLTableElement | null
        const rowEl = state.cellEl.parentElement as HTMLTableRowElement | null
        let maxDelta = Number.POSITIVE_INFINITY
        const minDelta = MIN_WIDTH - state.startWidth
        if (tableEl && rowEl) {
          const tableWidth = getMaxTableWidth(tableEl)
          const cells = Array.from(rowEl.children) as HTMLTableCellElement[]
          const otherCount = cells.length - 1
          maxDelta = tableWidth - state.startWidth - otherCount * MIN_WIDTH
        }
        const rawDelta = e.clientX - state.startX
        setPreviewDelta(Math.max(minDelta, Math.min(maxDelta, rawDelta)))
      } else {
        const rawDelta = e.clientY - state.startY
        const minDelta = MIN_HEIGHT - state.startHeight
        setPreviewDelta(Math.max(minDelta, rawDelta))
      }
    }
    const onUp = () => {
      const state = dragRef.current
      if (state) {
        if (state.kind === "col") {
          const newWidth = state.startWidth + previewDelta
          commitColumn(state.cellEl, newWidth)
        } else {
          const newHeight = Math.max(MIN_HEIGHT, state.startHeight + previewDelta)
          commitRow(state.cellEl, newHeight)
        }
      }
      dragRef.current = null
      setDragging(null)
      setPreviewDelta(0)
    }
    window.addEventListener("mousemove", onMove)
    window.addEventListener("mouseup", onUp)
    return () => {
      window.removeEventListener("mousemove", onMove)
      window.removeEventListener("mouseup", onUp)
    }
  }, [dragging, previewDelta, commitColumn, commitRow])

  const startDrag = useCallback(
    (kind: DragKind) => (e: React.MouseEvent) => {
      if (!hoverCell) return
      e.preventDefault()
      e.stopPropagation()
      const rect = hoverCell.getBoundingClientRect()
      dragRef.current = {
        kind,
        startX: e.clientX,
        startY: e.clientY,
        startWidth: rect.width,
        startHeight: rect.height,
        cellEl: hoverCell,
      }
      setDragging(kind)
    },
    [hoverCell],
  )

  if (!pos) return null

  const colDelta = dragging === "col" ? previewDelta : 0
  const rowDelta = dragging === "row" ? previewDelta : 0

  return createPortal(
    <>
      {/* Right edge — column resizer */}
      <div
        onMouseDown={startDrag("col")}
        className={cn(
          "absolute z-30 cursor-col-resize",
          dragging === "col" ? "bg-blue-500" : "bg-transparent hover:bg-blue-400/60",
        )}
        style={{
          top: pos.top,
          left: pos.left + pos.width + colDelta - 2,
          width: 4,
          height: pos.height,
        }}
      />
      {/* Bottom edge — row resizer */}
      <div
        onMouseDown={startDrag("row")}
        className={cn(
          "absolute z-30 cursor-row-resize",
          dragging === "row" ? "bg-blue-500" : "bg-transparent hover:bg-blue-400/60",
        )}
        style={{
          top: pos.top + pos.height + rowDelta - 2,
          left: pos.left,
          width: pos.width,
          height: 4,
        }}
      />
    </>,
    anchorElem,
  )
}
