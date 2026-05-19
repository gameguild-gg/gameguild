import type { SerializedBlockNode } from "./base/serialized-block-node"

export interface RichTextData {
  /** Serialized Lexical EditorState JSON string (opaque to this module). */
  content: string
  title?: string
}

export type SerializedRichTextNode = SerializedBlockNode<"rich-text", RichTextData>
