/**
 * FigmaNode — `DecoratorBlockNode` rendering an embedded Figma file.
 * Ported from `lexical-playground/src/nodes/FigmaNode.tsx`.
 */
import * as React from "react"
import { BlockWithAlignableContents } from "@lexical/react/LexicalBlockWithAlignableContents"
import {
  DecoratorBlockNode,
  type SerializedDecoratorBlockNode,
} from "@lexical/react/LexicalDecoratorBlockNode"
import type {
  EditorConfig,
  ElementFormatType,
  LexicalNode,
  NodeKey,
  Spread,
} from "lexical"

type FigmaComponentProps = Readonly<{
  className: Readonly<{ base: string; focus: string }>
  format: ElementFormatType | null
  nodeKey: NodeKey
  documentID: string
}>

function FigmaComponent({ className, format, nodeKey, documentID }: FigmaComponentProps) {
  return (
    <BlockWithAlignableContents className={className} format={format} nodeKey={nodeKey}>
      <iframe
        width="560"
        height="315"
        src={`https://www.figma.com/embed?embed_host=lexical&url=https://www.figma.com/file/${documentID}`}
        allowFullScreen
      />
    </BlockWithAlignableContents>
  )
}

export type SerializedFigmaNode = Spread<{ documentID: string }, SerializedDecoratorBlockNode>

export class FigmaNode extends DecoratorBlockNode {
  __id: string

  static getType(): string {
    return "figma"
  }

  static clone(node: FigmaNode): FigmaNode {
    return new FigmaNode(node.__id, node.__format, node.__key)
  }

  static importJSON(serializedNode: SerializedFigmaNode): FigmaNode {
    return $createFigmaNode(serializedNode.documentID).updateFromJSON(serializedNode)
  }

  exportJSON(): SerializedFigmaNode {
    return { ...super.exportJSON(), documentID: this.__id }
  }

  constructor(id: string, format?: ElementFormatType, key?: NodeKey) {
    super(format, key)
    this.__id = id
  }

  updateDOM(): false {
    return false
  }

  getId(): string {
    return this.getLatest().__id
  }

  getTextContent(): string {
    return `https://www.figma.com/file/${this.__id}`
  }

  decorate(_editor: unknown, config: EditorConfig): React.JSX.Element {
    const embedBlockTheme = (config.theme as { embedBlock?: { base?: string; focus?: string } }).embedBlock ?? {}
    const className = { base: embedBlockTheme.base ?? "", focus: embedBlockTheme.focus ?? "" }
    return (
      <FigmaComponent
        className={className}
        format={this.__format}
        nodeKey={this.getKey()}
        documentID={this.__id}
      />
    )
  }
}

export function $createFigmaNode(documentID: string): FigmaNode {
  return new FigmaNode(documentID)
}

export function $isFigmaNode(
  node: FigmaNode | LexicalNode | null | undefined,
): node is FigmaNode {
  return node instanceof FigmaNode
}
