"use client"

import { useEffect, useState } from "react"
import type { Monaco } from "@monaco-editor/react"
import { shikiToMonaco } from "@shikijs/monaco"
import { createHighlighter, type Highlighter } from "shiki"
import { SHIKI_LANGS } from "@/components/block-content-editor/extras/code-studio/types"

type MonacoEditorModule = typeof import("monaco-editor")
type MonacoEditorRuntime = MonacoEditorModule extends { default: infer T } ? T : MonacoEditorModule

/**
 * Accepts either the `Monaco` namespace from `@monaco-editor/react`
 * (used by html/markdown/mermaid wrappers via `beforeMount`/`onMount`)
 * or a direct `import * as monaco from "monaco-editor"` namespace
 * (used by vega-lite which mounts Monaco manually). Both are structurally
 * compatible with `shikiToMonaco`.
 */
type MonacoNamespace = Monaco | MonacoEditorRuntime

let monacoLoaderConfigured = false
let monacoLoaderConfigPromise: Promise<void> | null = null

function resolveMonacoNamespace(module: MonacoEditorModule): MonacoEditorRuntime {
  const maybeDefault = (module as { default?: MonacoEditorRuntime }).default
  return maybeDefault ?? (module as unknown as MonacoEditorRuntime)
}

async function ensureMonacoLoaderConfigured(monaco?: MonacoNamespace): Promise<void> {
  if (monacoLoaderConfigured) return
  if (typeof window === "undefined") return

  if (monaco) {
    const { loader } = await import("@monaco-editor/react")
    loader.config({ monaco: monaco as Monaco })
    monacoLoaderConfigured = true
    return
  }

  if (!monacoLoaderConfigPromise) {
    monacoLoaderConfigPromise = Promise.all([
      import("@monaco-editor/react"),
      import("monaco-editor"),
    ])
      .then(([monacoReact, monacoModule]) => {
        monacoReact.loader.config({ monaco: resolveMonacoNamespace(monacoModule) as Monaco })
        monacoLoaderConfigured = true
      })
      .catch((error) => {
        monacoLoaderConfigPromise = null
        throw error
      })
  }

  await monacoLoaderConfigPromise
}

// `@monaco-editor/react` defaults to loading monaco-editor from a CDN, so
// without this it would run a *different* Monaco instance than the one
// vega-lite imports directly from `monaco-editor`. That mismatch means
// themes / languages registered on one instance are invisible to the
// other — the symptom is the vega editor (or any other Monaco surface)
// rendering with a broken half-applied theme as soon as Shiki binds to
// one of the two instances. Pinning the loader to our local npm package
// guarantees every Monaco surface in the app shares a single instance.
void ensureMonacoLoaderConfigured()

// Singleton highlighter + one-time monaco binding, shared across every
// Monaco-using editor (code-studio, html, markdown, mermaid, vega-lite).
let highlighter: Highlighter | null = null
let highlighterPromise: Promise<Highlighter> | null = null
let appliedToMonaco = false

// Listeners notified once Shiki has been applied to Monaco for the first
// time. Lets editors that mounted *before* Shiki finished loading swap
// from their fallback theme to the user's chosen Shiki theme.
const readyListeners = new Set<() => void>()

export function isShikiActive(): boolean {
  return appliedToMonaco
}

export async function getShikiHighlighter(): Promise<Highlighter> {
  if (highlighter) return highlighter
  if (!highlighterPromise) {
    highlighterPromise = createHighlighter({
      themes: [
        // Original set
        'github-dark',
        'github-light',
        'github-dark-default',
        'github-light-default',
        'github-dark-dimmed',
        'dark-plus',
        'light-plus',
        'catppuccin-mocha',
        'catppuccin-latte',
        'vitesse-dark',
        'vitesse-light',
        'monokai',
        'solarized-dark',
        'solarized-light',
        'dracula',
        'nord',
        // New themes
        'tokyo-night',
        'one-dark-pro',
        'one-light',
        'material-theme-ocean',
        'material-theme-lighter',
        'rose-pine',
        'rose-pine-dawn',
        'gruvbox-dark-medium',
        'gruvbox-light-medium',
        'night-owl',
        // High-contrast themes
        'github-dark-high-contrast',
        'github-light-high-contrast',
        'min-dark',
        'min-light',
        'slack-dark',
        'slack-ochin',
        'red',
      ],
      langs: SHIKI_LANGS,
    }).then((hl) => {
      highlighter = hl
      return hl
    })
  }
  return highlighterPromise
}

/**
 * Idempotently load Shiki and bind it to the provided Monaco instance.
 * Safe to call from every Monaco editor's `beforeMount` — the highlighter
 * and the `shikiToMonaco` binding are both global singletons, so the
 * first caller pays the cost and every subsequent caller is a no-op.
 *
 * Once this resolves, `isShikiActive()` returns `true` everywhere and any
 * Monaco theme name from `getShikiThemeName(...)` becomes valid for
 * `<Editor theme=...>` or `monaco.editor.setTheme(...)`.
 */
export async function ensureShikiLoaded(monaco: MonacoNamespace): Promise<void> {
  if (appliedToMonaco) return
  try {
    await ensureMonacoLoaderConfigured(monaco)
    const hl = await getShikiHighlighter()
    shikiToMonaco(hl, monaco as Monaco)
    appliedToMonaco = true
    readyListeners.forEach((fn) => {
      try {
        fn()
      } catch (error) {
        console.error('Shiki ready listener threw:', error)
      }
    })
  } catch (error) {
    console.error('Failed to load Shiki:', error)
  }
}

/**
 * React hook returning `true` once Shiki has been bound to Monaco. Use it
 * to force a re-render in editors that may have mounted before the first
 * `ensureShikiLoaded(monaco)` call resolved, so their `theme={...}` prop
 * can switch from `vs-dark`/`light` to the user's selected Shiki theme.
 */
export function useShikiReady(): boolean {
  const [ready, setReady] = useState<boolean>(appliedToMonaco)
  useEffect(() => {
    if (appliedToMonaco) {
      setReady(true)
      return
    }
    const listener = () => setReady(true)
    readyListeners.add(listener)
    return () => {
      readyListeners.delete(listener)
    }
  }, [])
  return ready
}
