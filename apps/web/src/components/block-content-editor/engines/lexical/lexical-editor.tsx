"use client"

/**
 * Compatibility shim.
 *
 * The Lexical engine was removed. Several files under `nodes/` are now dead
 * Lexical DecoratorNode classes (their type exports are still consumed, but
 * the class bodies are no longer instantiated by any runtime editor). This
 * module re-creates the small set of contexts and the `Editor` component
 * those dead-code paths import, so typechecking and bundling continue to
 * work without resurrecting the engine.
 */

import { createContext, type ReactNode } from "react"
import type { EnhancedStorageAdapter } from "@/components/block-content-editor/lib/storage/editor/enhanced-storage-adapter"

export const EditorLoadingContext = createContext<boolean>(false)
export const StorageAdapterContext = createContext<EnhancedStorageAdapter | null>(null)
export const ProjectIdContext = createContext<string | null>(null)

// Stub Editor component referenced by the dead `nodes/project-node.tsx`
// render path. It intentionally does nothing — the surrounding decorator
// nodes are no longer mounted by any live editor.
export function Editor(_props: { children?: ReactNode; [key: string]: unknown }): null {
  return null
}
