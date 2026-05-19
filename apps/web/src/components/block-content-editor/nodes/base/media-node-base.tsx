import type { SerializedBlockNode } from "./serialized-block-node"

export type MediaType = "image" | "video" | "audio"

export interface BaseMediaData {
  type: MediaType
  src: string
  alt?: string
  caption?: string
  size?: number // Size as a percentage (1-100)
  isNew?: boolean

  // Video specific
  videoType?: string
  embedType?: "direct" | "youtube" | "vimeo" | "dailymotion"

  // Audio specific
  audioType?: string
  embedAudioType?: "direct" | "youtube" | "spotify" | "soundcloud"

  // Grid positioning
  isPlaceholder?: boolean // If true, this is an empty placeholder
  isStatic?: boolean // If true, position is fixed and won't auto-reorder
  gridPosition?: number // Absolute position in the grid (for static items)
}

export type SerializedMediaNode = SerializedBlockNode<string, BaseMediaData>
