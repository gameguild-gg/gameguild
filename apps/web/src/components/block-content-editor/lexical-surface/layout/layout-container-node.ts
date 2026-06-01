/**
 * LayoutContainerNode — root of a "columns layout" block.
 *
 * Ported from `lexical-playground/src/nodes/LayoutContainerNode`. Holds
 * a CSS grid `template-columns` (e.g. `"1fr 1fr"`) and contains one or
 * more `LayoutItemNode` children, one per column.
 */
import {
  ElementNode,
  type DOMExportOutput,
  type EditorConfig,
  type LexicalNode,
  type LexicalUpdateJSON,
  type NodeKey,
  type SerializedElementNode,
  type Spread,
} from "lexical"
import { addClassNamesToElement } from "@lexical/utils"

export type LayoutBorderStyle = "solid"

type LayoutContainerStyleOptions = {
  borderAlwaysVisible: boolean
  borderStyle: LayoutBorderStyle
  borderColor: string | null
}

export type SerializedLayoutContainerNode = Spread<
  {
    templateColumns: string
    borderAlwaysVisible?: boolean
    borderStyle?: LayoutBorderStyle
    borderColor?: string | null
  },
  SerializedElementNode
>

const DEFAULT_LAYOUT_STYLE: LayoutContainerStyleOptions = {
  borderAlwaysVisible: false,
  borderStyle: "solid",
  borderColor: null,
}

function isLayoutBorderStyle(value: unknown): value is LayoutBorderStyle {
  return value === "solid"
}

export class LayoutContainerNode extends ElementNode {
  __templateColumns: string
  __borderAlwaysVisible: boolean
  __borderStyle: LayoutBorderStyle
  __borderColor: string | null

  constructor(
    templateColumns: string,
    options: Partial<LayoutContainerStyleOptions> = {},
    key?: NodeKey,
  ) {
    super(key)
    this.__templateColumns = templateColumns
    this.__borderAlwaysVisible =
      options.borderAlwaysVisible ?? DEFAULT_LAYOUT_STYLE.borderAlwaysVisible
    this.__borderStyle = options.borderStyle ?? DEFAULT_LAYOUT_STYLE.borderStyle
    this.__borderColor =
      typeof options.borderColor === "string" && options.borderColor.trim() !== ""
        ? options.borderColor
        : null
  }

  static getType(): string {
    return "layout-container"
  }

  static clone(node: LayoutContainerNode): LayoutContainerNode {
    return new LayoutContainerNode(
      node.__templateColumns,
      {
        borderAlwaysVisible: node.__borderAlwaysVisible,
        borderStyle: node.__borderStyle,
        borderColor: node.__borderColor,
      },
      node.__key,
    )
  }

  private applyBorderPresentation(dom: HTMLElement): void {
    dom.setAttribute(
      "data-layout-border-always",
      this.__borderAlwaysVisible ? "true" : "false",
    )
    dom.setAttribute("data-layout-border-style", this.__borderStyle)
    dom.style.setProperty("--layout-border-style", this.__borderStyle)
    if (this.__borderColor) {
      dom.style.setProperty("--layout-border-color", this.__borderColor)
    } else {
      dom.style.removeProperty("--layout-border-color")
    }
  }

  createDOM(config: EditorConfig): HTMLElement {
    const dom = document.createElement("div")
    dom.style.display = "grid"
    dom.style.gap = "10px"
    dom.style.gridTemplateColumns = this.__templateColumns
    dom.setAttribute("data-lexical-layout-container", "true")
    this.applyBorderPresentation(dom)
    const themeClass = (config.theme as Record<string, unknown>).layoutContainer
    if (typeof themeClass === "string") {
      addClassNamesToElement(dom, themeClass)
    }
    return dom
  }

  updateDOM(prevNode: this, dom: HTMLElement): boolean {
    if (prevNode.__templateColumns !== this.__templateColumns) {
      dom.style.gridTemplateColumns = this.__templateColumns
    }
    if (
      prevNode.__borderAlwaysVisible !== this.__borderAlwaysVisible ||
      prevNode.__borderStyle !== this.__borderStyle ||
      prevNode.__borderColor !== this.__borderColor
    ) {
      this.applyBorderPresentation(dom)
    }
    return false
  }

  exportDOM(): DOMExportOutput {
    const element = document.createElement("div")
    element.style.display = "grid"
    element.style.gap = "10px"
    element.style.gridTemplateColumns = this.__templateColumns
    element.setAttribute("data-lexical-layout-container", "true")
    element.setAttribute(
      "data-layout-border-always",
      this.__borderAlwaysVisible ? "true" : "false",
    )
    element.setAttribute("data-layout-border-style", this.__borderStyle)
    if (this.__borderColor) {
      element.setAttribute("data-layout-border-color", this.__borderColor)
      element.style.setProperty("--layout-border-color", this.__borderColor)
    }
    element.style.setProperty("--layout-border-style", this.__borderStyle)
    return { element }
  }

  static importJSON(json: SerializedLayoutContainerNode): LayoutContainerNode {
    return $createLayoutContainerNode(json.templateColumns, {
      borderAlwaysVisible: json.borderAlwaysVisible === true,
      borderStyle: isLayoutBorderStyle(json.borderStyle)
        ? json.borderStyle
        : DEFAULT_LAYOUT_STYLE.borderStyle,
      borderColor:
        typeof json.borderColor === "string" && json.borderColor.trim() !== ""
          ? json.borderColor
          : null,
    }).updateFromJSON(json)
  }

  updateFromJSON(
    serializedNode: LexicalUpdateJSON<SerializedLayoutContainerNode>,
  ): this {
    const borderStyle = isLayoutBorderStyle(serializedNode.borderStyle)
      ? serializedNode.borderStyle
      : DEFAULT_LAYOUT_STYLE.borderStyle
    const borderColor =
      typeof serializedNode.borderColor === "string" &&
      serializedNode.borderColor.trim() !== ""
        ? serializedNode.borderColor
        : null

    return super
      .updateFromJSON(serializedNode)
      .setTemplateColumns(serializedNode.templateColumns)
      .setBorderAlwaysVisible(serializedNode.borderAlwaysVisible === true)
      .setBorderStyle(borderStyle)
      .setBorderColor(borderColor)
  }

  isShadowRoot(): boolean {
    return true
  }

  canBeEmpty(): boolean {
    return false
  }

  exportJSON(): SerializedLayoutContainerNode {
    return {
      ...super.exportJSON(),
      templateColumns: this.__templateColumns,
      borderAlwaysVisible: this.__borderAlwaysVisible,
      borderStyle: this.__borderStyle,
      borderColor: this.__borderColor,
    }
  }

  getTemplateColumns(): string {
    return this.getLatest().__templateColumns
  }

  setTemplateColumns(templateColumns: string): this {
    const self = this.getWritable()
    self.__templateColumns = templateColumns
    return self
  }

  getBorderAlwaysVisible(): boolean {
    return this.getLatest().__borderAlwaysVisible
  }

  setBorderAlwaysVisible(next: boolean): this {
    const self = this.getWritable()
    self.__borderAlwaysVisible = next
    return self
  }

  getBorderStyle(): LayoutBorderStyle {
    return this.getLatest().__borderStyle
  }

  setBorderStyle(next: LayoutBorderStyle): this {
    const self = this.getWritable()
    void next
    self.__borderStyle = "solid"
    return self
  }

  getBorderColor(): string | null {
    return this.getLatest().__borderColor
  }

  setBorderColor(next: string | null): this {
    const self = this.getWritable()
    self.__borderColor =
      typeof next === "string" && next.trim() !== "" ? next : null
    return self
  }
}

export function $createLayoutContainerNode(
  templateColumns: string = "1fr 1fr",
  options: Partial<LayoutContainerStyleOptions> = {},
): LayoutContainerNode {
  return new LayoutContainerNode(templateColumns, options)
}

export function $isLayoutContainerNode(
  node: LexicalNode | null | undefined,
): node is LayoutContainerNode {
  return node instanceof LayoutContainerNode
}
