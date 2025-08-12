"use client"

import { useEffect, useRef, useState } from "react"
import type { MermaidData } from "@/components/editor/nodes/mermaid-node"

interface PreviewMermaidProps {
  data: MermaidData
}

export function PreviewMermaid({ data }: PreviewMermaidProps) {
  const [svgContent, setSvgContent] = useState<string>("")
  const [error, setError] = useState<string>("")
  const [isLoading, setIsLoading] = useState(false)
  const mermaidRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const renderDiagram = async () => {
      if (!data.code) return

      setIsLoading(true)
      setError("")

      try {
        // Dynamic import to ensure mermaid is loaded
        const mermaid = (await import("mermaid")).default

        // Initialize mermaid with proper configuration
        mermaid.initialize({
          startOnLoad: false,
          theme: "default",
          securityLevel: "loose",
          fontFamily: "inherit",
          flowchart: {
            useMaxWidth: true,
            htmlLabels: true,
          },
        })

        // Generate unique ID for this diagram
        const id = `mermaid-preview-${Date.now()}`

        // Render the diagram
        const { svg } = await mermaid.render(id, data.code)
        setSvgContent(svg)
        setError("")
      } catch (err: any) {
        console.error("Error rendering Mermaid diagram:", err)
        setError(err?.message || "Failed to render diagram")
        setSvgContent("")
      } finally {
        setIsLoading(false)
      }
    }

    renderDiagram()
  }, [data.code])

  if (isLoading) {
    return (
      <div className="my-4 p-4 border rounded-lg bg-gray-50">
        <div className="text-gray-600 text-center">Rendering diagram...</div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="my-4 p-4 border border-red-300 rounded-lg bg-red-50">
        <div className="text-red-700 font-medium">Mermaid Diagram Error</div>
        <div className="text-red-600 text-sm mt-1">{error}</div>
      </div>
    )
  }

  return (
    <div className="my-4" style={{ width: `${data.size || 100}%` }}>
      <div className="border rounded-lg bg-white p-4 shadow-sm">
        {data.title && <h3 className="text-lg font-semibold mb-2 text-center">{data.title}</h3>}

        <div
          ref={mermaidRef}
          className="flex justify-center items-center min-h-[200px]"
          dangerouslySetInnerHTML={{ __html: svgContent }}
        />

        {data.caption && <p className="text-sm text-gray-600 mt-2 text-center italic">{data.caption}</p>}
      </div>
    </div>
  )
}
