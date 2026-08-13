/**
 * EquationNode — DecoratorNode storing a LaTeX equation + inline flag.
 * Ported from `packages/lexical-playground/src/nodes/EquationNode.tsx`.
 */
import * as React from "react";
import katex from "katex";
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
import EquationComponent from "./equation-component";

export type EquationAlign = "left" | "center" | "right";

export type SerializedEquationNode = Spread<
  {
    equation: string;
    inline: boolean;
    fontSize?: number;
    align?: EquationAlign;
  },
  SerializedLexicalNode
>;

function $convertEquationElement(
  domNode: HTMLElement,
): null | DOMConversionOutput {
  let equation = domNode.getAttribute("data-lexical-equation");
  const inline = domNode.getAttribute("data-lexical-inline") === "true";
  const fontSizeAttr = domNode.getAttribute("data-lexical-equation-font-size");
  const alignAttr = domNode.getAttribute(
    "data-lexical-equation-align",
  ) as EquationAlign | null;
  equation = atob(equation || "");
  if (equation) {
    const node = $createEquationNode(equation, inline);
    if (fontSizeAttr) node.setFontSize(parseFloat(fontSizeAttr));
    if (
      alignAttr === "left" ||
      alignAttr === "center" ||
      alignAttr === "right"
    ) {
      node.setAlign(alignAttr);
    }
    return { node };
  }
  return null;
}

export class EquationNode extends DecoratorNode<React.JSX.Element> {
  __equation: string;
  __inline: boolean;
  __fontSize: number;
  __align: EquationAlign;

  static getType() {
    return "equation";
  }

  static clone(node: EquationNode): EquationNode {
    return new EquationNode(
      node.__equation,
      node.__inline,
      node.__key,
      node.__fontSize,
      node.__align,
    );
  }

  constructor(
    equation: string,
    inline?: boolean,
    key?: NodeKey,
    fontSize?: number,
    align?: EquationAlign,
  ) {
    super(key);
    this.__equation = equation;
    this.__inline = inline ?? false;
    this.__fontSize = fontSize ?? 1;
    this.__align = align ?? "left";
  }

  static importJSON(serializedNode: SerializedEquationNode): EquationNode {
    const node = $createEquationNode(
      serializedNode.equation,
      serializedNode.inline,
    ).updateFromJSON(serializedNode);
    if (serializedNode.fontSize) node.setFontSize(serializedNode.fontSize);
    if (serializedNode.align) node.setAlign(serializedNode.align);
    return node;
  }

  exportJSON(): SerializedEquationNode {
    return {
      ...super.exportJSON(),
      equation: this.getEquation(),
      inline: this.__inline,
      fontSize: this.__fontSize,
      align: this.__align,
    };
  }

  createDOM(_config: EditorConfig): HTMLElement {
    const el = document.createElement(this.__inline ? "span" : "div");
    el.className = "editor-equation";
    if (!this.__inline) {
      el.style.textAlign = this.__align;
    }
    return el;
  }

  exportDOM(): DOMExportOutput {
    const el = document.createElement(this.__inline ? "span" : "div");
    const equation = btoa(this.__equation);
    el.setAttribute("data-lexical-equation", equation);
    el.setAttribute("data-lexical-inline", `${this.__inline}`);
    el.setAttribute("data-lexical-equation-font-size", `${this.__fontSize}`);
    el.setAttribute("data-lexical-equation-align", this.__align);
    if (!this.__inline) el.style.textAlign = this.__align;
    el.style.fontSize = `${this.__fontSize}em`;
    katex.render(this.__equation, el, {
      displayMode: !this.__inline,
      errorColor: "#cc0000",
      output: "html",
      strict: "warn",
      throwOnError: false,
      trust: false,
    });
    return { element: el };
  }

  static importDOM(): DOMConversionMap | null {
    return {
      div: (domNode) => {
        if (!(domNode as HTMLElement).hasAttribute("data-lexical-equation"))
          return null;
        return { conversion: $convertEquationElement, priority: 2 };
      },
      span: (domNode) => {
        if (!(domNode as HTMLElement).hasAttribute("data-lexical-equation"))
          return null;
        return { conversion: $convertEquationElement, priority: 1 };
      },
    };
  }

  updateDOM(prevNode: this): boolean {
    return (
      this.__inline !== prevNode.__inline || this.__align !== prevNode.__align
    );
  }

  getTextContent(): string {
    return this.__equation;
  }

  getEquation(): string {
    return this.__equation;
  }

  setEquation(equation: string): void {
    const writable = this.getWritable();
    writable.__equation = equation;
  }

  getFontSize(): number {
    return this.__fontSize;
  }

  setFontSize(fontSize: number): void {
    const writable = this.getWritable();
    writable.__fontSize = fontSize;
  }

  getAlign(): EquationAlign {
    return this.__align;
  }

  setAlign(align: EquationAlign): void {
    const writable = this.getWritable();
    writable.__align = align;
  }

  decorate(): React.JSX.Element {
    return (
      <EquationComponent
        equation={this.__equation}
        inline={this.__inline}
        fontSize={this.__fontSize}
        align={this.__align}
        nodeKey={this.__key}
      />
    );
  }
}

export function $createEquationNode(
  equation = "",
  inline = false,
  fontSize?: number,
  align?: EquationAlign,
): EquationNode {
  return $applyNodeReplacement(
    new EquationNode(equation, inline, undefined, fontSize, align),
  );
}

export function $isEquationNode(
  node: LexicalNode | null | undefined,
): node is EquationNode {
  return node instanceof EquationNode;
}
