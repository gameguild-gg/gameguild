import type { SerializedEditorState } from "lexical"
import type { SerializedBlockNode } from "./base/serialized-block-node"

export interface RichTextData {
  /** Serialized Lexical EditorState object (opaque to this module). */
  content: SerializedEditorState | null
  title?: string
}

export type SerializedRichTextNode = SerializedBlockNode<"rich-text", RichTextData>
