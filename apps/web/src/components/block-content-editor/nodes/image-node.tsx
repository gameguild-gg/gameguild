import type { SerializedBlockNode } from "./base/serialized-block-node"

export interface ImageData {
  src: string
  alt: string
  caption?: string
  size?: number // Size as a percentage (1-100)
  isNew?: boolean
}

export type SerializedImageNode = SerializedBlockNode<"image", ImageData>
