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
"use client";

import * as React from "react";
import { useCallback, useMemo, useRef, useState } from "react";
import { LexicalComposer } from "@lexical/react/LexicalComposer";
import { RichTextPlugin } from "@lexical/react/LexicalRichTextPlugin";
import { ContentEditable } from "@lexical/react/LexicalContentEditable";
import { HistoryPlugin } from "@lexical/react/LexicalHistoryPlugin";
import { OnChangePlugin } from "@lexical/react/LexicalOnChangePlugin";
import { ListPlugin } from "@lexical/react/LexicalListPlugin";
import { LinkPlugin } from "@lexical/react/LexicalLinkPlugin";
import { CheckListPlugin } from "@lexical/react/LexicalCheckListPlugin";
import { TabIndentationPlugin } from "@lexical/react/LexicalTabIndentationPlugin";
import { LexicalErrorBoundary } from "@lexical/react/LexicalErrorBoundary";
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import type {
  EditorState,
  LexicalEditor,
  SerializedEditorState,
} from "lexical";
import { cn } from "@/lib/utils";

import { SHARED_LEXICAL_NODES } from "../lib/lexical/shared-lexical-config";
import {
  buildInitialEditorState,
  stripSelection,
} from "../lib/lexical/initial-editor-state";
import { BlockEmbedPlugin } from "../plugins/block-embed-plugin";
import { BlockInsertMenuPlugin } from "../plugins/block-insert-menu-plugin";
import { LEXICAL_SURFACE_THEME } from "./theme";
import {
  ToolbarPlugin,
  ToolbarContextProvider,
  useToolbarState,
} from "./toolbar";
import { ShortcutsPlugin } from "./shortcuts";
import { EquationsPlugin } from "./equation";
import { ExcalidrawPlugin } from "./excalidraw";
import { EmojiPickerPlugin } from "./emoji";
import { AutoEmbedPlugin } from "./embeds";
import { ContextMenuPlugin } from "./context-menu";
import { CodeActionMenuPlugin, CodeHighlightPlugin } from "./code-action";
import {
  TablePlugin,
  TableActionMenuPlugin,
  TableCellResizerPlugin,
  TableInsertHandlesPlugin,
} from "./table";
import { LayoutPlugin, LayoutActionMenuPlugin } from "./layout";
import { CollapsiblePlugin, CollapsibleActionMenuPlugin } from "./collapsible";
import { StickyPlugin } from "./sticky";
import { AdmonitionPlugin } from "./admonition";
import { ButtonPlugin } from "./button";
import { DividerPlugin } from "./divider";
import { MermaidPlugin } from "./mermaid";
import { VegaLitePlugin } from "./vega-lite";
import { MediaPlugin } from "./media";
import {
  FloatingLinkEditorPlugin,
  FloatingTextFormatToolbarPlugin,
} from "./floating";
import { ComponentPickerPlugin } from "./picker";
import { DraggableBlockPlugin } from "./draggable";
import {
  pageSettingsToStyle,
  isPagedLayout,
  PagesPlugin,
  type PageSettings,
} from "./page";

export type LexicalSurfaceFeatures = {
  /** Top toolbar (block format, font, color, alignment, …). Default: true */
  toolbar?: boolean;
  /** Bubble toolbar over selected text. Default: true */
  floatingTextFormat?: boolean;
  /** Bubble link editor when cursor is on a `LinkNode`. Default: true */
  floatingLinkEditor?: boolean;
  /** Drag handle on the left margin of every block. Default: true */
  draggable?: boolean;
  /** Native playground `/` slash menu (paragraph, headings, lists, …). Default: true */
  picker?: boolean;
  /** Our `BlockEmbedPlugin` (renders embeddable blocks). Default: true */
  blockEmbed?: boolean;
  /** Our `BlockInsertMenuPlugin` ("//" trigger). Default: true */
  blockInsertMenu?: boolean;
  /** Apply page-size/margin/orientation from the toolbar to the editable area. Default: true */
  pageLayout?: boolean;
  /** Keyboard shortcuts (Ctrl+\\, Ctrl+Shift+1/2/3, Alt+Shift+1..3, etc.). Default: true */
  shortcuts?: boolean;
  /** KaTeX equations via `INSERT_EQUATION_COMMAND` + `/Equation` picker item. Default: true */
  equation?: boolean;
  /** Excalidraw drawings via `INSERT_EXCALIDRAW_COMMAND` + `/Excalidraw` picker item. Default: true */
  excalidraw?: boolean;
  /** Emoji picker via `:` typeahead trigger. Default: true */
  emoji?: boolean;
  /** Auto-embed YouTube/X/Figma URLs. Default: true */
  autoEmbed?: boolean;
  /** Right-click context menu (cut/copy/paste/delete). Default: true */
  contextMenu?: boolean;
  /** Floating menu on hovered code blocks (lang + copy). Default: true */
  codeAction?: boolean;
  /** Tables (`@lexical/table`) + `/Table` picker item. Default: true */
  table?: boolean;
  /** Columns Layout via `INSERT_LAYOUT_COMMAND` + +Insert dialog. Default: true */
  layout?: boolean;
  /** Collapsible container via `INSERT_COLLAPSIBLE_COMMAND` + +Insert item. Default: true */
  collapsible?: boolean;
  /** Sticky notes. Default: true */
  sticky?: boolean;
  /** Admonition callouts. Default: true */
  admonition?: boolean;
  /** Styled action buttons. Default: true */
  button?: boolean;
  /** Configurable section divider. Default: true */
  divider?: boolean;
  /** Mermaid diagrams. Default: true */
  mermaid?: boolean;
  /** Vega-Lite charts. Default: true */
  vegaLite?: boolean;
  /** Media block (Image, Video, Audio, Gallery). Default: true */
  media?: boolean;
  /** Lexical built-ins. Defaults: true */
  history?: boolean;
  list?: boolean;
  link?: boolean;
  checkList?: boolean;
  tabIndentation?: boolean;
};

export interface LexicalSurfaceProps {
  initialState?: SerializedEditorState | null;
  onChange?: (state: SerializedEditorState, editor: LexicalEditor) => void;
  placeholder?: React.ReactNode;
  /** Accessible name applied to the content-editable region. */
  accessibleLabel?: string;
  readOnly?: boolean;
  namespace?: string;
  features?: LexicalSurfaceFeatures;
  /** Tailwind className applied to the `ContentEditable`. */
  contentClassName?: string;
  /** Tailwind className for the surface wrapper. */
  className?: string;
  /** Inline style passthrough for the editable area (e.g. minHeight). */
  contentStyle?: React.CSSProperties;
  /** Re-mount when this value changes; useful for external state resets. */
  mountKey?: string | number;
  /** Slot rendered right after `<LexicalComposer>` opens (e.g. custom header). */
  headerSlot?: React.ReactNode;
  /** Optional wrapper function to customize how the toolbar is styled/rendered. */
  toolbarWrapper?: (toolbar: React.ReactNode) => React.ReactNode;
  /** Enable internal content scroll container (useful for container scroll mode). */
  contentScrollable?: boolean;
  /** Optional initial page settings for the internal Lexical toolbar context. */
  initialPageSettings?: PageSettings;
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
  layout: true,
  collapsible: true,
  sticky: true,
  admonition: true,
  button: true,
  divider: true,
  mermaid: true,
  vegaLite: true,
  media: true,
  history: true,
  list: true,
  link: true,
  checkList: true,
  tabIndentation: true,
};

function resolveFeatures(
  features: LexicalSurfaceFeatures | undefined,
  readOnly: boolean,
): Required<LexicalSurfaceFeatures> {
  const merged = { ...DEFAULT_FEATURES, ...features };
  if (readOnly) {
    // In read-only mode, hard-disable every interactive plugin.
    merged.toolbar = false;
    merged.floatingTextFormat = false;
    merged.floatingLinkEditor = false;
    merged.draggable = false;
    merged.picker = false;
    merged.blockInsertMenu = false;
    merged.history = false;
  }
  return merged;
}

// ─── Inner editor body ──────────────────────────────────────────────────────
// Holds editor-scoped state (activeEditor, isLinkEditMode) so the top
// toolbar can render ABOVE the editable area while still sharing state
// with the floating link editor rendered below.

function EditorBody({
  features,
  onChange,
  placeholder,
  accessibleLabel,
  readOnly,
  contentClassName,
  contentStyle,
  className,
  headerSlot,
  toolbarWrapper,
  contentScrollable,
}: {
  features: Required<LexicalSurfaceFeatures>;
  onChange?: (state: SerializedEditorState, editor: LexicalEditor) => void;
  placeholder?: React.ReactNode;
  accessibleLabel?: string;
  readOnly: boolean;
  contentClassName?: string;
  contentStyle?: React.CSSProperties;
  className?: string;
  headerSlot?: React.ReactNode;
  toolbarWrapper?: (toolbar: React.ReactNode) => React.ReactNode;
  contentScrollable?: boolean;
}) {
  const [editor] = useLexicalComposerContext();
  const [activeEditor, setActiveEditor] = useState<LexicalEditor>(editor);
  const [isLinkEditMode, setIsLinkEditMode] = useState(false);
  const [anchorElem, setAnchorElem] = useState<HTMLElement | null>(null);
  const { pageSettings } = useToolbarState();
  const paged = features.pageLayout && isPagedLayout(pageSettings);
  // The flat width card style only applies to pageless surfaces; paged
  // surfaces render real `PageNode` sheets managed by `PagesPlugin`.
  const pageStyle =
    features.pageLayout && !paged
      ? pageSettingsToStyle(pageSettings)
      : undefined;

  const handleChange = useCallback(
    (editorState: EditorState, editorInstance: LexicalEditor) => {
      if (onChange) {
        onChange(editorState.toJSON(), editorInstance);
      }
    },
    [onChange],
  );

  return (
    <>
      {features.toolbar &&
        (() => {
          const toolbarNode = (
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
          );
          return toolbarWrapper ? toolbarWrapper(toolbarNode) : toolbarNode;
        })()}
      {headerSlot}
      <div
        className={cn(
          "relative",
          features.pageLayout && "bg-gray-100 dark:bg-gray-950 py-6",
          contentScrollable &&
            "flex-1 overflow-y-auto overflow-x-hidden min-h-0 scroll-container",
          className,
        )}
        ref={setAnchorElem}
      >
        {(() => {
          const editable = (
            <ContentEditable
              aria-label={accessibleLabel}
              readOnly={readOnly}
              tabIndex={readOnly ? -1 : 0}
              data-lexical-readonly={readOnly ? "true" : "false"}
              style={contentStyle}
              className={cn(
                "lexical-editor outline-none text-base text-gray-900 dark:text-gray-100",
                readOnly ? "lexical-readonly" : "lexical-editable",
                !paged && "relative",
                !features.pageLayout && "px-4 py-3",
                features.pageLayout && !paged && "py-3",
                paged && "min-h-full box-border",
                paged && "px-8 py-8",
                contentClassName,
              )}
            />
          );
          const richText = (
            <RichTextPlugin
              contentEditable={editable}
              placeholder={
                placeholder && !paged ? (
                  <div
                    className="pointer-events-none absolute select-none text-gray-400 dark:text-gray-500"
                    style={{ top: 12, left: 16 }}
                  >
                    {placeholder}
                  </div>
                ) : null
              }
              ErrorBoundary={LexicalErrorBoundary}
            />
          );
          return paged ? (
            richText
          ) : (
            <div
              className={cn(features.pageLayout && "mx-auto relative")}
              style={pageStyle}
            >
              {richText}
            </div>
          );
        })()}
        {features.pageLayout && (
          <PagesPlugin pageSettings={pageSettings} enabled={paged} />
        )}
        {features.history && <HistoryPlugin />}
        {features.list && <ListPlugin />}
        {features.checkList && <CheckListPlugin />}
        {features.link && <LinkPlugin />}
        {features.tabIndentation && <TabIndentationPlugin />}
        {features.picker && <ComponentPickerPlugin />}
        {features.shortcuts && (
          <ShortcutsPlugin setIsLinkEditMode={setIsLinkEditMode} />
        )}
        {features.equation && <EquationsPlugin />}
        {features.excalidraw && <ExcalidrawPlugin />}
        {features.emoji && <EmojiPickerPlugin />}
        {features.autoEmbed && <AutoEmbedPlugin />}
        {features.contextMenu && <ContextMenuPlugin />}
        {features.codeAction && anchorElem && (
          <CodeActionMenuPlugin anchorElem={anchorElem} />
        )}
        <CodeHighlightPlugin />
        {features.table && <TablePlugin />}
        {anchorElem && features.table && (
          <TableActionMenuPlugin anchorElem={anchorElem} />
        )}
        {anchorElem && features.table && (
          <TableCellResizerPlugin anchorElem={anchorElem} />
        )}
        {anchorElem && features.table && (
          <TableInsertHandlesPlugin anchorElem={anchorElem} />
        )}
        {features.layout && <LayoutPlugin />}
        {anchorElem && features.layout && (
          <LayoutActionMenuPlugin anchorElem={anchorElem} />
        )}
        {features.collapsible && <CollapsiblePlugin />}
        {anchorElem && features.collapsible && (
          <CollapsibleActionMenuPlugin anchorElem={anchorElem} />
        )}
        {features.sticky && <StickyPlugin />}
        {features.admonition && <AdmonitionPlugin />}
        {features.button && <ButtonPlugin />}
        {features.divider && <DividerPlugin />}
        {features.mermaid && <MermaidPlugin />}
        {features.vegaLite && <VegaLitePlugin />}
        {features.media && <MediaPlugin />}
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
        {anchorElem && features.draggable && (
          <DraggableBlockPlugin anchorElem={anchorElem} />
        )}
        {onChange && (
          <OnChangePlugin onChange={handleChange} ignoreSelectionChange />
        )}
      </div>
    </>
  );
}

// ─── Main component ─────────────────────────────────────────────────────────

export function LexicalSurface({
  initialState,
  onChange,
  placeholder,
  accessibleLabel,
  readOnly = false,
  namespace = "LexicalSurface",
  features,
  contentClassName,
  className,
  contentStyle,
  mountKey,
  headerSlot,
  toolbarWrapper,
  contentScrollable,
  initialPageSettings,
}: LexicalSurfaceProps) {
  const resolvedFeatures = useMemo(
    () => resolveFeatures(features, readOnly),
    [features, readOnly],
  );
  // Re-mount when caller passes a new `mountKey` (e.g. external state reset).
  const seedRef = useRef<SerializedEditorState | null | undefined>(
    initialState,
  );

  // For read-only renders we strip the persisted selection so Lexical
  // doesn't trigger a browser scroll on hydration.
  const seedState = readOnly ? stripSelection(initialState) : initialState;

  const initialConfig = useMemo(
    () => ({
      namespace,
      nodes: SHARED_LEXICAL_NODES,
      theme: LEXICAL_SURFACE_THEME,
      editable: !readOnly,
      editorState: buildInitialEditorState(seedState ?? null),
      onError: (error: Error) => {
        // eslint-disable-next-line no-console
        console.error(`[${namespace}]`, error);
      },
    }),
    // Mount-time only. Caller forces a re-mount with `mountKey`.
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [mountKey, readOnly, namespace],
  );

  // Track the latest initialState in a ref so consumers can swap it in
  // by bumping `mountKey` without our memoization fighting back.
  seedRef.current = initialState;

  return (
    <LexicalComposer key={mountKey} initialConfig={initialConfig}>
      <ToolbarContextProvider initialPageSettings={initialPageSettings}>
        <EditorBody
          features={resolvedFeatures}
          onChange={onChange}
          placeholder={placeholder}
          accessibleLabel={accessibleLabel}
          readOnly={readOnly}
          contentClassName={contentClassName}
          contentStyle={contentStyle}
          className={className}
          headerSlot={headerSlot}
          toolbarWrapper={toolbarWrapper}
          contentScrollable={contentScrollable}
        />
      </ToolbarContextProvider>
    </LexicalComposer>
  );
}
