'use client';

import type { TestPlan, TestReport } from 'emception';
import { computeScore } from '@/lib/emception/scoring';

export function PublicTestEstimateBanner({
  report,
  plan,
  maxScore,
  passingScore,
}: {
  report: TestReport;
  plan: TestPlan;
  maxScore: number;
  passingScore: number;
}) {
  let scoreText: string | null = null;
  let unavailable = false;
  try {
    if (plan.cases.length > 0) {
      const { score } = computeScore(
        report,
        plan,
        maxScore,
        passingScore,
      );
      scoreText = Number.isFinite(score) ? `${score}/${maxScore}` : null;
    }
    unavailable = scoreText === null;
  } catch {
    unavailable = true;
  }

  if (unavailable) {
    return (
      <div
        role="alert"
        data-testid="public-test-estimate-unavailable"
        className="rounded-md border border-amber-500/30 bg-amber-500/10 p-3 text-sm text-amber-100"
      >
        Estimate unavailable.
      </div>
    );
  }

  const total = report.passed + report.failed;
  return (
    <div
      role="status"
      data-testid="public-test-estimate-banner"
      className="rounded-md border border-sky-500/30 bg-sky-500/10 p-3 text-sm text-sky-100"
    >
      Your public tests: {report.passed}/{total} passed (estimated score:{' '}
      {scoreText}). This is an estimate based on public tests only — hidden
      tests may change your final grade.
    </div>
  );
}
