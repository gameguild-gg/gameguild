import type { SerializedBlockNode } from "./base/serialized-block-node"

export type ButtonVariant = "solid" | "outline" | "soft" | "minimal"
export type ButtonSize = "sm" | "md" | "lg" | "xl" | "xxl"
export type ButtonActionType = "url" | "download" | "copy" | "email"
export type IconVariant = 0 | 1 | 2
export type IconPosition = "left" | "right" | "top" | "bottom"
export type IconSize = "sm" | "md" | "lg"
export type ColorPalette = "blue" | "green" | "orange" | "red" | "custom"
export type FontFamily = "sans" | "display" | "roboto"
export type FontSize = "sm" | "md" | "lg"

export interface ButtonData {
  text: string
  url: string
  actionType: ButtonActionType
  variant: ButtonVariant
  size: ButtonSize
  showIcon: boolean
  iconVariant: IconVariant
  iconPosition: IconPosition
  iconSize: IconSize
  colorPalette: ColorPalette
  customColors?: {
    primary: string
    secondary: string
    text: string
    hoverPrimary: string
    hoverSecondary: string
    hoverText: string
  }
  fontFamily: FontFamily
  fontSize: FontSize
  isNew?: boolean
}

export type SerializedButtonNode = SerializedBlockNode<"button", ButtonData>
