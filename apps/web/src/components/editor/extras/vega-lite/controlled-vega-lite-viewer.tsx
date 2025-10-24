"use client"

import { useEffect, useState, useRef } from "react"
import { VegaLiteViewer } from "@/components/ui/vega-lite-viewer"

interface ControlledVegaLiteViewerProps {
  spec: string
  layout?: "square" | "rectangular"
  theme?: string
  title?: string
  caption?: string
  size?: number
  showControls?: boolean
  allowFullscreen?: boolean
  className?: string
  updateTrigger: number // When this changes, update the chart
}

export function ControlledVegaLiteViewer({ 
  spec,
  layout = "rectangular", 
  theme = "default",
  title,
  caption,
  size = 100,
  showControls = true,
  allowFullscreen = true,
  className = "",
  updateTrigger
}: ControlledVegaLiteViewerProps) {
  const [currentSpec, setCurrentSpec] = useState(spec)
  const [currentLayout, setCurrentLayout] = useState(layout)
  const [currentTheme, setCurrentTheme] = useState(theme)
  const [currentTitle, setCurrentTitle] = useState(title)
  const [currentCaption, setCurrentCaption] = useState(caption)
  const [containerHeight, setContainerHeight] = useState<number | null>(null)
  const previousUpdateTrigger = useRef(updateTrigger)
  const containerRef = useRef<HTMLDivElement>(null)

  // Capture initial container height
  useEffect(() => {
    const timer = setTimeout(() => {
      if (containerRef.current && containerHeight === null) {
        const height = containerRef.current.offsetHeight
        if (height > 0) {
          setContainerHeight(height)
        }
      }
    }, 500)

    return () => clearTimeout(timer)
  }, [containerHeight])

  // Update internal state only when updateTrigger changes
  useEffect(() => {
    if (updateTrigger !== previousUpdateTrigger.current) {
      // Preserve current container height during update
      if (containerRef.current && containerHeight === null) {
        setContainerHeight(containerRef.current.offsetHeight)
      }
      
      // Instant update - no delays or transitions
      setCurrentSpec(spec)
      setCurrentLayout(layout)
      setCurrentTheme(theme)
      setCurrentTitle(title)
      setCurrentCaption(caption)
      previousUpdateTrigger.current = updateTrigger
    }
  }, [updateTrigger, spec, layout, theme, title, caption, containerHeight])

  return (
    <div 
      ref={containerRef}
      className="relative"
      style={{ 
        height: containerHeight ? `${containerHeight}px` : 'auto',
        minHeight: layout === "square" ? "500px" : "400px"
      }}
    >
      <VegaLiteViewer 
        spec={currentSpec}
        layout={currentLayout}
        theme={currentTheme}
        title={currentTitle}
        caption={currentCaption}
        size={size}
        showControls={showControls}
        allowFullscreen={allowFullscreen}
        className={className}
      />
    </div>
  )
}