/**
 * MermaidLexicalNode — DecoratorNode storing mermaid diagram code,
 * type, theme, title, caption, and size settings.
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
import { MermaidLexicalComponent } from "./mermaid-component"

export type MermaidDiagramType =
  | "flowchart"
  | "class"
  | "sequence"
  | "xyChart"
  | "radar"
  | "quadrant"
  | "sankey"
  | "state"
  | "c4context"
  | "architecture"
  | "er"
  | "gantt"
  | "pie"
  | "gitgraph"
  | "mindmap"
  | "journey"
  | "timeline"
  | "quadrantChart"
  | "requirement"
  | "c4Context"
  | "c4Container"
  | "c4Component"
  | "c4Dynamic"
  | "c4Deployment"
  | "treemap-beta"
  | "kanban"

export type MermaidThemeName =
  | "default"
  | "dark"
  | "forest"
  | "neutral"
  | "base"
  | "default-dark"
  | "forest-dark"
  | "neutral-dark"
  | "base-dark"

export type MermaidThemeMode = "system" | "light" | "dark" | "both"

export type SerializedMermaidLexicalNode = Spread<
  {
    code: string
    diagramType: MermaidDiagramType
    theme: MermaidThemeName
    themeMode: MermaidThemeMode
    title: string
    caption: string
    size: number
  },
  SerializedLexicalNode
>

function $convertMermaidElement(domNode: HTMLElement): null | DOMConversionOutput {
  const code = domNode.getAttribute("data-lexical-mermaid-code") || ""
  const node = $createMermaidLexicalNode(code)
  return { node }
}

export class MermaidLexicalNode extends DecoratorNode<React.JSX.Element> {
  __code: string
  __diagramType: MermaidDiagramType
  __theme: MermaidThemeName
  __themeMode: MermaidThemeMode
  __title: string
  __caption: string
  __size: number

  static getType() {
    return "lexical-mermaid"
  }

  static clone(node: MermaidLexicalNode): MermaidLexicalNode {
    return new MermaidLexicalNode(
      node.__code,
      node.__diagramType,
      node.__theme,
      node.__themeMode,
      node.__title,
      node.__caption,
      node.__size,
      node.__key,
    )
  }

  constructor(
    code?: string,
    diagramType?: MermaidDiagramType,
    theme?: MermaidThemeName,
    themeMode?: MermaidThemeMode,
    title?: string,
    caption?: string,
    size?: number,
    key?: NodeKey,
  ) {
    super(key)
    this.__code = code ?? ""
    this.__diagramType = diagramType ?? "flowchart"
    this.__theme = theme ?? "default"
    this.__themeMode = themeMode ?? "system"
    this.__title = title ?? ""
    this.__caption = caption ?? ""
    this.__size = size ?? 100
  }

  static importJSON(s: SerializedMermaidLexicalNode): MermaidLexicalNode {
    return $applyNodeReplacement(new MermaidLexicalNode(
      s.code, s.diagramType, s.theme, s.themeMode, s.title, s.caption, s.size,
    ))
  }

  exportJSON(): SerializedMermaidLexicalNode {
    return {
      ...super.exportJSON(),
      code: this.__code,
      diagramType: this.__diagramType,
      theme: this.__theme,
      themeMode: this.__themeMode,
      title: this.__title,
      caption: this.__caption,
      size: this.__size,
    }
  }

  createDOM(_config: EditorConfig): HTMLElement {
    const el = document.createElement("div")
    el.className = "lexical-mermaid-wrapper my-4"
    return el
  }

  exportDOM(): DOMExportOutput {
    const el = document.createElement("div")
    el.setAttribute("data-lexical-mermaid", "true")
    el.setAttribute("data-lexical-mermaid-code", this.__code)
    el.setAttribute("data-lexical-mermaid-type", this.__diagramType)
    el.textContent = this.__code
    return { element: el }
  }

  static importDOM(): DOMConversionMap | null {
    return {
      div: (domNode) => {
        if (!(domNode as HTMLElement).hasAttribute("data-lexical-mermaid")) return null
        return { conversion: $convertMermaidElement, priority: 2 }
      },
    }
  }

  updateDOM(_prevNode: this): boolean { return false }
  getTextContent(): string { return this.__code }

  // ── Getters / Setters ──
  getCode(): string { return this.__code }
  setCode(v: string): void { this.getWritable().__code = v }
  getDiagramType(): MermaidDiagramType { return this.__diagramType }
  setDiagramType(v: MermaidDiagramType): void { this.getWritable().__diagramType = v }
  getTheme(): MermaidThemeName { return this.__theme }
  setTheme(v: MermaidThemeName): void { this.getWritable().__theme = v }
  getThemeMode(): MermaidThemeMode { return this.__themeMode }
  setThemeMode(v: MermaidThemeMode): void { this.getWritable().__themeMode = v }
  getTitle(): string { return this.__title }
  setTitle(v: string): void { this.getWritable().__title = v }
  getCaption(): string { return this.__caption }
  setCaption(v: string): void { this.getWritable().__caption = v }
  getSize(): number { return this.__size }
  setSize(v: number): void { this.getWritable().__size = v }

  decorate(): React.JSX.Element {
    return (
      <MermaidLexicalComponent
        code={this.__code}
        diagramType={this.__diagramType}
        theme={this.__theme}
        themeMode={this.__themeMode}
        title={this.__title}
        caption={this.__caption}
        size={this.__size}
        nodeKey={this.__key}
      />
    )
  }
}

export function $createMermaidLexicalNode(
  code = "",
  diagramType: MermaidDiagramType = "flowchart",
): MermaidLexicalNode {
  return $applyNodeReplacement(
    new MermaidLexicalNode(code, diagramType),
  )
}

export function $isMermaidLexicalNode(
  node: LexicalNode | null | undefined,
): node is MermaidLexicalNode {
  return node instanceof MermaidLexicalNode
}
