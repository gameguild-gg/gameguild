/**
 * CollapsibleTitleNode — the `<summary>` of a `CollapsibleContainerNode`.
 *
 * Ported from `lexical-playground/src/plugins/CollapsibleExtension/CollapsibleTitleNode`.
 */
import {
  $createParagraphNode,
  $isElementNode,
  ElementNode,
  type EditorConfig,
  type LexicalEditor,
  type LexicalNode,
  type RangeSelection,
} from "lexical"

import { $isCollapsibleContainerNode } from "./collapsible-container-node"
import { $isCollapsibleContentNode } from "./collapsible-content-node"

export class CollapsibleTitleNode extends ElementNode {
  static getType(): string {
    return "collapsible-title"
  }

  static clone(node: CollapsibleTitleNode): CollapsibleTitleNode {
    return new CollapsibleTitleNode(node.__key)
  }

  createDOM(_config: EditorConfig, _editor: LexicalEditor): HTMLElement {
    const dom = document.createElement("summary")
    dom.classList.add("Collapsible__title")
    return dom
  }

  updateDOM(): boolean {
    return false
  }

  static importJSON(): CollapsibleTitleNode {
    return $createCollapsibleTitleNode()
  }

  exportJSON() {
    return {
      ...super.exportJSON(),
      type: "collapsible-title",
      version: 1,
    }
  }

  collapseAtStart(): true {
    this.getParentOrThrow().insertBefore(this)
    return true
  }

  insertNewAfter(_: RangeSelection, restoreSelection = true): ElementNode {
    const containerNode = this.getParentOrThrow()
    if (!$isCollapsibleContainerNode(containerNode)) {
      throw new Error(
        "CollapsibleTitleNode expects to be child of CollapsibleContainerNode",
      )
    }
    if (containerNode.getOpen()) {
      const contentNode = this.getNextSibling()
      if (!$isCollapsibleContentNode(contentNode)) {
        throw new Error(
          "CollapsibleTitleNode expects to have CollapsibleContentNode sibling",
        )
      }
      const firstChild = contentNode.getFirstChild()
      if ($isElementNode(firstChild)) {
        return firstChild
      }
      const paragraph = $createParagraphNode()
      contentNode.append(paragraph)
      return paragraph
    }
    const paragraph = $createParagraphNode()
    containerNode.insertAfter(paragraph, restoreSelection)
    return paragraph
  }
}

export function $createCollapsibleTitleNode(): CollapsibleTitleNode {
  return new CollapsibleTitleNode()
}

export function $isCollapsibleTitleNode(
  node: LexicalNode | null | undefined,
): node is CollapsibleTitleNode {
  return node instanceof CollapsibleTitleNode
}
