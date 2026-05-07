"use client"

import { type SerializedLexicalNode } from "lexical"
import { MediaNodeBase, type BaseMediaData, type SerializedMediaNode } from "./base/media-node-base"
import { MediaComponent } from "./media-component"

export interface VideoData {
  src: string
  type?: string
  alt?: string
  caption?: string
  size?: number
  isNew?: boolean
}

export interface SerializedVideoNode extends SerializedLexicalNode {
  type: "video"
  data: VideoData
  version: 1
}

export class VideoNode extends MediaNodeBase {
  getType(): string {
    return "video"
  }

  static getType(): string {
    return "video"
  }

  clone(node: VideoNode): VideoNode {
    return new VideoNode(node.__data as BaseMediaData, node.__key)
  }

  static clone(node: VideoNode): VideoNode {
    return new VideoNode(node.__data as BaseMediaData, node.__key)
  }

  constructor(data: VideoData | BaseMediaData, key?: string) {
    const baseData: BaseMediaData = {
      type: "video",
      src: data.src,
      alt: data.alt,
      caption: data.caption,
      size: data.size ?? 100,
      isNew: (data as VideoData).isNew || (data as BaseMediaData).isNew,
      videoType: (data as VideoData).type || (data as BaseMediaData).videoType,
      embedType: (data as BaseMediaData).embedType || "direct",
    }
    super(baseData, key)
  }

  createDOM(): HTMLElement {
    const div = document.createElement("div")
    div.style.display = "contents"
    return div
  }

  updateDOM(): false {
    return false
  }

  exportJSON(): SerializedMediaNode {
    return {
      type: "video",
      data: this.__data,
      version: 1,
    }
  }

  static importJSON(serializedNode: SerializedVideoNode | SerializedMediaNode): VideoNode {
    return new VideoNode(serializedNode.data)
  }

  decorate(): React.JSX.Element {
    return <MediaComponent data={this.__data} nodeKey={this.__key} NodeClass={VideoNode} />
  }
}

export function $createVideoNode(data: VideoData | BaseMediaData): VideoNode {
  return new VideoNode(data)
}
