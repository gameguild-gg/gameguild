export type MediaType = "image" | "video" | "audio"

export interface BaseMediaData {
  type: MediaType
  src: string
  alt?: string
  caption?: string
  size?: number
  isNew?: boolean

  videoType?: string
  embedType?: "direct" | "youtube" | "vimeo" | "dailymotion"

  audioType?: string
  embedAudioType?: "direct" | "youtube" | "spotify" | "soundcloud"

  isPlaceholder?: boolean
  isStatic?: boolean
  gridPosition?: number
}
