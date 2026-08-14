/**
 * AdmonitionLexicalNode — DecoratorNode storing admonition type, title,
 * content, design style, and optional custom colors.
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
import type { AdmonitionType } from "./admonition";
import { AdmonitionLexicalComponent } from "./admonition-component";

export type AdmonitionDesign =
  "default" | "compact" | "bordered" | "vertical-bar";

export type SerializedAdmonitionLexicalNode = Spread<
  {
    admonitionType: AdmonitionType;
    title: string;
    content: string;
    design: AdmonitionDesign;
    customBorderColor: string;
    customTextColor: string;
  },
  SerializedLexicalNode
>;

function $convertAdmonitionElement(
  domNode: HTMLElement,
): null | DOMConversionOutput {
  const admonitionType = (domNode.getAttribute(
    "data-lexical-admonition-type",
  ) || "note") as AdmonitionType;
  const title = domNode.getAttribute("data-lexical-admonition-title") || "";
  const content = domNode.getAttribute("data-lexical-admonition-content") || "";
  const design = (domNode.getAttribute("data-lexical-admonition-design") ||
    "default") as AdmonitionDesign;
  const customBorderColor =
    domNode.getAttribute("data-lexical-admonition-border-color") || "";
  const customTextColor =
    domNode.getAttribute("data-lexical-admonition-text-color") || "";
  const node = $createAdmonitionLexicalNode(
    admonitionType,
    title,
    content,
    design,
    customBorderColor,
    customTextColor,
  );
  return { node };
}

export class AdmonitionLexicalNode extends DecoratorNode<React.JSX.Element> {
  __admonitionType: AdmonitionType;
  __title: string;
  __content: string;
  __design: AdmonitionDesign;
  __customBorderColor: string;
  __customTextColor: string;

  static getType() {
    return "lexical-admonition";
  }

  static clone(node: AdmonitionLexicalNode): AdmonitionLexicalNode {
    return new AdmonitionLexicalNode(
      node.__admonitionType,
      node.__title,
      node.__content,
      node.__design,
      node.__customBorderColor,
      node.__customTextColor,
      node.__key,
    );
  }

  constructor(
    admonitionType?: AdmonitionType,
    title?: string,
    content?: string,
    design?: AdmonitionDesign,
    customBorderColor?: string,
    customTextColor?: string,
    key?: NodeKey,
  ) {
    super(key);
    this.__admonitionType = admonitionType ?? "note";
    this.__title = title ?? "";
    this.__content = content ?? "";
    this.__design = design ?? "default";
    this.__customBorderColor = customBorderColor ?? "";
    this.__customTextColor = customTextColor ?? "";
  }

  static importJSON(
    serializedNode: SerializedAdmonitionLexicalNode,
  ): AdmonitionLexicalNode {
    return $createAdmonitionLexicalNode(
      serializedNode.admonitionType,
      serializedNode.title,
      serializedNode.content,
      serializedNode.design ?? "default",
      serializedNode.customBorderColor ?? "",
      serializedNode.customTextColor ?? "",
    );
  }

  exportJSON(): SerializedAdmonitionLexicalNode {
    return {
      ...super.exportJSON(),
      admonitionType: this.__admonitionType,
      title: this.__title,
      content: this.__content,
      design: this.__design,
      customBorderColor: this.__customBorderColor,
      customTextColor: this.__customTextColor,
    };
  }

  createDOM(_config: EditorConfig): HTMLElement {
    const el = document.createElement("div");
    el.className = "lexical-admonition-wrapper my-4";
    return el;
  }

  exportDOM(): DOMExportOutput {
    const el = document.createElement("div");
    el.setAttribute("data-lexical-admonition-type", this.__admonitionType);
    el.setAttribute("data-lexical-admonition-title", this.__title);
    el.setAttribute("data-lexical-admonition-content", this.__content);
    el.setAttribute("data-lexical-admonition-design", this.__design);
    if (this.__customBorderColor)
      el.setAttribute(
        "data-lexical-admonition-border-color",
        this.__customBorderColor,
      );
    if (this.__customTextColor)
      el.setAttribute(
        "data-lexical-admonition-text-color",
        this.__customTextColor,
      );
    el.textContent = `${this.__title}: ${this.__content}`;
    return { element: el };
  }

  static importDOM(): DOMConversionMap | null {
    return {
      div: (domNode) => {
        if (
          !(domNode as HTMLElement).hasAttribute("data-lexical-admonition-type")
        )
          return null;
        return { conversion: $convertAdmonitionElement, priority: 2 };
      },
    };
  }

  updateDOM(_prevNode: this): boolean {
    return false;
  }

  getTextContent(): string {
    return `${this.__title}: ${this.__content}`;
  }

  // ── Getters / Setters ──
  getAdmonitionType(): AdmonitionType {
    return this.__admonitionType;
  }
  setAdmonitionType(type: AdmonitionType): void {
    this.getWritable().__admonitionType = type;
  }

  getTitle(): string {
    return this.__title;
  }
  setTitle(title: string): void {
    this.getWritable().__title = title;
  }

  getContent(): string {
    return this.__content;
  }
  setContent(content: string): void {
    this.getWritable().__content = content;
  }

  getDesign(): AdmonitionDesign {
    return this.__design;
  }
  setDesign(design: AdmonitionDesign): void {
    this.getWritable().__design = design;
  }

  getCustomBorderColor(): string {
    return this.__customBorderColor;
  }
  setCustomBorderColor(color: string): void {
    this.getWritable().__customBorderColor = color;
  }

  getCustomTextColor(): string {
    return this.__customTextColor;
  }
  setCustomTextColor(color: string): void {
    this.getWritable().__customTextColor = color;
  }

  decorate(): React.JSX.Element {
    return (
      <AdmonitionLexicalComponent
        admonitionType={this.__admonitionType}
        title={this.__title}
        content={this.__content}
        design={this.__design}
        customBorderColor={this.__customBorderColor}
        customTextColor={this.__customTextColor}
        nodeKey={this.__key}
      />
    );
  }
}

export function $createAdmonitionLexicalNode(
  admonitionType: AdmonitionType = "note",
  title = "",
  content = "",
  design: AdmonitionDesign = "default",
  customBorderColor = "",
  customTextColor = "",
): AdmonitionLexicalNode {
  return $applyNodeReplacement(
    new AdmonitionLexicalNode(
      admonitionType,
      title,
      content,
      design,
      customBorderColor,
      customTextColor,
    ),
  );
}

export function $isAdmonitionLexicalNode(
  node: LexicalNode | null | undefined,
): node is AdmonitionLexicalNode {
  return node instanceof AdmonitionLexicalNode;
}
