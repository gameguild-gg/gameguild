"use client"

import { useRef, useState, useEffect } from "react"
import type { SerializedHTMLNode } from "../../nodes/html-node"
import DOMPurify from "dompurify"

export function PreviewHTML({ node }: { node: SerializedHTMLNode }) {
  const iframeRef = useRef<HTMLIFrameElement>(null)
  const [iframeHeight, setIframeHeight] = useState(0)

  const getPreviewContent = () => {
    const sanitizedHtml = DOMPurify.sanitize(node.data.content)
    return `
    <!DOCTYPE html>
    <html>
      <head>
        <base target="_blank">
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1">
      </head>
      <body>
        ${sanitizedHtml}
      </body>
    </html>
  `
  }

  useEffect(() => {
    const iframe = iframeRef.current
    if (!iframe) return

    const handleLoad = () => {
      const height = iframe.contentWindow?.document.documentElement.scrollHeight || 0
      setIframeHeight(height)
    }

    iframe.addEventListener("load", handleLoad)
    return () => iframe.removeEventListener("load", handleLoad)
  }, [])

  return (
    <div className="my-0">
      <iframe
        ref={iframeRef}
        srcDoc={getPreviewContent()}
        className="w-full rounded-md border"
        style={{ height: iframeHeight ? `${iframeHeight + 2}px` : "0" }}
        sandbox="allow-scripts allow-popups allow-same-origin"
        title="HTML Preview"
      />
    </div>
  )
}
