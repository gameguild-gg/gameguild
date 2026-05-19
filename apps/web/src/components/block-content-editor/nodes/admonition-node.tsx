import type { AdmonitionType } from "@/components/block-content-editor/extras/admonition"
import type { SerializedBlockNode } from "./base/serialized-block-node"

export interface AdmonitionData {
  title: string
  content: string
  type: AdmonitionType
  customBorderColor?: string
  customTextColor?: string
  design?: "default" | "compact" | "bordered" | "vertical-bar"
  isNew?: boolean
}

export type SerializedAdmonitionNode = SerializedBlockNode<"admonition", AdmonitionData>
