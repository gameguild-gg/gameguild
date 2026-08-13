"use client";

import type { Dispatch } from "react";
import type { EditorState, LexicalEditor } from "lexical";
import { CheckListPlugin } from "@lexical/react/LexicalCheckListPlugin";
import { HistoryPlugin } from "@lexical/react/LexicalHistoryPlugin";
import { LinkPlugin } from "@lexical/react/LexicalLinkPlugin";
import { ListPlugin } from "@lexical/react/LexicalListPlugin";
import { OnChangePlugin } from "@lexical/react/LexicalOnChangePlugin";
import { TabIndentationPlugin } from "@lexical/react/LexicalTabIndentationPlugin";
import { AdmonitionPlugin } from "../features/admonition";
import { ButtonPlugin } from "../features/button";
import {
  CodeActionMenuPlugin,
  CodeHighlightPlugin,
} from "../editor-ui/code-actions";
import {
  CollapsibleActionMenuPlugin,
  CollapsiblePlugin,
} from "../features/collapsible";
import { ContextMenuPlugin } from "../editor-ui/context-menu";
import { DividerPlugin } from "../features/divider";
import { DraggableBlockPlugin } from "../editor-ui/draggable";
import { AutoEmbedPlugin } from "../features/embeds";
import { EmojiPickerPlugin } from "../editor-ui/emoji";
import { EquationsPlugin } from "../features/equation";
import { ExcalidrawPlugin } from "../features/excalidraw";
import type { LexicalSurfaceFeatures } from "../capabilities/feature-flags";
import {
  FloatingLinkEditorPlugin,
  FloatingTextFormatToolbarPlugin,
} from "../editor-ui/floating-toolbar";
import { LayoutActionMenuPlugin, LayoutPlugin } from "../features/layout";
import { MediaPlugin } from "../features/media";
import { MermaidPlugin } from "../features/mermaid";
import { ComponentPickerPlugin } from "../editor-ui/picker";
import { ShortcutsPlugin } from "../editor-ui/shortcuts";
import { StickyPlugin } from "../features/sticky";
import {
  TableActionMenuPlugin,
  TableCellResizerPlugin,
  TableInsertHandlesPlugin,
  TablePlugin,
} from "../features/table";
import { VegaLitePlugin } from "../features/vega-lite";

export function EditorPlugins({
  features,
  anchorElem,
  isLinkEditMode,
  setIsLinkEditMode,
  onChange,
}: {
  features: Required<LexicalSurfaceFeatures>;
  anchorElem: HTMLElement | null;
  isLinkEditMode: boolean;
  setIsLinkEditMode: Dispatch<boolean>;
  onChange?: (editorState: EditorState, editor: LexicalEditor) => void;
}) {
  return (
    <>
      {features.history && <HistoryPlugin />}
      {features.list && <ListPlugin />}
      {features.checkList && <CheckListPlugin />}
      {features.link && <LinkPlugin />}
      {features.tabIndentation && <TabIndentationPlugin />}
      {features.picker && <ComponentPickerPlugin features={features} />}
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

      {/* Code nodes always need highlighting support when deserialized. */}
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
      {onChange && <OnChangePlugin onChange={onChange} ignoreSelectionChange />}
    </>
  );
}
