"use client"

import { useEffect, useRef, useState } from "react"
import type { MermaidData } from "@/components/block-content-editor/nodes/mermaid-node"
import { AlertCircle } from "lucide-react"

interface ControlledMermaidViewerProps {
  data: MermaidData
  zoom?: number
  className?: string
  showError?: boolean
  showLoading?: boolean
  onRenderSuccess?: (svg: string) => void
  onRenderError?: (error: string) => void
}

export function ControlledMermaidViewer({
  data,
  zoom = 100,
  className = "",
  showError = true,
  showLoading = true,
  onRenderSuccess,
  onRenderError,
}: ControlledMermaidViewerProps) {
  const [svgContent, setSvgContent] = useState<string>("")
  const [error, setError] = useState<string>("")
  const [isLoading, setIsLoading] = useState(false)
  const mermaidRef = useRef<HTMLDivElement>(null)
  const onRenderSuccessRef = useRef(onRenderSuccess)
  const onRenderErrorRef = useRef(onRenderError)

  // Update refs when callbacks change
  useEffect(() => {
    onRenderSuccessRef.current = onRenderSuccess
    onRenderErrorRef.current = onRenderError
  }, [onRenderSuccess, onRenderError])

  useEffect(() => {
    const renderDiagram = async () => {
      if (!data.code || !data.code.trim()) {
        setSvgContent("")
        setError("")
        return
      }

      setIsLoading(true)
      setError("")

      try {
        const mermaid = (await import("mermaid")).default

        await new Promise<void>((resolve, reject) => {
          try {
            mermaid.initialize({
              startOnLoad: false,
              theme: "default",
              securityLevel: "loose",
              fontFamily: "inherit",
              flowchart: {
                useMaxWidth: true,
                htmlLabels: true,
              },
              logLevel: "error",
              suppressErrorRendering: true,
            })
            resolve()
          } catch (initError) {
            reject(initError)
          }
        })

        const id = `mermaid-viewer-${Date.now()}-${Math.random().toString(36).substring(2, 9)}`

        const renderPromise = mermaid.render(id, data.code)
        const timeoutPromise = new Promise((_, reject) => {
          setTimeout(() => reject(new Error("Rendering timeout")), 10000)
        })

        const { svg } = (await Promise.race([renderPromise, timeoutPromise])) as any

        if (!svg) {
          throw new Error("No SVG content generated")
        }

        setSvgContent(svg)
        setError("")
        onRenderSuccessRef.current?.(svg)
      } catch (err: any) {
        console.error("Error rendering Mermaid diagram:", err)

        let errorMessage = "Failed to render diagram"

        if (err?.message) {
          if (err.message.includes("Parse error")) {
            errorMessage = "Syntax error in diagram code"
          } else if (err.message.includes("timeout")) {
            errorMessage = "Diagram rendering timed out"
          } else if (err.message.includes("Empty")) {
            errorMessage = "No diagram code provided"
          } else {
            errorMessage = `Error: ${err.message}`
          }
        }

        setError(errorMessage)
        setSvgContent("")
        onRenderErrorRef.current?.(errorMessage)
      } finally {
        setIsLoading(false)
      }
    }

    renderDiagram()
  }, [data.code])

  if (isLoading && showLoading) {
    return (
      <div className={`flex items-center justify-center p-4 ${className}`}>
        <div className="text-gray-600 dark:text-gray-300 flex items-center gap-2">
          <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-gray-600 dark:border-gray-300"></div>
          <span>Rendering diagram...</span>
        </div>
      </div>
    )
  }

  if (error && showError) {
    return (
      <div className={`p-4 border border-red-300 dark:border-red-700 rounded-lg bg-red-50 dark:bg-red-950/30 ${className}`}>
        <div className="text-red-700 dark:text-red-400 font-medium flex items-center gap-2">
          <AlertCircle className="h-5 w-5" />
          <span>Mermaid Diagram Error</span>
        </div>
        <div className="text-red-600 dark:text-red-300 text-sm mt-1">{error}</div>
      </div>
    )
  }

  if (!svgContent) {
    return (
      <div className={`flex items-center justify-center p-4 text-gray-500 dark:text-gray-400 ${className}`}>
        <span>No diagram to display</span>
      </div>
    )
  }

  return (
    <div className={className}>
      <div
        ref={mermaidRef}
        className="flex justify-center items-center transition-transform duration-200 ease-in-out"
        style={{
          transform: `scale(${zoom / 100})`,
          transformOrigin: "center",
        }}
        dangerouslySetInnerHTML={{ __html: svgContent }}
        onError={(e) => {
          console.error("SVG display error:", e)
          setError("Error displaying rendered diagram")
        }}
      />
    </div>
  )
}
