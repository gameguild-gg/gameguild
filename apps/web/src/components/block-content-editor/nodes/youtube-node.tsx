import type { SerializedBlockNode } from "./base/serialized-block-node"

export interface YouTubeData {
  videoId: string
  title?: string
  caption?: string
  size?: number // Size as a percentage (1-100)
  isNew?: boolean // Flag to indicate if the video was newly inserted
  startAt?: number // Start time in seconds
  showControls?: boolean
  showInfo?: boolean
  showRelated?: boolean
}

export type SerializedYouTubeNode = SerializedBlockNode<"youtube", YouTubeData>
