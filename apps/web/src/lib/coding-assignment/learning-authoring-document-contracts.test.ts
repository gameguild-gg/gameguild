import type { CodingAuthoringDraft } from '@game-guild/coding';
import type { AuthoringContentPayload } from '@/lib/learning/authoring';
import type { GradingConfig } from './types';
import type { CodingAuthoringSurfaceProps } from '@game-guild/coding-surface';
import { describe, expectTypeOf, it } from 'vitest';
import {
  assertLearningCodingSaveAcknowledgement,
  assertLearningCodingPublicationAcknowledgement,
  prepareLearningCodingPublication,
  readLearningCodingDocument,
  type LearningCodingDocumentSource,
  type LearningCodingSaveExpectation,
} from './learning-authoring-document';

describe('UX04-C: Learning coding document contracts', () => {
  it('keeps incomplete authoring and publication as different operations', () => {
    expectTypeOf(readLearningCodingDocument).toEqualTypeOf<(source: LearningCodingDocumentSource) => CodingAuthoringDraft>();
    expectTypeOf(prepareLearningCodingPublication).toEqualTypeOf<
      (source: LearningCodingDocumentSource, payload: AuthoringContentPayload, grading: GradingConfig) => AuthoringContentPayload
    >();
  });

  it('validates unknown save responses against one frozen host payload and revision', () => {
    expectTypeOf(assertLearningCodingSaveAcknowledgement).toEqualTypeOf<(value: unknown, expected: LearningCodingSaveExpectation) => void>();
    expectTypeOf<LearningCodingDocumentSource['document']>().toEqualTypeOf<unknown>();
  });

  it('checks publication against the authoritative published content and the frozen candidate', () => {
    expectTypeOf(assertLearningCodingPublicationAcknowledgement).toEqualTypeOf<(value: unknown, expected: LearningCodingSaveExpectation) => void>();
  });

  it('binds the existing controlled surface without moving persistence or grading into its props', () => {
    expectTypeOf<CodingAuthoringSurfaceProps['draft']>().toEqualTypeOf<CodingAuthoringDraft>();
    expectTypeOf<CodingAuthoringSurfaceProps['onChange']>().toEqualTypeOf<(draft: CodingAuthoringDraft) => void>();
    expectTypeOf<CodingAuthoringSurfaceProps['manifestUrl']>().toEqualTypeOf<string | undefined>();
  });
});
