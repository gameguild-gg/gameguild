// Unit tests for `computeScore` — weighted scoring of a TestReport against
// a TestPlan. Pure function: types-only inputs, no engine dependency.
//
// Run: `node --test packages/core/src/testing/score.test.ts` (Node 24 strips types).

import assert from 'node:assert/strict';
import test from 'node:test';

import type { TestCase, TestCaseResult, TestPlan, TestReport } from '../types.ts';
import { computeScore } from './score.ts';

// --- Fixtures ----------------------------------------------------------------

/** Minimal clang-query case carrying only an optional weight + name. */
function caseOf(weight: number | undefined, name?: string): TestCase {
  const c: TestCase = { kind: 'clang-query', matcher: '', expect: 'found' };
  if (weight !== undefined) c.weight = weight;
  if (name !== undefined) c.name = name;
  return c;
}

function resultOf(name: string, passed: boolean, diagnostic?: string): TestCaseResult {
  const r: TestCaseResult = { name, passed, durationMs: 1 };
  if (diagnostic !== undefined) r.diagnostic = diagnostic;
  return r;
}

function reportOf(cases: TestCaseResult[]): TestReport {
  const passed = cases.filter((c) => c.passed).length;
  return {
    passed,
    failed: cases.length - passed,
    totalDurationMs: cases.length,
    cases,
  };
}

// --- Tests -------------------------------------------------------------------

test('computeScore: all pass → full score, passed=true, all-pass feedback', () => {
  const plan: TestPlan = {
    cases: [caseOf(1, 'a'), caseOf(2, 'b'), caseOf(3, 'c')],
  };
  const report = reportOf([
    resultOf('a', true), resultOf('b', true), resultOf('c', true),
  ]);

  const out = computeScore(report, plan, 100, 60);

  assert.equal(out.score, 100);
  assert.equal(out.passed, true);
  assert.match(out.feedback, /all 3.*pass/i);
});

test('computeScore: none pass → score=0, passed=false, lists 3 failing', () => {
  const plan: TestPlan = {
    cases: [caseOf(1, 'a'), caseOf(2, 'b'), caseOf(3, 'c')],
  };
  const report = reportOf([
    resultOf('a', false, 'boom-a'),
    resultOf('b', false, 'boom-b'),
    resultOf('c', false, 'boom-c'),
  ]);

  const out = computeScore(report, plan, 100, 60);

  assert.equal(out.score, 0);
  assert.equal(out.passed, false);
  assert.match(out.feedback, /a/);
  assert.match(out.feedback, /b/);
  assert.match(out.feedback, /c/);
});

test('computeScore: only weight-1 case passes → score=17 (round(1/6*100)), passed=false', () => {
  const plan: TestPlan = {
    cases: [caseOf(1, 'a'), caseOf(2, 'b'), caseOf(3, 'c')],
  };
  const report = reportOf([
    resultOf('a', true),
    resultOf('b', false, 'boom-b'),
    resultOf('c', false, 'boom-c'),
  ]);

  const out = computeScore(report, plan, 100, 60);

  assert.equal(out.score, 17);
  assert.equal(out.passed, false);
  // feedback lists ONLY the 2 failing cases, not the passing one
  assert.doesNotMatch(out.feedback, /\ba\b/);
  assert.match(out.feedback, /b/);
  assert.match(out.feedback, /c/);
});

test('computeScore: omitted weights default to 1 → score=67 (round(2/3*100))', () => {
  const plan: TestPlan = {
    cases: [caseOf(undefined, 'a'), caseOf(undefined, 'b'), caseOf(undefined, 'c')],
  };
  const report = reportOf([
    resultOf('a', true),
    resultOf('b', false, 'boom-b'),
    resultOf('c', true),
  ]);

  const out = computeScore(report, plan, 100, 60);

  assert.equal(out.score, 67);
  assert.equal(out.passed, true);
});

test('computeScore: passingScore boundary (score==passingScore) → passed=true via >=', () => {
  // weights [1,1], one passes → score = round(1/2 * 100) = 50 == passingScore
  const plan: TestPlan = { cases: [caseOf(1, 'a'), caseOf(1, 'b')] };
  const report = reportOf([resultOf('a', true), resultOf('b', false, 'boom')]);

  const out = computeScore(report, plan, 100, 50);

  assert.equal(out.score, 50);
  assert.equal(out.passed, true, 'score == passingScore must pass (>=, not >)');
});

test('computeScore: diagnostic truncated to 500 chars in feedback', () => {
  const long = 'x'.repeat(800);
  const plan: TestPlan = { cases: [caseOf(1, 'a')] };
  const report = reportOf([resultOf('a', false, long)]);

  const out = computeScore(report, plan, 100, 60);

  // feedback contains the name + at most 500 chars of diagnostic
  assert.match(out.feedback, /a/);
  const body = out.feedback;
  // Count the run of 'x' chars — must be exactly 500
  const xs = body.match(/x+/)?.[0] ?? '';
  assert.equal(xs.length, 500, 'diagnostic truncated to 500 chars');
});
