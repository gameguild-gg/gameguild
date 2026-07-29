import type { BlockStorage } from '@/components/block-content-editor/lib/storage/editor/block-structure';

export type AssessmentExecutionContext = 'practice' | 'graded';

export interface StructuredAnswerPayload {
  answers: Record<string, {
    selectedOptionIds?: string[];
    textAnswers?: Record<string, string>;
    categorizations?: Record<string, string[]>;
    ordering?: string[];
    rating?: number;
  }>;
}

export const EMPTY_ASSESSMENT_BLOCK_STORAGE = { order: [], blocks: {} } satisfies BlockStorage;

export const QUIZ_ANSWER_KEY_FIELDS = [
  'correctOptionId',
  'correctOptionIds',
  'correctAnswer',
  'acceptedAnswers',
  'correctValue',
  'correctRating',
  'correctAnswerPlain',
  'formula',
  'hotspots',
  'highlights',
] as const;

export function isBlockStorage(value: unknown): value is BlockStorage {
  if (!value || typeof value !== 'object') return false;
  const candidate = value as Partial<BlockStorage>;
  return Array.isArray(candidate.order) && Boolean(candidate.blocks) && typeof candidate.blocks === 'object';
}

export function coerceBlockStorageDefinition(definition: unknown): BlockStorage {
  if (typeof definition === 'string') {
    try {
      const parsed = JSON.parse(definition) as unknown;
      return coerceBlockStorageDefinition(parsed);
    } catch {
      return EMPTY_ASSESSMENT_BLOCK_STORAGE;
    }
  }

  return isBlockStorage(definition) ? definition : EMPTY_ASSESSMENT_BLOCK_STORAGE;
}
