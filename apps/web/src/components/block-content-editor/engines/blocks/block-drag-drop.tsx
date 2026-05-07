"use client"

import { useState, useCallback, useRef, useEffect } from "react"
import { ArrowDown } from "lucide-react"
import type { BlockArray } from "@/components/block-content-editor/lib/storage/editor/block-structure"

// ============================================================================
// Drag Preview — "Mover para cá" marker at the insertion point
// ============================================================================

export function DragPreview({
  onDragOver,
  onDrop,
}: {
  onDragOver: (e: React.DragEvent) => void
  onDrop: () => void
}) {
  return (
    <div
      onDragOver={(e) => { e.preventDefault(); e.dataTransfer.dropEffect = "move"; onDragOver(e) }}
      onDrop={(e) => { e.preventDefault(); onDrop() }}
      className="my-2 rounded-xl border-3 border-dashed border-blue-500 dark:border-blue-400 bg-blue-100/80 dark:bg-blue-950/50 flex items-center justify-center gap-3 py-5 transition-all duration-150 shadow-lg shadow-blue-200/40 dark:shadow-blue-900/40"
    >
      <ArrowDown className="h-6 w-6 text-blue-600 dark:text-blue-300 animate-bounce" />
      <span className="text-base font-bold text-blue-600 dark:text-blue-300 tracking-wide uppercase">Mover para cá</span>
      <ArrowDown className="h-6 w-6 text-blue-600 dark:text-blue-300 animate-bounce" />
    </div>
  )
}

// ============================================================================
// useBlockDragDrop — all drag-and-drop state and handlers
// ============================================================================

interface UseBlockDragDropOptions {
  blocks: BlockArray
  onChange: (blocks: BlockArray) => void
  onDragStateChange?: (dragging: boolean) => void
  scrollToIndexRef: React.MutableRefObject<number | null>
}

export interface BlockDragDrop {
  isDragging: boolean
  dragIndex: number | null
  dropTargetIndex: number | null
  containerRef: React.RefObject<HTMLDivElement | null>
  setDropTargetIndex: React.Dispatch<React.SetStateAction<number | null>>
  handleDragStart: (index: number) => void
  handleDragEnd: () => void
  handleContainerDragOver: (e: React.DragEvent) => void
  handleContainerDragLeave: (e: React.DragEvent) => void
}

export function useBlockDragDrop({
  blocks,
  onChange,
  onDragStateChange,
  scrollToIndexRef,
}: UseBlockDragDropOptions): BlockDragDrop {
  const [isDragging, setIsDragging] = useState(false)
  const [dragIndex, setDragIndex] = useState<number | null>(null)
  const [dropTargetIndex, setDropTargetIndex] = useState<number | null>(null)
  const containerRef = useRef<HTMLDivElement>(null)
  const autoScrollRAF = useRef<number | null>(null)
  const lastDragY = useRef(0)

  const handleDragStart = useCallback((index: number) => {
    setDragIndex(index)
    requestAnimationFrame(() => {
      setIsDragging(true)
      onDragStateChange?.(true)
    })
  }, [onDragStateChange])

  const handleDragEnd = useCallback(() => {
    if (dragIndex !== null && dropTargetIndex !== null) {
      const fromIndex = dragIndex
      let toIndex = dropTargetIndex
      if (toIndex !== fromIndex && toIndex !== fromIndex + 1) {
        const next = [...blocks]
        const [moved] = next.splice(fromIndex, 1)
        if (toIndex > fromIndex) toIndex -= 1
        next.splice(toIndex, 0, moved!)
        onChange(next)
        scrollToIndexRef.current = toIndex
      }
    }
    setIsDragging(false)
    onDragStateChange?.(false)
    setDragIndex(null)
    setDropTargetIndex(null)
    if (autoScrollRAF.current) cancelAnimationFrame(autoScrollRAF.current)
  }, [dragIndex, dropTargetIndex, blocks, onChange, onDragStateChange, scrollToIndexRef])

  const handleContainerDragOver = useCallback((e: React.DragEvent) => {
    e.preventDefault()
    lastDragY.current = e.clientY
  }, [])

  const handleContainerDragLeave = useCallback((e: React.DragEvent) => {
    if (!e.currentTarget.contains(e.relatedTarget as Node)) {
      setDropTargetIndex(null)
    }
  }, [])

  // Auto-scroll when dragging near viewport edges
  useEffect(() => {
    if (!isDragging) return
    const scrollStep = () => {
      const y = lastDragY.current
      if (y === 0) { autoScrollRAF.current = requestAnimationFrame(scrollStep); return }
      const scrollZone = 150
      const maxSpeed = 40
      let scrollParent: HTMLElement | null = containerRef.current?.parentElement ?? null
      while (scrollParent && scrollParent !== document.documentElement) {
        const { overflowY } = getComputedStyle(scrollParent)
        if (/(auto|scroll)/.test(overflowY)) break
        scrollParent = scrollParent.parentElement
      }
      if (!scrollParent) scrollParent = document.documentElement
      const rect = scrollParent === document.documentElement
        ? { top: 0, bottom: window.innerHeight }
        : scrollParent.getBoundingClientRect()
      const distTop = y - rect.top
      const distBottom = rect.bottom - y
      if (distTop < scrollZone) {
        scrollParent.scrollTop -= Math.round(maxSpeed * (1 - distTop / scrollZone))
      } else if (distBottom < scrollZone) {
        scrollParent.scrollTop += Math.round(maxSpeed * (1 - distBottom / scrollZone))
      }
      autoScrollRAF.current = requestAnimationFrame(scrollStep)
    }
    autoScrollRAF.current = requestAnimationFrame(scrollStep)
    return () => { if (autoScrollRAF.current) cancelAnimationFrame(autoScrollRAF.current) }
  }, [isDragging])

  return {
    isDragging,
    dragIndex,
    dropTargetIndex,
    containerRef,
    setDropTargetIndex,
    handleDragStart,
    handleDragEnd,
    handleContainerDragOver,
    handleContainerDragLeave,
  }
}
