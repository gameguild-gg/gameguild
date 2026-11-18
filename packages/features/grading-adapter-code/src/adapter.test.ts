import { describe, expect, it } from 'vitest';
import { ReviewCapabilityRegistry, parseScoreValue } from '@game-guild/grading';
import { parseCodingExecutionReport, parseCodingSubmission } from '@game-guild/coding';
import { codeAdapterFixtureV1 } from './testing';
import { allocateCodeTestScores } from './allocation';
import { codeAssessmentTypeAdapter } from './adapter';
import { classifyCodeReviewCapability, registerCodeGradingCapabilities } from './capabilities';
import { CODE_ASSESSMENT_TYPE_ADAPTER, CODE_AUTOMATED_REVIEW_HANDLER, CODE_EXECUTION_MODE, FROZEN_CODE_ADAPTER_VERSIONS } from './contracts';
import { createCodeSubmissionEnvelope } from './responses';

describe('code grading adapter', () => {
  it('pins a new adapter version while preserving the frozen backend version inventory', () => {
    expect(FROZEN_CODE_ADAPTER_VERSIONS).toEqual(['1', '2', '3']);
    expect(CODE_ASSESSMENT_TYPE_ADAPTER.version).toBe('4');
    expect(CODE_EXECUTION_MODE).toBe('server-external');
    expect(codeAdapterFixtureV1.frozenBackendAdapters.map(({ version }) => version)).toEqual(['1', '2', '3']);
  });

  it('allocates score with integers, exact conservation, and stable ID remainder ties', () => {
    const projected = codeAssessmentTypeAdapter.projectAuthoring(codeAdapterFixtureV1.input)[0]!;
    expect(projected.testScoreAllocation).toEqual(codeAdapterFixtureV1.expectedAllocation);
    expect(projected.testScoreAllocation.reduce((sum, item) => sum + item.maxScore, 0)).toBe(projected.maxScore);

    expect(
      allocateCodeTestScores(
        [
          { id: 'b', weightUnits: 1 },
          { id: 'a', weightUnits: 1 },
          { id: 'c', weightUnits: 1 },
        ],
        parseScoreValue(2),
      ),
    ).toEqual([
      { testId: 'b', maxScore: 1 },
      { testId: 'a', maxScore: 1 },
      { testId: 'c', maxScore: 0 },
    ]);
  });

  it('creates a learner delivery without private files, tests, or answer keys', () => {
    const projected = codeAssessmentTypeAdapter.projectAuthoring(codeAdapterFixtureV1.input);
    const delivery = codeAssessmentTypeAdapter.createDelivery('revision-1', 'a'.repeat(64), projected);
    const serialized = JSON.stringify(delivery);

    expect(delivery.itemOrder).toEqual(['code']);
    expect(serialized).not.toContain('private-harness');
    expect(serialized).not.toContain('private-function');
    expect(serialized).not.toContain('private-case-1');
    expect(serialized).not.toContain('visibility');
  });

  it('decodes only learner-permitted source files and rejects forged scores', () => {
    const projected = codeAssessmentTypeAdapter.projectAuthoring(codeAdapterFixtureV1.input);
    const envelope = createCodeSubmissionEnvelope(
      parseCodingSubmission({
        schemaVersion: 1,
        files: [
          {
            fileId: 'main',
            path: 'main.cpp',
            encoding: 'text',
            content: 'int main() { return 2; }',
          },
        ],
      }),
    );
    expect(codeAssessmentTypeAdapter.decodeResponse(envelope, projected).submission.files).toHaveLength(1);
    expect(() =>
      codeAssessmentTypeAdapter.decodeResponse(
        {
          ...envelope,
          payload: { ...envelope.payload, score: 100 },
        },
        projected,
      ),
    ).toThrow(/unknown fields/);
  });

  it('maps failed tests to a valid final score without exposing private expectations', () => {
    const projected = codeAssessmentTypeAdapter.projectAuthoring(codeAdapterFixtureV1.input);
    const mapped = codeAssessmentTypeAdapter.mapExecutionReport(projected, codeAdapterFixtureV1.report);

    expect(mapped.result.state).toBe('final');
    expect(mapped.result.score).toBe(codeAdapterFixtureV1.expectedFinalScore);
    expect(mapped.result.items[0]).toMatchObject({
      state: 'graded',
      score: codeAdapterFixtureV1.expectedFinalScore,
      reviewMethod: 'AutomatedReview',
      handlerKey: CODE_AUTOMATED_REVIEW_HANDLER.key,
    });
    expect(JSON.stringify(mapped)).not.toContain('private-case-1');
    expect(JSON.stringify(mapped)).not.toContain('private harness');
  });

  it.each(['partial', 'executor-error', 'cancelled', 'timed-out'] as const)('keeps %s execution pending instead of converting it to zero', (executionState) => {
    const report = parseCodingExecutionReport({
      ...codeAdapterFixtureV1.report,
      executionState,
      receiptHash: executionState === 'partial' ? codeAdapterFixtureV1.report.receiptHash : undefined,
      outcomes: executionState === 'partial' ? [{ ...codeAdapterFixtureV1.report.outcomes[0]!, state: 'passed' }] : [],
      failureCode: executionState === 'partial' ? undefined : 'worker-unavailable',
    });
    const mapped = codeAssessmentTypeAdapter.mapExecutionReport(codeAssessmentTypeAdapter.projectAuthoring(codeAdapterFixtureV1.input), report);
    expect(mapped.result).toMatchObject({ state: 'partial', score: null });
    expect(mapped.result.items[0]).toMatchObject({ state: 'pending', score: null });
  });

  it('rejects incomplete completed reports and reports from another toolchain', () => {
    const projected = codeAssessmentTypeAdapter.projectAuthoring(codeAdapterFixtureV1.input);
    expect(() =>
      codeAssessmentTypeAdapter.mapExecutionReport(
        projected,
        parseCodingExecutionReport({
          ...codeAdapterFixtureV1.report,
          outcomes: [codeAdapterFixtureV1.report.outcomes[0]],
        }),
      ),
    ).toThrow(/every frozen test/);
    expect(() =>
      codeAssessmentTypeAdapter.mapExecutionReport(
        projected,
        parseCodingExecutionReport({
          ...codeAdapterFixtureV1.report,
          toolchain: { ...codeAdapterFixtureV1.report.toolchain, version: 'other' },
        }),
      ),
    ).toThrow(/frozen toolchain/);
  });

  it('declares automated and instructor review support without registering a code-specific instructor', () => {
    expect(classifyCodeReviewCapability('AutomatedReview')).toBe('automated-review-after-trusted-execution');
    expect(classifyCodeReviewCapability('InstructorReview')).toBe('instructor-review');
    expect(classifyCodeReviewCapability('AIReview')).toBe('unsupported');

    const registry = new ReviewCapabilityRegistry();
    registerCodeGradingCapabilities(registry);
    expect(
      registry.resolveComponent('assessment-type-adapter', CODE_ASSESSMENT_TYPE_ADAPTER.key, CODE_ASSESSMENT_TYPE_ADAPTER.version, 'official-submission'),
    ).not.toBeNull();
    expect(
      registry.resolveReview('AutomatedReview', CODE_AUTOMATED_REVIEW_HANDLER.key, CODE_AUTOMATED_REVIEW_HANDLER.version, 'official-submission'),
    ).not.toBeNull();
  });
});
