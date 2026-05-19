import type { SerializedBlockNode } from "./base/serialized-block-node"

export interface HeaderData {
  text: string
  level: 1 | 2 | 3 | 4 | 5 | 6
  style: "default" | "underlined" | "bordered" | "gradient" | "accent"
}

export type SerializedHeaderNode = SerializedBlockNode<"header", HeaderData>
