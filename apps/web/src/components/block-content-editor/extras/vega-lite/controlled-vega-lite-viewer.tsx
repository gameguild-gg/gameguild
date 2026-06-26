"use client"

import { useEffect, useState, useRef } from "react"
import { VegaLiteViewer } from "@/components/block-content-editor/extras/vega-lite/vega-lite-viewer"
import { loadCsvDataIntoSpec } from "@/components/block-content-editor/extras/vega-lite/vega-csv-loader"

interface ControlledVegaLiteViewerProps {
  spec: string
  layout?: "square" | "rectangular"
  themeLight?: string
  themeDark?: string
  title?: string
  caption?: string
  size?: number
  showControls?: boolean
  allowFullscreen?: boolean
  className?: string
  updateTrigger: number // When this changes, update the chart
  data?: Record<string, string> // Data files for inline loading (CSV/JSON)
}

export function ControlledVegaLiteViewer({ 
  spec,
  layout = "rectangular", 
  themeLight = "default",
  themeDark = "dark",
  title,
  caption,
  size = 100,
  showControls = true,
  allowFullscreen = true,
  className = "",
  updateTrigger,
  data = {}
}: ControlledVegaLiteViewerProps) {
  const [currentSpec, setCurrentSpec] = useState(spec)
  const [currentLayout, setCurrentLayout] = useState(layout)
  const [currentThemeLight, setCurrentThemeLight] = useState(themeLight)
  const [currentThemeDark, setCurrentThemeDark] = useState(themeDark)
  const [currentTitle, setCurrentTitle] = useState(title)
  const [currentCaption, setCurrentCaption] = useState(caption)
  const [containerHeight, setContainerHeight] = useState<number | null>(null)
  const previousUpdateTrigger = useRef(updateTrigger)
  const containerRef = useRef<HTMLDivElement>(null)

  // Process spec with data files (CSV and JSON)
  const processedSpec = (() => {
    try {
      if (Object.keys(data).length > 0) {
        const processed = loadCsvDataIntoSpec(currentSpec, data)
        return JSON.stringify(processed)
      }
      return currentSpec
    } catch (error) {
      console.error('Erro ao processar dados:', error)
      return currentSpec
    }
  })()

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
      setCurrentThemeLight(themeLight)
      setCurrentThemeDark(themeDark)
      setCurrentTitle(title)
      setCurrentCaption(caption)
      previousUpdateTrigger.current = updateTrigger
    }
  }, [updateTrigger, spec, layout, themeLight, themeDark, title, caption, containerHeight])

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
        spec={processedSpec}
        layout={currentLayout}
        themeLight={currentThemeLight}
        themeDark={currentThemeDark}
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