/**
 * VegaLiteLexicalNode — DecoratorNode storing Vega-Lite chart specifications,
 * title, caption, size, theme, themeMode, layout, and attached CSV/JSON datasets.
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
import { VegaLiteLexicalComponent } from "./vega-lite-component";
import type { VegaLiteData } from "./vega-lite-data";

export type SerializedVegaLiteLexicalNode = Spread<
  VegaLiteData,
  SerializedLexicalNode
>;

function $convertVegaLiteElement(
  domNode: HTMLElement,
): null | DOMConversionOutput {
  const spec = domNode.getAttribute("data-lexical-vega-lite-spec") || "{}";
  const node = $createVegaLiteLexicalNode(spec);
  return { node };
}

export class VegaLiteLexicalNode extends DecoratorNode<React.JSX.Element> {
  __spec: string;
  __title: string;
  __caption: string;
  __size: number;
  __theme: NonNullable<VegaLiteData["theme"]>;
  __themeMode: NonNullable<VegaLiteData["themeMode"]>;
  __layout: NonNullable<VegaLiteData["layout"]>;
  __data: Record<string, string>;

  static getType() {
    return "lexical-vega-lite";
  }

  static clone(node: VegaLiteLexicalNode): VegaLiteLexicalNode {
    return new VegaLiteLexicalNode(
      node.__spec,
      node.__title,
      node.__caption,
      node.__size,
      node.__theme,
      node.__themeMode,
      node.__layout,
      node.__data,
      node.__key,
    );
  }

  constructor(
    spec?: string,
    title?: string,
    caption?: string,
    size?: number,
    theme?: VegaLiteData["theme"],
    themeMode?: VegaLiteData["themeMode"],
    layout?: VegaLiteData["layout"],
    data?: Record<string, string>,
    key?: NodeKey,
  ) {
    super(key);
    this.__spec = spec ?? "{}";
    this.__title = title ?? "";
    this.__caption = caption ?? "";
    this.__size = size ?? 100;
    this.__theme = theme ?? "default";
    this.__themeMode = themeMode ?? "system";
    this.__layout = layout ?? "rectangular";
    this.__data = data ?? {};
  }

  static importJSON(
    serializedNode: SerializedLexicalNode & Record<string, unknown>,
  ): VegaLiteLexicalNode {
    const s = serializedNode as Partial<SerializedVegaLiteLexicalNode>;
    return $applyNodeReplacement(
      new VegaLiteLexicalNode(
        s.spec,
        s.title,
        s.caption,
        s.size,
        s.theme,
        s.themeMode,
        s.layout,
        s.data,
      ),
    );
  }

  exportJSON(): SerializedVegaLiteLexicalNode {
    return {
      ...super.exportJSON(),
      spec: this.__spec,
      title: this.__title,
      caption: this.__caption,
      size: this.__size,
      theme: this.__theme,
      themeMode: this.__themeMode,
      layout: this.__layout,
      data: this.__data,
    };
  }

  createDOM(_config: EditorConfig): HTMLElement {
    const el = document.createElement("div");
    el.className = "lexical-vega-lite-wrapper my-4";
    return el;
  }

  exportDOM(): DOMExportOutput {
    const el = document.createElement("div");
    el.setAttribute("data-lexical-vega-lite", "true");
    el.setAttribute("data-lexical-vega-lite-spec", this.__spec);
    el.textContent = this.__spec;
    return { element: el };
  }

  static importDOM(): DOMConversionMap | null {
    return {
      div: (domNode) => {
        if (!(domNode as HTMLElement).hasAttribute("data-lexical-vega-lite"))
          return null;
        return { conversion: $convertVegaLiteElement, priority: 2 };
      },
    };
  }

  updateDOM(_prevNode: this): boolean {
    return false;
  }
  getTextContent(): string {
    return this.__spec;
  }

  // ── Getters / Setters ──
  getSpec(): string {
    return this.__spec;
  }
  setSpec(v: string): void {
    this.getWritable().__spec = v;
  }
  getTitle(): string {
    return this.__title;
  }
  setTitle(v: string): void {
    this.getWritable().__title = v;
  }
  getCaption(): string {
    return this.__caption;
  }
  setCaption(v: string): void {
    this.getWritable().__caption = v;
  }
  getSize(): number {
    return this.__size;
  }
  setSize(v: number): void {
    this.getWritable().__size = v;
  }
  getTheme(): NonNullable<VegaLiteData["theme"]> {
    return this.__theme;
  }
  setTheme(v: NonNullable<VegaLiteData["theme"]>): void {
    this.getWritable().__theme = v;
  }
  getThemeMode(): NonNullable<VegaLiteData["themeMode"]> {
    return this.__themeMode;
  }
  setThemeMode(v: NonNullable<VegaLiteData["themeMode"]>): void {
    this.getWritable().__themeMode = v;
  }
  getLayout(): NonNullable<VegaLiteData["layout"]> {
    return this.__layout;
  }
  setLayout(v: NonNullable<VegaLiteData["layout"]>): void {
    this.getWritable().__layout = v;
  }
  getData(): Record<string, string> {
    return this.__data;
  }
  setData(v: Record<string, string>): void {
    this.getWritable().__data = v;
  }

  decorate(): React.JSX.Element {
    return (
      <VegaLiteLexicalComponent
        spec={this.__spec}
        title={this.__title}
        caption={this.__caption}
        size={this.__size}
        theme={this.__theme}
        themeMode={this.__themeMode}
        layout={this.__layout}
        data={this.__data}
        nodeKey={this.__key}
      />
    );
  }
}

export function $createVegaLiteLexicalNode(spec = "{}"): VegaLiteLexicalNode {
  return $applyNodeReplacement(new VegaLiteLexicalNode(spec));
}

export function $isVegaLiteLexicalNode(
  node: LexicalNode | null | undefined,
): node is VegaLiteLexicalNode {
  return node instanceof VegaLiteLexicalNode;
}
