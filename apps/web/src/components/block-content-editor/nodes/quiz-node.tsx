import type { QuizEntry } from "@game-guild/quiz"
import type { SerializedBlockNode } from "./base/serialized-block-node"

export type SerializedQuizNode = SerializedBlockNode<"quiz", QuizEntry>
export type SerializedQuizBlockNode = SerializedQuizNode
