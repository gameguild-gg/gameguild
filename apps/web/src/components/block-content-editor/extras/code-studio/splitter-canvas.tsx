"use client"

import { Panel, PanelGroup, PanelResizeHandle } from "react-resizable-panels"
import {
  createContext,
  Fragment,
  useCallback,
  useContext,
  useState,
  type DragEvent,
  type ReactNode,
} from "react"
import { Trash2, GripVertical, X } from "lucide-react"
import type { LayoutNode, LeafPanel, PanelType } from "./types"
import { isLeaf } from "./tree-operations"
import { cn } from "@/lib/utils"

const DRAG_MIME = "application/x-code-studio-panel"

type DockPosition = "top" | "right" | "bottom" | "left"

interface SplitterCanvasProps {
  root: LayoutNode
  renderLeaf: (leaf: LeafPanel) => ReactNode
  /**
   * Called when the user drags a divider. Receives the split node id and the
   * resulting percent sizes (length === children count). Optional — when
   * omitted the canvas is fully read-only (no handles).
   */
  onSplitResize?: (splitId: string, sizes: number[]) => void
  /** Called when a leaf is dropped onto one of another leaf's quadrants. */
  onMovePanel?: (sourcePanelId: string, targetPanelId: string, position: DockPosition) => void
  /** Called when a leaf is dropped onto the trash zone. */
  onRemovePanel?: (panelId: string) => void
  /** When false, the layout-edit chrome (leaf headers, drop quadrants, trash)
   *  is hidden and panels are not draggable. Independent from `resizable`. */
  editable?: boolean
  /** When false, divider handles are inert. When true (default), users can
   *  drag dividers regardless of edit mode. `onSplitResize` may still be
   *  omitted to keep the changes ephemeral (e.g. preview). */
  resizable?: boolean
  className?: string
}

interface DragCtxShape {
  dragSourceId: string | null
  setDragSourceId: (id: string | null) => void
  onMovePanel?: SplitterCanvasProps["onMovePanel"]
  onRemovePanel?: SplitterCanvasProps["onRemovePanel"]
  editable: boolean
  resizable: boolean
}

const DragCtx = createContext<DragCtxShape | null>(null)

function useDragCtx(): DragCtxShape {
  const ctx = useContext(DragCtx)
  if (!ctx) throw new Error("SplitterCanvas children must be rendered inside SplitterCanvas")
  return ctx
}

/**
 * Recursive renderer that walks a splitter tree and emits nested PanelGroup /
 * Panel / PanelResizeHandle from `react-resizable-panels`.
 *
 * In edit mode, each leaf is wrapped with a chrome bar (drag handle + × remove)
 * and four drop quadrants (top / right / bottom / left) so panels can be
 * re-docked via drag-and-drop. A trash zone appears at the bottom-right
 * during a drag.
 */
export function SplitterCanvas({
  root,
  renderLeaf,
  onSplitResize,
  onMovePanel,
  onRemovePanel,
  editable = true,
  resizable = true,
  className,
}: SplitterCanvasProps) {
  const [dragSourceId, setDragSourceId] = useState<string | null>(null)

  const handleDragEndOnRoot = useCallback(() => {
    setDragSourceId(null)
  }, [])

  const ctxValue: DragCtxShape = {
    dragSourceId,
    setDragSourceId,
    onMovePanel,
    onRemovePanel,
    editable,
    resizable,
  }

  return (
    <DragCtx.Provider value={ctxValue}>
      <div className={cn("relative h-full w-full min-h-0", className)} onDragEnd={handleDragEndOnRoot}>
        <RenderNode node={root} renderLeaf={renderLeaf} onSplitResize={onSplitResize} />
        {editable && dragSourceId && onRemovePanel && <TrashZone sourceId={dragSourceId} />}
      </div>
    </DragCtx.Provider>
  )
}

interface RenderNodeProps {
  node: LayoutNode
  renderLeaf: (leaf: LeafPanel) => ReactNode
  onSplitResize?: (splitId: string, sizes: number[]) => void
}

function RenderNode({ node, renderLeaf, onSplitResize }: RenderNodeProps) {
  if (isLeaf(node)) {
    return <LeafFrame leaf={node} renderLeaf={renderLeaf} />
  }
  return <SplitGroup node={node} renderLeaf={renderLeaf} onSplitResize={onSplitResize} />
}

interface LeafFrameProps {
  leaf: LeafPanel
  renderLeaf: (leaf: LeafPanel) => ReactNode
}

function LeafFrame({ leaf, renderLeaf }: LeafFrameProps) {
  const { dragSourceId, setDragSourceId, onMovePanel, onRemovePanel, editable } = useDragCtx()
  const isDragging = dragSourceId === leaf.id

  const handleDragStart = useCallback(
    (e: DragEvent<HTMLDivElement>) => {
      e.dataTransfer.effectAllowed = "move"
      e.dataTransfer.setData(DRAG_MIME, leaf.id)
      setDragSourceId(leaf.id)
    },
    [leaf.id, setDragSourceId],
  )

  const handleDragEnd = useCallback(() => {
    setDragSourceId(null)
  }, [setDragSourceId])

  return (
    <div
      className={cn(
        "relative h-full w-full min-h-0 min-w-0 flex flex-col border rounded-lg overflow-hidden bg-white dark:bg-gray-800 transition-opacity",
        "border-gray-200 dark:border-gray-700",
        isDragging && "opacity-40",
      )}
    >
      {editable && (
        <LeafHeader
          leaf={leaf}
          onDragStart={handleDragStart}
          onDragEnd={handleDragEnd}
          onRemove={onRemovePanel ? () => onRemovePanel(leaf.id) : undefined}
        />
      )}
      <div className="relative flex-1 min-h-0 min-w-0">
        {renderLeaf(leaf)}
      </div>
      {editable && onMovePanel && dragSourceId && dragSourceId !== leaf.id && (
        <DropQuadrants targetId={leaf.id} sourceId={dragSourceId} onMove={onMovePanel} onEnd={handleDragEnd} />
      )}
    </div>
  )
}

interface LeafHeaderProps {
  leaf: LeafPanel
  onDragStart: (e: DragEvent<HTMLDivElement>) => void
  onDragEnd: () => void
  onRemove?: () => void
}

function LeafHeader({ leaf, onDragStart, onDragEnd, onRemove }: LeafHeaderProps) {
  return (
    <div
      className="relative z-20 shrink-0 flex items-center justify-between gap-1 h-6 px-1 bg-gray-100 dark:bg-gray-900 border-b border-gray-200 dark:border-gray-800 select-none cursor-grab active:cursor-grabbing"
      draggable
      onDragStart={onDragStart}
      onDragEnd={onDragEnd}
    >
      <div className="flex items-center gap-1 min-w-0">
        <GripVertical className="h-3 w-3 text-gray-400 dark:text-gray-500 shrink-0" />
        <span className={cn("inline-block h-1.5 w-1.5 rounded-full shrink-0", leafToneFor(leaf.type))} />
        <span className="text-[10px] uppercase tracking-wide font-semibold text-gray-600 dark:text-gray-400 truncate">
          {labelFor(leaf.type)}
        </span>
      </div>
      {onRemove && (
        <button
          type="button"
          onMouseDown={(e) => e.stopPropagation()}
          onClick={onRemove}
          className="p-0.5 rounded hover:bg-red-100 dark:hover:bg-red-900/30 text-gray-500 hover:text-red-600 dark:hover:text-red-400 shrink-0"
          title="Remove panel"
        >
          <X className="h-3 w-3" />
        </button>
      )}
    </div>
  )
}

interface DropQuadrantsProps {
  targetId: string
  sourceId: string
  onMove: (sourceId: string, targetId: string, position: DockPosition) => void
  onEnd: () => void
}

function DropQuadrants({ targetId, sourceId, onMove, onEnd }: DropQuadrantsProps) {
  const [hover, setHover] = useState<DockPosition | null>(null)

  const buildHandlers = (position: DockPosition) => ({
    onDragOver: (e: DragEvent<HTMLDivElement>) => {
      e.preventDefault()
      e.dataTransfer.dropEffect = "move"
      if (hover !== position) setHover(position)
    },
    onDragLeave: () => {
      if (hover === position) setHover(null)
    },
    onDrop: (e: DragEvent<HTMLDivElement>) => {
      e.preventDefault()
      setHover(null)
      onMove(sourceId, targetId, position)
      onEnd()
    },
  })

  const zoneClass = (pos: DockPosition) =>
    cn(
      "absolute pointer-events-auto transition-colors",
      hover === pos ? "bg-blue-500/30 ring-2 ring-blue-500 ring-inset" : "bg-blue-500/0",
    )

  return (
    <div className="absolute inset-0 z-10 pointer-events-none">
      <div className={cn(zoneClass("top"), "top-0 left-0 right-0 h-1/4")} {...buildHandlers("top")} />
      <div className={cn(zoneClass("bottom"), "bottom-0 left-0 right-0 h-1/4")} {...buildHandlers("bottom")} />
      <div className={cn(zoneClass("left"), "top-1/4 bottom-1/4 left-0 w-1/4")} {...buildHandlers("left")} />
      <div className={cn(zoneClass("right"), "top-1/4 bottom-1/4 right-0 w-1/4")} {...buildHandlers("right")} />
    </div>
  )
}

interface TrashZoneProps {
  sourceId: string
}

function TrashZone({ sourceId }: TrashZoneProps) {
  const { onRemovePanel, setDragSourceId } = useDragCtx()
  const [hover, setHover] = useState(false)

  if (!onRemovePanel) return null

  return (
    <div
      className={cn(
        "absolute bottom-3 right-3 z-30 flex items-center gap-1.5 px-3 py-2 rounded-lg border-2 border-dashed shadow-sm transition-colors",
        hover
          ? "bg-red-100 dark:bg-red-900/40 border-red-500 text-red-700 dark:text-red-300"
          : "bg-white dark:bg-gray-900 border-red-400/60 text-red-600 dark:text-red-400",
      )}
      onDragOver={(e) => {
        e.preventDefault()
        e.dataTransfer.dropEffect = "move"
        setHover(true)
      }}
      onDragLeave={() => setHover(false)}
      onDrop={(e) => {
        e.preventDefault()
        setHover(false)
        onRemovePanel(sourceId)
        setDragSourceId(null)
      }}
    >
      <Trash2 className="h-4 w-4" />
      <span className="text-xs font-semibold">Drop to remove</span>
    </div>
  )
}

interface SplitGroupProps {
  node: Extract<LayoutNode, { kind: "split" }>
  renderLeaf: (leaf: LeafPanel) => ReactNode
  onSplitResize?: (splitId: string, sizes: number[]) => void
}

function SplitGroup({ node, renderLeaf, onSplitResize }: SplitGroupProps) {
  const { resizable } = useDragCtx()
  const handleLayout = useCallback(
    (sizes: number[]) => {
      onSplitResize?.(node.id, sizes)
    },
    [node.id, onSplitResize],
  )

  return (
    <PanelGroup
      direction={node.direction}
      onLayout={resizable && onSplitResize ? handleLayout : undefined}
      autoSaveId={undefined}
      id={node.id}
      className="h-full w-full"
    >
      {node.children.map((child, idx) => {
        const minSize = minSizeForChild(child)
        const childKey = childKeyFor(child, idx)
        return (
          <Fragment key={childKey}>
            <Panel
              order={idx}
              defaultSize={node.sizes[idx] ?? 100 / node.children.length}
              minSize={minSize}
              className="h-full w-full min-h-0 min-w-0"
            >
              <RenderNode node={child} renderLeaf={renderLeaf} onSplitResize={onSplitResize} />
            </Panel>
            {idx < node.children.length - 1 && (
              <PanelResizeHandle
                disabled={!resizable}
                className={cn(
                  "relative shrink-0 transition-colors",
                  node.direction === "horizontal" ? "w-1.5 cursor-col-resize" : "h-1.5 cursor-row-resize",
                  resizable
                    ? "bg-transparent hover:bg-blue-500/40 data-resize-handle-active:bg-blue-500/60"
                    : "bg-transparent pointer-events-none",
                )}
              />
            )}
          </Fragment>
        )
      })}
    </PanelGroup>
  )
}

function minSizeForChild(child: LayoutNode): number {
  if (!isLeaf(child)) return 5
  switch (child.type) {
    case "output":
      return 10
    case "explorer":
      return 12
    case "full-editor":
    case "focus-editor":
      return 20
    default:
      return 10
  }
}

function childKeyFor(child: LayoutNode, idx: number): string {
  if (isLeaf(child)) return `leaf-${child.id}`
  return `split-${child.id}-${idx}`
}

function labelFor(type: PanelType): string {
  switch (type) {
    case "explorer":
      return "Files"
    case "full-editor":
      return "Editor"
    case "focus-editor":
      return "Focus editor"
    case "output":
      return "Output"
    default:
      return type
  }
}

function leafToneFor(type: PanelType): string {
  switch (type) {
    case "explorer":
      return "bg-green-500"
    case "full-editor":
      return "bg-blue-500"
    case "focus-editor":
      return "bg-cyan-500"
    case "output":
      return "bg-purple-500"
    default:
      return "bg-gray-400"
  }
}
