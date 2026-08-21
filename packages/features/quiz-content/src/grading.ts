import {
  createDisabledGradingDefinition,
  createQuizGradingDefinition,
  syncQuizGradingDefinition,
  validateGradingDefinition,
  type ContentGradingDefinition,
} from "@game-guild/grading";
import { QUIZ_CONTENT_SCHEMA_VERSION } from "./constants";
import { assertQuizContentDocument } from "./parsing";
import {
  quizContentItemsToStorage,
  quizDocumentToBlocks,
} from "./storage";
import type {
  QuizContentDocument,
  QuizContentDocumentInput,
} from "./types";

export function readQuizContentGrading(
  document: QuizContentDocument,
): ContentGradingDefinition {
  return document.grading ?? createDisabledGradingDefinition();
}

export function enableQuizContentGrading(
  document: QuizContentDocument,
  options: Parameters<typeof createQuizGradingDefinition>[1] = {},
): QuizContentDocument {
  return {
    ...document,
    grading: createQuizGradingDefinition(quizDocumentToBlocks(document), options),
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
  updater: (current: ContentGradingDefinition) => ContentGradingDefinition,
): QuizContentDocument {
  const current = readQuizContentGrading(document);
  const next = updater(current);
  if (!next.enabled) return disableQuizContentGrading(document);
  return {
    ...document,
    grading: syncQuizGradingDefinition(
      quizDocumentToBlocks(document),
      validateGradingDefinition(next),
    ),
  };
}

export function syncQuizContentGrading(
  document: QuizContentDocument,
): QuizContentDocument {
  if (!document.grading?.enabled) return disableQuizContentGrading(document);
  return {
    ...document,
    grading: syncQuizGradingDefinition(
      quizDocumentToBlocks(document),
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
  if (grading?.enabled) {
    document = {
      ...document,
      grading: syncQuizGradingDefinition(quizDocumentToBlocks(document), grading),
    };
  }
  return assertQuizContentDocument(document);
}
