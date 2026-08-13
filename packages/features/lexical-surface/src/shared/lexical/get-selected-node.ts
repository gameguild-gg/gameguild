/**
 * Ported verbatim from facebook/lexical playground
 * (`packages/lexical-playground/src/utils/getSelectedNode.ts`).
 *
 * Returns the node at the "logical end" of the current range
 * selection — used by the toolbar to determine which leaf node
 * (TextNode / ElementNode) drives format detection.
 */
import { $isAtNodeEnd } from "@lexical/selection";
import { ElementNode, RangeSelection, TextNode } from "lexical";

export function getSelectedNode(
  selection: RangeSelection,
): TextNode | ElementNode {
  const anchor = selection.anchor;
  const focus = selection.focus;
  const anchorNode = selection.anchor.getNode();
  const focusNode = selection.focus.getNode();
  if (anchorNode === focusNode) {
    return anchorNode;
  }
  const isBackward = selection.isBackward();
  if (isBackward) {
    return $isAtNodeEnd(focus) ? anchorNode : focusNode;
  }
  return $isAtNodeEnd(anchor) ? anchorNode : focusNode;
}
