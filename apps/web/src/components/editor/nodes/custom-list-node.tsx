"use client"

import { ListNode, SerializedListNode, ListType } from "@lexical/list"
import type { LexicalNode, NodeKey, EditorConfig } from "lexical"

export interface SerializedCustomListNode extends SerializedListNode {
  listStyleType?: string
}

export class CustomListNode extends ListNode {
  __listStyleType: string

  static getType(): string {
    return "custom-list"
  }

  constructor(listType: ListType, start: number, listStyleType?: string, key?: NodeKey) {
    super(listType, start, key)
    this.__listStyleType = listStyleType || "decimal"
  }

  getListStyleType(): string {
    return this.__listStyleType
  }

  setListStyleType(listStyleType: string): void {
    const writable = this.getWritable()
    writable.__listStyleType = listStyleType
  }

  static clone(node: CustomListNode): CustomListNode {
    return new CustomListNode(
      node.getListType(),
      node.getStart(),
      node.__listStyleType,
      node.__key
    )
  }

  createDOM(config: EditorConfig): HTMLElement {
    const element = super.createDOM(config)
    if (this.getListType() === "number") {
      // Limpar todas as classes CSS do tema
      element.className = ""
      // Aplicar apenas os estilos necessários
      element.style.listStyleType = this.__listStyleType || "decimal"
      element.style.listStylePosition = "inside"
      element.style.paddingLeft = "1rem"
      element.style.marginTop = "1rem"
      element.style.marginBottom = "1rem"
      element.setAttribute("data-list-style-type", this.__listStyleType || "decimal")
    }
    return element
  }

  updateDOM(prevNode: CustomListNode, dom: HTMLElement, config: EditorConfig): boolean {
    const result = super.updateDOM(prevNode as this, dom, config)
    if (this.getListType() === "number") {
      // Limpar todas as classes CSS do tema
      dom.className = ""
      // Aplicar apenas os estilos necessários
      dom.style.listStyleType = this.__listStyleType || "decimal"
      dom.style.listStylePosition = "inside"
      dom.style.paddingLeft = "1rem"
      dom.style.marginTop = "1rem"
      dom.style.marginBottom = "1rem"
      dom.setAttribute("data-list-style-type", this.__listStyleType || "decimal")
    }
    return result
  }

  static importJSON(serializedNode: SerializedCustomListNode): CustomListNode {
    const { listType, start, listStyleType } = serializedNode
    return new CustomListNode(listType as ListType, start, listStyleType)
  }

  exportJSON(): SerializedCustomListNode {
    return {
      ...super.exportJSON(),
      listStyleType: this.__listStyleType,
      type: "custom-list",
    }
  }
}

export function $createCustomListNode(
  listType: ListType,
  start = 1,
  listStyleType = "decimal"
): CustomListNode {
  return new CustomListNode(listType, start, listStyleType)
}

export function $isCustomListNode(
  node: LexicalNode | null | undefined
): node is CustomListNode {
  return node instanceof CustomListNode
}
