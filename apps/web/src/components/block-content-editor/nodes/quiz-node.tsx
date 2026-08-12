import type { QuizEntry } from "@/components/block-content-editor/extras/quiz"
import type { SerializedBlockNode } from "./base/serialized-block-node"

export type SerializedQuizNode = SerializedBlockNode<"quiz", QuizEntry>
export type SerializedQuizBlockNode = SerializedQuizNode
