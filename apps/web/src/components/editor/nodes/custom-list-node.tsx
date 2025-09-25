"use client"

import { ListNode, SerializedListNode, ListType } from "@lexical/list"
import type { LexicalNode, NodeKey, EditorConfig } from "lexical"

export interface SerializedCustomListNode extends SerializedListNode {
  listStyleType?: string
  markerColor?: string
}

export class CustomListNode extends ListNode {
  __listStyleType: string
  __markerColor: string

  static getType(): string {
    return "list"  // Usar o tipo padrão para compatibilidade
  }

  constructor(listType: ListType, start: number, listStyleType?: string, markerColor?: string, key?: NodeKey) {
    super(listType, start, key)
    this.__listStyleType = listStyleType || "decimal"
    this.__markerColor = markerColor || "oklch(0.488 0.243 264.376)"
  }

  getListStyleType(): string {
    return this.__listStyleType
  }

  setListStyleType(listStyleType: string): void {
    const writable = this.getWritable()
    writable.__listStyleType = listStyleType
  }

  getMarkerColor(): string {
    return this.__markerColor
  }

  setMarkerColor(markerColor: string): void {
    const writable = this.getWritable()
    writable.__markerColor = markerColor
  }

  static clone(node: CustomListNode): CustomListNode {
    return new CustomListNode(
      node.getListType(),
      node.getStart(),
      node.__listStyleType,
      node.__markerColor,
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
    const markerColor = this.__markerColor || "oklch(0.488 0.243 264.376)"
    
    // Estilos básicos para todas as listas
    element.style.listStylePosition = "inside"
    element.style.paddingLeft = "1rem"
    element.style.marginTop = "1rem"
    element.style.marginBottom = "1rem"
    
    // Aplicar cor aos marcadores padrão (para disc, circle, square, etc.)
    element.style.color = markerColor
    
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
      case "greek-upper":
        // Numeração grega customizada
        element.style.listStyleType = "none"
        element.style.counterReset = "greek-counter"
        this.addGreekNumberStyles(element, markerColor)
        break
      case "circled":
        element.style.listStyleType = "none"
        element.style.counterReset = "circled-counter"
        this.addCircledNumberStyles(element, markerColor)
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
        this.addArrowListStyles(element, markerColor)
        break
      case "star":
        element.style.listStyleType = "none"
        element.setAttribute("data-star-list", "true")
        this.addStarListStyles(element, markerColor)
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

  private addGreekNumberStyles(element: HTMLElement, markerColor: string): void {
    // Criar estilos CSS para numeração grega se ainda não existirem
    if (!document.querySelector('#greek-number-style')) {
      const style = document.createElement('style')
      style.id = 'greek-number-style'
      style.textContent = `
        ol[data-list-style-type="greek-upper"] {
          counter-reset: greek-counter;
        }
        ol[data-list-style-type="greek-upper"] li::before {
          counter-increment: greek-counter;
          content: attr(data-greek-number);
          font-weight: bold;
          color: ${markerColor};
          margin-right: 0.5rem;
          display: inline-block;
        }
      `
      document.head.appendChild(style)
    }
  }

  private addCircledNumberStyles(element: HTMLElement, markerColor: string): void {
    // Criar estilos CSS para números circulados se ainda não existirem
    if (!document.querySelector('#circled-number-style')) {
      const style = document.createElement('style')
      style.id = 'circled-number-style'
      style.textContent = `
        ol[data-list-style-type="circled"] {
          counter-reset: circled-counter;
        }
        ol[data-list-style-type="circled"] li::before {
          counter-increment: circled-counter;
          content: "(" counter(circled-counter) ")";
          font-weight: bold;
          color: ${markerColor};
          margin-right: 0.5rem;
          display: inline-block;
        }
      `
      document.head.appendChild(style)
    }
  }

  private addArrowListStyles(element: HTMLElement, markerColor: string): void {
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
          color: ${markerColor};
          margin-right: 0.5rem;
          display: inline-block;
        }
      `
      document.head.appendChild(style)
    }
  }

  private addStarListStyles(element: HTMLElement, markerColor: string): void {
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
          color: ${markerColor};
          margin-right: 0.5rem;
          display: inline-block;
        }
      `
      document.head.appendChild(style)
    }
  }

  static importJSON(serializedNode: SerializedCustomListNode): CustomListNode {
    const { listType, start, listStyleType, markerColor } = serializedNode
    return new CustomListNode(listType as ListType, start, listStyleType, markerColor)
  }

  exportJSON(): SerializedCustomListNode {
    return {
      ...super.exportJSON(),
      listStyleType: this.__listStyleType,
      markerColor: this.__markerColor,
      type: "list",  // Manter compatibilidade com o tipo padrão
    }
  }
}

export function $createCustomListNode(
  listType: ListType,
  start = 1,
  listStyleType = "decimal",
  markerColor = "oklch(0.488 0.243 264.376)"
): CustomListNode {
  return new CustomListNode(listType, start, listStyleType, markerColor)
}

export function $isCustomListNode(
  node: LexicalNode | null | undefined
): node is CustomListNode {
  return node instanceof CustomListNode
}
