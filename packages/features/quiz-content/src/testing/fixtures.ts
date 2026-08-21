import {
  createCategorizationEntry,
  createEssayEntry,
  createFillInTheBlankEntry,
  createFormulaEntry,
  createHighlightEntry,
  createHotspotEntry,
  createMatchingEntry,
  createMultipleChoiceEntry,
  createNumericEntry,
  createOrderingEntry,
  createRatingEntry,
  createShortAnswerEntry,
  createSingleChoiceEntry,
  createTrueFalseEntry,
  type QuizEntry,
} from "@game-guild/quiz";
import { QUIZ_BLOCK_TYPE, QUIZ_CONTENT_SCHEMA_VERSION } from "../constants";
import type { QuizContentDocument } from "../types";

export const ALL_QUIZ_ENTRY_FIXTURES: QuizEntry[] = [
  createSingleChoiceEntry("Single"),
  createMultipleChoiceEntry("Multiple"),
  createTrueFalseEntry("Boolean"),
  createFillInTheBlankEntry("Fill ___"),
  createShortAnswerEntry("Short"),
  createEssayEntry("Essay"),
  createMatchingEntry("Matching"),
  createOrderingEntry("Ordering"),
  createCategorizationEntry("Categories"),
  createRatingEntry("Rating"),
  createNumericEntry("Numeric"),
  createFormulaEntry("Formula"),
  createHotspotEntry("Hotspot"),
  createHighlightEntry("Highlight"),
];

export function createAllQuestionTypesDocument(): QuizContentDocument {
  return {
    schemaVersion: QUIZ_CONTENT_SCHEMA_VERSION,
    order: ALL_QUIZ_ENTRY_FIXTURES.map((_, index) => [
      String(index + 1),
      QUIZ_BLOCK_TYPE,
    ]),
    blocks: Object.fromEntries(
      ALL_QUIZ_ENTRY_FIXTURES.map((entry, index) => [String(index + 1), entry]),
    ),
  };
}
