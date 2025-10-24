"use client"

import { useEffect, useState, useRef } from "react"
import { BarChart3, ZoomIn, ZoomOut, RotateCcw, Maximize2 } from "lucide-react"
import { Button } from "@/components/ui/button"
import { useVegaLiteChart, renderVegaChart } from "@/components/ui/vega-lite-chart"

interface VegaLiteViewerProps {
  spec: string
  layout?: "square" | "rectangular"
  theme?: string
  title?: string
  caption?: string
  size?: number
  showControls?: boolean
  allowFullscreen?: boolean
  className?: string
}

export function VegaLiteViewer({ 
  spec,
  layout = "rectangular", 
  theme = "default",
  title,
  caption,
  size = 100,
  showControls = true,
  allowFullscreen = true,
  className = ""
}: VegaLiteViewerProps) {
  const [zoom, setZoom] = useState(100)
  const [isFullscreen, setIsFullscreen] = useState(false)
  const [fullscreenZoom, setFullscreenZoom] = useState(150) // Start larger in fullscreen
  const [position, setPosition] = useState({ x: 0, y: 0 })
  const [fullscreenPosition, setFullscreenPosition] = useState({ x: 0, y: 0 })
  const [isDragging, setIsDragging] = useState(false)
  const [isFullscreenDragging, setIsFullscreenDragging] = useState(false)
  const [dragStart, setDragStart] = useState({ x: 0, y: 0 })
  const [fullscreenDragStart, setFullscreenDragStart] = useState({ x: 0, y: 0 })
  const [lastPosition, setLastPosition] = useState({ x: 0, y: 0 })
  const [lastFullscreenPosition, setLastFullscreenPosition] = useState({ x: 0, y: 0 })

  // Use the new hook for chart data processing
  const { parsedSpec, isLoading, error, vegaRef, fullscreenVegaRef } = useVegaLiteChart({
    spec,
    layout,
    theme,
    title
  })

  // Container ref for zoom/pan functionality
  const containerRef = useRef<HTMLDivElement>(null)

  // Render chart when parsedSpec is available
  useEffect(() => {
    const loadChart = async () => {
      if (!parsedSpec || !vegaRef.current) {
        console.log("VegaLiteViewer: Skipping render - missing spec or container")
        return
      }

      try {
        await renderVegaChart(vegaRef.current, parsedSpec, layout, title)
        console.log("VegaLiteViewer: Chart rendered successfully")
      } catch (err: any) {
        console.error("VegaLiteViewer: Error rendering chart:", err)
        if (vegaRef.current) {
          vegaRef.current.innerHTML = `
            <div class="text-red-500 p-3 rounded-md bg-red-50/50 dark:bg-red-900/10 max-w-full">
              <div class="font-medium text-sm">Error rendering chart:</div>
              <div class="text-xs mt-1 opacity-80">${err.message || "Unknown error"}</div>
            </div>
          `
        }
      }
    }

    loadChart()
  }, [parsedSpec, layout, title])

  // Render fullscreen chart when fullscreen is toggled
  useEffect(() => {
    const loadFullscreenChart = async () => {
      if (isFullscreen && fullscreenVegaRef.current && parsedSpec) {
        try {
          await renderVegaChart(fullscreenVegaRef.current, parsedSpec, layout, title)
        } catch (err: any) {
          if (fullscreenVegaRef.current) {
            fullscreenVegaRef.current.innerHTML = `
              <div class="text-red-500 p-3 rounded-md bg-red-50/50 dark:bg-red-900/10 max-w-full">
                <div class="font-medium text-sm">Error rendering chart:</div>
                <div class="text-xs mt-1 opacity-80">${err.message || "Unknown error"}</div>
              </div>
            `
          }
        }
      }
    }

    loadFullscreenChart()
  }, [isFullscreen, parsedSpec, layout, title])

  // Handle escape key for fullscreen
  useEffect(() => {
    const handleEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        setIsFullscreen(false)
      }
    }

    if (isFullscreen) {
      document.addEventListener("keydown", handleEscape)
      return () => document.removeEventListener("keydown", handleEscape)
    }
  }, [isFullscreen])

  // Reset position when zoom returns to 100%
  useEffect(() => {
    if (zoom === 100 && (position.x !== 0 || position.y !== 0)) {
      // Smooth transition to center when returning to 100%
      const resetTimer = setTimeout(() => {
        setPosition({ x: 0, y: 0 })
      }, 50)
      return () => clearTimeout(resetTimer)
    }
  }, [zoom, position.x, position.y])

  // Reset fullscreen position when zoom returns to 100%
  useEffect(() => {
    if (fullscreenZoom === 100 && (fullscreenPosition.x !== 0 || fullscreenPosition.y !== 0)) {
      // Smooth transition to center when returning to 100%
      const resetTimer = setTimeout(() => {
        setFullscreenPosition({ x: 0, y: 0 })
      }, 50)
      return () => clearTimeout(resetTimer)
    }
  }, [fullscreenZoom, fullscreenPosition.x, fullscreenPosition.y])

  // Zoom control functions
  const handleZoomIn = () => {
    setZoom((prev) => Math.min(prev + 25, 300))
  }

  const handleZoomOut = () => {
    setZoom((prev) => Math.max(prev - 25, 100))
  }

  const handleZoomReset = () => {
    setZoom(100)
    setPosition({ x: 0, y: 0 })
  }

  const handleFullscreen = () => {
    setIsFullscreen(true)
  }

  // Fullscreen zoom control functions
  const handleFullscreenZoomIn = () => {
    setFullscreenZoom((prev) => Math.min(prev + 50, 500))
  }

  const handleFullscreenZoomOut = () => {
    setFullscreenZoom((prev) => Math.max(prev - 50, 100))
  }

  const handleFullscreenZoomReset = () => {
    setFullscreenZoom(150) // Reset to larger default
    setFullscreenPosition({ x: 0, y: 0 }) // Reset position when resetting zoom
  }

  // Fullscreen Pan/Drag control functions
  const handleFullscreenMouseDown = (e: React.MouseEvent) => {
    if (fullscreenZoom > 100) {
      setIsFullscreenDragging(true)
      setFullscreenDragStart({ x: e.clientX, y: e.clientY })
      setLastFullscreenPosition(fullscreenPosition)
      e.preventDefault()
      e.stopPropagation()
    }
  }

  const handleFullscreenMouseMove = (e: React.MouseEvent) => {
    if (isFullscreenDragging && fullscreenZoom > 100) {
      e.preventDefault()
      const deltaX = (e.clientX - fullscreenDragStart.x) * 0.8 // Damping factor for smoother movement
      const deltaY = (e.clientY - fullscreenDragStart.y) * 0.8
      setFullscreenPosition({
        x: lastFullscreenPosition.x + deltaX,
        y: lastFullscreenPosition.y + deltaY
      })
    }
  }

  const handleFullscreenMouseUp = () => {
    if (isFullscreenDragging) {
      setIsFullscreenDragging(false)
    }
  }

  const handleFullscreenMouseLeave = () => {
    if (isFullscreenDragging) {
      setIsFullscreenDragging(false)
    }
  }

  // Fullscreen wheel zoom for smoother zoom experience
  const handleFullscreenWheel = (e: React.WheelEvent) => {
    if (e.ctrlKey || e.metaKey) {
      e.preventDefault()
      const delta = e.deltaY > 0 ? -25 : 25 // Larger increments for fullscreen
      setFullscreenZoom((prev) => {
        const newZoom = Math.max(100, Math.min(500, prev + delta))
        return newZoom
      })
    }
  }

  // Pan/Drag control functions
  const handleMouseDown = (e: React.MouseEvent) => {
    if (zoom > 100) {
      setIsDragging(true)
      setDragStart({ x: e.clientX, y: e.clientY })
      setLastPosition(position)
      e.preventDefault()
      e.stopPropagation()
    }
  }

  const handleMouseMove = (e: React.MouseEvent) => {
    if (isDragging && zoom > 100) {
      e.preventDefault()
      const deltaX = (e.clientX - dragStart.x) * 0.8 // Damping factor for smoother movement
      const deltaY = (e.clientY - dragStart.y) * 0.8
      setPosition({
        x: lastPosition.x + deltaX,
        y: lastPosition.y + deltaY
      })
    }
  }

  const handleMouseUp = () => {
    if (isDragging) {
      setIsDragging(false)
    }
  }

  const handleMouseLeave = () => {
    if (isDragging) {
      setIsDragging(false)
    }
  }

  // Wheel zoom for smoother zoom experience
  const handleWheel = (e: React.WheelEvent) => {
    if (e.ctrlKey || e.metaKey) {
      e.preventDefault()
      const delta = e.deltaY > 0 ? -5 : 5 // Smaller increments for smooth zoom
      setZoom((prev) => {
        const newZoom = Math.max(100, Math.min(300, prev + delta))
        return newZoom
      })
    }
  }

  if (!spec) {
    return (
      <div className={`my-4 py-8 ${className}`}>
        <div className="text-center text-gray-400 dark:text-gray-500">
          <BarChart3 className="h-8 w-8 mx-auto mb-2 opacity-50" />
          <p className="text-sm">No Vega-Lite specification provided</p>
        </div>
      </div>
    )
  }

  return (
    <>
      <div className={`relative group my-4 ${className}`} style={{ width: `${size}%` }}>
        {/* Title - Outside the chart area */}
        {title && (
          <h3 className="text-lg font-semibold mb-3 text-center dark:text-white">
            {title}
          </h3>
        )}

        {/* Chart Container - Minimal design */}
        <div className="relative">
          {/* Floating Controls */}
          {showControls && (
            <div className="absolute top-3 right-3 z-50 flex items-center gap-1 bg-white/95 dark:bg-gray-900/95 backdrop-blur-md rounded-md p-1 shadow-lg border border-gray-200/50 dark:border-gray-700/50 opacity-0 group-hover:opacity-100 transition-all duration-300">
              <Button
                variant="ghost"
                size="sm"
                onClick={handleZoomOut}
                disabled={zoom <= 100}
                className="h-8 w-8 p-0"
                title="Zoom Out"
              >
                <ZoomOut className="h-4 w-4" />
              </Button>
              <span className="text-xs font-mono px-2 py-1 bg-gray-100 dark:bg-gray-700 rounded min-w-[3rem] text-center">
                {zoom}%
              </span>
              <Button
                variant="ghost"
                size="sm"
                onClick={handleZoomIn}
                disabled={zoom >= 300}
                className="h-8 w-8 p-0"
                title="Zoom In"
              >
                <ZoomIn className="h-4 w-4" />
              </Button>
              <Button
                variant="ghost"
                size="sm"
                onClick={handleZoomReset}
                className="h-8 w-8 p-0"
                title="Reset Zoom"
              >
                <RotateCcw className="h-4 w-4" />
              </Button>
              {allowFullscreen && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={handleFullscreen}
                  className="h-8 w-8 p-0 relative z-60"
                  title="Fullscreen"
                >
                  <Maximize2 className="h-4 w-4" />
                </Button>
              )}
            </div>
          )}

          {/* Chart Content - Minimal container */}
          <div 
            ref={containerRef}
            className={`w-full flex justify-center items-center relative overflow-hidden rounded-lg ${
              layout === "square" ? "min-h-[450px]" : "min-h-[350px]"
            } ${zoom > 100 ? "cursor-move" : "cursor-default"}`}
            onMouseDown={handleMouseDown}
            onMouseMove={handleMouseMove}
            onMouseUp={handleMouseUp}
            onMouseLeave={handleMouseLeave}
            onWheel={handleWheel}
            style={{ 
              position: "relative",
              zIndex: 10
            }}
          >
            {isLoading ? (
              <div className="text-gray-400 dark:text-gray-500 text-sm">Rendering chart...</div>
            ) : error ? (
              <div className="text-red-500 p-3 rounded-md bg-red-50/50 dark:bg-red-900/10 max-w-full">
                <div className="font-medium text-sm">Error rendering chart:</div>
                <div className="text-xs mt-1 opacity-80">{error}</div>
              </div>
            ) : spec ? (
              <div
                ref={vegaRef}
                className="flex justify-center items-center w-full"
                style={{
                  transform: `scale(${zoom / 100}) translate(${position.x / (zoom / 100)}px, ${position.y / (zoom / 100)}px)`,
                  transformOrigin: "center",
                  minHeight: layout === "square" ? "400px" : "300px",
                  minWidth: "200px",
                  position: "relative",
                  zIndex: 5,
                  userSelect: zoom > 100 ? "none" : "auto",
                  pointerEvents: zoom > 100 ? "none" : "auto",
                  transition: isDragging ? "none" : "transform 0.15s cubic-bezier(0.4, 0, 0.2, 1)"
                }}
                // Chart will be rendered directly into vegaRef
              />
            ) : (
              <div className="text-gray-400 dark:text-gray-500 text-sm">No chart specification provided</div>
            )}
          </div>
        </div>

        {/* Caption - Outside the chart area */}
        {caption && (
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-3 text-center italic">
            {caption}
          </p>
        )}
      </div>

      {/* Fullscreen Modal */}
      {isFullscreen && allowFullscreen && (
        <div className="fixed inset-0 bg-black/80 backdrop-blur-sm flex items-center justify-center z-50 p-4">
          <div className="bg-white dark:bg-gray-900 border dark:border-gray-700 rounded-lg shadow-2xl w-full max-w-7xl h-[90vh] flex flex-col">
            {/* Fullscreen Header */}
            <div className="flex items-center justify-between p-4 border-b border-gray-200 dark:border-gray-700">
              <div className="flex items-center gap-2">
                <BarChart3 className="h-5 w-5 text-blue-600 dark:text-blue-400" />
                <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">
                  {title || "Vega-Lite Chart"}
                </h2>
              </div>
              
              {/* Fullscreen Zoom Controls */}
              <div className="flex items-center gap-4">
                <div className="flex items-center gap-1 bg-gray-100 dark:bg-gray-800 rounded-md p-1">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={handleFullscreenZoomOut}
                    disabled={fullscreenZoom <= 100}
                    className="h-8 w-8 p-0"
                    title="Zoom Out"
                  >
                    <ZoomOut className="h-4 w-4" />
                  </Button>
                  <span className="text-xs font-mono px-2 py-1 bg-gray-200 dark:bg-gray-700 rounded min-w-[3rem] text-center">
                    {fullscreenZoom}%
                  </span>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={handleFullscreenZoomIn}
                    disabled={fullscreenZoom >= 500}
                    className="h-8 w-8 p-0"
                    title="Zoom In"
                  >
                    <ZoomIn className="h-4 w-4" />
                  </Button>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={handleFullscreenZoomReset}
                    className="h-8 w-8 p-0"
                    title="Reset Zoom"
                  >
                    <RotateCcw className="h-4 w-4" />
                  </Button>
                </div>
                
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => setIsFullscreen(false)}
                  className="hover:bg-gray-100 dark:hover:bg-gray-800"
                >
                  ✕
                </Button>
              </div>
            </div>

            {/* Fullscreen Content */}
            <div className="flex-1 p-6 overflow-auto bg-gray-50 dark:bg-gray-900">
              <div 
                className={`flex justify-center items-center h-full ${
                  fullscreenZoom > 100 ? "cursor-move" : "cursor-default"
                }`}
                onMouseDown={handleFullscreenMouseDown}
                onMouseMove={handleFullscreenMouseMove}
                onMouseUp={handleFullscreenMouseUp}
                onMouseLeave={handleFullscreenMouseLeave}
                onWheel={handleFullscreenWheel}
              >
                <div
                  ref={fullscreenVegaRef}
                  className="flex justify-center items-center max-w-full max-h-full transition-transform duration-200 ease-in-out"
                  style={{
                    transform: `scale(${fullscreenZoom / 100}) translate(${fullscreenPosition.x / (fullscreenZoom / 100)}px, ${fullscreenPosition.y / (fullscreenZoom / 100)}px)`,
                    transformOrigin: "center",
                    userSelect: fullscreenZoom > 100 ? "none" : "auto",
                    pointerEvents: fullscreenZoom > 100 ? "none" : "auto",
                    transition: isFullscreenDragging ? "none" : "transform 0.15s cubic-bezier(0.4, 0, 0.2, 1)"
                  }}
                  // Chart will be rendered directly into fullscreenVegaRef
                />
              </div>
            </div>

            {caption && (
              <div className="p-4 border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-800">
                <p className="text-center text-gray-600 dark:text-gray-300 italic">{caption}</p>
              </div>
            )}
          </div>
        </div>
      )}
    </>
  )
}