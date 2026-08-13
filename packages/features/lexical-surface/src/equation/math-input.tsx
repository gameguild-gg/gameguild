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
import { cn } from "@game-guild/ui/lib/utils"

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

      // Sinaliza globalmente quando o teclado virtual está visível.
      // Marcamos `<body data-math-keyboard-open>` para que diálogos /
      // popovers possam ignorar cliques fora enquanto o teclado estiver
      // aberto (o teclado é montado em `document.body`, fora da árvore
      // de qualquer Radix Dialog/Popover).
      const vk = (
        window as unknown as { mathVirtualKeyboard?: EventTarget & { visible: boolean } }
      ).mathVirtualKeyboard
      if (vk) {
        const sync = () => {
          if (vk.visible) document.body.setAttribute("data-math-keyboard-open", "true")
          else document.body.removeAttribute("data-math-keyboard-open")
        }
        vk.addEventListener("geometrychange", sync)
        // `update-state` é disparado em show/hide; cobre os dois eventos.
        vk.addEventListener("update-state" as keyof HTMLElementEventMap, sync)
        sync()

        // Guarda global: enquanto o teclado virtual estiver aberto,
        // interceptamos pointerdown/mousedown na fase de captura para
        // que detectores de "clique fora" (Radix Dialog/Popover, etc.)
        // não fechem nada quando o usuário toca em qualquer ponto fora
        // do teclado. Apenas chamamos `stopPropagation` — o evento de
        // `click` resultante ainda atinge botões normalmente, então
        // ações explícitas (Save/Cancel) continuam funcionando.
        const guard = (e: Event) => {
          if (!document.body.hasAttribute("data-math-keyboard-open")) return
          const target = e.target as HTMLElement | null
          if (!target) return
          if (target.closest(".ML__keyboard, .ML__virtual-keyboard, math-field")) return
          e.stopPropagation()
        }
        document.addEventListener("pointerdown", guard, true)
        document.addEventListener("mousedown", guard, true)
        document.addEventListener("touchstart", guard, true)
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
  // Turns `true` when the `<math-field>` has been mounted AND its internal mathfield
  // (`this.mathfield`) already exists — only then is it safe to set
  // `value`, call `focus()`, etc. Without this guard, MathLive throws
  // `TypeError: can't access property "options", this.mathfield is
  // undefined` during subsequent blur.
  const [elementReady, setElementReady] = useState(false)

  useEffect(() => {
    let cancelled = false
    ensureMathLive().then(() => {
      if (!cancelled) setReady(true)
    })
    return () => {
      cancelled = true
    }
  }, [])

  // Waits for the custom element to finish initializing.
  useEffect(() => {
    if (!ready) return
    const mf = ref.current
    if (!mf) return
    let cancelled = false
    let raf = 0
    const check = (attempt = 0) => {
      if (cancelled) return
      // MathfieldElement stores the internal instance in `_mathfield`
      // (created asynchronously inside the connectedCallback).
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const m = mf as any
      if (m._mathfield != null || m.mathfield != null) {
        setElementReady(true)
        return
      }
      if (attempt > 120) {
        // Fallback: assume ready after ~2s to avoid blocking the UI if
        // the internal name changes in future versions.
        setElementReady(true)
        return
      }
      raf = requestAnimationFrame(() => check(attempt + 1))
    }
    raf = requestAnimationFrame(() => check(0))
    return () => {
      cancelled = true
      cancelAnimationFrame(raf)
    }
  }, [ready])

  // Keep MathLive value in sync with React-controlled `value`.
  useEffect(() => {
    if (!elementReady) return
    const mf = ref.current
    if (!mf) return
    try {
      if (mf.value !== value) mf.value = value ?? ""
    } catch {
      /* ignore */
    }
  }, [value, elementReady])

  useEffect(() => {
    if (!elementReady) return
    const mf = ref.current
    if (!mf) return
    try { mf.readOnly = !!readOnly } catch { /* ignore */ }
  }, [readOnly, elementReady])

  useEffect(() => {
    if (!elementReady) return
    const mf = ref.current
    if (!mf || placeholder === undefined) return
    try { mf.placeholder = placeholder } catch { /* ignore */ }
  }, [placeholder, elementReady])

  useEffect(() => {
    if (!elementReady) return
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
  }, [onChange, onCommit, elementReady])

  useEffect(() => {
    if (!elementReady || !autoFocus) return
    const mf = ref.current
    if (!mf) return
    try {
      mf.focus?.()
    } catch {
      /* ignore — user can focus manually */
    }
  }, [autoFocus, elementReady])

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
