import { toQuizLearnerEntry, type QuizAuthoringEntry } from '@game-guild/quiz';
import { validateGradingDefinition } from '../../config';
import type { ContentGradingDefinition } from '../../types';
import type { QuizBlockLike, QuizBlockStorageLike, QuizQuestionLike } from './types';
import {
  asQuizQuestion,
  asRecord,
  asRecordArray,
  asStringArray,
  cloneValue,
  getQuizQuestionType,
  isBlockStorage,
  pickQuestionFields,
  rotateAnswerKeyFirstValues,
  withDefinedFields,
} from './utils';

export function redactQuizBlockStorage(
  contentBody: QuizBlockStorageLike,
  grading: ContentGradingDefinition,
): QuizBlockStorageLike {
  // Preserve the block storage shape while redacting only quiz blocks. Other
  // block types remain intact so learner delivery can reuse the content body.
  const blocks = isBlockStorage(contentBody)
    ? Object.fromEntries(
      Object.entries(contentBody.blocks).map(([id, data]) => {
        const type = contentBody.order?.find(([blockId]) => blockId === id)?.[1];
        return [id, type === 'quiz' ? redactQuizQuestion(data) : cloneValue(data)];
      }),
    )
    : {};

  return {
    ...contentBody,
    blocks,
    grading: grading.enabled ? validateGradingDefinition(grading) : undefined,
  };
}

export function redactQuizBlocks(
  blocks: readonly QuizBlockLike[],
  _grading: ContentGradingDefinition,
): QuizBlockLike[] {
  return blocks.map((block) => ({
    ...block,
    data: block.type === 'quiz' ? redactQuizQuestion(block.data) : cloneValue(block.data),
  }));
}

function redactQuizQuestion(value: unknown): unknown {
  const question = asQuizQuestion(value);
  const type = getQuizQuestionType(question);
  if (!question || !type) return redactUnknownQuestion(value);
  if (isCompleteDomainShape(question)) {
    return toQuizLearnerEntry(question as unknown as QuizAuthoringEntry);
  }

  // Each known quiz type keeps only the fields needed to render and collect a
  // learner answer. Correct answers stay in authoring/server-owned storage.
  const base = redactedQuestionBase(question);

  switch (type) {
    case 'SINGLE_CHOICE':
      return {
        ...base,
        options: cloneValue(question.options ?? []),
      };

    case 'MULTIPLE_CHOICE':
      return withDefinedFields({
        ...base,
        options: cloneValue(question.options ?? []),
      }, {
        selectionLimit: cloneValue(question.selectionLimit),
      });

    case 'TRUE_FALSE':
      return base;

    case 'FILL_IN_THE_BLANK':
      return {
        ...base,
        blanks: asRecordArray(question.blanks).map(redactFillBlankField),
      };

    case 'SHORT_ANSWER':
      return base;

    case 'ESSAY':
      return withDefinedFields(base, {
        minWordCount: cloneValue(question.minWordCount),
        maxWordCount: cloneValue(question.maxWordCount),
        showWordCount: cloneValue(question.showWordCount),
      });

    case 'MATCHING': {
      const pairs = asRecordArray(question.pairs);
      // Learners need selectable right-side options, but the original pair
      // mapping is the answer key and must not be preserved in `pairs`.
      const rightOptions = rotateAnswerKeyFirstValues([
        ...pairs.map((pair) => String(pair.right ?? '')).filter(Boolean),
        ...asStringArray(question.distractors),
      ]);

      return {
        ...base,
        pairs: pairs.map((pair) => pickQuestionFields(pair, ['id', 'left'])),
        rightOptions,
      };
    }

    case 'ORDERING':
      return {
        ...base,
        items: asRecordArray(question.items).map((item) => pickQuestionFields(item, ['id', 'text'])),
      };

    case 'CATEGORIZATION':
      return {
        ...base,
        categories: cloneValue(question.categories ?? []),
        items: asRecordArray(question.items).map((item) => pickQuestionFields(item, ['id', 'text'])),
      };

    case 'RATING':
      return {
        ...base,
        scale: cloneValue(question.scale ?? null),
      };

    case 'NUMERIC':
      // Numeric currently exposes the formula as part of the prompt model, but
      // tolerance stays server-owned because it defines correctness.
      return withDefinedFields(base, {
        variables: cloneValue(question.variables ?? []),
        formula: cloneValue(question.formula),
        decimalPlaces: cloneValue(question.decimalPlaces),
      });

    case 'FORMULA':
      // Formula questions need server-generated prompts for grading-enabled
      // runtime; the hidden formula itself is never learner-safe.
      return withDefinedFields(base, {
        variables: cloneValue(question.variables ?? []),
        decimalPlaces: cloneValue(question.decimalPlaces),
      });

    case 'HOTSPOT':
      return withDefinedFields(base, {
        imageAssetUri: cloneValue(question.imageAssetUri),
        imageWidth: cloneValue(question.imageWidth),
        imageHeight: cloneValue(question.imageHeight),
      });

    case 'HIGHLIGHT':
      return withDefinedFields(base, {
        plainText: cloneValue(question.plainText),
      });

    default:
      return redactUnknownQuestion(value);
  }
}

function isCompleteDomainShape(question: QuizQuestionLike): boolean {
  if (typeof question.stem !== 'string' || !asRecord(question.settings)) return false;
  switch (question.type) {
    case 'SINGLE_CHOICE':
    case 'MULTIPLE_CHOICE':
      return Array.isArray(question.options);
    case 'FILL_IN_THE_BLANK':
      return Array.isArray(question.blanks);
    case 'MATCHING':
      return Array.isArray(question.pairs);
    case 'ORDERING':
      return Array.isArray(question.items);
    case 'CATEGORIZATION':
      return Array.isArray(question.categories) && Array.isArray(question.items);
    case 'RATING':
      return Boolean(asRecord(question.scale));
    case 'NUMERIC':
    case 'FORMULA':
      return Array.isArray(question.variables);
    case 'HOTSPOT':
      return Array.isArray(question.hotspots);
    case 'HIGHLIGHT':
      return typeof question.plainText === 'string' && Array.isArray(question.highlights);
    case 'TRUE_FALSE':
    case 'SHORT_ANSWER':
    case 'ESSAY':
      return true;
    default:
      return false;
  }
}

function redactedQuestionBase(question: QuizQuestionLike): Record<string, unknown> {
  const feedback = redactFeedback(question.feedback);
  return withDefinedFields({}, {
    type: cloneValue(question.type),
    stem: cloneValue(question.stem),
    points: cloneValue(question.points),
    settings: cloneValue(question.settings),
    feedback,
  });
}

function redactFeedback(value: unknown): unknown {
  const feedback = asRecord(value);
  if (!feedback || feedback.general === undefined) return undefined;
  return { general: cloneValue(feedback.general) };
}

function redactFillBlankField(value: unknown): unknown {
  const blank = asRecord(value);
  const input = asRecord(blank?.input);
  const type = typeof input?.type === 'string' ? input.type : undefined;
  if (!blank || !input || !type) return cloneValue(value);

  const base = pickQuestionFields(blank, ['id', 'position']);

  switch (type) {
    case 'TEXT':
      return {
        ...base,
        input: { type },
      };

    case 'NUMBER':
      return {
        ...base,
        input: withDefinedFields({ type }, {
          unit: cloneValue(input.unit),
          requireUnit: cloneValue(input.requireUnit),
          requiredPrecision: cloneValue(input.requiredPrecision),
          allowNegative: cloneValue(input.allowNegative),
        }),
      };

    case 'DROPDOWN':
      // Option lists are render data, not proof that this payload can be graded
      // locally. The first option convention is handled only by authoring/server.
      return {
        ...base,
        input: {
          type,
          options: rotateAnswerKeyFirstValues(asStringArray(input.options)),
        },
      };

    case 'WORDBANK':
      // Word-bank words are render data, not answer-key authority.
      return {
        ...base,
        input: {
          type,
          words: rotateAnswerKeyFirstValues(asStringArray(input.words)),
        },
      };

    default:
      return {
        ...base,
        input: { type },
      };
  }
}

function redactUnknownQuestion(value: unknown): unknown {
  const question = asRecord(value);
  if (!question) return cloneValue(value);
  const next: Record<string, unknown> = {};
  // Unknown shapes are handled defensively so adding a new quiz type does not
  // accidentally leak common answer-key fields before its adapter is updated.
  for (const [key, field] of Object.entries(question)) {
    if (isAnswerKeyField(key)) continue;
    if (key === 'blanks' && Array.isArray(field)) {
      next[key] = field.map(redactFillBlankField);
      continue;
    }
    if (key === 'items' && Array.isArray(field)) {
      next[key] = field.map(redactCategorizationOrOrderingItem);
      continue;
    }
    next[key] = cloneValue(field);
  }
  return next;
}

function redactCategorizationOrOrderingItem(value: unknown): unknown {
  const item = asRecord(value);
  if (!item) return cloneValue(value);
  return Object.fromEntries(
    Object.entries(item).filter(([key]) => !isAnswerKeyField(key)),
  );
}

function isAnswerKeyField(key: string): boolean {
  return [
    'acceptedAnswers',
    'caseSensitive',
    'correctAnswer',
    'correctAnswerPlain',
    'correctCategoryIds',
    'correctOptionId',
    'correctOptionIds',
    'correctPosition',
    'correctRating',
    'correctValue',
    'formula',
    'highlights',
    'hotspots',
    'requireFormatting',
    'sourceText',
    'tolerance',
    'toleranceType',
    'zones',
  ].includes(key);
}
