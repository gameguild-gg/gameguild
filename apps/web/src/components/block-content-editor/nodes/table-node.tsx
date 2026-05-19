import type { SerializedBlockNode } from "./base/serialized-block-node"

export type TableStyle =
  | "default"
  | "striped"
  | "bordered"
  | "minimal"
  | "modern"
  | "grid"
  | "accent"
  | "dark"
  | "colorful"
  | "professional"

export interface TableCellData {
  content: string
  isHeader: boolean
}

export interface TableData {
  rows: number
  columns: number
  style: TableStyle
  showHeader: boolean
  showBorders: boolean
  cells: TableCellData[][]
  caption?: string
  isNew?: boolean
}

export type SerializedTableNode = SerializedBlockNode<"table", TableData>
