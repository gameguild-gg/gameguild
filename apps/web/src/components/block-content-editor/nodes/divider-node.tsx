import type { SerializedBlockNode } from "./base/serialized-block-node"

export type DividerStyle = "simple" | "double" | "dashed" | "dotted" | "gradient"
export type DividerThickness = "thin" | "medium" | "thick"
export type DividerSpacing = "xs" | "sm" | "md" | "lg" | "xl"
export type ColorPalette = "blue" | "green" | "orange" | "red" | "purple" | "custom"

export interface DividerData {
  style: DividerStyle
  thickness: DividerThickness
  spacing: DividerSpacing
  colorPalette: ColorPalette
  customColor?: string
  isNew?: boolean
}

export type SerializedDividerNode = SerializedBlockNode<"divider", DividerData>
