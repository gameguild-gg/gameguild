"use client";

import * as React from "react";
import { useCallback, useState } from "react";
import {
  $getRoot,
  type EditorState,
  type LexicalEditor,
  type SerializedEditorState,
} from "lexical";
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import { ContentEditable } from "@lexical/react/LexicalContentEditable";
import { LexicalErrorBoundary } from "@lexical/react/LexicalErrorBoundary";
import { RichTextPlugin } from "@lexical/react/LexicalRichTextPlugin";
import { cn } from "@game-guild/ui/lib/utils";
import type { LexicalSurfaceFeatures } from "../capabilities/feature-flags";
import {
  isPagedLayout,
  pageSettingsToStyle,
  PagesPlugin,
} from "../features/page";
import { ToolbarPlugin, useToolbarState } from "../editor-ui/top-toolbar";
import { EditorPlugins } from "./editor-plugins";

export type EditorBodyProps = {
  features: Required<LexicalSurfaceFeatures>;
  onChange?: (state: SerializedEditorState, editor: LexicalEditor) => void;
  onContentChange?: (change: {
    state: SerializedEditorState;
    plainText: string;
  }) => void;
  placeholder?: React.ReactNode;
  accessibleLabel?: string;
  readOnly: boolean;
  contentClassName?: string;
  contentStyle?: React.CSSProperties;
  className?: string;
  headerSlot?: React.ReactNode;
  toolbarWrapper?: (toolbar: React.ReactNode) => React.ReactNode;
  contentScrollable?: boolean;
};

export function EditorBody({
  features,
  onChange,
  onContentChange,
  placeholder,
  accessibleLabel,
  readOnly,
  contentClassName,
  contentStyle,
  className,
  headerSlot,
  toolbarWrapper,
  contentScrollable,
}: EditorBodyProps) {
  const [editor] = useLexicalComposerContext();
  const [activeEditor, setActiveEditor] = useState<LexicalEditor>(editor);
  const [isLinkEditMode, setIsLinkEditMode] = useState(false);
  const [anchorElem, setAnchorElem] = useState<HTMLElement | null>(null);
  const { pageSettings } = useToolbarState();
  const paged = features.pageLayout && isPagedLayout(pageSettings);
  const pageStyle =
    features.pageLayout && !paged
      ? pageSettingsToStyle(pageSettings)
      : undefined;

  const handleChange = useCallback(
    (editorState: EditorState, editorInstance: LexicalEditor) => {
      onChange?.(editorState.toJSON(), editorInstance);
      if (onContentChange) {
        onContentChange({
          state: editorState.toJSON(),
          plainText: editorState.read(() => $getRoot().getTextContent()),
        });
      }
    },
    [onChange, onContentChange],
  );

  const toolbar = features.toolbar ? (
    <ToolbarPlugin
      editor={editor}
      activeEditor={activeEditor}
      setActiveEditor={setActiveEditor}
      setIsLinkEditMode={setIsLinkEditMode}
      features={features}
    />
  ) : null;

  return (
    <>
      {toolbar && (toolbarWrapper ? toolbarWrapper(toolbar) : toolbar)}
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
                paged && "min-h-full box-border px-8 py-8",
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
        <EditorPlugins
          features={features}
          anchorElem={anchorElem}
          isLinkEditMode={isLinkEditMode}
          setIsLinkEditMode={setIsLinkEditMode}
          onChange={onChange || onContentChange ? handleChange : undefined}
        />
      </div>
    </>
  );
}
