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
import { DividerLexicalComponent } from "./divider-component";

export type DividerStyle =
  "simple" | "double" | "dashed" | "dotted" | "gradient";
export type DividerThickness = "thin" | "medium" | "thick";
export type DividerSpacing = "xs" | "sm" | "md" | "lg" | "xl";
export type DividerColorPalette =
  "blue" | "green" | "orange" | "red" | "purple" | "custom";

export type SerializedDividerLexicalNode = Spread<
  {
    style: DividerStyle;
    thickness: DividerThickness;
    spacing: DividerSpacing;
    colorPalette: DividerColorPalette;
    customColor: string | null;
  },
  SerializedLexicalNode
>;

function $convertDividerElement(
  _domNode: HTMLElement,
): null | DOMConversionOutput {
  const node = $createDividerLexicalNode();
  return { node };
}

export class DividerLexicalNode extends DecoratorNode<React.JSX.Element> {
  __style: DividerStyle;
  __thickness: DividerThickness;
  __spacing: DividerSpacing;
  __colorPalette: DividerColorPalette;
  __customColor: string | null;

  static getType() {
    return "lexical-divider";
  }

  static clone(node: DividerLexicalNode): DividerLexicalNode {
    return new DividerLexicalNode(
      node.__style,
      node.__thickness,
      node.__spacing,
      node.__colorPalette,
      node.__customColor,
      node.__key,
    );
  }

  constructor(
    style?: DividerStyle,
    thickness?: DividerThickness,
    spacing?: DividerSpacing,
    colorPalette?: DividerColorPalette,
    customColor?: string | null,
    key?: NodeKey,
  ) {
    super(key);
    this.__style = style ?? "simple";
    this.__thickness = thickness ?? "medium";
    this.__spacing = spacing ?? "md";
    this.__colorPalette = colorPalette ?? "blue";
    this.__customColor = customColor ?? null;
  }

  static importJSON(s: SerializedDividerLexicalNode): DividerLexicalNode {
    return $applyNodeReplacement(
      new DividerLexicalNode(
        s.style,
        s.thickness,
        s.spacing,
        s.colorPalette,
        s.customColor,
      ),
    );
  }

  exportJSON(): SerializedDividerLexicalNode {
    return {
      ...super.exportJSON(),
      style: this.__style,
      thickness: this.__thickness,
      spacing: this.__spacing,
      colorPalette: this.__colorPalette,
      customColor: this.__customColor,
    };
  }

  createDOM(_config: EditorConfig): HTMLElement {
    const el = document.createElement("div");
    el.className = "lexical-divider-wrapper";
    return el;
  }

  exportDOM(): DOMExportOutput {
    const el = document.createElement("hr");
    el.setAttribute("data-lexical-divider", "true");
    return { element: el };
  }

  static importDOM(): DOMConversionMap | null {
    return {
      hr: (domNode) => {
        if (!(domNode as HTMLElement).hasAttribute("data-lexical-divider"))
          return null;
        return { conversion: $convertDividerElement, priority: 2 };
      },
    };
  }

  updateDOM(_prevNode: this): boolean {
    return false;
  }

  // ── Getters / Setters ──
  getStyle(): DividerStyle {
    return this.__style;
  }
  setStyle(v: DividerStyle): void {
    this.getWritable().__style = v;
  }
  getThickness(): DividerThickness {
    return this.__thickness;
  }
  setThickness(v: DividerThickness): void {
    this.getWritable().__thickness = v;
  }
  getSpacing(): DividerSpacing {
    return this.__spacing;
  }
  setSpacing(v: DividerSpacing): void {
    this.getWritable().__spacing = v;
  }
  getColorPalette(): DividerColorPalette {
    return this.__colorPalette;
  }
  setColorPalette(v: DividerColorPalette): void {
    this.getWritable().__colorPalette = v;
  }
  getCustomColor(): string | null {
    return this.__customColor;
  }
  setCustomColor(v: string | null): void {
    this.getWritable().__customColor = v;
  }

  decorate(): React.JSX.Element {
    return (
      <DividerLexicalComponent
        style={this.__style}
        thickness={this.__thickness}
        spacing={this.__spacing}
        colorPalette={this.__colorPalette}
        customColor={this.__customColor}
        nodeKey={this.__key}
      />
    );
  }
}

export function $createDividerLexicalNode(
  style: DividerStyle = "simple",
  thickness: DividerThickness = "medium",
  spacing: DividerSpacing = "md",
): DividerLexicalNode {
  return $applyNodeReplacement(
    new DividerLexicalNode(style, thickness, spacing),
  );
}

export function $isDividerLexicalNode(
  node: LexicalNode | null | undefined,
): node is DividerLexicalNode {
  return node instanceof DividerLexicalNode;
}
