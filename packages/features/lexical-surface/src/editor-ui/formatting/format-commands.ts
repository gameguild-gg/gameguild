/**
 * Block-formatting helpers ported from facebook/lexical playground
 * (`packages/lexical-playground/src/plugins/ToolbarPlugin/utils.ts`).
 *
 * Font controls live separately from these block and clear-format commands.
 */
import { $createCodeNode } from "@lexical/code";
import {
  INSERT_CHECK_LIST_COMMAND,
  INSERT_ORDERED_LIST_COMMAND,
  INSERT_UNORDERED_LIST_COMMAND,
} from "@lexical/list";
import { $isDecoratorBlockNode } from "@lexical/react/LexicalDecoratorBlockNode";
import {
  $createHeadingNode,
  $createQuoteNode,
  $isHeadingNode,
  $isQuoteNode,
  HeadingTagType,
} from "@lexical/rich-text";
import { $setBlocksType } from "@lexical/selection";
import { $isTableSelection } from "@lexical/table";
import {
  $findMatchingParent,
  $getNearestBlockElementAncestorOrThrow,
} from "@lexical/utils";
import {
  $addUpdateTag,
  $createParagraphNode,
  $createRangeSelection,
  $getSelection,
  $isBlockElementNode,
  $isLineBreakNode,
  $isRangeSelection,
  $isTextNode,
  $setSelection,
  $splitNode,
  ElementNode,
  LexicalEditor,
  LexicalNode,
  RangeSelection,
  SKIP_DOM_SELECTION_TAG,
  SKIP_SELECTION_FOCUS_TAG,
} from "lexical";

export const formatParagraph = (editor: LexicalEditor) => {
  editor.update(() => {
    $addUpdateTag(SKIP_SELECTION_FOCUS_TAG);
    const selection = $getSelection();
    $setBlocksType(selection, () => $createParagraphNode());
  });
};

export const formatHeading = (
  editor: LexicalEditor,
  blockType: string,
  headingSize: HeadingTagType,
) => {
  if (blockType !== headingSize) {
    editor.update(() => {
      $addUpdateTag(SKIP_SELECTION_FOCUS_TAG);
      const selection = $getSelection();
      $setBlocksType(selection, () => $createHeadingNode(headingSize));
    });
  }
};

export const formatBulletList = (editor: LexicalEditor, blockType: string) => {
  if (blockType !== "bullet") {
    editor.update(() => {
      $addUpdateTag(SKIP_SELECTION_FOCUS_TAG);
      editor.dispatchCommand(INSERT_UNORDERED_LIST_COMMAND, undefined);
    });
  } else {
    formatParagraph(editor);
  }
};

export const formatCheckList = (editor: LexicalEditor, blockType: string) => {
  if (blockType !== "check") {
    editor.update(() => {
      $addUpdateTag(SKIP_SELECTION_FOCUS_TAG);
      editor.dispatchCommand(INSERT_CHECK_LIST_COMMAND, undefined);
    });
  } else {
    formatParagraph(editor);
  }
};

export const formatNumberedList = (
  editor: LexicalEditor,
  blockType: string,
) => {
  if (blockType !== "number") {
    editor.update(() => {
      $addUpdateTag(SKIP_SELECTION_FOCUS_TAG);
      editor.dispatchCommand(INSERT_ORDERED_LIST_COMMAND, undefined);
    });
  } else {
    formatParagraph(editor);
  }
};

export const formatQuote = (editor: LexicalEditor, blockType: string) => {
  if (blockType !== "quote") {
    editor.update(() => {
      $addUpdateTag(SKIP_SELECTION_FOCUS_TAG);
      const selection = $getSelection();
      $setBlocksType(selection, () => $createQuoteNode());
    });
  }
};

function $findBlockAncestor(node: LexicalNode): ElementNode | null {
  return $findMatchingParent(node, $isBlockElementNode);
}

function $splitBlocksByLineBreaks(selection: RangeSelection): void {
  const blocks: Set<ElementNode> = new Set();
  for (const node of selection.getNodes()) {
    const block = $findBlockAncestor(node);
    if (block !== null) {
      blocks.add(block);
    }
  }
  for (const point of [selection.anchor, selection.focus]) {
    const block = $findBlockAncestor(point.getNode());
    if (block !== null) {
      blocks.add(block);
    }
  }

  const anchorKey = selection.anchor.key;
  const anchorOffset = selection.anchor.offset;
  const anchorType = selection.anchor.type;
  const focusKey = selection.focus.key;
  const focusOffset = selection.focus.offset;
  const focusType = selection.focus.type;

  for (const block of blocks) {
    const children = block.getChildren();
    const lbIndices: number[] = [];
    for (let i = 0; i < children.length; i++) {
      if ($isLineBreakNode(children[i])) {
        lbIndices.push(i);
      }
    }
    if (lbIndices.length === 0) {
      continue;
    }
    for (let j = lbIndices.length - 1; j >= 0; j--) {
      const lbIndex = lbIndices[j];
      if (lbIndex === undefined) continue;
      const [, rightBlock] = $splitNode(block, lbIndex);
      const firstChild = rightBlock.getFirstChild();
      if ($isLineBreakNode(firstChild)) {
        firstChild.remove();
      }
    }
  }

  const newSelection = $createRangeSelection();
  newSelection.anchor.set(anchorKey, anchorOffset, anchorType);
  newSelection.focus.set(focusKey, focusOffset, focusType);
  $setSelection(newSelection);
}

export const formatCode = (editor: LexicalEditor, blockType: string) => {
  if (blockType !== "code") {
    editor.update(() => {
      $addUpdateTag(SKIP_SELECTION_FOCUS_TAG);
      let selection = $getSelection();
      if (!selection) {
        return;
      }
      if (!$isRangeSelection(selection) || selection.isCollapsed()) {
        $setBlocksType(selection, () => $createCodeNode());
      } else {
        $splitBlocksByLineBreaks(selection);
        selection = $getSelection();
        if (!$isRangeSelection(selection)) {
          return;
        }
        const textContent = selection.getTextContent();
        const codeNode = $createCodeNode();
        const trailingParagraph = $createParagraphNode();
        selection.insertNodes([codeNode, trailingParagraph]);
        selection = codeNode.select();
        selection.insertRawText(textContent);
        if (trailingParagraph.isAttached() && trailingParagraph.isEmpty()) {
          trailingParagraph.remove();
        }
      }
    });
  }
};

export const clearFormatting = (
  editor: LexicalEditor,
  skipRefocus: boolean = false,
) => {
  editor.update(() => {
    if (skipRefocus) {
      $addUpdateTag(SKIP_DOM_SELECTION_TAG);
    }
    const selection = $getSelection();
    if ($isRangeSelection(selection) || $isTableSelection(selection)) {
      const anchor = selection.anchor;
      const focus = selection.focus;
      const extractedNodes = selection.extract();

      if (anchor.key === focus.key && anchor.offset === focus.offset) {
        return;
      }

      extractedNodes.forEach((node) => {
        if ($isTextNode(node)) {
          if (node.getStyle() !== "") {
            node.setStyle("");
          }
          if (node.getFormat() !== 0) {
            node.setFormat(0);
          }
          const nearestBlockElement =
            $getNearestBlockElementAncestorOrThrow(node);
          if (nearestBlockElement.getFormat() !== 0) {
            nearestBlockElement.setFormat("");
          }
          if (nearestBlockElement.getIndent() !== 0) {
            nearestBlockElement.setIndent(0);
          }
        } else if ($isHeadingNode(node) || $isQuoteNode(node)) {
          node.replace($createParagraphNode(), true);
        } else if ($isDecoratorBlockNode(node)) {
          node.setFormat("");
        }
      });
    }
  });
};

export const isKeyboardInput = (
  event: MouseEvent | PointerEvent | React.MouseEvent,
): boolean => {
  if ("pointerId" in event && "pointerType" in event) {
    return event.pointerId === -1 && event.pointerType === "";
  }
  return event?.detail === 0;
};
