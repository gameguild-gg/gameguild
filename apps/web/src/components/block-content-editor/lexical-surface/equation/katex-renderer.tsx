/**
 * KaTeX renderer. Ported from
 * `packages/lexical-playground/src/ui/KatexRenderer.tsx`. Uses
 * `katex.render` with `throwOnError: false` so equation errors display
 * inline (no error boundary needed).
 */
"use client"

import { useEffect, useRef } from "react"
import katex from "katex"

export function KatexRenderer({
  equation,
  inline,
  onDoubleClick,
}: Readonly<{
  equation: string
  inline: boolean
  onDoubleClick?: () => void
}>) {
  const ref = useRef<HTMLSpanElement>(null)

  useEffect(() => {
    const el = ref.current
    if (el) {
      katex.render(equation, el, {
        displayMode: !inline,
        errorColor: "#cc0000",
        output: "html",
        strict: "warn",
        throwOnError: false,
        trust: false,
      })
    }
  }, [equation, inline])

  return (
    <>
      <img
        src="data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"
        width="0"
        height="0"
        alt=""
      />
      <span role="button" tabIndex={-1} onDoubleClick={onDoubleClick} ref={ref} />
      <img
        src="data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"
        width="0"
        height="0"
        alt=""
      />
    </>
  )
}
