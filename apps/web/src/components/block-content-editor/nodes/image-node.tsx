"use client"

import { type SerializedLexicalNode } from "lexical"
import { MediaNodeBase, type BaseMediaData, type SerializedMediaNode } from "./base/media-node-base"
import { MediaComponent } from "./media-component"

export interface ImageData {
  src: string
  alt: string
  caption?: string
  size?: number // Size as a percentage (1-100)
  isNew?: boolean
}

export interface SerializedImageNode extends SerializedLexicalNode {
  type: "image"
  data: ImageData
  version: 1
}

export class ImageNode extends MediaNodeBase {
  getType(): string {
    return "image"
  }

  static getType(): string {
    return "image"
  }

  clone(node: ImageNode): ImageNode {
    return new ImageNode(node.__data as BaseMediaData, node.__key)
  }

  static clone(node: ImageNode): ImageNode {
    return new ImageNode(node.__data as BaseMediaData, node.__key)
  }

  constructor(data: ImageData | BaseMediaData, key?: string) {
    const baseData: BaseMediaData = {
      type: "image",
      src: data.src,
      alt: (data as ImageData).alt || data.alt,
      caption: data.caption,
      size: data.size ?? 100,
      isNew: (data as ImageData).isNew || (data as BaseMediaData).isNew,
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
      type: "image",
      data: this.__data,
      version: 1,
    }
  }

  static importJSON(serializedNode: SerializedImageNode | SerializedMediaNode): ImageNode {
    return new ImageNode(serializedNode.data)
  }

  decorate(): React.JSX.Element {
    return <MediaComponent data={this.__data} nodeKey={this.__key} NodeClass={ImageNode} />
  }
}

export function $createImageNode(data: ImageData | BaseMediaData): ImageNode {
  return new ImageNode(data)
}
