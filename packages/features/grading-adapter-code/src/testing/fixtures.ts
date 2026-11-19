import { codingContractFixture } from '@game-guild/coding/testing';
import { parseScoreValue } from '@game-guild/grading';
import fixture from '../../fixtures/code-adapter-cases.json';
import type { CodeGradingItemInput } from '../contracts';

export const codeAdapterFixture = Object.freeze({
  input: {
    itemId: fixture.itemId,
    maxScore: parseScoreValue(fixture.maxScore),
    definition: codingContractFixture.authoring,
  } satisfies CodeGradingItemInput,
  report: codingContractFixture.executionReport,
  expectedAllocation: structuredClone(fixture.expectedAllocation),
  expectedFinalScore: parseScoreValue(fixture.expectedFinalScore),
});
