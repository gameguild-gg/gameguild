"use client"

import { useState, useRef, useEffect } from "react"
import type { SerializedEditorState } from "lexical"
import { PreviewRenderer } from "./preview-renderer"
import { ChevronLeft, ChevronRight } from "lucide-react"

interface PreviewRendererType2Props {
  leftState: SerializedEditorState
  rightState: SerializedEditorState
}

export function PreviewRendererType2({ leftState, rightState }: PreviewRendererType2Props) {
  const [leftWidth, setLeftWidth] = useState(50) // Percentage
  const [isDragging, setIsDragging] = useState(false)
  const [isLeftCollapsed, setIsLeftCollapsed] = useState(false)
  const [isRightCollapsed, setIsRightCollapsed] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)

  const handleMouseDown = (e: React.MouseEvent) => {
    e.preventDefault()
    setIsDragging(true)
    document.body.style.cursor = 'col-resize'
    document.body.style.userSelect = 'none'
  }

  useEffect(() => {
    let animationFrameId: number | null = null

    const handleMouseMove = (e: MouseEvent) => {
      if (!isDragging || !containerRef.current) return

      if (animationFrameId !== null) {
        cancelAnimationFrame(animationFrameId)
      }

      animationFrameId = requestAnimationFrame(() => {
        if (!containerRef.current) return

        const container = containerRef.current
        const containerRect = container.getBoundingClientRect()
        const containerWidth = containerRect.width
        const mouseX = e.clientX - containerRect.left
        
        // Calculate percentage with smooth interpolation
        let percentage = (mouseX / containerWidth) * 100
        
        // Clamp between 5% and 95% for more freedom
        percentage = Math.max(5, Math.min(95, percentage))
        
        // Check for collapse thresholds (more aggressive)
        if (percentage < 8) {
          setIsLeftCollapsed(true)
          setIsRightCollapsed(false)
        } else if (percentage > 92) {
          setIsRightCollapsed(true)
          setIsLeftCollapsed(false)
        } else {
          setIsLeftCollapsed(false)
          setIsRightCollapsed(false)
          setLeftWidth(percentage)
        }
      })
    }

    const handleMouseUp = () => {
      setIsDragging(false)
      document.body.style.cursor = ''
      document.body.style.userSelect = ''
      if (animationFrameId !== null) {
        cancelAnimationFrame(animationFrameId)
      }
    }

    if (isDragging) {
      document.addEventListener("mousemove", handleMouseMove, { passive: true })
      document.addEventListener("mouseup", handleMouseUp)
    }

    return () => {
      document.removeEventListener("mousemove", handleMouseMove)
      document.removeEventListener("mouseup", handleMouseUp)
      if (animationFrameId !== null) {
        cancelAnimationFrame(animationFrameId)
      }
    }
  }, [isDragging])

  const expandLeft = () => {
    setIsLeftCollapsed(false)
    setIsRightCollapsed(false)
    setLeftWidth(50)
  }

  const expandRight = () => {
    setIsRightCollapsed(false)
    setIsLeftCollapsed(false)
    setLeftWidth(50)
  }

  const getLeftWidth = () => {
    if (isLeftCollapsed) return "0%"
    if (isRightCollapsed) return "100%"
    return `${leftWidth}%`
  }

  const getRightWidth = () => {
    if (isRightCollapsed) return "0%"
    if (isLeftCollapsed) return "100%"
    return `${100 - leftWidth}%`
  }

  return (
    <div ref={containerRef} className="flex flex-col lg:flex-row w-full h-full relative select-none">
      {/* Left Panel */}
      <div
        className="relative border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900 overflow-hidden"
        style={{ 
          width: getLeftWidth(),
          transition: isLeftCollapsed || isRightCollapsed ? 'width 0.3s ease-in-out' : 'none',
          willChange: isDragging ? 'width' : 'auto'
        }}
      >
        {!isLeftCollapsed && (
          <div className="p-6 sm:p-8 md:p-12 h-full overflow-y-auto">
            <PreviewRenderer serializedState={leftState} />
          </div>
        )}
      </div>

      {/* Collapsed Left Button */}
      {isLeftCollapsed && (
        <button
          onClick={expandLeft}
          className="absolute left-0 top-1/2 -translate-y-1/2 z-20 bg-gray-200 hover:bg-gray-300 dark:bg-gray-700 dark:hover:bg-gray-600 p-2 rounded-r-md shadow-lg transition-colors duration-200"
          title="Expand left panel"
        >
          <ChevronRight className="w-4 h-4 text-gray-700 dark:text-gray-300" />
        </button>
      )}

      {/* Draggable Divider */}
      {!isLeftCollapsed && !isRightCollapsed && (
        <div
          className="hidden lg:flex w-1 bg-gray-300 dark:bg-gray-700 cursor-col-resize hover:bg-blue-500 dark:hover:bg-blue-500 transition-colors duration-150 relative group"
          onMouseDown={handleMouseDown}
          style={{ willChange: isDragging ? 'background-color' : 'auto' }}
        >
          <div className="absolute inset-y-0 -left-2 -right-2 group-hover:bg-blue-500/10" />
          <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-1 h-12 bg-gray-400 dark:bg-gray-600 rounded-full group-hover:bg-blue-500 transition-colors duration-150" />
        </div>
      )}

      {/* Collapsed Right Button */}
      {isRightCollapsed && (
        <button
          onClick={expandRight}
          className="absolute right-0 top-1/2 -translate-y-1/2 z-20 bg-gray-200 hover:bg-gray-300 dark:bg-gray-700 dark:hover:bg-gray-600 p-2 rounded-l-md shadow-lg transition-colors duration-200"
          title="Expand right panel"
        >
          <ChevronLeft className="w-4 h-4 text-gray-700 dark:text-gray-300" />
        </button>
      )}

      {/* Right Panel */}
      <div
        className="relative border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900 lg:border-l-0 overflow-hidden"
        style={{ 
          width: getRightWidth(),
          transition: isLeftCollapsed || isRightCollapsed ? 'width 0.3s ease-in-out' : 'none',
          willChange: isDragging ? 'width' : 'auto'
        }}
      >
        {!isRightCollapsed && (
          <div className="p-6 sm:p-8 md:p-12 h-full overflow-y-auto">
            <PreviewRenderer serializedState={rightState} />
          </div>
        )}
      </div>
    </div>
  )
}

