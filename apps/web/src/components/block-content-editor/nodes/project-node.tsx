import type { SerializedBlockNode } from "./base/serialized-block-node"

export interface ProjectData {
  projectId: string
  projectName: string
  /** Serialized Lexical EditorState JSON (opaque to this module). */
  editorState: unknown
  isLocalCopy: boolean
  isReference?: boolean
  wasReference?: boolean
  originalProjectId?: string
  size?: number
  caption?: string
}

export type SerializedProjectNode = SerializedBlockNode<"project", ProjectData>
