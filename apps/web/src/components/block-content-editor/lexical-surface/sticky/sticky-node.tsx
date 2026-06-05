/**
 * StickyNode — DecoratorNode storing sticky note text, color (hex string), and style.
 */
import * as React from "react"
import {
  $applyNodeReplacement,
  DecoratorNode,
  type DOMConversionMap,
  type DOMConversionOutput,
  type DOMExportOutput,
  type EditorConfig,
  type LexicalNode,
  type NodeKey,
  type SerializedLexicalNode,
  type Spread,
} from "lexical"
import { StickyComponent } from "./sticky-component"

export type StickyStyle = "classic" | "formal" | "modern"

export type SerializedStickyNode = Spread<
  {
    text: string
    color: string
    style: StickyStyle
  },
  SerializedLexicalNode
>

function $convertStickyElement(domNode: HTMLElement): null | DOMConversionOutput {
  const text = domNode.getAttribute("data-lexical-sticky-text") || ""
  const color = domNode.getAttribute("data-lexical-sticky-color") || "#fef3c7"
  const style = (domNode.getAttribute("data-lexical-sticky-style") || "classic") as StickyStyle
  const node = $createStickyNode(text, color, style)
  return { node }
}

export class StickyNode extends DecoratorNode<React.JSX.Element> {
  __text: string
  __color: string
  __style: StickyStyle

  static getType() {
    return "sticky"
  }

  static clone(node: StickyNode): StickyNode {
    return new StickyNode(node.__text, node.__color, node.__style, node.__key)
  }

  constructor(text: string, color?: string, style?: StickyStyle, key?: NodeKey) {
    super(key)
    this.__text = text
    this.__color = color ?? "#fef3c7"
    this.__style = style ?? "classic"
  }

  static importJSON(serializedNode: SerializedStickyNode): StickyNode {
    const node = $createStickyNode(serializedNode.text, serializedNode.color, serializedNode.style)
    return node
  }

  exportJSON(): SerializedStickyNode {
    return {
      ...super.exportJSON(),
      text: this.getText(),
      color: this.getColor(),
      style: this.getStyle(),
    }
  }

  createDOM(_config: EditorConfig): HTMLElement {
    const el = document.createElement("div")
    el.className = "lexical-sticky-wrapper my-4"
    return el
  }

  exportDOM(): DOMExportOutput {
    const el = document.createElement("div")
    el.setAttribute("data-lexical-sticky-text", this.__text)
    el.setAttribute("data-lexical-sticky-color", this.__color)
    el.setAttribute("data-lexical-sticky-style", this.__style)
    el.textContent = this.__text
    return { element: el }
  }

  static importDOM(): DOMConversionMap | null {
    return {
      div: (domNode) => {
        if (!(domNode as HTMLElement).hasAttribute("data-lexical-sticky-text")) return null
        return { conversion: $convertStickyElement, priority: 2 }
      },
    }
  }

  updateDOM(_prevNode: this): boolean {
    return false
  }

  getTextContent(): string {
    return this.__text
  }

  getText(): string {
    return this.__text
  }

  setText(text: string): void {
    const writable = this.getWritable()
    writable.__text = text
  }

  getColor(): string {
    return this.__color
  }

  setColor(color: string): void {
    const writable = this.getWritable()
    writable.__color = color
  }

  getStyle(): StickyStyle {
    return this.__style
  }

  setStyle(style: StickyStyle): void {
    const writable = this.getWritable()
    writable.__style = style
  }

  decorate(): React.JSX.Element {
    return (
      <StickyComponent
        text={this.__text}
        color={this.__color}
        style={this.__style}
        nodeKey={this.__key}
      />
    )
  }
}

export function $createStickyNode(
  text = "",
  color = "#fef3c7",
  style: StickyStyle = "classic"
): StickyNode {
  return $applyNodeReplacement(new StickyNode(text, color, style))
}

export function $isStickyNode(node: LexicalNode | null | undefined): node is StickyNode {
  return node instanceof StickyNode
}
