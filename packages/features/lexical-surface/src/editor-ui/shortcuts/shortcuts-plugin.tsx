/**
 * ShortcutsPlugin — keyboard shortcuts ported from
 * `packages/lexical-playground/src/plugins/ShortcutsPlugin/index.tsx`.
 *
 * Registers a high-priority KEY_DOWN listener that intercepts the
 * shortcut combos defined in `./shortcuts.ts` and dispatches the
 * corresponding format command. Uses `useToolbarState()` to read
 * current blockType (so toggles work correctly).
 */
"use client";

import { useEffect } from "react";
import { useLexicalComposerContext } from "@lexical/react/LexicalComposerContext";
import {
  COMMAND_PRIORITY_NORMAL,
  FORMAT_ELEMENT_COMMAND,
  FORMAT_TEXT_COMMAND,
  INDENT_CONTENT_COMMAND,
  KEY_DOWN_COMMAND,
  OUTDENT_CONTENT_COMMAND,
} from "lexical";
import { TOGGLE_LINK_COMMAND } from "@lexical/link";
import {
  clearFormatting,
  formatBulletList,
  formatCheckList,
  formatCode,
  formatHeading,
  formatNumberedList,
  formatParagraph,
  formatQuote,
} from "../formatting/format-commands";
import { useToolbarState } from "../top-toolbar/toolbar-context";
import {
  isCapitalize,
  isCenterAlign,
  isClearFormatting,
  isFormatBulletList,
  isFormatCheckList,
  isFormatCode,
  isFormatHeading,
  isFormatNumberedList,
  isFormatParagraph,
  isFormatQuote,
  isIndent,
  isJustifyAlign,
  isLeftAlign,
  isLowercase,
  isOutdent,
  isRightAlign,
  isStrikethrough,
  isSubscript,
  isSuperscript,
  isUppercase,
} from "./shortcuts";

export function ShortcutsPlugin({
  setIsLinkEditMode,
}: {
  setIsLinkEditMode?: (v: boolean) => void;
}) {
  const [editor] = useLexicalComposerContext();
  const { toolbarState } = useToolbarState();

  useEffect(() => {
    const handle = (event: KeyboardEvent): boolean => {
      const blockType = toolbarState.blockType;

      const heading = isFormatHeading(event);
      if (heading) {
        event.preventDefault();
        formatHeading(editor, blockType, heading);
        return true;
      }
      if (isFormatParagraph(event)) {
        event.preventDefault();
        formatParagraph(editor);
        return true;
      }
      if (isFormatBulletList(event)) {
        event.preventDefault();
        formatBulletList(editor, blockType);
        return true;
      }
      if (isFormatNumberedList(event)) {
        event.preventDefault();
        formatNumberedList(editor, blockType);
        return true;
      }
      if (isFormatCheckList(event)) {
        event.preventDefault();
        formatCheckList(editor, blockType);
        return true;
      }
      if (isFormatQuote(event)) {
        event.preventDefault();
        formatQuote(editor, blockType);
        return true;
      }
      if (isFormatCode(event)) {
        event.preventDefault();
        formatCode(editor, blockType);
        return true;
      }
      if (isLowercase(event)) {
        event.preventDefault();
        editor.dispatchCommand(FORMAT_TEXT_COMMAND, "lowercase");
        return true;
      }
      if (isUppercase(event)) {
        event.preventDefault();
        editor.dispatchCommand(FORMAT_TEXT_COMMAND, "uppercase");
        return true;
      }
      if (isCapitalize(event)) {
        event.preventDefault();
        editor.dispatchCommand(FORMAT_TEXT_COMMAND, "capitalize");
        return true;
      }
      if (isStrikethrough(event)) {
        event.preventDefault();
        editor.dispatchCommand(FORMAT_TEXT_COMMAND, "strikethrough");
        return true;
      }
      if (isSubscript(event)) {
        event.preventDefault();
        editor.dispatchCommand(FORMAT_TEXT_COMMAND, "subscript");
        return true;
      }
      if (isSuperscript(event)) {
        event.preventDefault();
        editor.dispatchCommand(FORMAT_TEXT_COMMAND, "superscript");
        return true;
      }
      if (isIndent(event)) {
        event.preventDefault();
        editor.dispatchCommand(INDENT_CONTENT_COMMAND, undefined);
        return true;
      }
      if (isOutdent(event)) {
        event.preventDefault();
        editor.dispatchCommand(OUTDENT_CONTENT_COMMAND, undefined);
        return true;
      }
      if (isLeftAlign(event)) {
        event.preventDefault();
        editor.dispatchCommand(FORMAT_ELEMENT_COMMAND, "left");
        return true;
      }
      if (isCenterAlign(event)) {
        event.preventDefault();
        editor.dispatchCommand(FORMAT_ELEMENT_COMMAND, "center");
        return true;
      }
      if (isRightAlign(event)) {
        event.preventDefault();
        editor.dispatchCommand(FORMAT_ELEMENT_COMMAND, "right");
        return true;
      }
      if (isJustifyAlign(event)) {
        event.preventDefault();
        editor.dispatchCommand(FORMAT_ELEMENT_COMMAND, "justify");
        return true;
      }
      if (isClearFormatting(event)) {
        event.preventDefault();
        clearFormatting(editor);
        return true;
      }
      // Insert link (Ctrl+K) opens the link editor when not already a link.
      if (
        event.code === "KeyK" &&
        (event.ctrlKey || event.metaKey) &&
        !event.shiftKey
      ) {
        event.preventDefault();
        if (setIsLinkEditMode) {
          setIsLinkEditMode(true);
        }
        editor.dispatchCommand(TOGGLE_LINK_COMMAND, "https://");
        return true;
      }
      return false;
    };

    return editor.registerCommand(
      KEY_DOWN_COMMAND,
      handle,
      COMMAND_PRIORITY_NORMAL,
    );
  }, [editor, setIsLinkEditMode, toolbarState.blockType]);

  return null;
}
