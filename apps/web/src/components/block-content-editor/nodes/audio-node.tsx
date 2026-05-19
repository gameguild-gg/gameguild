import type { SerializedBlockNode } from "./base/serialized-block-node"

export interface AudioData {
  src: string
  type?: string
  caption?: string
  size?: number
  isNew?: boolean
  title?: string
  artist?: string
}

export type SerializedAudioNode = SerializedBlockNode<"audio", AudioData>
