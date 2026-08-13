/**
 * CollapsibleContentNode — the body of a `CollapsibleContainerNode`,
 * rendered as a `<div class="Collapsible__content">`.
 *
 * Ported from `lexical-playground/src/plugins/CollapsibleExtension/CollapsibleContentNode`.
 */
import {
  ElementNode,
  type DOMExportOutput,
  type EditorConfig,
  type LexicalEditor,
  type LexicalNode,
  type SerializedElementNode,
} from "lexical"

type SerializedCollapsibleContentNode = SerializedElementNode

export class CollapsibleContentNode extends ElementNode {
  static getType(): string {
    return "collapsible-content"
  }

  static clone(node: CollapsibleContentNode): CollapsibleContentNode {
    return new CollapsibleContentNode(node.__key)
  }

  createDOM(_config: EditorConfig, _editor: LexicalEditor): HTMLElement {
    const dom = document.createElement("div")
    dom.classList.add("Collapsible__content")
    return dom
  }

  updateDOM(): boolean {
    return false
  }

  exportDOM(): DOMExportOutput {
    const element = document.createElement("div")
    element.classList.add("Collapsible__content")
    element.setAttribute("data-lexical-collapsible-content", "true")
    return { element }
  }

  static importJSON(
    serializedNode: SerializedCollapsibleContentNode,
  ): CollapsibleContentNode {
    return $createCollapsibleContentNode().updateFromJSON(serializedNode)
  }

  isShadowRoot(): boolean {
    return true
  }
}

export function $createCollapsibleContentNode(): CollapsibleContentNode {
  return new CollapsibleContentNode()
}

export function $isCollapsibleContentNode(
  node: LexicalNode | null | undefined,
): node is CollapsibleContentNode {
  return node instanceof CollapsibleContentNode
}
