/**
 * MediaLexicalNode — DecoratorNode storing media type, src, alt, size, captions,
 * video/audio specifics, and gallery items/columns/aspect layout.
 */
import * as React from "react"
import {
  $applyNodeReplacement,
  DecoratorNode,
  type DOMConversionMap,
  type DOMConversionOutput,
  type DOMExportOutput,
  type EditorConfig,
  type LexicalNode,
  type NodeKey,
  type SerializedLexicalNode,
  type Spread,
} from "lexical"
import { MediaLexicalComponent } from "./media-component"
import type { BaseMediaData, MediaType } from "./media-data"

export type SerializedMediaLexicalNode = Spread<
  {
    mediaType: MediaType
    src: string
    alt: string
    caption: string
    size: number
    videoType: string
    embedType: BaseMediaData["embedType"]
    audioType: string
    embedAudioType: BaseMediaData["embedAudioType"]
    galleryItems: BaseMediaData[]
    galleryColumns: number
    galleryCaption: string
    galleryAspect: "square" | "landscape" | "classic" | "auto"
    showCellCaptions?: boolean
    showGalleryCaption?: boolean
    showCaption?: boolean
  },
  SerializedLexicalNode
>

function $convertMediaElement(domNode: HTMLElement): null | DOMConversionOutput {
  const src = domNode.getAttribute("data-lexical-media-src") || ""
  const type = (domNode.getAttribute("data-lexical-media-type") || "image") as MediaType
  const node = $createMediaLexicalNode(type, src)
  return { node }
}

export class MediaLexicalNode extends DecoratorNode<React.JSX.Element> {
  __mediaType: MediaType
  __src: string
  __alt: string
  __caption: string
  __size: number
  __videoType: string
  __embedType: BaseMediaData["embedType"]
  __audioType: string
  __embedAudioType: BaseMediaData["embedAudioType"]
  __galleryItems: BaseMediaData[]
  __galleryColumns: number
  __galleryCaption: string
  __galleryAspect: "square" | "landscape" | "classic" | "auto"
  __showCellCaptions: boolean
  __showGalleryCaption: boolean
  __showCaption: boolean

  static getType() {
    return "lexical-media"
  }

  static clone(node: MediaLexicalNode): MediaLexicalNode {
    return new MediaLexicalNode(
      node.__mediaType,
      node.__src,
      node.__alt,
      node.__caption,
      node.__size,
      node.__videoType,
      node.__embedType,
      node.__audioType,
      node.__embedAudioType,
      node.__galleryItems,
      node.__galleryColumns,
      node.__galleryCaption,
      node.__galleryAspect,
      node.__showCellCaptions,
      node.__showGalleryCaption,
      node.__showCaption,
      node.__key,
    )
  }

  constructor(
    mediaType?: MediaType,
    src?: string,
    alt?: string,
    caption?: string,
    size?: number,
    videoType?: string,
    embedType?: BaseMediaData["embedType"],
    audioType?: string,
    embedAudioType?: BaseMediaData["embedAudioType"],
    galleryItems?: BaseMediaData[],
    galleryColumns?: number,
    galleryCaption?: string,
    galleryAspect?: "square" | "landscape" | "classic" | "auto",
    showCellCaptions?: boolean,
    showGalleryCaption?: boolean,
    showCaption?: boolean,
    key?: NodeKey,
  ) {
    super(key)
    this.__mediaType = mediaType ?? "image"
    this.__src = src ?? ""
    this.__alt = alt ?? ""
    this.__caption = caption ?? ""
    this.__size = size ?? 100
    this.__videoType = videoType ?? "video/mp4"
    this.__embedType = embedType ?? "direct"
    this.__audioType = audioType ?? "audio/mpeg"
    this.__embedAudioType = embedAudioType ?? "direct"
    this.__galleryItems = galleryItems ?? []
    this.__galleryColumns = galleryColumns ?? 2
    this.__galleryCaption = galleryCaption ?? ""
    this.__galleryAspect = galleryAspect ?? "auto"
    this.__showCellCaptions = showCellCaptions ?? false
    this.__showGalleryCaption = showGalleryCaption ?? false
    this.__showCaption = showCaption ?? false
  }

  static importJSON(s: SerializedMediaLexicalNode): MediaLexicalNode {
    return $applyNodeReplacement(new MediaLexicalNode(
      s.mediaType,
      s.src,
      s.alt,
      s.caption,
      s.size,
      s.videoType,
      s.embedType,
      s.audioType,
      s.embedAudioType,
      s.galleryItems,
      s.galleryColumns,
      s.galleryCaption,
      s.galleryAspect ?? "auto",
      s.showCellCaptions ?? true,
      s.showGalleryCaption ?? true,
      s.showCaption ?? true,
    ))
  }

  exportJSON(): SerializedMediaLexicalNode {
    return {
      ...super.exportJSON(),
      mediaType: this.__mediaType,
      src: this.__src,
      alt: this.__alt,
      caption: this.__caption,
      size: this.__size,
      videoType: this.__videoType,
      embedType: this.__embedType,
      audioType: this.__audioType,
      embedAudioType: this.__embedAudioType,
      galleryItems: this.__galleryItems,
      galleryColumns: this.__galleryColumns,
      galleryCaption: this.__galleryCaption,
      galleryAspect: this.__galleryAspect,
      showCellCaptions: this.__showCellCaptions,
      showGalleryCaption: this.__showGalleryCaption,
      showCaption: this.__showCaption,
    }
  }

  createDOM(_config: EditorConfig): HTMLElement {
    const el = document.createElement("div")
    el.className = "lexical-media-wrapper my-4"
    return el
  }

  exportDOM(): DOMExportOutput {
    const el = document.createElement("div")
    el.setAttribute("data-lexical-media", "true")
    el.setAttribute("data-lexical-media-type", this.__mediaType)
    el.setAttribute("data-lexical-media-src", this.__src)
    return { element: el }
  }

  static importDOM(): DOMConversionMap | null {
    return {
      div: (domNode) => {
        if (!(domNode as HTMLElement).hasAttribute("data-lexical-media")) return null
        return { conversion: $convertMediaElement, priority: 2 }
      },
    }
  }

  updateDOM(_prevNode: this): boolean { return false }

  // Getters / Setters
  getMediaType(): MediaType { return this.__mediaType }
  setMediaType(v: MediaType): void { this.getWritable().__mediaType = v }
  getSrc(): string { return this.__src }
  setSrc(v: string): void { this.getWritable().__src = v }
  getAlt(): string { return this.__alt }
  setAlt(v: string): void { this.getWritable().__alt = v }
  getCaption(): string { return this.__caption }
  setCaption(v: string): void { this.getWritable().__caption = v }
  getSize(): number { return this.__size }
  setSize(v: number): void { this.getWritable().__size = v }
  getVideoType(): string { return this.__videoType }
  setVideoType(v: string): void { this.getWritable().__videoType = v }
  getEmbedType(): BaseMediaData["embedType"] { return this.__embedType }
  setEmbedType(v: BaseMediaData["embedType"]): void { this.getWritable().__embedType = v }
  getAudioType(): string { return this.__audioType }
  setAudioType(v: string): void { this.getWritable().__audioType = v }
  getEmbedAudioType(): BaseMediaData["embedAudioType"] { return this.__embedAudioType }
  setEmbedAudioType(v: BaseMediaData["embedAudioType"]): void { this.getWritable().__embedAudioType = v }
  getGalleryItems(): BaseMediaData[] { return this.__galleryItems }
  setGalleryItems(v: BaseMediaData[]): void { this.getWritable().__galleryItems = v }
  getGalleryColumns(): number { return this.__galleryColumns }
  setGalleryColumns(v: number): void { this.getWritable().__galleryColumns = v }
  getGalleryCaption(): string { return this.__galleryCaption }
  setGalleryCaption(v: string): void { this.getWritable().__galleryCaption = v }
  getGalleryAspect(): "square" | "landscape" | "classic" | "auto" { return this.__galleryAspect }
  setGalleryAspect(v: "square" | "landscape" | "classic" | "auto"): void { this.getWritable().__galleryAspect = v }
  getShowCellCaptions(): boolean { return this.__showCellCaptions }
  setShowCellCaptions(v: boolean): void { this.getWritable().__showCellCaptions = v }
  getShowGalleryCaption(): boolean { return this.__showGalleryCaption }
  setShowGalleryCaption(v: boolean): void { this.getWritable().__showGalleryCaption = v }
  getShowCaption(): boolean { return this.__showCaption }
  setShowCaption(v: boolean): void { this.getWritable().__showCaption = v }

  decorate(): React.JSX.Element {
    return (
      <MediaLexicalComponent
        mediaType={this.__mediaType}
        src={this.__src}
        alt={this.__alt}
        caption={this.__caption}
        size={this.__size}
        videoType={this.__videoType}
        embedType={this.__embedType}
        audioType={this.__audioType}
        embedAudioType={this.__embedAudioType}
        galleryItems={this.__galleryItems}
        galleryColumns={this.__galleryColumns}
        galleryCaption={this.__galleryCaption}
        galleryAspect={this.__galleryAspect}
        showCellCaptions={this.__showCellCaptions}
        showGalleryCaption={this.__showGalleryCaption}
        showCaption={this.__showCaption}
        nodeKey={this.__key}
      />
    )
  }
}

export function $createMediaLexicalNode(
  mediaType: MediaType = "image",
  src = "",
): MediaLexicalNode {
  return $applyNodeReplacement(
    new MediaLexicalNode(mediaType, src),
  )
}

export function $isMediaLexicalNode(
  node: LexicalNode | null | undefined,
): node is MediaLexicalNode {
  return node instanceof MediaLexicalNode
}
