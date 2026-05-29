/**
 * EquationNode — DecoratorNode storing a LaTeX equation + inline flag.
 * Ported from `packages/lexical-playground/src/nodes/EquationNode.tsx`.
 */
import * as React from "react"
import katex from "katex"
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
import EquationComponent from "./equation-component"

export type SerializedEquationNode = Spread<
  {
    equation: string
    inline: boolean
  },
  SerializedLexicalNode
>

function $convertEquationElement(domNode: HTMLElement): null | DOMConversionOutput {
  let equation = domNode.getAttribute("data-lexical-equation")
  const inline = domNode.getAttribute("data-lexical-inline") === "true"
  equation = atob(equation || "")
  if (equation) {
    return { node: $createEquationNode(equation, inline) }
  }
  return null
}

export class EquationNode extends DecoratorNode<React.JSX.Element> {
  __equation: string
  __inline: boolean

  static getType() {
    return "equation"
  }

  static clone(node: EquationNode): EquationNode {
    return new EquationNode(node.__equation, node.__inline, node.__key)
  }

  constructor(equation: string, inline?: boolean, key?: NodeKey) {
    super(key)
    this.__equation = equation
    this.__inline = inline ?? false
  }

  static importJSON(serializedNode: SerializedEquationNode): EquationNode {
    return $createEquationNode(serializedNode.equation, serializedNode.inline).updateFromJSON(
      serializedNode,
    )
  }

  exportJSON(): SerializedEquationNode {
    return {
      ...super.exportJSON(),
      equation: this.getEquation(),
      inline: this.__inline,
    }
  }

  createDOM(_config: EditorConfig): HTMLElement {
    const el = document.createElement(this.__inline ? "span" : "div")
    el.className = "editor-equation"
    return el
  }

  exportDOM(): DOMExportOutput {
    const el = document.createElement(this.__inline ? "span" : "div")
    const equation = btoa(this.__equation)
    el.setAttribute("data-lexical-equation", equation)
    el.setAttribute("data-lexical-inline", `${this.__inline}`)
    katex.render(this.__equation, el, {
      displayMode: !this.__inline,
      errorColor: "#cc0000",
      output: "html",
      strict: "warn",
      throwOnError: false,
      trust: false,
    })
    return { element: el }
  }

  static importDOM(): DOMConversionMap | null {
    return {
      div: (domNode) => {
        if (!(domNode as HTMLElement).hasAttribute("data-lexical-equation")) return null
        return { conversion: $convertEquationElement, priority: 2 }
      },
      span: (domNode) => {
        if (!(domNode as HTMLElement).hasAttribute("data-lexical-equation")) return null
        return { conversion: $convertEquationElement, priority: 1 }
      },
    }
  }

  updateDOM(prevNode: this): boolean {
    return this.__inline !== prevNode.__inline
  }

  getTextContent(): string {
    return this.__equation
  }

  getEquation(): string {
    return this.__equation
  }

  setEquation(equation: string): void {
    const writable = this.getWritable()
    writable.__equation = equation
  }

  decorate(): React.JSX.Element {
    return (
      <EquationComponent
        equation={this.__equation}
        inline={this.__inline}
        nodeKey={this.__key}
      />
    )
  }
}

export function $createEquationNode(equation = "", inline = false): EquationNode {
  return $applyNodeReplacement(new EquationNode(equation, inline))
}

export function $isEquationNode(node: LexicalNode | null | undefined): node is EquationNode {
  return node instanceof EquationNode
}
