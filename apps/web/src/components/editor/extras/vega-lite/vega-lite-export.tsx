"use client"

import { Button } from "@/components/ui/button"
import { Download } from "lucide-react"

interface VegaLiteExportProps {
  spec: string
  theme?: string
  layout?: "square" | "rectangular"
  title?: string
  isValid: boolean
  disabled?: boolean
  className?: string
}

export function VegaLiteExport({ 
  spec, 
  theme = "default", 
  layout = "rectangular", 
  title, 
  isValid, 
  disabled = false,
  className = "" 
}: VegaLiteExportProps) {
  
  const isDisabled = disabled || !spec.trim() || !isValid

  const handleDownloadSVG = async () => {
    if (isDisabled) return

    try {
      // Parse the specification
      let parsedSpec
      try {
        parsedSpec = typeof spec === 'string' ? JSON.parse(spec) : spec
      } catch (parseError) {
        console.error("Invalid JSON specification for download")
        return
      }

      // Apply theme if specified
      if (theme && theme !== "default") {
        parsedSpec.config = parsedSpec.config || {}
        parsedSpec.config.theme = theme
      }

      // Apply layout settings
      if (layout === "square") {
        parsedSpec.width = 400
        parsedSpec.height = 400
      } else if (layout === "rectangular") {
        parsedSpec.width = "container"
        parsedSpec.height = 300
      }

      // Dynamic import of Vega-Lite and Vega
      const vegaLiteImport = await import("vega-lite" as any).catch(() => null)
      const vegaImport = await import("vega" as any).catch(() => null)
      
      if (!vegaLiteImport || !vegaImport) {
        console.error("Vega-Lite not available for download")
        return
      }

      // Compile Vega-Lite spec to Vega spec
      const vegaSpec = vegaLiteImport.compile(parsedSpec).spec

      // Create a new view for SVG generation
      const view = new vegaImport.View(vegaImport.parse(vegaSpec))
        .renderer("svg")
        .initialize()

      await view.runAsync()

      // Get SVG string
      const svgString = await view.toSVG()

      // Create blob and download
      const blob = new Blob([svgString], { type: 'image/svg+xml' })
      const url = URL.createObjectURL(blob)
      
      // Create download link
      const link = document.createElement('a')
      link.href = url
      link.download = `${title || 'vega-lite-chart'}.svg`
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      
      // Clean up
      URL.revokeObjectURL(url)

      console.log("SVG downloaded successfully")
    } catch (err: any) {
      console.error("Error downloading SVG:", err)
    }
  }

  const handleDownloadPNG = async () => {
    if (isDisabled) return

    try {
      // Parse the specification
      let parsedSpec
      try {
        parsedSpec = typeof spec === 'string' ? JSON.parse(spec) : spec
      } catch (parseError) {
        console.error("Invalid JSON specification for download")
        return
      }

      // Apply theme if specified
      if (theme && theme !== "default") {
        parsedSpec.config = parsedSpec.config || {}
        parsedSpec.config.theme = theme
      }

      // Apply layout settings with higher resolution for PNG
      if (layout === "square") {
        parsedSpec.width = 800  // Double resolution for PNG
        parsedSpec.height = 800
      } else if (layout === "rectangular") {
        parsedSpec.width = 1200
        parsedSpec.height = 600
      }

      // Dynamic import of Vega-Lite and Vega
      const vegaLiteImport = await import("vega-lite" as any).catch(() => null)
      const vegaImport = await import("vega" as any).catch(() => null)
      
      if (!vegaLiteImport || !vegaImport) {
        console.error("Vega-Lite not available for download")
        return
      }

      // Compile Vega-Lite spec to Vega spec
      const vegaSpec = vegaLiteImport.compile(parsedSpec).spec

      // Create a new view for PNG generation
      const view = new vegaImport.View(vegaImport.parse(vegaSpec))
        .renderer("canvas")
        .initialize()

      await view.runAsync()

      // Get PNG as canvas and convert to blob
      const canvas = await view.toCanvas()
      canvas.toBlob((blob: Blob | null) => {
        if (blob) {
          const url = URL.createObjectURL(blob)
          
          // Create download link
          const link = document.createElement('a')
          link.href = url
          link.download = `${title || 'vega-lite-chart'}.png`
          document.body.appendChild(link)
          link.click()
          document.body.removeChild(link)
          
          // Clean up
          URL.revokeObjectURL(url)
          
          console.log("PNG downloaded successfully")
        }
      }, 'image/png')

    } catch (err: any) {
      console.error("Error downloading PNG:", err)
    }
  }

  return (
    <div className={`flex items-center gap-2 ${className}`}>
      <Button
        variant="outline"
        size="sm"
        onClick={handleDownloadSVG}
        disabled={isDisabled}
        className="flex items-center gap-2 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800"
        title="Download as SVG"
      >
        <Download className="h-4 w-4" />
        SVG
      </Button>
      <Button
        variant="outline"
        size="sm"
        onClick={handleDownloadPNG}
        disabled={isDisabled}
        className="flex items-center gap-2 border-gray-300 dark:border-gray-600 hover:bg-gray-50 dark:hover:bg-gray-800"
        title="Download as PNG"
      >
        <Download className="h-4 w-4" />
        PNG
      </Button>
    </div>
  )
}