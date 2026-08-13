/**
 * StickyNode — DecoratorNode storing sticky note text, color (hex),
 * visual style, size, and absolute position offsets.
 */
import * as React from "react";
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
} from "lexical";
import { StickyComponent } from "./sticky-component";

export type StickyStyle = "classic" | "formal" | "modern";
export type StickySize = "wide" | "compact";

export type SerializedStickyNode = Spread<
  {
    text: string;
    color: string;
    style: StickyStyle;
    size: StickySize;
    xOffset: number;
    yOffset: number;
  },
  SerializedLexicalNode
>;

function $convertStickyElement(
  domNode: HTMLElement,
): null | DOMConversionOutput {
  const text = domNode.getAttribute("data-lexical-sticky-text") || "";
  const color = domNode.getAttribute("data-lexical-sticky-color") || "#fef3c7";
  const style = (domNode.getAttribute("data-lexical-sticky-style") ||
    "classic") as StickyStyle;
  const size = (domNode.getAttribute("data-lexical-sticky-size") ||
    "wide") as StickySize;
  const xOffset = parseFloat(
    domNode.getAttribute("data-lexical-sticky-x") || "0",
  );
  const yOffset = parseFloat(
    domNode.getAttribute("data-lexical-sticky-y") || "0",
  );
  const node = $createStickyNode(text, color, style, size, xOffset, yOffset);
  return { node };
}

export class StickyNode extends DecoratorNode<React.JSX.Element> {
  __text: string;
  __color: string;
  __style: StickyStyle;
  __size: StickySize;
  __xOffset: number;
  __yOffset: number;

  static getType() {
    return "sticky";
  }

  static clone(node: StickyNode): StickyNode {
    return new StickyNode(
      node.__text,
      node.__color,
      node.__style,
      node.__size,
      node.__xOffset,
      node.__yOffset,
      node.__key,
    );
  }

  constructor(
    text: string,
    color?: string,
    style?: StickyStyle,
    size?: StickySize,
    xOffset?: number,
    yOffset?: number,
    key?: NodeKey,
  ) {
    super(key);
    this.__text = text;
    this.__color = color ?? "#fef3c7";
    this.__style = style ?? "classic";
    this.__size = size ?? "wide";
    this.__xOffset = xOffset ?? 0;
    this.__yOffset = yOffset ?? 0;
  }

  static importJSON(serializedNode: SerializedStickyNode): StickyNode {
    return $createStickyNode(
      serializedNode.text,
      serializedNode.color,
      serializedNode.style,
      serializedNode.size ?? "wide",
      serializedNode.xOffset ?? 0,
      serializedNode.yOffset ?? 0,
    );
  }

  exportJSON(): SerializedStickyNode {
    return {
      ...super.exportJSON(),
      text: this.getText(),
      color: this.getColor(),
      style: this.getStyle(),
      size: this.getSize(),
      xOffset: this.getXOffset(),
      yOffset: this.getYOffset(),
    };
  }

  createDOM(_config: EditorConfig): HTMLElement {
    const el = document.createElement("div");
    // The wrapper is zero-height and pointer-transparent so the sticky
    // floats above the document without blocking content behind it.
    el.style.position = "relative";
    el.style.height = "0";
    el.style.overflow = "visible";
    el.style.pointerEvents = "none";
    el.style.zIndex = "10";
    return el;
  }

  exportDOM(): DOMExportOutput {
    const el = document.createElement("div");
    el.setAttribute("data-lexical-sticky-text", this.__text);
    el.setAttribute("data-lexical-sticky-color", this.__color);
    el.setAttribute("data-lexical-sticky-style", this.__style);
    el.setAttribute("data-lexical-sticky-size", this.__size);
    el.setAttribute("data-lexical-sticky-x", String(this.__xOffset));
    el.setAttribute("data-lexical-sticky-y", String(this.__yOffset));
    el.textContent = this.__text;
    return { element: el };
  }

  static importDOM(): DOMConversionMap | null {
    return {
      div: (domNode) => {
        if (!(domNode as HTMLElement).hasAttribute("data-lexical-sticky-text"))
          return null;
        return { conversion: $convertStickyElement, priority: 2 };
      },
    };
  }

  updateDOM(_prevNode: this): boolean {
    return false;
  }

  getTextContent(): string {
    return this.__text;
  }

  // ── Text ──
  getText(): string {
    return this.__text;
  }
  setText(text: string): void {
    this.getWritable().__text = text;
  }

  // ── Color ──
  getColor(): string {
    return this.__color;
  }
  setColor(color: string): void {
    this.getWritable().__color = color;
  }

  // ── Style ──
  getStyle(): StickyStyle {
    return this.__style;
  }
  setStyle(style: StickyStyle): void {
    this.getWritable().__style = style;
  }

  // ── Size ──
  getSize(): StickySize {
    return this.__size;
  }
  setSize(size: StickySize): void {
    this.getWritable().__size = size;
  }

  // ── Position offsets ──
  getXOffset(): number {
    return this.__xOffset;
  }
  setXOffset(x: number): void {
    this.getWritable().__xOffset = x;
  }
  getYOffset(): number {
    return this.__yOffset;
  }
  setYOffset(y: number): void {
    this.getWritable().__yOffset = y;
  }

  setPosition(x: number, y: number): void {
    const writable = this.getWritable();
    writable.__xOffset = x;
    writable.__yOffset = y;
  }

  decorate(): React.JSX.Element {
    return (
      <StickyComponent
        text={this.__text}
        color={this.__color}
        style={this.__style}
        size={this.__size}
        xOffset={this.__xOffset}
        yOffset={this.__yOffset}
        nodeKey={this.__key}
      />
    );
  }
}

export function $createStickyNode(
  text = "",
  color = "#fef3c7",
  style: StickyStyle = "classic",
  size: StickySize = "wide",
  xOffset = 0,
  yOffset = 0,
): StickyNode {
  return $applyNodeReplacement(
    new StickyNode(text, color, style, size, xOffset, yOffset),
  );
}

export function $isStickyNode(
  node: LexicalNode | null | undefined,
): node is StickyNode {
  return node instanceof StickyNode;
}
