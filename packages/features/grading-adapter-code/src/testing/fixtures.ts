import { codingContractFixtureV1 } from '@game-guild/coding/testing';
import { parseScoreValue } from '@game-guild/grading';
import fixture from '../../fixtures/code-adapter-cases-v1.json';
import type { CodeGradingItemInputV1 } from '../contracts';

export const codeAdapterFixtureV1 = Object.freeze({
  input: {
    itemId: fixture.itemId,
    maxScore: parseScoreValue(fixture.maxScore),
    definition: codingContractFixtureV1.authoring,
  } satisfies CodeGradingItemInputV1,
  report: codingContractFixtureV1.executionReport,
  expectedAllocation: structuredClone(fixture.expectedAllocation),
  expectedFinalScore: parseScoreValue(fixture.expectedFinalScore),
  frozenBackendAdapters: structuredClone(fixture.frozenBackendAdapters),
});
