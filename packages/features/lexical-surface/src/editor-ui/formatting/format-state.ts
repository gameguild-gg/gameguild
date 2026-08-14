import { $isCodeNode, CodeNode } from "@lexical/code";
import { ListNode } from "@lexical/list";
import { $isHeadingNode } from "@lexical/rich-text";
import {
  $getSelectionStyleValueForProperty,
  $isParentElementRTL,
} from "@lexical/selection";
import { $findMatchingParent, $getNearestNodeOfType } from "@lexical/utils";
import {
  $getSelection,
  $isElementNode,
  $isNodeSelection,
  $isRangeSelection,
  type ElementFormatType,
  type LexicalNode,
  type RangeSelection,
} from "lexical";
import { blockTypeToBlockName, DEFAULT_FONT_SIZE } from "./format-config";

export const CODE_FONT_FAMILY_VALUE =
  "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace";

function parseInlineStyle(style: string): Record<string, string> {
  const result: Record<string, string> = {};
  for (const declaration of style.split(";")) {
    const separator = declaration.indexOf(":");
    if (separator <= 0) continue;
    const property = declaration.slice(0, separator).trim().toLowerCase();
    const value = declaration.slice(separator + 1).trim();
    if (property) result[property] = value;
  }
  return result;
}

export function upsertCssProperty(
  prev: string,
  prop: string,
  value: string,
): string {
  const declarations = (prev || "")
    .split(";")
    .map((declaration) => declaration.trim())
    .filter(Boolean)
    .filter((declaration) => {
      const separator = declaration.indexOf(":");
      return (
        separator >= 0 &&
        declaration.slice(0, separator).trim().toLowerCase() !==
          prop.toLowerCase()
      );
    });
  declarations.push(`${prop}: ${value}`);
  return declarations.join("; ");
}

export function $getEnclosingCodeNode(): CodeNode | null {
  const selection = $getSelection();
  const nodes =
    selection && ($isRangeSelection(selection) || $isNodeSelection(selection))
      ? selection.getNodes()
      : [];
  for (const node of nodes) {
    const code = $findMatchingParent(node, $isCodeNode);
    if (code) return code as CodeNode;
    if ($isCodeNode(node)) return node;
  }
  return null;
}

export function $readTextFormatState(selection: RangeSelection) {
  const codeNode = $getEnclosingCodeNode();
  const codeStyle = codeNode ? parseInlineStyle(codeNode.getStyle()) : null;

  return {
    isBold: selection.hasFormat("bold"),
    isItalic: selection.hasFormat("italic"),
    isUnderline: selection.hasFormat("underline"),
    isStrikethrough: selection.hasFormat("strikethrough"),
    isSubscript: selection.hasFormat("subscript"),
    isSuperscript: selection.hasFormat("superscript"),
    isHighlight: selection.hasFormat("highlight"),
    isCode: selection.hasFormat("code"),
    isLowercase: selection.hasFormat("lowercase"),
    isUppercase: selection.hasFormat("uppercase"),
    isCapitalize: selection.hasFormat("capitalize"),
    fontSize:
      codeStyle?.["font-size"] ??
      $getSelectionStyleValueForProperty(
        selection,
        "font-size",
        `${DEFAULT_FONT_SIZE}px`,
      ),
    fontFamily:
      codeStyle?.["font-family"] ??
      (codeNode
        ? CODE_FONT_FAMILY_VALUE
        : $getSelectionStyleValueForProperty(
            selection,
            "font-family",
            "Arial",
          )),
    fontColor: $getSelectionStyleValueForProperty(selection, "color", "#000"),
    bgColor: $getSelectionStyleValueForProperty(
      selection,
      "background-color",
      "#fff",
    ),
    isRTL: $isParentElementRTL(selection),
  };
}

export function $readBlockFormatState(node: LexicalNode): {
  blockType: keyof typeof blockTypeToBlockName;
  elementFormat: ElementFormatType;
} {
  const topLevelElement = node.getTopLevelElementOrThrow();
  const parentList = $getNearestNodeOfType(node, ListNode);
  const candidate = parentList
    ? parentList.getListType()
    : $isHeadingNode(topLevelElement)
      ? topLevelElement.getTag()
      : topLevelElement.getType();
  const blockType =
    candidate in blockTypeToBlockName
      ? (candidate as keyof typeof blockTypeToBlockName)
      : "paragraph";

  return {
    blockType,
    elementFormat: $isElementNode(topLevelElement)
      ? topLevelElement.getFormatType()
      : "left",
  };
}
