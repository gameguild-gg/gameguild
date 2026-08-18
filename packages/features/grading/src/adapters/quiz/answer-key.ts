import type { AnswerKey, ContentGradingDefinition } from '../../types';
import type { QuizBlockLike } from './types';
import {
  asQuizQuestion,
  asRecord,
  asRecordArray,
  asStringArray,
  getQuizQuestionType,
  pickQuestionFields,
} from './utils';

export function extractQuizAnswerKeyFromBlocks(
  blocks: readonly QuizBlockLike[],
  _grading: ContentGradingDefinition,
): AnswerKey {
  const items: Record<string, unknown> = {};
  for (const block of blocks) {
    if (block.type === 'quiz') items[block.id] = extractQuizQuestionAnswerKey(block.data);
  }
  return { items };
}

function extractQuizQuestionAnswerKey(value: unknown): unknown {
  const question = asQuizQuestion(value);
  const type = getQuizQuestionType(question);
  if (!question || !type) return null;

  switch (type) {
    case 'SINGLE_CHOICE':
      return pickQuestionFields(question, ['type', 'correctOptionId']);

    case 'MULTIPLE_CHOICE':
      return pickQuestionFields(question, ['type', 'correctOptionIds']);

    case 'TRUE_FALSE':
      return pickQuestionFields(question, ['type', 'correctAnswer']);

    case 'FILL_IN_THE_BLANK':
      return {
        type,
        blanks: asRecordArray(question.blanks).map(extractFillBlankAnswerKey),
      };

    case 'SHORT_ANSWER':
      return pickQuestionFields(question, ['type', 'acceptedAnswers', 'caseSensitive']);

    case 'ESSAY':
      return pickQuestionFields(question, ['type', 'correctAnswer', 'correctAnswerPlain', 'requireFormatting']);

    case 'MATCHING':
      return {
        type,
        pairs: asRecordArray(question.pairs).map((pair) => pickQuestionFields(pair, ['id', 'right'])),
      };

    case 'ORDERING':
      return {
        type,
        items: asRecordArray(question.items).map((item) => pickQuestionFields(item, ['id', 'correctPosition'])),
      };

    case 'CATEGORIZATION':
      return {
        type,
        items: asRecordArray(question.items).map((item) => pickQuestionFields(item, ['id', 'correctCategoryIds'])),
      };

    case 'RATING':
      return pickQuestionFields(question, ['type', 'correctRating']);

    case 'NUMERIC':
    case 'FORMULA':
      return pickQuestionFields(question, ['type', 'variables', 'formula', 'toleranceType', 'tolerance', 'decimalPlaces']);

    case 'HOTSPOT':
      return pickQuestionFields(question, ['type', 'imageWidth', 'imageHeight', 'hotspots']);

    case 'HIGHLIGHT':
      return pickQuestionFields(question, ['type', 'highlights']);

    default:
      return null;
  }
}

function extractFillBlankAnswerKey(value: unknown): unknown {
  const blank = asRecord(value);
  const input = asRecord(blank?.input);
  const safeInput = input ?? {};
  const type = typeof input?.type === 'string' ? input.type : undefined;
  const base = pickQuestionFields(blank ?? {}, ['id']);

  switch (type) {
    case 'TEXT':
      return {
        ...base,
        input: pickQuestionFields(safeInput, ['type', 'acceptedAnswers', 'caseSensitive']),
      };

    case 'NUMBER':
      return {
        ...base,
        input: pickQuestionFields(safeInput, [
          'type',
          'correctValue',
          'tolerance',
          'requiredPrecision',
          'unit',
          'requireUnit',
          'allowNegative',
        ]),
      };

    case 'DROPDOWN':
      return {
        ...base,
        input: {
          type,
          options: [asStringArray(safeInput.options)[0] ?? ''],
        },
      };

    case 'WORDBANK':
      return {
        ...base,
        input: {
          type,
          words: [asStringArray(safeInput.words)[0] ?? ''],
        },
      };

    default:
      return {
        ...base,
        input: { type },
      };
  }
}
