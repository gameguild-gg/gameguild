/**
 * ButtonLexicalNode — DecoratorNode storing button text, URL, action type,
 * variant, size, icon settings, color palette, and font settings.
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
import { ButtonLexicalComponent } from "./button-component"

export type ButtonVariant = "solid" | "outline" | "soft" | "minimal"
export type ButtonSize = "sm" | "md" | "lg" | "xl" | "xxl"
export type ButtonActionType = "url" | "download" | "copy" | "email"
export type IconVariant = 0 | 1 | 2
export type IconPosition = "left" | "right" | "top" | "bottom"
export type IconSize = "sm" | "md" | "lg"
export type ColorPalette = "blue" | "green" | "orange" | "red" | "custom"
export type FontFamily = "sans" | "display" | "roboto"
export type FontSize = "sm" | "md" | "lg"

export interface ButtonCustomColors {
  primary: string
  secondary: string
  text: string
  hoverPrimary: string
  hoverSecondary: string
  hoverText: string
}

export type SerializedButtonLexicalNode = Spread<
  {
    text: string
    url: string
    actionType: ButtonActionType
    variant: ButtonVariant
    btnSize: ButtonSize
    showIcon: boolean
    iconVariant: IconVariant
    iconPosition: IconPosition
    iconSize: IconSize
    colorPalette: ColorPalette
    customColors: ButtonCustomColors | null
    fontFamily: FontFamily
    fontSize: FontSize
  },
  SerializedLexicalNode
>

function $convertButtonElement(domNode: HTMLElement): null | DOMConversionOutput {
  const text = domNode.getAttribute("data-lexical-button-text") || "Click me"
  const url = domNode.getAttribute("data-lexical-button-url") || ""
  const actionType = (domNode.getAttribute("data-lexical-button-action") || "url") as ButtonActionType
  const node = $createButtonLexicalNode(text, url, actionType)
  return { node }
}

export class ButtonLexicalNode extends DecoratorNode<React.JSX.Element> {
  __text: string
  __url: string
  __actionType: ButtonActionType
  __variant: ButtonVariant
  __btnSize: ButtonSize
  __showIcon: boolean
  __iconVariant: IconVariant
  __iconPosition: IconPosition
  __iconSize: IconSize
  __colorPalette: ColorPalette
  __customColors: ButtonCustomColors | null
  __fontFamily: FontFamily
  __fontSize: FontSize

  static getType() {
    return "lexical-button"
  }

  static clone(node: ButtonLexicalNode): ButtonLexicalNode {
    return new ButtonLexicalNode(
      node.__text, node.__url, node.__actionType, node.__variant, node.__btnSize,
      node.__showIcon, node.__iconVariant, node.__iconPosition, node.__iconSize,
      node.__colorPalette, node.__customColors, node.__fontFamily, node.__fontSize,
      node.__key,
    )
  }

  constructor(
    text?: string,
    url?: string,
    actionType?: ButtonActionType,
    variant?: ButtonVariant,
    btnSize?: ButtonSize,
    showIcon?: boolean,
    iconVariant?: IconVariant,
    iconPosition?: IconPosition,
    iconSize?: IconSize,
    colorPalette?: ColorPalette,
    customColors?: ButtonCustomColors | null,
    fontFamily?: FontFamily,
    fontSize?: FontSize,
    key?: NodeKey,
  ) {
    super(key)
    this.__text = text ?? "Click me"
    this.__url = url ?? ""
    this.__actionType = actionType ?? "url"
    this.__variant = variant ?? "solid"
    this.__btnSize = btnSize ?? "md"
    this.__showIcon = showIcon ?? true
    this.__iconVariant = iconVariant ?? 0
    this.__iconPosition = iconPosition ?? "right"
    this.__iconSize = iconSize ?? "md"
    this.__colorPalette = colorPalette ?? "blue"
    this.__customColors = customColors ?? null
    this.__fontFamily = fontFamily ?? "sans"
    this.__fontSize = fontSize ?? "md"
  }

  static importJSON(s: SerializedButtonLexicalNode): ButtonLexicalNode {
    return $applyNodeReplacement(new ButtonLexicalNode(
      s.text, s.url, s.actionType, s.variant, s.btnSize,
      s.showIcon, s.iconVariant, s.iconPosition, s.iconSize,
      s.colorPalette, s.customColors, s.fontFamily, s.fontSize,
    ))
  }

  exportJSON(): SerializedButtonLexicalNode {
    return {
      ...super.exportJSON(),
      text: this.__text,
      url: this.__url,
      actionType: this.__actionType,
      variant: this.__variant,
      btnSize: this.__btnSize,
      showIcon: this.__showIcon,
      iconVariant: this.__iconVariant,
      iconPosition: this.__iconPosition,
      iconSize: this.__iconSize,
      colorPalette: this.__colorPalette,
      customColors: this.__customColors,
      fontFamily: this.__fontFamily,
      fontSize: this.__fontSize,
    }
  }

  createDOM(_config: EditorConfig): HTMLElement {
    const el = document.createElement("div")
    el.className = "lexical-button-wrapper my-4"
    return el
  }

  exportDOM(): DOMExportOutput {
    const el = document.createElement("div")
    el.setAttribute("data-lexical-button-text", this.__text)
    el.setAttribute("data-lexical-button-url", this.__url)
    el.setAttribute("data-lexical-button-action", this.__actionType)
    el.textContent = this.__text
    return { element: el }
  }

  static importDOM(): DOMConversionMap | null {
    return {
      div: (domNode) => {
        if (!(domNode as HTMLElement).hasAttribute("data-lexical-button-text")) return null
        return { conversion: $convertButtonElement, priority: 2 }
      },
    }
  }

  updateDOM(_prevNode: this): boolean { return false }
  getTextContent(): string { return this.__text }

  // ── Getters / Setters ──
  getText(): string { return this.__text }
  setText(v: string): void { this.getWritable().__text = v }
  getUrl(): string { return this.__url }
  setUrl(v: string): void { this.getWritable().__url = v }
  getActionType(): ButtonActionType { return this.__actionType }
  setActionType(v: ButtonActionType): void { this.getWritable().__actionType = v }
  getVariant(): ButtonVariant { return this.__variant }
  setVariant(v: ButtonVariant): void { this.getWritable().__variant = v }
  getBtnSize(): ButtonSize { return this.__btnSize }
  setBtnSize(v: ButtonSize): void { this.getWritable().__btnSize = v }
  getShowIcon(): boolean { return this.__showIcon }
  setShowIcon(v: boolean): void { this.getWritable().__showIcon = v }
  getIconVariant(): IconVariant { return this.__iconVariant }
  setIconVariant(v: IconVariant): void { this.getWritable().__iconVariant = v }
  getIconPosition(): IconPosition { return this.__iconPosition }
  setIconPosition(v: IconPosition): void { this.getWritable().__iconPosition = v }
  getIconSize(): IconSize { return this.__iconSize }
  setIconSize(v: IconSize): void { this.getWritable().__iconSize = v }
  getColorPalette(): ColorPalette { return this.__colorPalette }
  setColorPalette(v: ColorPalette): void { this.getWritable().__colorPalette = v }
  getCustomColors(): ButtonCustomColors | null { return this.__customColors }
  setCustomColors(v: ButtonCustomColors | null): void { this.getWritable().__customColors = v }
  getFontFamily(): FontFamily { return this.__fontFamily }
  setFontFamily(v: FontFamily): void { this.getWritable().__fontFamily = v }
  getFontSize(): FontSize { return this.__fontSize }
  setFontSize(v: FontSize): void { this.getWritable().__fontSize = v }

  decorate(): React.JSX.Element {
    return (
      <ButtonLexicalComponent
        text={this.__text}
        url={this.__url}
        actionType={this.__actionType}
        variant={this.__variant}
        btnSize={this.__btnSize}
        showIcon={this.__showIcon}
        iconVariant={this.__iconVariant}
        iconPosition={this.__iconPosition}
        iconSize={this.__iconSize}
        colorPalette={this.__colorPalette}
        customColors={this.__customColors}
        fontFamily={this.__fontFamily}
        fontSize={this.__fontSize}
        nodeKey={this.__key}
      />
    )
  }
}

export function $createButtonLexicalNode(
  text = "Click me",
  url = "",
  actionType: ButtonActionType = "url",
  variant: ButtonVariant = "solid",
  btnSize: ButtonSize = "md",
): ButtonLexicalNode {
  return $applyNodeReplacement(
    new ButtonLexicalNode(text, url, actionType, variant, btnSize),
  )
}

export function $isButtonLexicalNode(
  node: LexicalNode | null | undefined,
): node is ButtonLexicalNode {
  return node instanceof ButtonLexicalNode
}
