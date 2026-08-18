import { toQuizLearnerEntry } from "@game-guild/quiz";
import type {
  QuizContentDocument,
  QuizLearnerContentDocument,
  QuizRuntimeContentDocument,
} from "./types";

export function toQuizLearnerContentDocument(
  document: QuizContentDocument,
): QuizLearnerContentDocument {
  return {
    schemaVersion: document.schemaVersion,
    order: document.order.map(([id, type]) => [id, type]),
    blocks: Object.fromEntries(
      document.order.map(([id]) => [id, toQuizLearnerEntry(document.blocks[id]!)]),
    ),
    ...(document.grading ? { grading: document.grading } : {}),
  };
}

export function prepareQuizContentForRuntime(
  document: QuizContentDocument,
  mode: "local-practice" | "server-graded",
): QuizRuntimeContentDocument {
  return mode === "server-graded"
    ? { mode, document: toQuizLearnerContentDocument(document) }
    : { mode, document };
}
