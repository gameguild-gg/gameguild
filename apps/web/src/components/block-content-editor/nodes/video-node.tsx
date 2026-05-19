import type { SerializedBlockNode } from "./base/serialized-block-node"

export interface VideoData {
  src: string
  type?: string
  alt?: string
  caption?: string
  size?: number
  isNew?: boolean
}

export type SerializedVideoNode = SerializedBlockNode<"video", VideoData>
