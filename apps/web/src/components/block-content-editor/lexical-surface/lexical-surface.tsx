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
import { ToolbarPlugin, ToolbarContextProvider, useToolbarState } from "./toolbar"
import { ShortcutsPlugin } from "./shortcuts"
import { EquationsPlugin } from "./equation"
import { ExcalidrawPlugin } from "./excalidraw"
import { EmojiPickerPlugin } from "./emoji"
import { AutoEmbedPlugin } from "./embeds"
import { ContextMenuPlugin } from "./context-menu"
import { CodeActionMenuPlugin, CodeHighlightPlugin } from "./code-action"
import { TablePlugin, TableActionMenuPlugin, TableCellResizerPlugin, TableInsertHandlesPlugin } from "./table"
import {
  FloatingLinkEditorPlugin,
  FloatingTextFormatToolbarPlugin,
} from "./floating"
import { ComponentPickerPlugin } from "./picker"
import { DraggableBlockPlugin } from "./draggable"
import { pageSettingsToStyle, isPagedLayout, pageMarginPx } from "./page"

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
  /** Apply page-size/margin/orientation from the toolbar to the editable area. Default: true */
  pageLayout?: boolean
  /** Keyboard shortcuts (Ctrl+\\, Ctrl+Shift+1/2/3, Alt+Shift+1..3, etc.). Default: true */
  shortcuts?: boolean
  /** KaTeX equations via `INSERT_EQUATION_COMMAND` + `/Equation` picker item. Default: true */
  equation?: boolean
  /** Excalidraw drawings via `INSERT_EXCALIDRAW_COMMAND` + `/Excalidraw` picker item. Default: true */
  excalidraw?: boolean
  /** Emoji picker via `:` typeahead trigger. Default: true */
  emoji?: boolean
  /** Auto-embed YouTube/X/Figma URLs. Default: true */
  autoEmbed?: boolean
  /** Right-click context menu (cut/copy/paste/delete). Default: true */
  contextMenu?: boolean
  /** Floating menu on hovered code blocks (lang + copy). Default: true */
  codeAction?: boolean
  /** Tables (`@lexical/table`) + `/Table` picker item. Default: true */
  table?: boolean
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
  pageLayout: true,
  shortcuts: true,
  equation: true,
  excalidraw: true,
  emoji: true,
  autoEmbed: true,
  contextMenu: true,
  codeAction: true,
  table: true,
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

// ─── Inner editor body ──────────────────────────────────────────────────────
// Holds editor-scoped state (activeEditor, isLinkEditMode) so the top
// toolbar can render ABOVE the editable area while still sharing state
// with the floating link editor rendered below.

function EditorBody({
  features,
  onChange,
  placeholder,
  readOnly,
  contentClassName,
  contentStyle,
  className,
  headerSlot,
}: {
  features: Required<LexicalSurfaceFeatures>
  onChange?: (state: SerializedEditorState, editor: LexicalEditor) => void
  placeholder?: React.ReactNode
  readOnly: boolean
  contentClassName?: string
  contentStyle?: React.CSSProperties
  className?: string
  headerSlot?: React.ReactNode
}) {
  const [editor] = useLexicalComposerContext()
  const [activeEditor, setActiveEditor] = useState<LexicalEditor>(editor)
  const [isLinkEditMode, setIsLinkEditMode] = useState(false)
  const [anchorElem, setAnchorElem] = useState<HTMLElement | null>(null)
  const { pageSettings } = useToolbarState()
  const pageStyle = features.pageLayout ? pageSettingsToStyle(pageSettings) : undefined
  const paged = features.pageLayout && isPagedLayout(pageSettings)
  const marginPx = paged ? pageMarginPx(pageSettings) : 0

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
          features={{
            blockEmbed: features.blockEmbed,
            blockInsertMenu: features.blockInsertMenu,
            pageLayout: features.pageLayout,
          }}
        />
      )}
      {headerSlot}
      <div
        className={cn(
          "relative",
          features.pageLayout && "bg-gray-100 dark:bg-gray-950 py-6",
          className,
        )}
        ref={setAnchorElem}
      >
        <div
          className={cn(
            features.pageLayout && "mx-auto relative",
            paged &&
              "bg-white dark:bg-gray-900 shadow-md border border-gray-200 dark:border-gray-700",
          )}
          style={pageStyle}
        >
          {paged && (
            <div
              aria-hidden="true"
              className="pointer-events-none absolute border border-dashed border-gray-300 dark:border-gray-600"
              style={{
                top: marginPx,
                left: marginPx,
                right: marginPx,
                bottom: marginPx,
              }}
            />
          )}
          <RichTextPlugin
            contentEditable={
              <ContentEditable
                readOnly={readOnly}
                tabIndex={readOnly ? -1 : 0}
                style={contentStyle}
                className={cn(
                  "outline-none text-base text-gray-900 dark:text-gray-100 relative",
                  !features.pageLayout && "px-4 py-3",
                  features.pageLayout && !paged && "py-3",
                  contentClassName,
                )}
              />
            }
            placeholder={
              placeholder ? (
                <div
                  className="pointer-events-none absolute select-none text-gray-400 dark:text-gray-500"
                  style={
                    paged
                      ? { top: marginPx, left: marginPx }
                      : { top: 12, left: 16 }
                  }
                >
                  {placeholder}
                </div>
              ) : null
            }
            ErrorBoundary={LexicalErrorBoundary}
          />
        </div>
        {features.history && <HistoryPlugin />}
        {features.list && <ListPlugin />}
        {features.checkList && <CheckListPlugin />}
        {features.link && <LinkPlugin />}
        {features.horizontalRule && <HorizontalRulePlugin />}
        {features.tabIndentation && <TabIndentationPlugin />}
        {features.picker && <ComponentPickerPlugin />}
        {features.shortcuts && <ShortcutsPlugin setIsLinkEditMode={setIsLinkEditMode} />}
        {features.equation && <EquationsPlugin />}
        {features.excalidraw && <ExcalidrawPlugin />}
        {features.emoji && <EmojiPickerPlugin />}
        {features.autoEmbed && <AutoEmbedPlugin />}
        {features.contextMenu && <ContextMenuPlugin />}
        {features.codeAction && anchorElem && <CodeActionMenuPlugin anchorElem={anchorElem} />}
        <CodeHighlightPlugin />
        {features.table && <TablePlugin />}
        {anchorElem && features.table && <TableActionMenuPlugin anchorElem={anchorElem} />}
        {anchorElem && features.table && <TableCellResizerPlugin anchorElem={anchorElem} />}
        {anchorElem && features.table && <TableInsertHandlesPlugin anchorElem={anchorElem} />}
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
      </div>
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
        <EditorBody
          features={resolvedFeatures}
          onChange={onChange}
          placeholder={placeholder}
          readOnly={readOnly}
          contentClassName={contentClassName}
          contentStyle={contentStyle}
          className={className}
          headerSlot={headerSlot}
        />
      </ToolbarContextProvider>
    </LexicalComposer>
  )
}
