import { describe, expect, it } from 'vitest';
import { ReviewCapabilityRegistry, parseScoreValue } from '@game-guild/grading';
import { codingContractFixture } from '@game-guild/coding/testing';
import { compileCodingAuthoringDraft, parseCodingExecutionReport, parseCodingSubmission } from '@game-guild/coding';
import draftFixtures from '../../coding/fixtures/coding-authoring-drafts.json';
import { codeAdapterFixture } from './testing';
import { allocateCodeTestScores } from './allocation';
import { codeAssessmentTypeAdapter } from './adapter';
import { classifyCodeReviewCapability, registerCodeGradingCapabilities } from './capabilities';
import { CODE_ASSESSMENT_TYPE_ADAPTER, CODE_AUTOMATED_REVIEW_HANDLER, CODE_EXECUTION_MODE, CODE_RESPONSE_PAYLOAD_SCHEMA } from './contracts';
import { createCodeSubmissionEnvelope } from './responses';

describe('code grading adapter', () => {
  it('UX02-C: retains a frozen problem and IDs without delivering protected reference replacements', () => {
    const source = compileCodingAuthoringDraft(draftFixtures.function);
    const projected = codeAssessmentTypeAdapter.projectAuthoring({ ...codeAdapterFixture.input, definition: source });
    source.problem!.statement = 'A later problem';
    source.files[0]!.content = 'A later starter';
    const delivery = codeAssessmentTypeAdapter.createDelivery('revision-frozen', 'a'.repeat(64), projected);
    expect(delivery.definitionRevisionId).toBe('revision-frozen');
    expect(delivery.items.code?.learnerPayload.definition.problem?.statement).toBe('Return the sum of two integers.');
    expect(delivery.items.code?.learnerPayload.definition.publicTests[0]?.id).toBe('addition');
    expect(delivery.items.code?.learnerPayload.definition.files[0]?.id).toBe('main-file');
    expect(JSON.stringify(delivery)).not.toContain('SYNTHETIC-TEACHER-REFERENCE');
    expect(JSON.stringify(projected)).toContain('SYNTHETIC-TEACHER-REFERENCE');
    expect(JSON.stringify(projected)).not.toContain('A later problem');
  });

  it('uses the single canonical adapter, handler, and payload identities', () => {
    expect(CODE_ASSESSMENT_TYPE_ADAPTER).toEqual({ key: 'code-assessment-type', version: '1' });
    expect(CODE_AUTOMATED_REVIEW_HANDLER).toEqual({ key: 'code-wasm-review', version: '1' });
    expect(CODE_RESPONSE_PAYLOAD_SCHEMA).toBe('code-files');
    expect(CODE_EXECUTION_MODE).toBe('server-external');
  });

  it('allocates score with integers, exact conservation, and stable ID remainder ties', () => {
    const projected = codeAssessmentTypeAdapter.projectAuthoring(codeAdapterFixture.input)[0]!;
    expect(projected.testScoreAllocation).toEqual(codeAdapterFixture.expectedAllocation);
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
    const projected = codeAssessmentTypeAdapter.projectAuthoring(codeAdapterFixture.input);
    const delivery = codeAssessmentTypeAdapter.createDelivery('revision-1', 'a'.repeat(64), projected);
    const serialized = JSON.stringify(delivery);

    expect(delivery.itemOrder).toEqual(['code']);
    expect(serialized).not.toContain('private-harness');
    expect(serialized).not.toContain('private-function');
    expect(serialized).not.toContain('private-case-1');
    expect(serialized).not.toContain('visibility');
  });

  it('preserves the explicit exercise type in the canonical learner delivery', () => {
    const projected = codeAssessmentTypeAdapter.projectAuthoring({
      ...codeAdapterFixture.input,
      definition: codingContractFixture.authoring,
    });
    const delivery = codeAssessmentTypeAdapter.createDelivery('revision-2', 'b'.repeat(64), projected);

    expect(delivery.items.code?.learnerPayload.definition).toMatchObject({
      schemaVersion: 1,
      exerciseType: 'standard-io',
    });
  });

  it('decodes only learner-permitted source files and rejects forged scores', () => {
    const projected = codeAssessmentTypeAdapter.projectAuthoring(codeAdapterFixture.input);
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
    const projected = codeAssessmentTypeAdapter.projectAuthoring(codeAdapterFixture.input);
    const mapped = codeAssessmentTypeAdapter.mapExecutionReport(projected, codeAdapterFixture.report);

    expect(mapped.result.state).toBe('final');
    expect(mapped.result.score).toBe(codeAdapterFixture.expectedFinalScore);
    expect(mapped.result.items[0]).toMatchObject({
      state: 'graded',
      score: codeAdapterFixture.expectedFinalScore,
      reviewMethod: 'AutomatedReview',
      handlerKey: CODE_AUTOMATED_REVIEW_HANDLER.key,
    });
    expect(JSON.stringify(mapped)).not.toContain('private-case-1');
    expect(JSON.stringify(mapped)).not.toContain('private harness');
  });

  it.each(['partial', 'executor-error', 'cancelled', 'timed-out'] as const)('keeps %s execution pending instead of converting it to zero', (executionState) => {
    const report = parseCodingExecutionReport({
      ...codeAdapterFixture.report,
      executionState,
      receiptHash: executionState === 'partial' ? codeAdapterFixture.report.receiptHash : undefined,
      outcomes: executionState === 'partial' ? [{ ...codeAdapterFixture.report.outcomes[0]!, state: 'passed' }] : [],
      failureCode: executionState === 'partial' ? undefined : 'worker-unavailable',
    });
    const mapped = codeAssessmentTypeAdapter.mapExecutionReport(codeAssessmentTypeAdapter.projectAuthoring(codeAdapterFixture.input), report);
    expect(mapped.result).toMatchObject({ state: 'partial', score: null });
    expect(mapped.result.items[0]).toMatchObject({ state: 'pending', score: null });
  });

  it('rejects incomplete completed reports and reports from another toolchain', () => {
    const projected = codeAssessmentTypeAdapter.projectAuthoring(codeAdapterFixture.input);
    expect(() =>
      codeAssessmentTypeAdapter.mapExecutionReport(
        projected,
        parseCodingExecutionReport({
          ...codeAdapterFixture.report,
          outcomes: [codeAdapterFixture.report.outcomes[0]],
        }),
      ),
    ).toThrow(/every frozen test/);
    expect(() =>
      codeAssessmentTypeAdapter.mapExecutionReport(
        projected,
        parseCodingExecutionReport({
          ...codeAdapterFixture.report,
          toolchain: { ...codeAdapterFixture.report.toolchain, version: 'other' },
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
