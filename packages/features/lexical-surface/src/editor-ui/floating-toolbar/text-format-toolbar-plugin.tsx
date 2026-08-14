/**
 * FloatingTextFormatToolbarPlugin — ported from facebook/lexical
 * playground. It exposes the same text and block formatting vocabulary as
 * the top toolbar, arranged for use near a selected range.
 */
"use client";

import * as React from "react";
import { Dispatch, useCallback, useEffect, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { useMergeRefs } from "@floating-ui/react";
import { $isCodeHighlightNode } from "@lexical/code";
import { $isLinkNode, TOGGLE_LINK_COMMAND } from "@lexical/link";
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import { mergeRegister } from "@lexical/utils";
import { $patchStyleText } from "@lexical/selection";
import {
  $getSelection,
  $addUpdateTag,
  $isParagraphNode,
  $isRangeSelection,
  $isTextNode,
  COMMAND_PRIORITY_LOW,
  ElementFormatType,
  FORMAT_TEXT_COMMAND,
  getDOMSelection,
  HISTORIC_TAG,
  LexicalEditor,
  SKIP_DOM_SELECTION_TAG,
  SELECTION_CHANGE_COMMAND,
} from "lexical";
import { cn } from "@game-guild/ui/lib/utils";
import {
  $readBlockFormatState,
  $readTextFormatState,
  BlockFormatDropDown,
  blockTypeToBlockName,
  CaseFormatDropDown,
  DEFAULT_FONT_SIZE,
  ElementFormatDropdown,
  FontDropDown,
  FontSizeStepper,
} from "../formatting";
import { DropdownColorPicker } from "../../shared/ui/dropdown-color-picker";
import {
  BoldIcon,
  CodeInlineIcon,
  ItalicIcon,
  LinkIcon,
  UnderlineIcon,
  TextColorIcon,
  BgColorIcon,
} from "../../icons";
import { getSelectedNode } from "../../shared/lexical/get-selected-node";
import {
  getDOMRangeRect,
  setFloatingElemPosition,
} from "../../shared/positioning/floating-position";

function isFloatingToolbarPopoverNode(node: Node | null): boolean {
  const element = node instanceof Element ? node : node?.parentElement;
  return (
    element?.closest('[data-lexical-floating-toolbar-popover="true"]') !== null
  );
}

function BubbleButton({
  active,
  onClick,
  title,
  ariaLabel,
  disabled,
  children,
}: {
  active?: boolean;
  onClick: () => void;
  title: string;
  ariaLabel: string;
  disabled?: boolean;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      title={title}
      aria-label={ariaLabel}
      disabled={disabled}
      className={cn(
        "inline-flex items-center justify-center w-8 h-8 rounded",
        "text-gray-800 dark:text-white",
        "hover:bg-gray-100 dark:hover:bg-white/10",
        active && "bg-gray-200 dark:bg-white/20",
        disabled && "opacity-40 pointer-events-none",
      )}
    >
      {children}
    </button>
  );
}

function BubbleDivider() {
  return <div className="w-px h-5 mx-0.5 bg-gray-200 dark:bg-white/20" />;
}

function TextFormatFloatingToolbar({
  editor,
  anchorElem,
  isLink,
  isBold,
  isItalic,
  isUnderline,
  isCode,
  isStrikethrough,
  isSubscript,
  isSuperscript,
  isHighlight,
  isLowercase,
  isUppercase,
  isCapitalize,
  fontSize,
  fontFamily,
  fontColor,
  bgColor,
  blockType,
  elementFormat,
  isRTL,
  setIsLinkEditMode,
  ref,
}: {
  editor: LexicalEditor;
  anchorElem: HTMLElement;
  isBold: boolean;
  isCode: boolean;
  isItalic: boolean;
  isLink: boolean;
  isStrikethrough: boolean;
  isUnderline: boolean;
  isSubscript: boolean;
  isSuperscript: boolean;
  isHighlight: boolean;
  isLowercase: boolean;
  isUppercase: boolean;
  isCapitalize: boolean;
  fontSize: string;
  fontFamily: string;
  fontColor: string;
  bgColor: string;
  blockType: keyof typeof blockTypeToBlockName;
  elementFormat: ElementFormatType;
  isRTL: boolean;
  setIsLinkEditMode: Dispatch<boolean>;
  ref?: React.Ref<HTMLDivElement | null>;
}) {
  const popupRef = useRef<HTMLDivElement | null>(null);
  const mergedRef = useMergeRefs([popupRef, ref]);

  const insertLink = useCallback(() => {
    if (!isLink) {
      setIsLinkEditMode(true);
      editor.dispatchCommand(TOGGLE_LINK_COMMAND, "https://");
    } else {
      setIsLinkEditMode(false);
      editor.dispatchCommand(TOGGLE_LINK_COMMAND, null);
    }
  }, [editor, isLink, setIsLinkEditMode]);

  const applyStyleText = useCallback(
    (
      styles: Record<string, string>,
      skipHistoryStack = false,
      skipRefocus = false,
    ) => {
      editor.update(
        () => {
          if (skipRefocus) {
            $addUpdateTag(SKIP_DOM_SELECTION_TAG);
          }
          const selection = $getSelection();
          if ($isRangeSelection(selection)) {
            $patchStyleText(selection, styles);
          }
        },
        skipHistoryStack ? { tag: HISTORIC_TAG } : {},
      );
    },
    [editor],
  );

  // Hide while a drag is in progress (upstream parity).
  useEffect(() => {
    function onMouseMove(e: MouseEvent) {
      if (popupRef.current && (e.buttons === 1 || e.buttons === 3)) {
        if (popupRef.current.style.pointerEvents !== "none") {
          const el = document.elementFromPoint(e.clientX, e.clientY);
          if (!popupRef.current.contains(el)) {
            popupRef.current.style.pointerEvents = "none";
          }
        }
      }
    }
    function onMouseUp() {
      if (popupRef.current && popupRef.current.style.pointerEvents !== "auto") {
        popupRef.current.style.pointerEvents = "auto";
      }
    }
    document.addEventListener("mousemove", onMouseMove);
    document.addEventListener("mouseup", onMouseUp);
    return () => {
      document.removeEventListener("mousemove", onMouseMove);
      document.removeEventListener("mouseup", onMouseUp);
    };
  }, []);

  const $updateTextFormatFloatingToolbar = useCallback(() => {
    const selection = $getSelection();
    const popupElem = popupRef.current;
    const nativeSelection = getDOMSelection(editor._window);

    if (popupElem === null) {
      return;
    }

    const rootElement = editor.getRootElement();
    if (
      selection !== null &&
      nativeSelection !== null &&
      !nativeSelection.isCollapsed &&
      rootElement !== null &&
      rootElement.contains(nativeSelection.anchorNode)
    ) {
      const rangeRect = getDOMRangeRect(nativeSelection, rootElement);
      setFloatingElemPosition(rangeRect, popupElem, anchorElem, isLink);
    }
  }, [editor, anchorElem, isLink]);

  useEffect(() => {
    const scrollerElem = anchorElem.parentElement;
    const update = () => {
      editor.getEditorState().read(
        () => {
          $updateTextFormatFloatingToolbar();
        },
        { editor },
      );
    };
    window.addEventListener("resize", update);
    scrollerElem?.addEventListener("scroll", update);
    return () => {
      window.removeEventListener("resize", update);
      scrollerElem?.removeEventListener("scroll", update);
    };
  }, [editor, $updateTextFormatFloatingToolbar, anchorElem]);

  useEffect(() => {
    editor.getEditorState().read(
      () => {
        $updateTextFormatFloatingToolbar();
      },
      { editor },
    );
    return mergeRegister(
      editor.registerUpdateListener(({ editorState }) => {
        editorState.read(
          () => {
            $updateTextFormatFloatingToolbar();
          },
          { editor },
        );
      }),
      editor.registerCommand(
        SELECTION_CHANGE_COMMAND,
        () => {
          $updateTextFormatFloatingToolbar();
          return false;
        },
        COMMAND_PRIORITY_LOW,
      ),
    );
  }, [editor, $updateTextFormatFloatingToolbar]);

  if (!editor.isEditable()) {
    return null;
  }

  return (
    <div
      ref={mergedRef}
      className={cn(
        "absolute top-0 left-0 flex max-w-[calc(100%-1rem)] flex-wrap items-center gap-0.5 p-1 rounded-md shadow-lg",
        "bg-white text-gray-900 border border-gray-200",
        "dark:bg-gray-900 dark:text-white dark:border-gray-700",
        "opacity-0 will-change-transform pointer-events-auto z-50",
      )}
      style={{ transform: "translate(-10000px, -10000px)" }}
    >
      <BlockFormatDropDown
        editor={editor}
        blockType={blockType}
        compact
        preserveSelection
      />
      <FontDropDown
        editor={editor}
        value={fontFamily}
        style="font-family"
        compact
        preserveSelection
      />
      <FontSizeStepper editor={editor} value={fontSize} preserveSelection />
      <BubbleDivider />
      <BubbleButton
        active={isBold}
        onClick={() => editor.dispatchCommand(FORMAT_TEXT_COMMAND, "bold")}
        title="Bold"
        ariaLabel="Format text as bold"
      >
        <BoldIcon className="w-4 h-4" />
      </BubbleButton>
      <BubbleButton
        active={isItalic}
        onClick={() => editor.dispatchCommand(FORMAT_TEXT_COMMAND, "italic")}
        title="Italic"
        ariaLabel="Format text as italic"
      >
        <ItalicIcon className="w-4 h-4" />
      </BubbleButton>
      <BubbleButton
        active={isUnderline}
        onClick={() => editor.dispatchCommand(FORMAT_TEXT_COMMAND, "underline")}
        title="Underline"
        ariaLabel="Format text as underlined"
      >
        <UnderlineIcon className="w-4 h-4" />
      </BubbleButton>
      <BubbleButton
        active={isCode}
        onClick={() => editor.dispatchCommand(FORMAT_TEXT_COMMAND, "code")}
        title="Inline code"
        ariaLabel="Insert inline code"
      >
        <CodeInlineIcon className="w-4 h-4" />
      </BubbleButton>
      <CaseFormatDropDown
        editor={editor}
        isLowercase={isLowercase}
        isUppercase={isUppercase}
        isCapitalize={isCapitalize}
        isStrikethrough={isStrikethrough}
        isSubscript={isSubscript}
        isSuperscript={isSuperscript}
        isHighlight={isHighlight}
        preserveSelection
      />
      <BubbleDivider />
      <DropdownColorPicker
        color={fontColor}
        onChange={(value, skipHistoryStack, skipRefocus) =>
          applyStyleText({ color: value }, skipHistoryStack, skipRefocus)
        }
        buttonAriaLabel="Formatting text color"
        buttonIcon={<TextColorIcon className="w-4 h-4" />}
        title="Text color"
        preserveSelection
      />
      <DropdownColorPicker
        color={bgColor}
        onChange={(value, skipHistoryStack, skipRefocus) =>
          applyStyleText(
            { "background-color": value },
            skipHistoryStack,
            skipRefocus,
          )
        }
        buttonAriaLabel="Formatting background color"
        buttonIcon={<BgColorIcon className="w-4 h-4" />}
        title="Background color"
        preserveSelection
      />
      <ElementFormatDropdown
        editor={editor}
        value={elementFormat}
        isRTL={isRTL}
        compact
        preserveSelection
      />
      <BubbleButton
        active={isLink}
        onClick={insertLink}
        title="Insert link"
        ariaLabel="Insert link"
      >
        <LinkIcon className="w-4 h-4" />
      </BubbleButton>
    </div>
  );
}

function useFloatingTextFormatToolbar(
  editor: LexicalEditor,
  anchorElem: HTMLElement,
  setIsLinkEditMode: Dispatch<boolean>,
) {
  const [isText, setIsText] = useState(false);
  const [isLink, setIsLink] = useState(false);
  const [isBold, setIsBold] = useState(false);
  const [isItalic, setIsItalic] = useState(false);
  const [isUnderline, setIsUnderline] = useState(false);
  const [isStrikethrough, setIsStrikethrough] = useState(false);
  const [isSubscript, setIsSubscript] = useState(false);
  const [isSuperscript, setIsSuperscript] = useState(false);
  const [isHighlight, setIsHighlight] = useState(false);
  const [isCode, setIsCode] = useState(false);
  const [isLowercase, setIsLowercase] = useState(false);
  const [isUppercase, setIsUppercase] = useState(false);
  const [isCapitalize, setIsCapitalize] = useState(false);
  const [fontSize, setFontSize] = useState<string>(`${DEFAULT_FONT_SIZE}px`);
  const [fontFamily, setFontFamily] = useState("Arial");
  const [fontColor, setFontColor] = useState("#000");
  const [bgColor, setBgColor] = useState("#fff");
  const [blockType, setBlockType] =
    useState<keyof typeof blockTypeToBlockName>("paragraph");
  const [elementFormat, setElementFormat] = useState<ElementFormatType>("left");
  const [isRTL, setIsRTL] = useState(false);

  const ref = useRef<HTMLDivElement | null>(null);

  const updatePopup = useCallback(() => {
    // If the focus/selection is inside the bubble itself (e.g., font-size input),
    // we don't recalculate visibility — otherwise the bubble disappears as soon as the user clicks the input.
    const active = document.activeElement;
    if (
      ref.current &&
      active &&
      (ref.current.contains(active) || isFloatingToolbarPopoverNode(active))
    ) {
      return;
    }
    editor.getEditorState().read(
      () => {
        if (editor.isComposing()) {
          return;
        }
        const selection = $getSelection();
        const nativeSelection = getDOMSelection(editor._window);
        const rootElement = editor.getRootElement();

        // When the native selection is inside the bubble (input focused),
        // we ignore it — the editor's selection is preserved in Lexical's state.
        if (
          nativeSelection !== null &&
          ref.current &&
          (ref.current.contains(nativeSelection.anchorNode as Node | null) ||
            isFloatingToolbarPopoverNode(nativeSelection.anchorNode))
        ) {
          return;
        }

        if (
          nativeSelection !== null &&
          (!$isRangeSelection(selection) ||
            rootElement === null ||
            !rootElement.contains(nativeSelection.anchorNode))
        ) {
          setIsText(false);
          return;
        }
        if (!$isRangeSelection(selection)) {
          return;
        }

        const node = getSelectedNode(selection);
        const formatState = $readTextFormatState(selection);
        setIsBold(formatState.isBold);
        setIsItalic(formatState.isItalic);
        setIsUnderline(formatState.isUnderline);
        setIsStrikethrough(formatState.isStrikethrough);
        setIsSubscript(formatState.isSubscript);
        setIsSuperscript(formatState.isSuperscript);
        setIsHighlight(formatState.isHighlight);
        setIsCode(formatState.isCode);
        setIsLowercase(formatState.isLowercase);
        setIsUppercase(formatState.isUppercase);
        setIsCapitalize(formatState.isCapitalize);
        setFontSize(formatState.fontSize);
        setFontFamily(formatState.fontFamily);
        setFontColor(formatState.fontColor);
        setBgColor(formatState.bgColor);
        setIsRTL(formatState.isRTL);

        const parent = node.getParent();
        setIsLink($isLinkNode(parent) || $isLinkNode(node));

        const blockState = $readBlockFormatState(node);
        setBlockType(blockState.blockType);
        setElementFormat(blockState.elementFormat);

        if (
          !$isCodeHighlightNode(selection.anchor.getNode()) &&
          selection.getTextContent() !== ""
        ) {
          setIsText($isTextNode(node) || $isParagraphNode(node));
        } else {
          setIsText(false);
        }

        const rawTextContent = selection.getTextContent().replace(/\n/g, "");
        if (!selection.isCollapsed() && rawTextContent === "") {
          setIsText(false);
        }
      },
      { editor },
    );
  }, [editor]);

  useEffect(() => {
    document.addEventListener("selectionchange", updatePopup);
    return () => {
      document.removeEventListener("selectionchange", updatePopup);
    };
  }, [updatePopup]);

  useEffect(() => {
    const onDragStart = () => {
      if (ref.current) {
        ref.current.style.display = "none";
      }
    };
    const onDragEnd = () => {
      if (ref.current && ref.current.style.display === "none") {
        ref.current.style.display = "block";
      }
    };
    document.addEventListener("dragstart", onDragStart, true);
    document.addEventListener("dragend", onDragEnd, true);
    document.addEventListener("drop", onDragEnd, true);
    return () => {
      document.removeEventListener("dragstart", onDragStart, true);
      document.removeEventListener("dragend", onDragEnd, true);
      document.removeEventListener("drop", onDragEnd, true);
    };
  }, []);

  useEffect(() => {
    return mergeRegister(
      editor.registerUpdateListener(() => {
        updatePopup();
      }),
      editor.registerRootListener(() => {
        if (editor.getRootElement() === null) {
          setIsText(false);
        }
      }),
    );
  }, [editor, updatePopup]);

  if (!isText || isLink) {
    return null;
  }

  return createPortal(
    <TextFormatFloatingToolbar
      editor={editor}
      anchorElem={anchorElem}
      ref={ref}
      isLink={isLink}
      isBold={isBold}
      isItalic={isItalic}
      isStrikethrough={isStrikethrough}
      isUnderline={isUnderline}
      isCode={isCode}
      isSubscript={isSubscript}
      isSuperscript={isSuperscript}
      isHighlight={isHighlight}
      isLowercase={isLowercase}
      isUppercase={isUppercase}
      isCapitalize={isCapitalize}
      fontSize={fontSize}
      fontFamily={fontFamily}
      fontColor={fontColor}
      bgColor={bgColor}
      blockType={blockType}
      elementFormat={elementFormat}
      isRTL={isRTL}
      setIsLinkEditMode={setIsLinkEditMode}
    />,
    anchorElem,
  );
}

export default function FloatingTextFormatToolbarPlugin({
  anchorElem,
  setIsLinkEditMode,
}: {
  anchorElem: HTMLElement;
  setIsLinkEditMode: Dispatch<boolean>;
}) {
  const [editor] = useLexicalComposerContext();
  return useFloatingTextFormatToolbar(editor, anchorElem, setIsLinkEditMode);
}
