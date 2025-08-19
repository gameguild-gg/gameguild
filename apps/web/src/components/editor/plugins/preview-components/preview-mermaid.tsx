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
      if (!data.code) {
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

        const id = `mermaid-preview-${Date.now()}`

        if (!data.code.trim()) {
          throw new Error("Empty diagram code")
        }

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
      } finally {
        setIsLoading(false)
      }
    }

    try {
      renderDiagram()
    } catch (err) {
      console.error("Error in preview renderDiagram:", err)
      setError("Failed to initialize diagram rendering")
      setIsLoading(false)
    }
  }, [data.code])

  if (isLoading) {
    return (
      <div className="my-4 p-4 border rounded-lg bg-gray-50 dark:bg-gray-800">
        <div className="text-gray-600 dark:text-gray-300 text-center flex items-center justify-center gap-2">
          <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-gray-600"></div>
          Rendering diagram...
        </div>
      </div>
    )
  }

  if (error) {
    return (
      <div className="my-4 p-4 border border-red-300 rounded-lg bg-red-50 dark:bg-red-900/20 dark:border-red-700">
        <div className="text-red-700 dark:text-red-400 font-medium flex items-center gap-2">
          <svg className="h-5 w-5" fill="currentColor" viewBox="0 0 20 20">
            <path
              fillRule="evenodd"
              d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z"
              clipRule="evenodd"
            />
          </svg>
          Mermaid Diagram Error
        </div>
        <div className="text-red-600 dark:text-red-300 text-sm mt-1">{error}</div>
      </div>
    )
  }

  return (
    <div className="my-4" style={{ width: `${data.size || 100}%` }}>
      <div className="border rounded-lg bg-white dark:bg-gray-800 p-4 shadow-sm">
        {data.title && <h3 className="text-lg font-semibold mb-2 text-center dark:text-white">{data.title}</h3>}

        <div
          ref={mermaidRef}
          className="flex justify-center items-center min-h-[200px]"
          dangerouslySetInnerHTML={{ __html: svgContent }}
          onError={(e) => {
            console.error("SVG display error:", e)
            setError("Error displaying rendered diagram")
          }}
        />

        {data.caption && (
          <p className="text-sm text-gray-600 dark:text-gray-300 mt-2 text-center italic">{data.caption}</p>
        )}
      </div>
    </div>
  )
}
