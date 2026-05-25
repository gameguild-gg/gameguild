/**
 * Math input component (LaTeX-based)
 *
 * Thin React wrapper around the MathLive `<math-field>` web component.
 * The stored `value` is a LaTeX string. The component handles upgrading
 * the custom element on the client and forwarding `input` events as
 * `onChange(latex)` callbacks.
 *
 * The `<math-field>` element gives users a visual formula editor that
 * supports fractions, roots, integrals, sums, Greek letters, and the
 * full LaTeX math vocabulary — not just bare ASCII text.
 */

"use client"

import { useEffect, useRef, useState, type CSSProperties } from "react"
import type { MathfieldElement } from "mathlive"
import { cn } from "@/lib/utils"

interface MathInputProps {
  value: string
  onChange?: (latex: string) => void
  onCommit?: (latex: string) => void
  readOnly?: boolean
  placeholder?: string
  className?: string
  style?: CSSProperties
  autoFocus?: boolean
}

// Module-level promise so the MathLive runtime is loaded (and the
// `<math-field>` custom element registered) only once across the app.
let mathliveReady: Promise<void> | null = null
function ensureMathLive(): Promise<void> {
  if (typeof window === "undefined") return Promise.resolve()
  if (!mathliveReady) {
    mathliveReady = import("mathlive").then((mod) => {
      // Serve MathLive's KaTeX fonts and sounds from our own origin
      // (copied into `public/mathlive/` by `scripts/copy-mathlive-assets.cjs`).
      //
      // The directories MUST be absolute URLs (with scheme + host) — if we
      // pass a root-relative path like `/mathlive/fonts/`, MathLive's
      // `resolveUrl` falls through to a `fetch HEAD` against its own
      // script URL (the Next.js chunk), which logs an `Invalid URL`
      // error that Next's dev overlay surfaces as a fatal.
      const ME = (mod as unknown as { MathfieldElement?: typeof import("mathlive").MathfieldElement })
        .MathfieldElement
      if (ME) {
        const origin = window.location.origin
        ME.fontsDirectory = `${origin}/mathlive/fonts/`
        ME.soundsDirectory = `${origin}/mathlive/sounds/`
      }
    })
  }
  return mathliveReady
}

export function MathInput({
  value,
  onChange,
  onCommit,
  readOnly,
  placeholder,
  className,
  style,
  autoFocus,
}: MathInputProps) {
  const ref = useRef<MathfieldElement | null>(null)
  const [ready, setReady] = useState(false)

  useEffect(() => {
    let cancelled = false
    ensureMathLive().then(() => {
      if (!cancelled) setReady(true)
    })
    return () => {
      cancelled = true
    }
  }, [])

  // Keep MathLive value in sync with React-controlled `value`.
  useEffect(() => {
    if (!ready) return
    const mf = ref.current
    if (!mf) return
    if (mf.value !== value) {
      mf.value = value ?? ""
    }
  }, [value, ready])

  useEffect(() => {
    if (!ready) return
    const mf = ref.current
    if (!mf) return
    mf.readOnly = !!readOnly
  }, [readOnly, ready])

  useEffect(() => {
    if (!ready) return
    const mf = ref.current
    if (!mf || placeholder === undefined) return
    mf.placeholder = placeholder
  }, [placeholder, ready])

  useEffect(() => {
    if (!ready) return
    const mf = ref.current
    if (!mf) return
    const handleInput = () => onChange?.(mf.value)
    const handleChange = () => onCommit?.(mf.value)
    mf.addEventListener("input", handleInput)
    mf.addEventListener("change", handleChange)
    return () => {
      mf.removeEventListener("input", handleInput)
      mf.removeEventListener("change", handleChange)
    }
  }, [onChange, onCommit, ready])

  useEffect(() => {
    if (!ready || !autoFocus) return
    const mf = ref.current
    if (!mf) return
    const id = requestAnimationFrame(() => mf.focus?.())
    return () => cancelAnimationFrame(id)
  }, [autoFocus, ready])

  // The `<math-field>` tag is a custom element not in the React JSX
  // namespace. Cast through `any` to keep TS happy without polluting
  // the global JSX intrinsic-elements map.
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const Tag = "math-field" as any

  // Defer mounting `<math-field>` until MathLive has loaded AND we've
  // configured the font/sound directories. Mounting earlier triggers
  // `connectedCallback` -> `loadFonts()` with the default `./fonts/`
  // path, which resolves to a non-existent chunk URL and throws a
  // console error that the Next dev overlay surfaces as a fatal.
  if (!ready) {
    return (
      <div
        className={cn(
          "block w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-base text-gray-400 dark:text-gray-500 min-h-[2.5rem]",
          className,
        )}
        style={style}
        aria-busy="true"
      >
        {placeholder ?? "\u00A0"}
      </div>
    )
  }

  return (
    <Tag
      ref={ref}
      class={cn(
        "block w-full rounded-md border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-base text-gray-900 dark:text-gray-100 outline-none focus:border-blue-500",
        className,
      )}
      style={style}
    />
  )
}
