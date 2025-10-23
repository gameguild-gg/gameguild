"use client"

import { useEffect, useRef, useState } from "react"
import { BarChart3, AlertCircle, ZoomIn, ZoomOut, RotateCcw, Maximize2 } from "lucide-react"
import { Button } from "@/components/ui/button"

interface PreviewVegaLiteProps {
  node: {
    data: {
      spec: string
      title?: string
      caption?: string
      theme?: string
      layout?: "square" | "rectangular"
      size?: number
    }
  }
}

export function PreviewVegaLite({ node }: PreviewVegaLiteProps) {
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string>("")
  const [zoom, setZoom] = useState(100)
  const [isFullscreen, setIsFullscreen] = useState(false)
  const [position, setPosition] = useState({ x: 0, y: 0 })
  const [isDragging, setIsDragging] = useState(false)
  const [dragStart, setDragStart] = useState({ x: 0, y: 0 })
  const [lastPosition, setLastPosition] = useState({ x: 0, y: 0 })
  const vegaRef = useRef<HTMLDivElement>(null)
  const fullscreenVegaRef = useRef<HTMLDivElement>(null)
  const containerRef = useRef<HTMLDivElement>(null)

  console.log("Preview VegaLite: Full node structure:", JSON.stringify(node, null, 2))
  
  const { spec, title, caption, theme, layout, size } = node.data

  console.log("Preview VegaLite: Extracted data:", { 
    specLength: spec?.length || 0, 
    title, 
    caption, 
    theme, 
    layout, 
    size,
    specPreview: spec?.substring(0, 200) + "..."
  })

  const renderChart = async (container: HTMLElement, spec: string | any) => {
    if (!container || !spec) {
      console.log("Preview VegaLite: Missing container or spec")
      return
    }

    console.log("Preview VegaLite renderChart: Starting with container:", !!container, "spec type:", typeof spec)
    console.log("Preview VegaLite: Raw spec:", typeof spec === 'string' ? spec.substring(0, 500) + "..." : JSON.stringify(spec, null, 2))

    try {
      // Parse the specification
      let parsedSpec
      try {
        // Handle case where spec might already be an object
        if (typeof spec === 'object') {
          parsedSpec = spec
          console.log("Preview VegaLite: Spec is already an object")
        } else {
          parsedSpec = JSON.parse(spec)
          console.log("Preview VegaLite: Parsed spec successfully")
        }
        console.log("Preview VegaLite: Final parsed spec:", JSON.stringify(parsedSpec, null, 2))
      } catch (parseError) {
        console.error("Preview VegaLite: JSON parse error:", parseError)
        throw new Error("Invalid JSON specification")
      }

      // Validate that we have required fields
      if (!parsedSpec.data && !parsedSpec.datasets) {
        console.error("Preview VegaLite: No data field in spec")
        throw new Error("Vega-Lite spec missing data field")
      }

      if (!parsedSpec.mark && !parsedSpec.layer) {
        console.error("Preview VegaLite: No mark field in spec")
        throw new Error("Vega-Lite spec missing mark field")
      }

      // Apply theme if specified
      if (theme && theme !== "default") {
        parsedSpec.config = parsedSpec.config || {}
        parsedSpec.config.theme = theme
        console.log("Preview VegaLite: Applied theme:", theme)
      }

      // Apply layout specific configurations
      if (layout === "square") {
        parsedSpec.width = 400
        parsedSpec.height = 400
        console.log("Preview VegaLite: Applied square layout to spec")
      } else {
        // Rectangular layout - use specific dimensions
        parsedSpec.width = 800
        parsedSpec.height = 300
        console.log("Preview VegaLite: Applied rectangular layout to spec (800x300)")
      }

      // Try to use Vega-Lite if available, otherwise show placeholder
      try {
        // This will be caught if vega-lite is not installed
        console.log("Preview VegaLite: Attempting to import Vega-Lite...")
        const vegaLiteImport = await import("vega-lite" as any).catch(() => null)
        const vegaImport = await import("vega" as any).catch(() => null)
        
        if (!vegaLiteImport || !vegaImport) {
          console.log("Preview VegaLite: Vega-Lite not available, showing placeholder")
          throw new Error("Vega-Lite not available")
        }

        console.log("Preview VegaLite: Vega-Lite imported successfully, compiling spec...")
        // Compile Vega-Lite spec to Vega spec
        const vegaSpec = vegaLiteImport.compile(parsedSpec).spec
        console.log("Preview VegaLite: Spec compiled successfully")

        // Create a new view and render
        console.log("Preview VegaLite: Creating Vega view...")
        
        // Clear container first
        container.innerHTML = ""
        
        const view = new vegaImport.View(vegaImport.parse(vegaSpec), {
          renderer: "svg"
        })

        console.log("Preview VegaLite: Initializing view with container...")
        view.initialize(container)
        
        console.log("Preview VegaLite: Running view...")
        try {
          await view.runAsync()
          console.log("Preview VegaLite: View ran successfully")
          
          // Force update to ensure rendering
          view.hover()
          
        } catch (runError) {
          console.error("Preview VegaLite: Error during view.runAsync():", runError)
          throw runError
        }
        
        // Wait a bit for the DOM to be updated
        setTimeout(async () => {
          console.log("Preview VegaLite: Checking DOM after render...")
          console.log("Preview VegaLite: Current layout:", layout)
          console.log("Preview VegaLite: Container children count:", container.children.length)
          console.log("Preview VegaLite: Container innerHTML length:", container.innerHTML.length)
          
          if (container.children.length === 0) {
            console.error("Preview VegaLite: No children in container after render!")
            console.log("Preview VegaLite: Trying alternative rendering approach...")
            
            // Try alternative approach: get SVG directly from view
            try {
              console.log("Preview VegaLite: Trying toSVG() method...")
              const svgString = await view.toSVG()
              console.log("Preview VegaLite: Got SVG string, length:", svgString.length)
              container.innerHTML = svgString
              
              // Force display of the inserted SVG
              if (container.firstElementChild) {
                const svgElement = container.firstElementChild as HTMLElement
                svgElement.style.display = "block"
                
                if (layout === "square") {
                  svgElement.style.width = "400px"
                  svgElement.style.height = "400px"
                  svgElement.style.margin = "0 auto"
                  console.log("Preview VegaLite: Applied square styles to SVG")
                } else {
                  // Rectangular layout
                  svgElement.style.width = "100%"
                  svgElement.style.height = "auto"
                  svgElement.style.maxWidth = "100%"
                  console.log("Preview VegaLite: Applied rectangular styles to SVG")
                }
                
                console.log("Preview VegaLite: Applied styles to inserted SVG")
              }
            } catch (svgError) {
              console.error("Preview VegaLite: Error getting SVG:", svgError)
              
              // Try canvas approach as last resort
              try {
                console.log("Preview VegaLite: Trying canvas approach...")
                const canvas = await view.toCanvas()
                console.log("Preview VegaLite: Got canvas:", !!canvas)
                container.innerHTML = ""
                container.appendChild(canvas)
                
                // Apply styles to canvas
                canvas.style.display = "block"
                canvas.style.maxWidth = "100%"
                canvas.style.height = "auto"
                
                if (layout === "square") {
                  canvas.style.width = "400px"
                  canvas.style.height = "400px"
                  canvas.style.margin = "0 auto"
                } else {
                  // Rectangular layout
                  canvas.style.width = "800px"
                  canvas.style.height = "300px"
                  canvas.style.maxWidth = "100%"
                }
                
                console.log("Preview VegaLite: Canvas inserted and styled")
              } catch (canvasError) {
                console.error("Preview VegaLite: Canvas approach failed:", canvasError)
                
                // Last resort: try to re-initialize
                try {
                  view.initialize(container)
                  view.runAsync().then(() => {
                    console.log("Preview VegaLite: Retry render completed")
                  }).catch((err: any) => {
                    console.error("Preview VegaLite: Retry render failed:", err)
                  })
                } catch (initError) {
                  console.error("Preview VegaLite: Re-initialization failed:", initError)
                }
              }
            }
          }
          
          // Apply centering styles for square layout
          if (container.firstElementChild) {
            const svgElement = container.firstElementChild as HTMLElement
            svgElement.style.display = "block"
            svgElement.style.maxWidth = "100%"
            svgElement.style.height = "auto"
            
            console.log("Preview VegaLite: Applying styles for layout:", layout)
            
            if (layout === "square") {
              svgElement.style.width = "400px"
              svgElement.style.height = "400px"
              svgElement.style.margin = "0 auto"
              console.log("Preview VegaLite: Applied square layout centering")
            } else {
              // Rectangular layout
              svgElement.style.width = "800px"
              svgElement.style.height = "auto"
              svgElement.style.maxWidth = "100%"
              console.log("Preview VegaLite: Applied rectangular layout styling")
            }
            
            // Force minimum dimensions if SVG has no size
            const rect = svgElement.getBoundingClientRect()
            console.log("Preview VegaLite: SVG rect:", rect)
            if (rect.width === 0 || rect.height === 0) {
              if (layout === "square") {
                svgElement.style.width = "400px"
                svgElement.style.height = "400px"
              } else {
                svgElement.style.width = "800px"
                svgElement.style.height = "300px"
              }
              console.log("Preview VegaLite: Applied fallback dimensions for layout:", layout)
            }
            
            console.log("Preview VegaLite: Applied visibility styles to SVG")
            console.log("Preview VegaLite: SVG element:", svgElement.tagName)
            console.log("Preview VegaLite: SVG dimensions:", svgElement.getBoundingClientRect())
            console.log("Preview VegaLite: SVG innerHTML length:", svgElement.innerHTML.length)
          } else {
            console.log("Preview VegaLite: No SVG element found in container!")
          }
        }, 200)
      } catch (vegaError: any) {
        console.log("Preview VegaLite: Vega error, showing placeholder:", vegaError.message)
        // If vega-lite is not available, show placeholder
        container.innerHTML = `
          <div style="
            display: flex; 
            align-items: center; 
            justify-content: center; 
            height: 300px; 
            background: #f8f9fa; 
            border: 2px dashed #dee2e6; 
            border-radius: 8px;
            flex-direction: column;
            color: #6c757d;
            font-family: system-ui, -apple-system, sans-serif;
          ">
            <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M3 3v18h18"/>
              <path d="m19 9-5 5-4-4-3 3"/>
            </svg>
            <h3 style="margin: 16px 0 8px 0; font-size: 16px; font-weight: 600;">Vega-Lite Chart</h3>
            <p style="margin: 0; font-size: 14px; text-align: center; max-width: 300px;">
              ${title || "Interactive Data Visualization"}
            </p>
            <div style="margin-top: 12px; padding: 8px 12px; background: #e9ecef; border-radius: 4px; font-family: monospace; font-size: 12px;">
              Vega-Lite Preview (${vegaError.message})
            </div>
          </div>
        `
      }
    } catch (err: any) {
      console.error("Preview VegaLite: Error rendering chart:", err)
      throw err
    }
  }

  // Render chart when component mounts or data changes
  useEffect(() => {
    const loadChart = async () => {
      console.log("Preview VegaLite useEffect: spec exists?", !!spec, "vegaRef exists?", !!vegaRef.current)
      
      if (!spec || !vegaRef.current) {
        console.log("Preview VegaLite: Skipping render - missing spec or container")
        return
      }

      setIsLoading(true)
      setError("")

      try {
        vegaRef.current.innerHTML = ""
        console.log("Preview VegaLite: Attempting to render chart with spec:", spec.substring(0, 100) + "...")
        
        // Try to render with the spec as-is first, then try parsing
        let actualSpec = spec
        
        // If spec is a string, try to parse it
        if (typeof spec === 'string') {
          try {
            const parsed = JSON.parse(spec)
            actualSpec = parsed
            console.log("Preview VegaLite: Successfully parsed string spec")
          } catch (e) {
            console.log("Preview VegaLite: Spec is not valid JSON, using as-is")
          }
        }
        
        await renderChart(vegaRef.current, actualSpec)
        console.log("Preview VegaLite: Chart rendered successfully")
      } catch (err: any) {
        console.error("Preview VegaLite: Error rendering chart:", err)
        setError(err.message || "Failed to render chart")
        if (vegaRef.current) {
          vegaRef.current.innerHTML = `
            <div class="text-red-500 p-4 border border-red-300 rounded bg-red-50 dark:bg-red-900/20 dark:border-red-700">
              <div class="font-medium">Error rendering chart:</div>
              <div class="text-sm mt-1">${err.message || "Unknown error"}</div>
            </div>
          `
        }
      } finally {
        setIsLoading(false)
      }
    }

    loadChart()
  }, [spec, theme, layout])

  // Render fullscreen chart when fullscreen is toggled
  useEffect(() => {
    const loadFullscreenChart = async () => {
      if (isFullscreen && fullscreenVegaRef.current && spec) {
        try {
          fullscreenVegaRef.current!.innerHTML = ""
          await renderChart(fullscreenVegaRef.current!, spec)
        } catch (err: any) {
          if (fullscreenVegaRef.current) {
            fullscreenVegaRef.current.innerHTML = `
              <div class="text-red-500 p-4 border border-red-300 rounded bg-red-50">
                <div class="font-medium">Error rendering chart:</div>
                <div class="text-sm mt-1">${err.message || "Unknown error"}</div>
              </div>
            `
          }
        }
      }
    }

    loadFullscreenChart()
  }, [isFullscreen, spec, theme, layout])

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
      <div className="my-4 p-4 border border-gray-200 rounded-lg bg-gray-50 dark:bg-gray-800 dark:border-gray-700">
        <div className="text-center text-gray-500 dark:text-gray-400">
          <BarChart3 className="h-12 w-12 mx-auto mb-2" />
          <p>No Vega-Lite specification provided</p>
        </div>
      </div>
    )
  }

  return (
    <>
      <div className="relative group my-4" style={{ width: `${size || 100}%` }}>
        {/* Chart Container */}
        <div className="border rounded-lg bg-white dark:bg-gray-800 p-4 shadow-sm relative flex flex-col items-center">
          {title && <h3 className="text-lg font-semibold mb-2 text-center dark:text-white">{title}</h3>}

          {/* Zoom Controls */}
          <div className="absolute top-2 right-2 z-10 flex items-center gap-1 bg-white/90 dark:bg-gray-800/90 backdrop-blur-sm rounded-lg p-1 opacity-0 group-hover:opacity-100 transition-opacity duration-200">
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
            <Button
              variant="ghost"
              size="sm"
              onClick={handleFullscreen}
              className="h-8 w-8 p-0 relative z-20"
              title="Fullscreen"
            >
              <Maximize2 className="h-4 w-4" />
            </Button>
          </div>

          {/* Chart Content */}
          <div 
            ref={containerRef}
            className={`w-full flex justify-center items-center relative overflow-hidden ${
              layout === "square" ? "min-h-[450px]" : "min-h-[350px]"
            } ${zoom > 100 ? "cursor-move" : "cursor-default"}`}
            onMouseDown={handleMouseDown}
            onMouseMove={handleMouseMove}
            onMouseUp={handleMouseUp}
            onMouseLeave={handleMouseLeave}
            onWheel={handleWheel}
            style={{ 
              position: "relative",
              zIndex: 1
            }}
          >
            {isLoading ? (
              <div className="text-gray-500 dark:text-gray-400">Rendering chart...</div>
            ) : error ? (
              <div className="text-red-500 p-4 border border-red-300 rounded bg-red-50 dark:bg-red-900/20 dark:border-red-700 max-w-full">
                <div className="font-medium">Error rendering chart:</div>
                <div className="text-sm mt-1">{error}</div>
              </div>
            ) : spec ? (
              <div
                ref={vegaRef}
                className="flex justify-center items-center w-full bg-white border border-gray-200 rounded"
                style={{
                  transform: `scale(${zoom / 100}) translate(${position.x / (zoom / 100)}px, ${position.y / (zoom / 100)}px)`,
                  transformOrigin: "center",
                  minHeight: layout === "square" ? "400px" : "300px",
                  minWidth: "200px",
                  position: "relative",
                  zIndex: 0,
                  userSelect: zoom > 100 ? "none" : "auto",
                  pointerEvents: zoom > 100 ? "none" : "auto",
                  transition: isDragging ? "none" : "transform 0.15s cubic-bezier(0.4, 0, 0.2, 1)"
                }}
                // Chart will be rendered directly into vegaRef
              />
            ) : (
              <div className="text-gray-500 dark:text-gray-400">No chart specification provided</div>
            )}
          </div>

          {caption && (
            <p className="text-sm text-gray-600 dark:text-gray-300 mt-2 text-center italic">{caption}</p>
          )}
        </div>
      </div>

      {/* Fullscreen Modal */}
      {isFullscreen && (
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
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setIsFullscreen(false)}
                className="hover:bg-gray-100 dark:hover:bg-gray-800"
              >
                ✕
              </Button>
            </div>

            {/* Fullscreen Content */}
            <div className="flex-1 p-6 overflow-auto bg-gray-50 dark:bg-gray-900">
              <div className="flex justify-center items-center h-full">
                <div
                  ref={fullscreenVegaRef}
                  className="flex justify-center items-center max-w-full max-h-full"
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