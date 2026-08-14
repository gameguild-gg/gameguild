/**
 * ComponentPickerPlugin — slash (`/`) typeahead menu adapted from
 * facebook/lexical playground `ComponentPickerPlugin/index.tsx`.
 *
 * Core text options are always available. Feature-backed options are
 * filtered using the same flags that mount their command plugins.
 */
"use client";

import * as React from "react";
import { useCallback, useMemo, useState } from "react";
import { createPortal } from "react-dom";
import { $createCodeNode } from "@lexical/code";
import {
  INSERT_CHECK_LIST_COMMAND,
  INSERT_ORDERED_LIST_COMMAND,
  INSERT_UNORDERED_LIST_COMMAND,
} from "@lexical/list";
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import {
  LexicalTypeaheadMenuPlugin,
  MenuOption,
  useBasicTypeaheadTriggerMatch,
} from "@lexical/react/LexicalTypeaheadMenuPlugin";
import { $createHeadingNode, $createQuoteNode } from "@lexical/rich-text";
import { $setBlocksType } from "@lexical/selection";
import {
  $createParagraphNode,
  $getSelection,
  $isRangeSelection,
  LexicalEditor,
  TextNode,
} from "lexical";
import {
  BulletedListIcon,
  CheckListIcon,
  CodeBlockIcon,
  Heading1Icon,
  Heading2Icon,
  Heading3Icon,
  Heading4Icon,
  Heading5Icon,
  Heading6Icon,
  NumberedListIcon,
  ParagraphIcon,
  QuoteIcon,
} from "../../icons";
import type { LexicalSurfaceFeatures } from "../../capabilities/feature-flags";
import { getEnabledInsertions } from "../../capabilities/insertion-catalog";
import { InsertionDialog } from "../../capabilities/insertion-dialog";
import type { InsertionDialogDefinition } from "../../capabilities/insertion-types";

type IconCmp = React.ComponentType<{
  className?: string;
  style?: React.CSSProperties;
}>;

const menuStyle: React.CSSProperties = {
  backgroundColor: "var(--popover, #ffffff)",
  border: "1px solid var(--border, #d1d5db)",
  borderRadius: "0.375rem",
  boxShadow:
    "0 20px 25px -5px rgb(0 0 0 / 0.2), 0 8px 10px -6px rgb(0 0 0 / 0.2)",
  boxSizing: "border-box",
  color: "var(--popover-foreground, #111827)",
  maxHeight: "min(22.5rem, calc(100vh - 1rem))",
  minWidth: "15rem",
  overflowY: "auto",
  padding: "0.25rem",
  position: "relative",
  zIndex: 1000,
};

const optionStyle: React.CSSProperties = {
  alignItems: "center",
  backgroundColor: "transparent",
  border: 0,
  borderRadius: "0.25rem",
  color: "inherit",
  cursor: "pointer",
  display: "flex",
  fontSize: "0.875rem",
  gap: "0.5rem",
  lineHeight: "1.25rem",
  minWidth: 0,
  padding: "0.375rem 0.5rem",
  textAlign: "left",
  width: "100%",
};

const selectedOptionStyle: React.CSSProperties = {
  ...optionStyle,
  backgroundColor: "var(--primary, #2563eb)",
  color: "var(--primary-foreground, #ffffff)",
};

class ComponentPickerOption extends MenuOption {
  readonly title: string;
  readonly Icon: IconCmp;
  readonly keywords: string[];
  readonly onSelect: () => void;
  readonly dialog?: InsertionDialogDefinition;
  readonly enabled: boolean;

  constructor(
    title: string,
    options: {
      Icon: IconCmp;
      keywords?: string[];
      onSelect?: () => void;
      dialog?: InsertionDialogDefinition;
      enabled?: boolean;
    },
  ) {
    super(title);
    this.title = title;
    this.Icon = options.Icon;
    this.keywords = options.keywords ?? [];
    this.onSelect = (options.onSelect ?? (() => {})).bind(this);
    this.dialog = options.dialog;
    this.enabled = options.enabled ?? true;
  }
}

function getBaseOptions(
  editor: LexicalEditor,
  features: Required<LexicalSurfaceFeatures>,
): ComponentPickerOption[] {
  return [
    new ComponentPickerOption("Paragraph", {
      Icon: ParagraphIcon,
      keywords: ["normal", "paragraph", "p", "text"],
      onSelect: () =>
        editor.update(() => {
          const selection = $getSelection();
          if ($isRangeSelection(selection)) {
            $setBlocksType(selection, () => $createParagraphNode());
          }
        }),
    }),
    new ComponentPickerOption("Heading 1", {
      Icon: Heading1Icon,
      keywords: ["heading", "header", "h1"],
      onSelect: () =>
        editor.update(() => {
          const selection = $getSelection();
          if ($isRangeSelection(selection)) {
            $setBlocksType(selection, () => $createHeadingNode("h1"));
          }
        }),
    }),
    new ComponentPickerOption("Heading 2", {
      Icon: Heading2Icon,
      keywords: ["heading", "header", "h2"],
      onSelect: () =>
        editor.update(() => {
          const selection = $getSelection();
          if ($isRangeSelection(selection)) {
            $setBlocksType(selection, () => $createHeadingNode("h2"));
          }
        }),
    }),
    new ComponentPickerOption("Heading 3", {
      Icon: Heading3Icon,
      keywords: ["heading", "header", "h3"],
      onSelect: () =>
        editor.update(() => {
          const selection = $getSelection();
          if ($isRangeSelection(selection)) {
            $setBlocksType(selection, () => $createHeadingNode("h3"));
          }
        }),
    }),
    new ComponentPickerOption("Heading 4", {
      Icon: Heading4Icon,
      keywords: ["heading", "header", "h4"],
      onSelect: () =>
        editor.update(() => {
          const selection = $getSelection();
          if ($isRangeSelection(selection)) {
            $setBlocksType(selection, () => $createHeadingNode("h4"));
          }
        }),
    }),
    new ComponentPickerOption("Heading 5", {
      Icon: Heading5Icon,
      keywords: ["heading", "header", "h5"],
      onSelect: () =>
        editor.update(() => {
          const selection = $getSelection();
          if ($isRangeSelection(selection)) {
            $setBlocksType(selection, () => $createHeadingNode("h5"));
          }
        }),
    }),
    new ComponentPickerOption("Heading 6", {
      Icon: Heading6Icon,
      keywords: ["heading", "header", "h6"],
      onSelect: () =>
        editor.update(() => {
          const selection = $getSelection();
          if ($isRangeSelection(selection)) {
            $setBlocksType(selection, () => $createHeadingNode("h6"));
          }
        }),
    }),
    new ComponentPickerOption("Numbered List", {
      Icon: NumberedListIcon,
      keywords: ["numbered list", "ordered list", "ol"],
      onSelect: () =>
        editor.dispatchCommand(INSERT_ORDERED_LIST_COMMAND, undefined),
      enabled: features.list,
    }),
    new ComponentPickerOption("Bulleted List", {
      Icon: BulletedListIcon,
      keywords: ["bulleted list", "unordered list", "ul"],
      onSelect: () =>
        editor.dispatchCommand(INSERT_UNORDERED_LIST_COMMAND, undefined),
      enabled: features.list,
    }),
    new ComponentPickerOption("Check List", {
      Icon: CheckListIcon,
      keywords: ["check list", "todo list"],
      onSelect: () =>
        editor.dispatchCommand(INSERT_CHECK_LIST_COMMAND, undefined),
      enabled: features.list && features.checkList,
    }),
    new ComponentPickerOption("Quote", {
      Icon: QuoteIcon,
      keywords: ["block quote"],
      onSelect: () =>
        editor.update(() => {
          const selection = $getSelection();
          if ($isRangeSelection(selection)) {
            $setBlocksType(selection, () => $createQuoteNode());
          }
        }),
    }),
    new ComponentPickerOption("Code", {
      Icon: CodeBlockIcon,
      keywords: ["javascript", "python", "js", "codeblock"],
      onSelect: () =>
        editor.update(() => {
          const selection = $getSelection();
          if ($isRangeSelection(selection)) {
            if (selection.isCollapsed()) {
              $setBlocksType(selection, () => $createCodeNode());
            } else {
              const textContent = selection.getTextContent();
              const codeNode = $createCodeNode();
              selection.insertNodes([codeNode]);
              selection.insertRawText(textContent);
            }
          }
        }),
    }),
    ...getEnabledInsertions(features, "picker").map(
      (definition) =>
        new ComponentPickerOption(definition.label, {
          Icon: definition.Icon,
          keywords: [...definition.keywords],
          onSelect: definition.execute
            ? () => definition.execute?.(editor)
            : undefined,
          dialog: definition.dialog,
        }),
    ),
  ].filter((option) => option.enabled);
}

export default function ComponentPickerPlugin({
  features,
}: {
  features: Required<LexicalSurfaceFeatures>;
}) {
  const [editor] = useLexicalComposerContext();
  const [queryString, setQueryString] = useState<string | null>(null);
  const [pendingDialog, setPendingDialog] =
    useState<InsertionDialogDefinition | null>(null);

  const checkForTriggerMatch = useBasicTypeaheadTriggerMatch("/", {
    allowWhitespace: true,
    minLength: 0,
  });

  const options = useMemo(() => {
    const baseOptions = getBaseOptions(editor, features);
    if (!queryString) {
      return baseOptions;
    }
    const regex = new RegExp(queryString, "i");
    return baseOptions.filter(
      (option) =>
        regex.test(option.title) ||
        option.keywords.some((keyword) => regex.test(keyword)),
    );
  }, [editor, features, queryString]);

  const onSelectOption = useCallback(
    (
      selectedOption: ComponentPickerOption,
      nodeToRemove: TextNode | null,
      closeMenu: () => void,
    ) => {
      editor.update(() => {
        nodeToRemove?.remove();
        if (!selectedOption.dialog) {
          selectedOption.onSelect();
        }
        closeMenu();
      });
      if (selectedOption.dialog) {
        setPendingDialog(selectedOption.dialog);
      }
    },
    [editor],
  );

  return (
    <>
      <LexicalTypeaheadMenuPlugin<ComponentPickerOption>
        onQueryChange={setQueryString}
        onSelectOption={onSelectOption}
        triggerFn={checkForTriggerMatch}
        options={options}
        menuRenderFn={(
          anchorElementRef,
          { selectedIndex, selectOptionAndCleanUp, setHighlightedIndex },
        ) => {
          if (!anchorElementRef.current || options.length === 0) {
            return null;
          }

          anchorElementRef.current.style.zIndex = "1000";

          return createPortal(
            <div role="listbox" style={menuStyle}>
              {options.map((option, i) => {
                const Icon = option.Icon;
                const isSelected = selectedIndex === i;
                return (
                  <button
                    key={option.key}
                    ref={(el) => option.setRefElement(el)}
                    type="button"
                    role="option"
                    aria-selected={isSelected}
                    tabIndex={-1}
                    onMouseEnter={() => setHighlightedIndex(i)}
                    onClick={() => selectOptionAndCleanUp(option)}
                    style={isSelected ? selectedOptionStyle : optionStyle}
                  >
                    <Icon
                      style={{ flexShrink: 0, height: "1rem", width: "1rem" }}
                    />
                    <span
                      style={{
                        overflow: "hidden",
                        textOverflow: "ellipsis",
                        whiteSpace: "nowrap",
                      }}
                    >
                      {option.title}
                    </span>
                  </button>
                );
              })}
            </div>,
            anchorElementRef.current,
          );
        }}
      />
      <InsertionDialog
        definition={pendingDialog}
        activeEditor={editor}
        onClose={() => setPendingDialog(null)}
      />
    </>
  );
}
