"use client"

import type React from "react"
import { DecoratorNode, type SerializedLexicalNode } from "lexical"

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
  artist?: string
}

export interface SerializedMediaNode extends SerializedLexicalNode {
  type: string
  data: BaseMediaData
  version: 1
}

export abstract class MediaNodeBase extends DecoratorNode<React.JSX.Element> {
  __data: BaseMediaData

  constructor(data: BaseMediaData, key?: string) {
    super(key)
    this.__data = {
      ...data,
      size: data.size ?? 100,
    }
  }

  createDOM(): HTMLElement {
    return document.createElement("div")
  }

  updateDOM(): false {
    return false
  }

  setData(data: BaseMediaData): void {
    const writable = this.getWritable()
    writable.__data = data
  }

  exportJSON(): SerializedMediaNode {
    return {
      type: this.getType(),
      data: this.__data,
      version: 1,
    }
  }

  abstract getType(): string
  abstract clone(node: MediaNodeBase): MediaNodeBase
}
