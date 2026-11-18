import { addScoreValues, parseScoreValue, type GradeItemResultV1, type GradeResultV1, type ScoreValue } from '@game-guild/grading';
import { parseCodingExecutionReport, type CodingExecutionReportV1 } from '@game-guild/coding';
import {
  CODE_AUTOMATED_REVIEW_HANDLER,
  CODE_EXECUTION_EVIDENCE_SCHEMA_VERSION,
  type CodeExecutionEvidenceTestV1,
  type CodeExecutionEvidenceV1,
  type CodeGradeMappingV1,
  type CodeItemProjectionV1,
} from './contracts';
import { requireSingleItem } from './delivery';

export function mapCodeExecutionReportToGrade(items: readonly CodeItemProjectionV1[], reportValue: CodingExecutionReportV1): CodeGradeMappingV1 {
  const item = requireSingleItem(items);
  const report = parseCodingExecutionReport(reportValue);
  assertToolchain(item.definition.toolchain, report.toolchain);
  const tests = new Map(item.definition.tests.map((test) => [test.id, test]));
  const allocation = new Map(item.testScoreAllocation.map((entry) => [entry.testId, entry.maxScore]));
  for (const outcome of report.outcomes) {
    if (!tests.has(outcome.testId)) {
      throw new TypeError(`Code execution report references unknown test ${outcome.testId}.`);
    }
  }

  const evidenceId = `code-execution:${report.reportId}`;
  const evidenceTests: CodeExecutionEvidenceTestV1[] = report.outcomes.map((outcome) => {
    const maxScore = allocation.get(outcome.testId)!;
    return {
      testId: outcome.testId,
      state: outcome.state,
      awardedScore: outcome.state === 'passed' ? maxScore : outcome.state === 'failed' ? parseScoreValue(0) : null,
      maxScore,
      ...(outcome.evidenceRef ? { evidenceRef: outcome.evidenceRef } : {}),
    };
  });
  const evidence: CodeExecutionEvidenceV1 = {
    schemaVersion: CODE_EXECUTION_EVIDENCE_SCHEMA_VERSION,
    kind: 'code-execution-report',
    evidenceId,
    reportId: report.reportId,
    executionState: report.executionState,
    toolchain: report.toolchain,
    ...(report.receiptHash ? { receiptHash: report.receiptHash } : {}),
    tests: evidenceTests,
    ...(report.failureCode ? { failureCode: report.failureCode } : {}),
  };

  if (report.executionState === 'completed') {
    assertCompleteOutcomes(tests, report);
    const score = addScoreValues(
      evidenceTests.map(({ awardedScore }) => {
        if (awardedScore === null) throw new TypeError('Completed code outcome has no score allocation.');
        return awardedScore;
      }),
    );
    return {
      result: createResult(item, 'final', 'graded', score, evidenceId, 'Trusted code execution completed.'),
      evidence,
    };
  }

  return {
    result: createResult(item, 'partial', 'pending', null, evidenceId, 'Code execution did not produce a complete grade.'),
    evidence,
  };
}

function createResult(
  item: CodeItemProjectionV1,
  state: GradeResultV1['state'],
  itemState: GradeItemResultV1['state'],
  score: ScoreValue | null,
  evidenceId: string,
  feedback: string,
): GradeResultV1 {
  const gradeItem: GradeItemResultV1 = {
    itemId: item.itemId,
    state: itemState,
    score,
    maxScore: item.maxScore,
    feedback,
    evidenceRefs: [evidenceId],
    reviewMethod: 'AutomatedReview',
    handlerKey: CODE_AUTOMATED_REVIEW_HANDLER.key,
    handlerVersion: CODE_AUTOMATED_REVIEW_HANDLER.version,
  };
  return {
    schemaVersion: 1,
    state,
    score,
    maxScore: item.maxScore,
    items: [gradeItem],
    feedback,
    evidenceRefs: [evidenceId],
  };
}

function assertCompleteOutcomes(tests: ReadonlyMap<string, unknown>, report: CodingExecutionReportV1): void {
  if (report.outcomes.length !== tests.size || report.outcomes.some(({ state }) => state !== 'passed' && state !== 'failed')) {
    throw new TypeError('Completed code execution report must contain one final outcome for every frozen test.');
  }
  for (const testId of tests.keys()) {
    if (!report.outcomes.some((outcome) => outcome.testId === testId)) {
      throw new TypeError(`Completed code execution report is missing test ${testId}.`);
    }
  }
}

function assertToolchain(expected: CodeItemProjectionV1['definition']['toolchain'], actual: CodingExecutionReportV1['toolchain']): void {
  if (
    expected.key !== actual.key ||
    expected.version !== actual.version ||
    expected.runtimeAbi !== actual.runtimeAbi ||
    expected.artifactDigest !== actual.artifactDigest
  ) {
    throw new TypeError('Code execution report does not match the frozen toolchain identity.');
  }
}
