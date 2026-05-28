/**
 * Unified Lexical composer for every editing surface in the
 * block-content-editor. Built in Wave A of the playground refactor.
 *
 * One `<LexicalSurface />` replaces all four ad-hoc compositions
 * (inline rich-text, modal rich-text, preview renderer, essay quiz).
 * Callers toggle plugins via `features` flags.
 *
 * State boundary
 *   `initialState: SerializedEditorState | null`  — Lexical JSON object.
 *   `onChange(state: SerializedEditorState, editor)` — emits raw object.
 *
 * No legacy string serialization is supported; pre-launch breaking
 * change documented in `docs/DATA-FLOW.md`.
 */
"use client"

import * as React from "react"
import { useCallback, useMemo, useRef, useState } from "react"
import { LexicalComposer } from "@lexical/react/LexicalComposer"
import { RichTextPlugin } from "@lexical/react/LexicalRichTextPlugin"
import { ContentEditable } from "@lexical/react/LexicalContentEditable"
import { HistoryPlugin } from "@lexical/react/LexicalHistoryPlugin"
import { OnChangePlugin } from "@lexical/react/LexicalOnChangePlugin"
import { ListPlugin } from "@lexical/react/LexicalListPlugin"
import { LinkPlugin } from "@lexical/react/LexicalLinkPlugin"
import { CheckListPlugin } from "@lexical/react/LexicalCheckListPlugin"
import { HorizontalRulePlugin } from "@lexical/react/LexicalHorizontalRulePlugin"
import { TabIndentationPlugin } from "@lexical/react/LexicalTabIndentationPlugin"
import { LexicalErrorBoundary } from "@lexical/react/LexicalErrorBoundary"
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext"
import type { EditorState, LexicalEditor, SerializedEditorState } from "lexical"
import { cn } from "@/lib/utils"

import { SHARED_LEXICAL_NODES } from "../lib/lexical/shared-lexical-config"
import { buildInitialEditorState, stripSelection } from "../lib/lexical/initial-editor-state"
import { BlockEmbedPlugin } from "../plugins/block-embed-plugin"
import { BlockInsertMenuPlugin } from "../plugins/block-insert-menu-plugin"
import { LEXICAL_SURFACE_THEME } from "./theme"
import { ToolbarPlugin, ToolbarContextProvider } from "./toolbar"
import {
  FloatingLinkEditorPlugin,
  FloatingTextFormatToolbarPlugin,
} from "./floating"
import { ComponentPickerPlugin } from "./picker"
import { DraggableBlockPlugin } from "./draggable"

export type LexicalSurfaceFeatures = {
  /** Top toolbar (block format, font, color, alignment, …). Default: true */
  toolbar?: boolean
  /** Bubble toolbar over selected text. Default: true */
  floatingTextFormat?: boolean
  /** Bubble link editor when cursor is on a `LinkNode`. Default: true */
  floatingLinkEditor?: boolean
  /** Drag handle on the left margin of every block. Default: true */
  draggable?: boolean
  /** Native playground `/` slash menu (paragraph, headings, lists, …). Default: true */
  picker?: boolean
  /** Our `BlockEmbedPlugin` (renders embeddable blocks). Default: true */
  blockEmbed?: boolean
  /** Our `BlockInsertMenuPlugin` ("//" trigger). Default: true */
  blockInsertMenu?: boolean
  /** Lexical built-ins. Defaults: true */
  history?: boolean
  list?: boolean
  link?: boolean
  checkList?: boolean
  horizontalRule?: boolean
  tabIndentation?: boolean
}

export interface LexicalSurfaceProps {
  initialState?: SerializedEditorState | null
  onChange?: (state: SerializedEditorState, editor: LexicalEditor) => void
  placeholder?: React.ReactNode
  readOnly?: boolean
  namespace?: string
  features?: LexicalSurfaceFeatures
  /** Tailwind className applied to the `ContentEditable`. */
  contentClassName?: string
  /** Tailwind className for the surface wrapper. */
  className?: string
  /** Inline style passthrough for the editable area (e.g. minHeight). */
  contentStyle?: React.CSSProperties
  /** Re-mount when this value changes; useful for external state resets. */
  mountKey?: string | number
  /** Slot rendered right after `<LexicalComposer>` opens (e.g. custom header). */
  headerSlot?: React.ReactNode
}

const DEFAULT_FEATURES: Required<LexicalSurfaceFeatures> = {
  toolbar: true,
  floatingTextFormat: true,
  floatingLinkEditor: true,
  draggable: true,
  picker: true,
  blockEmbed: true,
  blockInsertMenu: true,
  history: true,
  list: true,
  link: true,
  checkList: true,
  horizontalRule: true,
  tabIndentation: true,
}

function resolveFeatures(
  features: LexicalSurfaceFeatures | undefined,
  readOnly: boolean,
): Required<LexicalSurfaceFeatures> {
  const merged = { ...DEFAULT_FEATURES, ...features }
  if (readOnly) {
    // In read-only mode, hard-disable every interactive plugin.
    merged.toolbar = false
    merged.floatingTextFormat = false
    merged.floatingLinkEditor = false
    merged.draggable = false
    merged.picker = false
    merged.blockInsertMenu = false
    merged.history = false
  }
  return merged
}

// ─── Inner toolbar bridge ───────────────────────────────────────────────────
// The top toolbar needs `editor`, `activeEditor`, `setActiveEditor`, and
// `setIsLinkEditMode` — state that lives above LexicalComposer in the
// upstream playground but is most cleanly owned here. We also share
// `setIsLinkEditMode` with the FloatingLinkEditor below.

function SurfacePlugins({
  features,
  anchorElem,
  onChange,
}: {
  features: Required<LexicalSurfaceFeatures>
  anchorElem: HTMLElement | null
  onChange?: (state: SerializedEditorState, editor: LexicalEditor) => void
}) {
  const [editor] = useLexicalComposerContext()
  const [activeEditor, setActiveEditor] = useState<LexicalEditor>(editor)
  const [isLinkEditMode, setIsLinkEditMode] = useState(false)

  const handleChange = useCallback(
    (editorState: EditorState, editorInstance: LexicalEditor) => {
      if (onChange) {
        onChange(editorState.toJSON(), editorInstance)
      }
    },
    [onChange],
  )

  return (
    <>
      {features.toolbar && (
        <ToolbarPlugin
          editor={editor}
          activeEditor={activeEditor}
          setActiveEditor={setActiveEditor}
          setIsLinkEditMode={setIsLinkEditMode}
        />
      )}
      {features.history && <HistoryPlugin />}
      {features.list && <ListPlugin />}
      {features.checkList && <CheckListPlugin />}
      {features.link && <LinkPlugin />}
      {features.horizontalRule && <HorizontalRulePlugin />}
      {features.tabIndentation && <TabIndentationPlugin />}
      {features.picker && <ComponentPickerPlugin />}
      {features.blockEmbed && <BlockEmbedPlugin />}
      {features.blockInsertMenu && <BlockInsertMenuPlugin />}
      {anchorElem && features.floatingTextFormat && (
        <FloatingTextFormatToolbarPlugin
          anchorElem={anchorElem}
          setIsLinkEditMode={setIsLinkEditMode}
        />
      )}
      {anchorElem && features.floatingLinkEditor && (
        <FloatingLinkEditorPlugin
          anchorElem={anchorElem}
          isLinkEditMode={isLinkEditMode}
          setIsLinkEditMode={setIsLinkEditMode}
        />
      )}
      {anchorElem && features.draggable && <DraggableBlockPlugin anchorElem={anchorElem} />}
      {onChange && <OnChangePlugin onChange={handleChange} ignoreSelectionChange />}
    </>
  )
}

// ─── Main component ─────────────────────────────────────────────────────────

export function LexicalSurface({
  initialState,
  onChange,
  placeholder,
  readOnly = false,
  namespace = "LexicalSurface",
  features,
  contentClassName,
  className,
  contentStyle,
  mountKey,
  headerSlot,
}: LexicalSurfaceProps) {
  const resolvedFeatures = useMemo(() => resolveFeatures(features, readOnly), [features, readOnly])
  const [anchorElem, setAnchorElem] = useState<HTMLElement | null>(null)
  // Re-mount when caller passes a new `mountKey` (e.g. external state reset).
  const seedRef = useRef<SerializedEditorState | null | undefined>(initialState)

  // For read-only renders we strip the persisted selection so Lexical
  // doesn't trigger a browser scroll on hydration.
  const seedState = readOnly ? stripSelection(initialState) : initialState

  const initialConfig = useMemo(
    () => ({
      namespace,
      nodes: SHARED_LEXICAL_NODES,
      theme: LEXICAL_SURFACE_THEME,
      editable: !readOnly,
      editorState: buildInitialEditorState(seedState ?? null),
      onError: (error: Error) => {
        // eslint-disable-next-line no-console
        console.error(`[${namespace}]`, error)
      },
    }),
    // Mount-time only. Caller forces a re-mount with `mountKey`.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [mountKey, readOnly, namespace],
  )

  // Track the latest initialState in a ref so consumers can swap it in
  // by bumping `mountKey` without our memoization fighting back.
  seedRef.current = initialState

  return (
    <LexicalComposer key={mountKey} initialConfig={initialConfig}>
      <ToolbarContextProvider>
        {headerSlot}
        <div className={cn("relative", className)} ref={setAnchorElem}>
          <RichTextPlugin
            contentEditable={
              <ContentEditable
                readOnly={readOnly}
                tabIndex={readOnly ? -1 : 0}
                style={contentStyle}
                className={cn(
                  "outline-none px-4 py-3 text-base text-gray-900 dark:text-gray-100",
                  contentClassName,
                )}
              />
            }
            placeholder={
              placeholder ? (
                <div className="pointer-events-none absolute left-4 top-3 select-none text-gray-400 dark:text-gray-500">
                  {placeholder}
                </div>
              ) : null
            }
            ErrorBoundary={LexicalErrorBoundary}
          />
          <SurfacePlugins
            features={resolvedFeatures}
            anchorElem={anchorElem}
            onChange={onChange}
          />
        </div>
      </ToolbarContextProvider>
    </LexicalComposer>
  )
}
