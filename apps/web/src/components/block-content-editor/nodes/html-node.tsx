import type { SerializedBlockNode } from "./base/serialized-block-node"

export interface HTMLData {
  content: string
}

export type SerializedHTMLNode = SerializedBlockNode<"html", HTMLData>
