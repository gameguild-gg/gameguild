/**
 * PageContentNode — inner container of a `PageNode`.
 *
 * Ported from `lexical-playground/src/plugins/PagesExtension/PageContentNode`.
 *
 * It is a **shadow root** (`isShadowRoot(): true`) so that selection,
 * draggable blocks, tables, etc. treat each page's content as if it were
 * the document root. This is what lets every other plugin keep working
 * unchanged while content is physically split across pages.
 */
import {
  ElementNode,
  type LexicalNode,
  type SerializedElementNode,
} from "lexical";
import { addClassNamesToElement } from "@lexical/utils";

import { $isPageNode, type PageNode } from "./page-node";

export type SerializedPageContentNode = SerializedElementNode;

export class PageContentNode extends ElementNode {
  static getType(): string {
    return "page-content";
  }

  static clone(node: PageContentNode): PageContentNode {
    return new PageContentNode(node.__key);
  }

  static importJSON(
    serializedNode: SerializedPageContentNode,
  ): PageContentNode {
    return $createPageContentNode().updateFromJSON(serializedNode);
  }

  createDOM(): HTMLElement {
    const dom = document.createElement("div");
    addClassNamesToElement(dom, "lexical-page-content outline-none");
    // Fill the full printable area so the caret can live across the whole
    // sheet body (including the bottom inset), matching word processors.
    dom.style.height = "100%";
    dom.style.minHeight = "100%";
    dom.style.display = "flex";
    dom.style.flexDirection = "column";
    return dom;
  }

  updateDOM(): boolean {
    return false;
  }

  getPageNode(): PageNode {
    const parent = this.getParent();
    if (!$isPageNode(parent)) {
      throw new Error("PageContentNode: Parent is not a PageNode");
    }
    return parent;
  }

  isShadowRoot(): boolean {
    return true;
  }

  excludeFromCopy(): boolean {
    return true;
  }

  canInsertTextBefore(): boolean {
    return false;
  }

  canInsertTextAfter(): boolean {
    return false;
  }

  canBeEmpty(): boolean {
    return false;
  }
}

export function $createPageContentNode(): PageContentNode {
  return new PageContentNode();
}

export function $isPageContentNode(
  node: LexicalNode | null | undefined,
): node is PageContentNode {
  return node instanceof PageContentNode;
}
