import {
  validateContentGradingDefinition,
  type ContentGradingDefinitionV2,
} from "@game-guild/grading";
import {
  createQuizGradingDefinition,
  syncQuizGradingDefinition,
} from "@game-guild/grading-adapter-quiz";
import { QUIZ_CONTENT_SCHEMA_VERSION } from "./constants";
import { assertQuizContentDocument } from "./parsing";
import { quizDocumentToGradingItems } from "./grading-projection";
import {
  quizContentItemsToStorage,
} from "./storage";
import type {
  QuizContentDocument,
  QuizContentDocumentInput,
} from "./types";

export function readQuizContentGrading(
  document: QuizContentDocument,
): ContentGradingDefinitionV2 | null {
  return document.grading ?? null;
}

export function enableQuizContentGrading(
  document: QuizContentDocument,
): QuizContentDocument {
  return {
    ...document,
    grading: createQuizGradingDefinition(quizDocumentToGradingItems(document)),
  };
}

export function disableQuizContentGrading(
  document: QuizContentDocument,
): QuizContentDocument {
  const { grading: _grading, ...content } = document;
  return content;
}

export function updateQuizContentGrading(
  document: QuizContentDocument,
  updater: (current: ContentGradingDefinitionV2) => ContentGradingDefinitionV2,
): QuizContentDocument {
  const current = readQuizContentGrading(document) ??
    createQuizGradingDefinition(quizDocumentToGradingItems(document));
  const next = validateContentGradingDefinition(updater(current));
  return {
    ...document,
    grading: syncQuizGradingDefinition(
      quizDocumentToGradingItems(document),
      next,
    ),
  };
}

export function syncQuizContentGrading(
  document: QuizContentDocument,
): QuizContentDocument {
  if (!document.grading) return document;
  return {
    ...document,
    grading: syncQuizGradingDefinition(
      quizDocumentToGradingItems(document),
      document.grading,
    ),
  };
}

export function quizContentItemsToDocument({
  items,
  grading,
}: QuizContentDocumentInput): QuizContentDocument {
  const storage = quizContentItemsToStorage(items);
  let document: QuizContentDocument = {
    schemaVersion: QUIZ_CONTENT_SCHEMA_VERSION,
    order: storage.order,
    blocks: storage.blocks,
  };
  if (grading) {
    document = {
      ...document,
      grading: syncQuizGradingDefinition(quizDocumentToGradingItems(document), grading),
    };
  }
  return assertQuizContentDocument(document);
}
