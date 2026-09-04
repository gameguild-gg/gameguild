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

export function isQuizRuntimeContentDocument(
  value: unknown,
): value is QuizRuntimeContentDocument {
  if (!value || typeof value !== "object" || Array.isArray(value)) return false;
  const runtime = value as Record<string, unknown>;
  if (runtime.mode !== "local-practice" && runtime.mode !== "server-graded") {
    return false;
  }
  if (!runtime.document || typeof runtime.document !== "object" || Array.isArray(runtime.document)) {
    return false;
  }

  const document = runtime.document as Record<string, unknown>;
  return Array.isArray(document.order) &&
    Boolean(document.blocks) &&
    typeof document.blocks === "object" &&
    !Array.isArray(document.blocks);
}
