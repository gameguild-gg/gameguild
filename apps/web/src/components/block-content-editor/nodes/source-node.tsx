import type { SerializedBlockNode } from "./base/serialized-block-node"

export type SourceType = "book" | "article" | "website" | "journal" | "paper" | "other"

export interface SourceItem {
  id: string
  type: SourceType
  author: string
  title: string
  publication?: string
  year?: string
  url?: string
  doi?: string
  pages?: string
  notes?: string
}

export interface SourceData {
  sources: SourceItem[]
  title?: string
  style?: "apa" | "mla" | "chicago" | "harvard" | "ieee" | "vancouver" | "ama" | "turabian" | "abnt"
  isNew?: boolean
}

export type SerializedSourceNode = SerializedBlockNode<"source", SourceData>
