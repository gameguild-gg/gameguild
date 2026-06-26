"use client"

import { useEffect, useMemo, useRef, useState } from "react"

import type { SerializedHTMLNode } from "../../nodes/html-node"
import { buildHTMLPreviewSrcDoc } from "../../extras/html/html-utils"

/**
 * Read-only preview of an "html" custom-node. Mirrors the editor's
 * sandbox model: no scripts, no same-origin, cross-file references
 * resolved against the block's `files` array via data URLs.
 *
 * The iframe self-reports its scroll height on load so the surrounding
 * page can flow naturally.
 */
export function PreviewHTML({ node }: { node: SerializedHTMLNode }) {
  const iframeRef = useRef<HTMLIFrameElement>(null)
  const [height, setHeight] = useState(0)

  const files = node.data.files ?? []
  const srcDoc = useMemo(() => buildHTMLPreviewSrcDoc(files), [files])

  useEffect(() => {
    const iframe = iframeRef.current
    if (!iframe) return
    const onLoad = () => {
      try {
        const doc = iframe.contentDocument
        if (doc?.documentElement) {
          setHeight(doc.documentElement.scrollHeight)
        }
      } catch {
        // Ignore — sandbox=" " keeps us same-origin-less.
      }
    }
    iframe.addEventListener("load", onLoad)
    return () => iframe.removeEventListener("load", onLoad)
  }, [srcDoc])

  return (
    <div className="my-0">
      <iframe
        ref={iframeRef}
        srcDoc={srcDoc}
        className="w-full rounded-md border"
        style={{ height: height ? `${height + 2}px` : "120px" }}
        sandbox=""
        title="HTML Preview"
      />
    </div>
  )
}
