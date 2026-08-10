/**
 * Shared scoring utility — wraps emception core `computeScore` for the web
 * domain. Maps backend TestCaseDto JSON → emception TestCase, delegates
 * scoring, and formats feedback as markdown.
 */

import { computeScore, type ScoreResult } from 'emception/testing';
import type { TestPlan, TestReport, TestCase } from 'emception';

// ── Backend TestCaseDto (Task 6 JSON shape) ──────────────────────────────

/** Loose structural type mirroring the C# TestCaseDto JSON serialization. */
export interface BackendTestCaseDto {
  kind: string;
  weight?: number;
  hidden?: boolean;
  // stdio
  stdin?: string;
  expectedStdout?: string | { pattern: string } | null;
  expectedStderr?: string | null;
  expectedExit?: number;
  // stdio-file
  inFile?: string;
  expectedOutFile?: string;
  // clang-query
  matcher?: string;
  expect?: string | { minCount: number };
  // doctest
  sourceFiles?: string[];
}

// ── Mapping ──────────────────────────────────────────────────────────────

function mapExpectedStdout(
  value: string | { pattern: string } | null | undefined,
): string | RegExp {
  if (value == null) return '';
  if (typeof value === 'string') return value;
  // { pattern: string } → RegExp
  return new RegExp(value.pattern);
}

/** Map a single backend DTO to an emception TestCase. */
function mapTestCase(dto: BackendTestCaseDto): TestCase {
  switch (dto.kind) {
    case 'stdio':
      return {
        kind: 'stdio',
        stdin: dto.stdin,
        expectedStdout: mapExpectedStdout(dto.expectedStdout),
        expectedStderr: dto.expectedStderr ?? undefined,
        expectedExit: dto.expectedExit,
        weight: dto.weight,
      };
    case 'stdio-file':
      return {
        kind: 'stdio-file',
        inFile: dto.inFile ?? '',
        expectedOutFile: dto.expectedOutFile ?? '',
        weight: dto.weight,
      };
    case 'clang-query':
      return {
        kind: 'clang-query',
        matcher: dto.matcher ?? '',
        expect: dto.expect as 'found' | 'not-found' | { minCount: number },
        weight: dto.weight,
      };
    case 'doctest':
      return {
        kind: 'doctest',
        sourceFiles: dto.sourceFiles ?? [],
        weight: dto.weight,
      };
    // ponytail: custom cases are instructor-JS only; pass through as placeholder
    case 'custom':
      return {
        kind: 'custom',
        // ponytail: custom run is not authored via DTO; throw at runtime if called
        run: () => {
          throw new Error('custom test cases cannot be executed from backend DTOs');
        },
        weight: dto.weight,
      };
    default:
      throw new Error(`Unknown test case kind: ${dto.kind}`);
  }
}

/** Map backend TestCaseDto[] to emception TestCase[]. */
export function mapTestCases(dtos: BackendTestCaseDto[]): TestCase[] {
  return dtos.map(mapTestCase);
}

// ── Scoring wrapper ──────────────────────────────────────────────────────

/** Definition shape consumed by scoreSubmission. */
export interface ScoringDefinition {
  testPlan: { cases: BackendTestCaseDto[] };
  maxScore: number;
  passingScore: number;
}

/**
 * Compute a weighted score by mapping backend DTOs → emception TestCase[]
 * and delegating to core `computeScore`. No duplicated math.
 */
export function scoreSubmission(
  definition: ScoringDefinition,
  report: TestReport,
): ScoreResult {
  const plan: TestPlan = { cases: mapTestCases(definition.testPlan.cases) };
  return computeScore(report, plan, definition.maxScore, definition.passingScore);
}

// ── Feedback formatter ───────────────────────────────────────────────────

/**
 * Produce markdown for the `AssessmentSubmission.Feedback` column.
 *
 * - Header with score
 * - Bullet per failing case (name + diagnostic)
 * - Footer noting it was auto-graded in-browser
 */
export function formatFeedback(report: TestReport, score: number): string {
  const maxScore = report.cases.reduce((sum, c) => sum + (c.passed ? 1 : 0), 0);
  // Use total cases count for the denominator in the display
  const totalCases = report.cases.length;
  const passingCases = report.passed;

  const lines: string[] = [
    '## Auto-grading result',
    '',
    `Score: ${score}/100`,
    '',
  ];

  const failing = report.cases.filter((c) => !c.passed);

  if (failing.length === 0) {
    lines.push(`All ${totalCases} test case(s) passed.`);
  } else {
    for (const c of failing) {
      const diag = c.diagnostic ?? 'no diagnostic';
      lines.push(`- **${c.name}**: ${diag}`);
    }
  }

  lines.push('', '_Auto-graded in-browser._');
  return lines.join('\n');
}

// ── Re-exports ───────────────────────────────────────────────────────────

export { computeScore } from 'emception/testing';
export type { ScoreResult } from 'emception/testing';
export type { TestPlan, TestReport, TestCase } from 'emception';
