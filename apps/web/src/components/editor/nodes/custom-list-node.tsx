"use client"

import { ListNode, SerializedListNode, ListType } from "@lexical/list"
import type { LexicalNode, NodeKey, EditorConfig } from "lexical"

export interface SerializedCustomListNode extends SerializedListNode {
  listStyleType?: string
}

export class CustomListNode extends ListNode {
  __listStyleType: string

  static getType(): string {
    return "list"  // Usar o tipo padrão para compatibilidade
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
    
    // Limpar todas as classes CSS do tema
    element.className = ""
    
    // Aplicar estilos baseado no tipo de lista (tanto ordered quanto unordered)
    this.applyListStyles(element)
    
    // Definir atributo apropriado baseado no tipo
    if (this.getListType() === "number") {
      element.setAttribute("data-list-style-type", this.__listStyleType || "decimal")
    } else {
      element.setAttribute("data-list-style-type", this.__listStyleType || "disc")
    }
    
    return element
  }

  updateDOM(prevNode: CustomListNode, dom: HTMLElement, config: EditorConfig): boolean {
    const result = super.updateDOM(prevNode as this, dom, config)
    
    // Limpar todas as classes CSS do tema
    dom.className = ""
    
    // Aplicar estilos baseado no tipo de lista (tanto ordered quanto unordered)
    this.applyListStyles(dom)
    
    // Definir atributo apropriado baseado no tipo
    if (this.getListType() === "number") {
      dom.setAttribute("data-list-style-type", this.__listStyleType || "decimal")
    } else {
      dom.setAttribute("data-list-style-type", this.__listStyleType || "disc")
    }
    
    return result
  }

  private applyListStyles(element: HTMLElement): void {
    const listType = this.__listStyleType || "decimal"
    
    // Estilos básicos para todas as listas
    element.style.listStylePosition = "inside"
    element.style.paddingLeft = "1rem"
    element.style.marginTop = "1rem"
    element.style.marginBottom = "1rem"
    
    // Estilos específicos por tipo
    switch (listType) {
      case "decimal":
      case "upper-alpha":
      case "lower-alpha":
      case "upper-roman":
      case "lower-roman":
      case "decimal-leading-zero":
        element.style.listStyleType = listType
        break
      // Estilos para listas não ordenadas (bullet)
      case "disc":
      case "circle":
      case "square":
        element.style.listStyleType = listType
        break
      case "arrow":
        element.style.listStyleType = "none"
        element.setAttribute("data-arrow-list", "true")
        this.addArrowListStyles(element)
        break
      case "star":
        element.style.listStyleType = "none"
        element.setAttribute("data-star-list", "true")
        this.addStarListStyles(element)
        break
      default:
        // Determinar estilo padrão baseado no tipo de lista
        if (this.getListType() === "bullet") {
          element.style.listStyleType = "disc"
        } else {
          element.style.listStyleType = "decimal"
        }
    }
  }

  private addArrowListStyles(element: HTMLElement): void {
    // Criar estilos CSS para setas se ainda não existirem
    if (!document.querySelector('#arrow-list-style')) {
      const style = document.createElement('style')
      style.id = 'arrow-list-style'
      style.textContent = `
        ol[data-arrow-list="true"], ul[data-arrow-list="true"] {
          list-style: none;
        }
        ol[data-arrow-list="true"] li::before, ul[data-arrow-list="true"] li::before {
          content: "▶";
          font-weight: bold;
          color: oklch(0.488 0.243 264.376);
          margin-right: 0.5rem;
          display: inline-block;
        }
      `
      document.head.appendChild(style)
    }
  }

  private addStarListStyles(element: HTMLElement): void {
    // Criar estilos CSS para estrelas se ainda não existirem
    if (!document.querySelector('#star-list-style')) {
      const style = document.createElement('style')
      style.id = 'star-list-style'
      style.textContent = `
        ol[data-star-list="true"], ul[data-star-list="true"] {
          list-style: none;
        }
        ol[data-star-list="true"] li::before, ul[data-star-list="true"] li::before {
          content: "★";
          font-weight: bold;
          color: oklch(0.488 0.243 264.376);
          margin-right: 0.5rem;
          display: inline-block;
        }
      `
      document.head.appendChild(style)
    }
  }

  static importJSON(serializedNode: SerializedCustomListNode): CustomListNode {
    const { listType, start, listStyleType } = serializedNode
    return new CustomListNode(listType as ListType, start, listStyleType)
  }

  exportJSON(): SerializedCustomListNode {
    return {
      ...super.exportJSON(),
      listStyleType: this.__listStyleType,
      type: "list",  // Manter compatibilidade com o tipo padrão
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
