import type { CodeStudioData } from "../extras/code-studio/types"
import type { SerializedBlockNode } from "./base/serialized-block-node"

export type SerializedCodeStudioNode = SerializedBlockNode<"code-studio", CodeStudioData>
