"use client"

import { type SerializedLexicalNode } from "lexical"
import { MediaNodeBase, type BaseMediaData, type SerializedMediaNode } from "./base/media-node-base"
import { MediaComponent } from "./media-component"

export interface AudioData {
  src: string
  type?: string
  artist?: string
  caption?: string
  size?: number
  isNew?: boolean
}

export interface SerializedAudioNode extends SerializedLexicalNode {
  type: "audio"
  data: AudioData
  version: 1
}

export class AudioNode extends MediaNodeBase {
  getType(): string {
    return "audio"
  }

  static getType(): string {
    return "audio"
  }

  clone(node: AudioNode): AudioNode {
    return new AudioNode(node.__data as BaseMediaData, node.__key)
  }

  static clone(node: AudioNode): AudioNode {
    return new AudioNode(node.__data as BaseMediaData, node.__key)
  }

  constructor(data: AudioData | BaseMediaData, key?: string) {
    const baseData: BaseMediaData = {
      type: "audio",
      src: data.src,
      artist: (data as AudioData).artist || (data as BaseMediaData).artist,
      caption: data.caption,
      size: data.size ?? 100,
      isNew: (data as AudioData).isNew || (data as BaseMediaData).isNew,
      audioType: (data as AudioData).type || (data as BaseMediaData).audioType,
      embedAudioType: (data as BaseMediaData).embedAudioType || "direct",
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
      type: "audio",
      data: this.__data,
      version: 1,
    }
  }

  static importJSON(serializedNode: SerializedAudioNode | SerializedMediaNode): AudioNode {
    return new AudioNode(serializedNode.data)
  }

  decorate(): React.JSX.Element {
    return <MediaComponent data={this.__data} nodeKey={this.__key} NodeClass={AudioNode} />
  }
}

export function $createAudioNode(data: AudioData | BaseMediaData): AudioNode {
  return new AudioNode(data)
}
