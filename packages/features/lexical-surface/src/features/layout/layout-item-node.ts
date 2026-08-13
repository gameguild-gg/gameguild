/**
 * LayoutItemNode — single column of a `LayoutContainerNode`.
 *
 * Ported from `lexical-playground/src/nodes/LayoutItemNode`.
 */
import {
  $isParagraphNode,
  ElementNode,
  type EditorConfig,
  type LexicalNode,
  type SerializedElementNode,
} from "lexical";
import { addClassNamesToElement } from "@lexical/utils";

export type SerializedLayoutItemNode = SerializedElementNode;

export function $isEmptyLayoutItemNode(node: LexicalNode): boolean {
  if (!$isLayoutItemNode(node) || node.getChildrenSize() !== 1) {
    return false;
  }
  const firstChild = node.getFirstChild();
  return $isParagraphNode(firstChild) && firstChild.isEmpty();
}

export class LayoutItemNode extends ElementNode {
  static getType(): string {
    return "layout-item";
  }

  static clone(node: LayoutItemNode): LayoutItemNode {
    return new LayoutItemNode(node.__key);
  }

  createDOM(config: EditorConfig): HTMLElement {
    const dom = document.createElement("div");
    dom.setAttribute("data-lexical-layout-item", "true");
    const themeClass = (config.theme as Record<string, unknown>).layoutItem;
    if (typeof themeClass === "string") {
      addClassNamesToElement(dom, themeClass);
    }
    return dom;
  }

  updateDOM(): boolean {
    return false;
  }

  collapseAtStart(): boolean {
    const parent = this.getParentOrThrow();
    if (
      this.is(parent.getFirstChild()) &&
      parent.getChildren().every($isEmptyLayoutItemNode)
    ) {
      parent.remove();
      return true;
    }
    return false;
  }

  static importJSON(serializedNode: SerializedLayoutItemNode): LayoutItemNode {
    return $createLayoutItemNode().updateFromJSON(serializedNode);
  }

  isShadowRoot(): boolean {
    return true;
  }
}

export function $createLayoutItemNode(): LayoutItemNode {
  return new LayoutItemNode();
}

export function $isLayoutItemNode(
  node: LexicalNode | null | undefined,
): node is LayoutItemNode {
  return node instanceof LayoutItemNode;
}
