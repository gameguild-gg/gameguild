import type { QuizEntry } from "@/components/block-content-editor/extras/quiz"
import type { SerializedBlockNode } from "./base/serialized-block-node"

export interface SerializedQuizNode {
  type: "quiz"
  entry: QuizEntry
  version: number
}

// Re-export the generic block-form too in case preview components ever need it.
export type SerializedQuizBlockNode = SerializedBlockNode<"quiz", QuizEntry>
