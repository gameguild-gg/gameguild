/**
 * TableActionMenuPlugin — chevron button on the currently-focused cell.
 * Opens a dropdown with insert/delete row/column, toggle header (label
 * reflects current state), row striping, freeze row/column, vertical
 * align, background color and delete table.
 */
"use client"

import * as React from "react"
import { useCallback, useEffect, useState } from "react"
import { createPortal } from "react-dom"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import {
  $deleteTableColumnAtSelection,
  $deleteTableRowAtSelection,
  $getTableCellNodeFromLexicalNode,
  $getTableNodeFromLexicalNodeOrThrow,
  $insertTableColumnAtSelection,
  $insertTableRowAtSelection,
  $isTableCellNode,
  $isTableSelection,
  TableCellHeaderStates,
} from "@lexical/table"
import { $getNodeByKey, $getSelection, $isRangeSelection } from "lexical"
import { ChevronDown, Table as TableIcon } from "lucide-react"
import { cn } from "@/lib/utils"
import ColorPicker from "../toolbar/color-picker"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuSub,
  DropdownMenuSubContent,
  DropdownMenuSubTrigger,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"

type CellState = {
  hasRowHeader: boolean
  hasColumnHeader: boolean
  rowStriping: boolean
  frozenRows: number
  frozenColumns: number
  verticalAlign: string | undefined
  backgroundColor: string | null
}

export function TableActionMenuPlugin({
  anchorElem,
}: {
  anchorElem: HTMLElement
}): React.ReactNode {
  const [editor] = useLexicalComposerContext()
  const [cellEl, setCellEl] = useState<HTMLElement | null>(null)
  const [pos, setPos] = useState<{ top: number; left: number } | null>(null)
  const [state, setState] = useState<CellState | null>(null)
  // Keys of every TableCellNode currently selected (≥1). For a plain
  // RangeSelection inside a single cell this contains just that cell;
  // for a TableSelection it contains all highlighted cells so the menu
  // can apply per-cell actions in batch.
  const [selectedCellKeys, setSelectedCellKeys] = useState<string[]>([])
  const isBatch = selectedCellKeys.length > 1

  useEffect(() => {
    const update = () => {
      editor.getEditorState().read(() => {
        const selection = $getSelection()
        let anchorCell = null as ReturnType<typeof $getTableCellNodeFromLexicalNode> | null
        let keys: string[] = []
        if ($isTableSelection(selection)) {
          // `selection.getNodes()` walks descendants and may report the
          // same cell multiple times; dedupe by node key.
          const seen = new Set<string>()
          const cells: ReturnType<typeof $getTableCellNodeFromLexicalNode>[] = []
          for (const n of selection.getNodes()) {
            const cell = $isTableCellNode(n)
              ? n
              : $getTableCellNodeFromLexicalNode(n)
            if (!cell) continue
            const k = cell.getKey()
            if (seen.has(k)) continue
            seen.add(k)
            cells.push(cell)
          }
          if (cells.length === 0) {
            setCellEl(null)
            setState(null)
            setSelectedCellKeys([])
            return
          }
          keys = Array.from(seen)
          // Anchor menu at the last cell of the selection (bottom-right).
          anchorCell = cells[cells.length - 1] ?? null
        } else if ($isRangeSelection(selection)) {
          const cellNode = $getTableCellNodeFromLexicalNode(
            selection.anchor.getNode(),
          )
          if (!cellNode || !$isTableCellNode(cellNode)) {
            setCellEl(null)
            setState(null)
            setSelectedCellKeys([])
            return
          }
          anchorCell = cellNode
          keys = [cellNode.getKey()]
        } else {
          setCellEl(null)
          setState(null)
          setSelectedCellKeys([])
          return
        }
        if (!anchorCell) return
        const tableNode = $getTableNodeFromLexicalNodeOrThrow(anchorCell)
        const dom = editor.getElementByKey(anchorCell.getKey())
        setCellEl(dom as HTMLElement | null)
        setSelectedCellKeys(keys)
        setState({
          hasRowHeader: anchorCell.hasHeaderState(TableCellHeaderStates.ROW),
          hasColumnHeader: anchorCell.hasHeaderState(
            TableCellHeaderStates.COLUMN,
          ),
          rowStriping: tableNode.getRowStriping(),
          frozenRows: tableNode.getFrozenRows(),
          frozenColumns: tableNode.getFrozenColumns(),
          verticalAlign: anchorCell.getVerticalAlign(),
          backgroundColor: anchorCell.getBackgroundColor(),
        })
      })
    }
    update()
    return editor.registerUpdateListener(update)
  }, [editor])

  useEffect(() => {
    if (!cellEl) {
      setPos(null)
      return
    }
    const compute = () => {
      const cellRect = cellEl.getBoundingClientRect()
      const anchorRect = anchorElem.getBoundingClientRect()
      setPos({
        top: cellRect.top - anchorRect.top + 4,
        left: cellRect.right - anchorRect.left - 22,
      })
    }
    compute()
    const ro = new ResizeObserver(compute)
    ro.observe(cellEl)
    window.addEventListener("scroll", compute, true)
    window.addEventListener("resize", compute)
    return () => {
      ro.disconnect()
      window.removeEventListener("scroll", compute, true)
      window.removeEventListener("resize", compute)
    }
  }, [cellEl, anchorElem])

  const run = useCallback(
    (fn: () => void) => {
      editor.update(() => fn())
    },
    [editor],
  )

  const deleteTable = useCallback(() => {
    editor.update(() => {
      const firstKey = selectedCellKeys[0]
      if (!firstKey) return
      const cell = $getNodeByKey(firstKey)
      if (!cell) return
      const tableNode = $getTableNodeFromLexicalNodeOrThrow(cell)
      tableNode.remove()
    })
  }, [editor, selectedCellKeys])

  const forEachSelectedCell = useCallback(
    (fn: (cell: ReturnType<typeof $getTableCellNodeFromLexicalNode>) => void) => {
      editor.update(() => {
        for (const key of selectedCellKeys) {
          const node = $getNodeByKey(key)
          if (node && $isTableCellNode(node)) fn(node)
        }
      })
    },
    [editor, selectedCellKeys],
  )

  const toggleRowHeader = useCallback(() => {
    forEachSelectedCell((cell) => {
      if (cell) cell.toggleHeaderStyle(TableCellHeaderStates.ROW)
    })
  }, [forEachSelectedCell])

  const toggleColumnHeader = useCallback(() => {
    forEachSelectedCell((cell) => {
      if (cell) cell.toggleHeaderStyle(TableCellHeaderStates.COLUMN)
    })
  }, [forEachSelectedCell])

  const toggleRowStriping = useCallback(() => {
    editor.update(() => {
      const firstKey = selectedCellKeys[0]
      if (!firstKey) return
      const cell = $getNodeByKey(firstKey)
      if (!cell) return
      const tableNode = $getTableNodeFromLexicalNodeOrThrow(cell)
      tableNode.setRowStriping(!tableNode.getRowStriping())
    })
  }, [editor, selectedCellKeys])

  const toggleFirstRowFreeze = useCallback(() => {
    editor.update(() => {
      const firstKey = selectedCellKeys[0]
      if (!firstKey) return
      const cell = $getNodeByKey(firstKey)
      if (!cell) return
      const tableNode = $getTableNodeFromLexicalNodeOrThrow(cell)
      tableNode.setFrozenRows(tableNode.getFrozenRows() === 0 ? 1 : 0)
    })
  }, [editor, selectedCellKeys])

  const toggleFirstColumnFreeze = useCallback(() => {
    editor.update(() => {
      const firstKey = selectedCellKeys[0]
      if (!firstKey) return
      const cell = $getNodeByKey(firstKey)
      if (!cell) return
      const tableNode = $getTableNodeFromLexicalNodeOrThrow(cell)
      tableNode.setFrozenColumns(tableNode.getFrozenColumns() === 0 ? 1 : 0)
    })
  }, [editor, selectedCellKeys])

  const setVerticalAlign = useCallback(
    (align: "top" | "middle" | "bottom") => {
      forEachSelectedCell((cell) => {
        if (cell) cell.setVerticalAlign(align)
      })
    },
    [forEachSelectedCell],
  )

  const setBackgroundColor = useCallback(
    (color: string | null) => {
      forEachSelectedCell((cell) => {
        if (cell) cell.setBackgroundColor(color)
      })
    },
    [forEachSelectedCell],
  )

  if (!cellEl || !pos || !state) return null

  return createPortal(
    <div
      className="absolute z-40"
      style={{ top: pos.top, left: pos.left }}
      onMouseDown={(e) => e.preventDefault()}
    >
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <button
            type="button"
            aria-label="Table actions"
            className={cn(
              "inline-flex items-center justify-center gap-0.5 rounded",
              "border shadow-sm",
              isBatch
                ? "h-6 px-1.5 bg-blue-600 text-white border-blue-700 hover:bg-blue-700"
                : "h-5 w-5 bg-white/90 dark:bg-gray-800/90 border-gray-300 dark:border-gray-700 text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700",
            )}
          >
            {isBatch ? (
              <>
                <TableIcon className="w-3.5 h-3.5" />
                <span className="text-[10px] font-semibold leading-none">
                  {selectedCellKeys.length}
                </span>
                <ChevronDown className="w-3 h-3" />
              </>
            ) : (
              <ChevronDown className="w-3.5 h-3.5" />
            )}
          </button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-56">
          <DropdownMenuSub>
            <DropdownMenuSubTrigger>Background color</DropdownMenuSubTrigger>
            <DropdownMenuSubContent className="p-3">
              <ColorPicker
                color={state.backgroundColor ?? "#ffffff"}
                onChange={(next) => setBackgroundColor(next)}
              />
              <DropdownMenuSeparator />
              <DropdownMenuItem onSelect={() => setBackgroundColor(null)}>
                Clear background
              </DropdownMenuItem>
            </DropdownMenuSubContent>
          </DropdownMenuSub>
          <DropdownMenuItem onSelect={toggleRowStriping}>
            {state.rowStriping ? "Remove row striping" : "Toggle row striping"}
          </DropdownMenuItem>
          <DropdownMenuSub>
            <DropdownMenuSubTrigger>Vertical align</DropdownMenuSubTrigger>
            <DropdownMenuSubContent>
              {(["top", "middle", "bottom"] as const).map((a) => (
                <DropdownMenuItem key={a} onSelect={() => setVerticalAlign(a)}>
                  {a.charAt(0).toUpperCase() + a.slice(1)}
                  {state.verticalAlign === a && (
                    <span className="ml-auto text-blue-600">✓</span>
                  )}
                </DropdownMenuItem>
              ))}
            </DropdownMenuSubContent>
          </DropdownMenuSub>
          <DropdownMenuItem onSelect={toggleFirstRowFreeze}>
            {state.frozenRows > 0
              ? "Unfreeze first row"
              : "Toggle first row freeze"}
          </DropdownMenuItem>
          <DropdownMenuItem onSelect={toggleFirstColumnFreeze}>
            {state.frozenColumns > 0
              ? "Unfreeze first column"
              : "Toggle first column freeze"}
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuItem onSelect={() => run(() => $insertTableRowAtSelection(false))}>
            Insert row above
          </DropdownMenuItem>
          <DropdownMenuItem onSelect={() => run(() => $insertTableRowAtSelection(true))}>
            Insert row below
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuItem onSelect={() => run(() => $insertTableColumnAtSelection(false))}>
            Insert column left
          </DropdownMenuItem>
          <DropdownMenuItem onSelect={() => run(() => $insertTableColumnAtSelection(true))}>
            Insert column right
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuItem onSelect={() => run(() => $deleteTableColumnAtSelection())}>
            Delete column
          </DropdownMenuItem>
          <DropdownMenuItem onSelect={() => run(() => $deleteTableRowAtSelection())}>
            Delete row
          </DropdownMenuItem>
          <DropdownMenuItem onSelect={deleteTable} className="text-red-600 focus:text-red-600">
            Delete table
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuItem onSelect={toggleRowHeader}>
            {state.hasRowHeader ? "Remove row header" : "Add row header"}
          </DropdownMenuItem>
          <DropdownMenuItem onSelect={toggleColumnHeader}>
            {state.hasColumnHeader ? "Remove column header" : "Add column header"}
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </div>,
    anchorElem,
  )
}
