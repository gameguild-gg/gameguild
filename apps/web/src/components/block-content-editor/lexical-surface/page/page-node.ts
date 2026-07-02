/**
 * PageNode — one physical page in the paged (Word-like) layout.
 *
 * Ported from `lexical-playground/src/plugins/PagesExtension/PageNode`.
 *
 * A `PageNode` always contains exactly one `PageContentNode`. The visible
 * sheet dimensions (width, height, margins) are driven by CSS custom
 * properties (`--page-width`, `--page-height`, `--page-margin-*`) that the
 * `PagesPlugin` writes onto the editor root, so a single node class works
 * for every page size / orientation / margin combination.
 */
import {
  $getEditor,
  ElementNode,
  type LexicalNode,
  type SerializedElementNode,
} from "lexical"
import { addClassNamesToElement } from "@lexical/utils"

import {
  $createPageContentNode,
  $isPageContentNode,
  type PageContentNode,
} from "./page-content-node"

export type SerializedPageNode = SerializedElementNode

export class PageNode extends ElementNode {
  static getType(): string {
    return "page"
  }

  static clone(node: PageNode): PageNode {
    return new PageNode(node.__key)
  }

  static importJSON(serializedNode: SerializedPageNode): PageNode {
    return $createPageNode().updateFromJSON(serializedNode)
  }

  createDOM(): HTMLElement {
    const dom = document.createElement("div")
    addClassNamesToElement(
      dom,
      "lexical-page relative mx-auto mb-6 bg-white dark:bg-gray-900 shadow-md border border-gray-200 dark:border-gray-700",
    )
    dom.style.boxSizing = "border-box"
    dom.style.width = "var(--page-width)"
    // Fixed sheet height (Word-like). Reflow moves content between pages,
    // so the visual paper must not stretch with content.
    dom.style.height = "var(--page-height)"
    dom.style.minHeight = "var(--page-height)"
    dom.style.paddingTop = "var(--page-margin-top)"
    dom.style.paddingRight = "var(--page-margin-right)"
    dom.style.paddingBottom = "var(--page-margin-bottom)"
    dom.style.paddingLeft = "var(--page-margin-left)"
    return dom
  }

  updateDOM(): boolean {
    return false
  }

  getContentNode(): PageContentNode {
    const content = this.getChildren().find($isPageContentNode)
    if (!content) throw new Error("PageNode: Content node not found")
    return content
  }

  getPageNumber(): number {
    const parent = this.getParent()
    if (parent === null) return -1
    let node = parent.getFirstChild()
    let index = 0
    while (node !== null) {
      if (this.is(node)) return index + 1
      if ($isPageNode(node)) index++
      node = node.getNextSibling()
    }
    return -1
  }

  getPageElement(): HTMLElement | null {
    return $getEditor().getElementByKey(this.getKey())
  }

  getPageContentElement(): HTMLElement | null {
    return $getEditor().getElementByKey(this.getContentNode().getKey())
  }

  getPreviousPage(): PageNode | null {
    let previousSibling = this.getPreviousSibling()
    while (previousSibling && !$isPageNode(previousSibling)) {
      previousSibling = previousSibling.getPreviousSibling()
    }
    if (!$isPageNode(previousSibling)) return null
    return previousSibling
  }

  getNextPage(): PageNode | null {
    let nextSibling = this.getNextSibling()
    while (nextSibling && !$isPageNode(nextSibling)) {
      nextSibling = nextSibling.getNextSibling()
    }
    if (!$isPageNode(nextSibling)) return null
    return nextSibling
  }

  excludeFromCopy(): boolean {
    return true
  }

  canInsertTextBefore(): boolean {
    return false
  }

  canInsertTextAfter(): boolean {
    return false
  }
}

export function $createPageNode(): PageNode {
  const page = new PageNode()
  // A page is never valid without its content container; create it eagerly so
  // `getContentNode()` is safe to call immediately after construction.
  page.append($createPageContentNode())
  return page
}

export function $isPageNode(node: LexicalNode | null | undefined): node is PageNode {
  return node instanceof PageNode
}
