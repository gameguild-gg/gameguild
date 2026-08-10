// Weighted scoring of a {@link TestReport} against its {@link TestPlan}.
//
// Pure function — types-only inputs, no engine dependency. Safe to call from
// any context (UI preview, instructor grading, server wrapper). The engine at
// `engine.ts:246-263` runs `plan.cases` sequentially and pushes results into
// `report.cases` in the same order, so plan.cases[i] ↔ report.cases[i] is
// guaranteed and the index-mapping below is sound even though case `name` is
// optional.

import type { TestPlan, TestReport } from '../types';

/** Result of {@link computeScore}. */
export interface ScoreResult {
  /** Integer score in `[0, maxScore]`. */
  score: number;
  /** `true` iff `score >= passingScore`. */
  passed: boolean;
  /** Human-readable feedback: all-pass summary, or one bullet per failing case. */
  feedback: string;
}

/** Default weight when a case omits `weight`. */
const DEFAULT_WEIGHT = 1;

/** Max chars of `diagnostic` carried into feedback (keeps output bounded). */
const MAX_DIAGNOSTIC_CHARS = 500;

/**
 * Compute a weighted score for `report` against `plan`.
 *
 * - Each case contributes its `weight` (default 1) to the denominator.
 * - Each passing case contributes its weight to the numerator.
 * - `score = round(passedWeight / totalWeight * maxScore)`.
 * - `passed = score >= passingScore` (boundary inclusive).
 * - `totalWeight === 0` (empty plan) yields `score = 0, passed = false`.
 */
export function computeScore(
  report: TestReport,
  plan: TestPlan,
  maxScore: number,
  passingScore: number,
): ScoreResult {
  const weights = plan.cases.map((c) => c.weight ?? DEFAULT_WEIGHT);
  const totalWeight = weights.reduce((sum, w) => sum + w, 0);

  let passedWeight = 0;
  report.cases.forEach((r, i) => {
    if (r.passed) passedWeight += weights[i] ?? DEFAULT_WEIGHT;
  });

  const score = totalWeight === 0 ? 0 : Math.round((passedWeight / totalWeight) * maxScore);
  const passed = totalWeight === 0 ? false : score >= passingScore;

  const failing = report.cases.filter((r) => !r.passed);
  const feedback =
    failing.length === 0
      ? `All ${report.cases.length} test case(s) passed.`
      : failing
          .map((r) => {
            const diag = r.diagnostic ? `: ${r.diagnostic.slice(0, MAX_DIAGNOSTIC_CHARS)}` : '';
            return `- [FAIL] ${r.name ?? 'unnamed'}${diag}`;
          })
          .join('\n');

  return { score, passed, feedback };
}
