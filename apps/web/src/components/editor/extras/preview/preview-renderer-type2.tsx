"use client"

import { useState, useRef, useEffect } from "react"
import type { SerializedEditorState } from "lexical"
import type { ProjectPreferences, PanelData } from "@/lib/storage/editor/project-preferences"
import { PreviewRenderer } from "./preview-renderer"
import { ChevronLeft, ChevronRight } from "lucide-react"
import { AdvancedMultiBlockPreview } from "./advanced-multi-block-preview"

interface PreviewRendererType2Props {
  blockStates: Record<string, SerializedEditorState>
  projectId?: string
  storageAdapter?: {
    load: (id: string) => Promise<any>
  }
  preferences?: ProjectPreferences
  onLayoutChange?: (panels: PanelData[], direction: "horizontal" | "vertical") => void
}

export function PreviewRendererType2({ blockStates, projectId, storageAdapter, preferences, onLayoutChange }: PreviewRendererType2Props) {
  // Get blocks for multi-panel display (1 or more)
  const blockEntries = Object.entries(blockStates).sort((a, b) => {
    const numA = parseInt(a[0].slice(1))
    const numB = parseInt(b[0].slice(1))
    return numA - numB
  })
  
  if (blockEntries.length === 0) {
    return <div className="p-8 text-center text-gray-500">No blocks available for preview</div>
  }
  
  // If 3+ blocks, use AdvancedMultiBlockPreview with panels and tabs
  if (blockEntries.length >= 3) {
    return (
      <AdvancedMultiBlockPreview
        blockStates={blockStates}
        projectId={projectId}
        storageAdapter={storageAdapter}
        preferences={preferences}
        isEditable={true}
        onLayoutChange={onLayoutChange}
      />
    )
  }
  
  const singleBlockWidth = preferences?.global?.type2SingleBlockWidth || "wide"
  
  // If only 1 block, show it in full width or narrow based on preference
  if (blockEntries.length === 1) {
    const [blockId, state] = blockEntries[0]!
    return (
      <div className={singleBlockWidth === "narrow" ? "flex justify-center w-full" : ""}>
        <div className={`border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900 ${
          singleBlockWidth === "narrow" ? "w-full max-w-4xl mx-auto" : "w-full"
        }`}>
          <div className="p-2 flex items-center justify-center border-b border-gray-200 dark:border-gray-700">
            <span className="text-sm font-medium text-gray-600 dark:text-gray-400">
              Panel 1
            </span>
          </div>
          <div className="p-6 sm:p-8 md:p-12">
            <PreviewRenderer 
              serializedState={state}
              projectId={projectId}
              storageAdapter={storageAdapter}
            />
          </div>
        </div>
      </div>
    )
  }
  
  // 2 blocks: show with resizable panels
  return <TwoBlockPreview blockStates={blockStates} blockEntries={blockEntries} projectId={projectId} storageAdapter={storageAdapter} />
}

// Separate component for 2-block preview to avoid hooks issues
function TwoBlockPreview({
  blockStates,
  blockEntries,
  projectId,
  storageAdapter,
}: {
  blockStates: Record<string, SerializedEditorState>
  blockEntries: [string, SerializedEditorState][]
  projectId?: string
  storageAdapter?: { load: (id: string) => Promise<any> }
}) {
  const firstBlock = blockEntries[0]!
  const secondBlock = blockEntries[1]!
  
  const [firstBlockId, firstState] = firstBlock
  const [secondBlockId, secondState] = secondBlock
  
  const [leftWidth, setLeftWidth] = useState(50) // Percentage
  const [isDragging, setIsDragging] = useState(false)
  const [isLeftCollapsed, setIsLeftCollapsed] = useState(false)
  const [isRightCollapsed, setIsRightCollapsed] = useState(false)
  const [isAnimating, setIsAnimating] = useState(false)
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
    setIsAnimating(true)
    setIsLeftCollapsed(false)
    setIsRightCollapsed(false)
    setLeftWidth(50)
    setTimeout(() => setIsAnimating(false), 250)
  }

  const expandRight = () => {
    setIsAnimating(true)
    setIsRightCollapsed(false)
    setIsLeftCollapsed(false)
    setLeftWidth(50)
    setTimeout(() => setIsAnimating(false), 250)
  }

  const getLeftWidth = () => {
    if (isLeftCollapsed) return "3%"
    if (isRightCollapsed) return "100%"
    return `${leftWidth}%`
  }

  const getRightWidth = () => {
    if (isRightCollapsed) return "3%"
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
          transition: (isLeftCollapsed || isRightCollapsed || isAnimating) ? 'width 0.25s cubic-bezier(0.34, 1.56, 0.64, 1)' : 'none',
          willChange: (isDragging || isAnimating) ? 'width' : 'auto'
        }}
      >
        {isLeftCollapsed ? (
          <div 
            className="h-full w-full bg-gradient-to-r from-gray-200 to-gray-300 dark:from-gray-700 dark:to-gray-800 cursor-col-resize hover:from-blue-400 hover:to-blue-500 dark:hover:from-blue-600 dark:hover:to-blue-700 transition-colors duration-150 flex items-center justify-center"
            onMouseDown={handleMouseDown}
            title="Drag to expand"
          >
            <ChevronRight className="w-5 h-5 text-gray-600 dark:text-gray-300" />
          </div>
        ) : (
          <div className="p-6 sm:p-8 md:p-12 h-full overflow-y-auto overflow-x-hidden break-words">
            <PreviewRenderer serializedState={firstState} projectId={projectId} storageAdapter={storageAdapter} />
          </div>
        )}
      </div>

      {/* Collapsed Left Button */}
      {isLeftCollapsed && (
        <button
          onClick={expandLeft}
          className="absolute left-0 top-4 z-20 bg-gray-200 hover:bg-gray-300 dark:bg-gray-700 dark:hover:bg-gray-600 p-2 rounded-r-md shadow-lg transition-all duration-200 hover:scale-110"
          title="Click to expand (or drag the panel)"
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
          className="absolute right-0 top-4 z-20 bg-gray-200 hover:bg-gray-300 dark:bg-gray-700 dark:hover:bg-gray-600 p-2 rounded-l-md shadow-lg transition-all duration-200 hover:scale-110"
          title="Click to expand (or drag the panel)"
        >
          <ChevronLeft className="w-4 h-4 text-gray-700 dark:text-gray-300" />
        </button>
      )}

      {/* Right Panel */}
      <div
        className="relative border border-gray-200 bg-white shadow-sm dark:border-gray-800 dark:bg-gray-900 lg:border-l-0 overflow-hidden"
        style={{ 
          width: getRightWidth(),
          transition: (isLeftCollapsed || isRightCollapsed || isAnimating) ? 'width 0.25s cubic-bezier(0.34, 1.56, 0.64, 1)' : 'none',
          willChange: (isDragging || isAnimating) ? 'width' : 'auto'
        }}
      >
        {isRightCollapsed ? (
          <div 
            className="h-full w-full bg-gradient-to-l from-gray-200 to-gray-300 dark:from-gray-700 dark:to-gray-800 cursor-col-resize hover:from-blue-400 hover:to-blue-500 dark:hover:from-blue-600 dark:hover:to-blue-700 transition-colors duration-150 flex items-center justify-center"
            onMouseDown={handleMouseDown}
            title="Drag to expand"
          >
            <ChevronLeft className="w-5 h-5 text-gray-600 dark:text-gray-300" />
          </div>
        ) : (
          <div className="p-6 sm:p-8 md:p-12 h-full overflow-y-auto overflow-x-hidden break-words">
            <PreviewRenderer serializedState={secondState} projectId={projectId} storageAdapter={storageAdapter} />
          </div>
        )}
      </div>
    </div>
  )
}

