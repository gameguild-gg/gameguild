import { describe, expect, it } from 'vitest';
import {
  scoreSubmission,
  formatFeedback,
  mapTestCases,
  type BackendTestCaseDto,
  type TestReport,
} from './scoring';

describe('mapTestCases', () => {
  it('maps stdio DTO to TestCase', () => {
    const dtos: BackendTestCaseDto[] = [
      {
        kind: 'stdio',
        stdin: 'hello',
        expectedStdout: 'hello world',
        expectedExit: 0,
        weight: 2,
      },
    ];
    const cases = mapTestCases(dtos);
    expect(cases).toHaveLength(1);
    expect(cases[0]).toEqual({
      kind: 'stdio',
      stdin: 'hello',
      expectedStdout: 'hello world',
      expectedExit: 0,
      weight: 2,
    });
  });

  it('maps stdio-file DTO to TestCase', () => {
    const dtos: BackendTestCaseDto[] = [
      {
        kind: 'stdio-file',
        inFile: 'input.txt',
        expectedOutFile: 'expected.txt',
        weight: 1,
      },
    ];
    const cases = mapTestCases(dtos);
    expect(cases[0]).toEqual({
      kind: 'stdio-file',
      inFile: 'input.txt',
      expectedOutFile: 'expected.txt',
      weight: 1,
    });
  });

  it('maps clang-query DTO to TestCase', () => {
    const dtos: BackendTestCaseDto[] = [
      {
        kind: 'clang-query',
        matcher: 'callExpr',
        expect: { minCount: 1 },
        weight: 3,
      },
    ];
    const cases = mapTestCases(dtos);
    expect(cases[0]).toEqual({
      kind: 'clang-query',
      matcher: 'callExpr',
      expect: { minCount: 1 },
      weight: 3,
    });
  });

  it('maps doctest DTO to TestCase', () => {
    const dtos: BackendTestCaseDto[] = [
      {
        kind: 'doctest',
        sourceFiles: ['test.cpp'],
        weight: 1,
      },
    ];
    const cases = mapTestCases(dtos);
    expect(cases[0]).toEqual({
      kind: 'doctest',
      sourceFiles: ['test.cpp'],
      weight: 1,
    });
  });

  it('converts {pattern: string} expectedStdout to RegExp', () => {
    const dtos: BackendTestCaseDto[] = [
      {
        kind: 'stdio',
        expectedStdout: { pattern: '^Hello.*' },
      },
    ];
    const cases = mapTestCases(dtos);
    expect(cases[0].kind).toBe('stdio');
    expect((cases[0] as { expectedStdout: RegExp }).expectedStdout).toBeInstanceOf(RegExp);
    expect((cases[0] as { expectedStdout: RegExp }).expectedStdout.source).toBe('^Hello.*');
  });

  it('throws on unknown kind', () => {
    const dtos = [{ kind: 'bogus', weight: 1 }] as unknown as BackendTestCaseDto[];
    expect(() => mapTestCases(dtos)).toThrow('Unknown test case kind: bogus');
  });

  it('passes through hidden and weight fields', () => {
    const dtos: BackendTestCaseDto[] = [
      {
        kind: 'stdio',
        expectedStdout: 'ok',
        hidden: true,
        weight: 5,
      },
    ];
    const cases = mapTestCases(dtos);
    expect(cases[0].weight).toBe(5);
  });
});

describe('scoreSubmission', () => {
  it('computes correct weighted score', () => {
    // 3 cases: weights [1, 2, 3], 2 pass (indices 0, 2) → passedWeight=4, totalWeight=6
    // score = round(4/6 * 100) = round(66.67) = 67
    const definition = {
      testPlan: {
        cases: [
          { kind: 'stdio', expectedStdout: 'a', weight: 1 },
          { kind: 'stdio', expectedStdout: 'b', weight: 2 },
          { kind: 'stdio', expectedStdout: 'c', weight: 3 },
        ] as BackendTestCaseDto[],
      },
      maxScore: 100,
      passingScore: 50,
    };
    const report: TestReport = {
      passed: 2,
      failed: 1,
      totalDurationMs: 100,
      cases: [
        { name: 'case1', passed: true, durationMs: 30 },
        { name: 'case2', passed: false, durationMs: 40, diagnostic: 'wrong output' },
        { name: 'case3', passed: true, durationMs: 30 },
      ],
    };
    const result = scoreSubmission(definition, report);
    expect(result.score).toBe(67);
    expect(result.passed).toBe(true);
  });

  it('returns score 0 and passed false for empty cases', () => {
    const definition = {
      testPlan: { cases: [] as BackendTestCaseDto[] },
      maxScore: 100,
      passingScore: 50,
    };
    const report: TestReport = {
      passed: 0,
      failed: 0,
      totalDurationMs: 0,
      cases: [],
    };
    const result = scoreSubmission(definition, report);
    expect(result.score).toBe(0);
    expect(result.passed).toBe(false);
  });

  it('delegates to core computeScore — no duplicated math', () => {
    // Verify the wrapper truly delegates: 1 case weight=10, passes → score = 100
    const definition = {
      testPlan: {
        cases: [{ kind: 'stdio', expectedStdout: 'x', weight: 10 }] as BackendTestCaseDto[],
      },
      maxScore: 100,
      passingScore: 0,
    };
    const report: TestReport = {
      passed: 1,
      failed: 0,
      totalDurationMs: 10,
      cases: [{ name: 'only', passed: true, durationMs: 10 }],
    };
    const result = scoreSubmission(definition, report);
    expect(result.score).toBe(100);
    expect(result.passed).toBe(true);
  });
});

describe('formatFeedback', () => {
  it('produces markdown with header, score, and failing cases', () => {
    const report: TestReport = {
      passed: 1,
      failed: 2,
      totalDurationMs: 100,
      cases: [
        { name: 'test_ok', passed: true, durationMs: 30 },
        { name: 'test_fail_one', passed: false, durationMs: 40, diagnostic: 'expected 42, got 0' },
        { name: 'test_fail_two', passed: false, durationMs: 30, diagnostic: 'segfault' },
      ],
    };
    const md = formatFeedback(report, 67);
    expect(md).toContain('## Auto-grading result');
    expect(md).toContain('Score: 67/100');
    expect(md).toContain('test_fail_one');
    expect(md).toContain('expected 42, got 0');
    expect(md).toContain('test_fail_two');
    expect(md).toContain('segfault');
    expect(md).toContain('Auto-graded in-browser');
  });

  it('handles missing diagnostics gracefully', () => {
    const report: TestReport = {
      passed: 0,
      failed: 1,
      totalDurationMs: 10,
      cases: [{ name: 'mystery', passed: false, durationMs: 10 }],
    };
    const md = formatFeedback(report, 0);
    expect(md).toContain('mystery');
    expect(md).toContain('no diagnostic');
  });

  it('shows all-pass summary when everything passes', () => {
    const report: TestReport = {
      passed: 2,
      failed: 0,
      totalDurationMs: 50,
      cases: [
        { name: 'a', passed: true, durationMs: 25 },
        { name: 'b', passed: true, durationMs: 25 },
      ],
    };
    const md = formatFeedback(report, 100);
    expect(md).toContain('## Auto-grading result');
    expect(md).toContain('Score: 100/100');
    expect(md).toContain('All 2 test case(s) passed');
  });
});
