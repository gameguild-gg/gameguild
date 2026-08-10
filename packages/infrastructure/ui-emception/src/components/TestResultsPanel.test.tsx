import { render, fireEvent, screen } from '@testing-library/react';
import TestResultsPanel from './TestResultsPanel';
import type { TestReport } from './TestResultsPanel';

const SAMPLE_REPORT: TestReport = {
  passed: 2,
  failed: 1,
  totalDurationMs: 230,
  cases: [
    { name: 'test_add', passed: true, durationMs: 80 },
    { name: 'test_sub', passed: true, durationMs: 50 },
    { name: 'test_div', passed: false, durationMs: 100, diagnostic: 'expected 5 got 0\nline 42' },
  ],
};

describe('TestResultsPanel', () => {
  it('renders totals: passed / failed / total', () => {
    render(<TestResultsPanel report={SAMPLE_REPORT} />);
    expect(screen.getByText(/2 passed/)).toBeTruthy();
    expect(screen.getByText(/1 failed/)).toBeTruthy();
    expect(screen.getByText(/3 total/)).toBeTruthy();
  });

  it('renders per-case rows with name, check/cross, duration', () => {
    render(<TestResultsPanel report={SAMPLE_REPORT} />);
    // Passed cases show check mark
    const case0 = screen.getByTestId('test-case-0');
    expect(case0.textContent).toContain('test_add');
    expect(case0.textContent).toContain('80ms');
    expect(case0.textContent).toContain('\u2713');

    // Failed case shows cross
    const case2 = screen.getByTestId('test-case-2');
    expect(case2.textContent).toContain('test_div');
    expect(case2.textContent).toContain('100ms');
    expect(case2.textContent).toContain('\u2717');
  });

  it('expands diagnostic on click for failed case', () => {
    render(<TestResultsPanel report={SAMPLE_REPORT} />);
    const case2 = screen.getByTestId('test-case-2');
    fireEvent.click(case2);
    const diag = screen.getByTestId('test-case-diagnostic-2');
    expect(diag.textContent).toContain('expected 5 got 0');
    expect(diag.textContent).toContain('line 42');
  });

  it('collapses diagnostic on second click', () => {
    render(<TestResultsPanel report={SAMPLE_REPORT} />);
    const case2 = screen.getByTestId('test-case-2');
    fireEvent.click(case2);
    expect(screen.getByTestId('test-case-diagnostic-2')).toBeTruthy();
    fireEvent.click(case2);
    expect(screen.queryByTestId('test-case-diagnostic-2')).toBeNull();
  });

  it('does not expand passed cases (no diagnostic)', () => {
    render(<TestResultsPanel report={SAMPLE_REPORT} />);
    const case0 = screen.getByTestId('test-case-0');
    fireEvent.click(case0);
    expect(screen.queryByTestId('test-case-diagnostic-0')).toBeNull();
  });

  it('shows score line when maxScore + passingScore provided', () => {
    render(<TestResultsPanel report={SAMPLE_REPORT} maxScore={100} passingScore={60} />);
    // 2/3 passed → round(2/3*100) = 67 → >= 60 → pass
    expect(screen.getByText(/Score: 67\/100/)).toBeTruthy();
  });

  it('shows failing score when below passing threshold', () => {
    const mostlyFailing: TestReport = {
      passed: 1,
      failed: 2,
      totalDurationMs: 100,
      cases: [
        { name: 'pass', passed: true, durationMs: 30 },
        { name: 'fail1', passed: false, durationMs: 30, diagnostic: 'bad' },
        { name: 'fail2', passed: false, durationMs: 40, diagnostic: 'bad' },
      ],
    };
    render(<TestResultsPanel report={mostlyFailing} maxScore={100} passingScore={60} />);
    // 1/3 passed → round(1/3*100) = 33 → < 60 → fail
    expect(screen.getByText(/Score: 33\/100/)).toBeTruthy();
  });

  it('hides score line when maxScore/passingScore omitted', () => {
    render(<TestResultsPanel report={SAMPLE_REPORT} />);
    expect(screen.queryByText(/Score:/)).toBeNull();
  });

  it('renders empty state for zero cases', () => {
    const emptyReport: TestReport = { passed: 0, failed: 0, totalDurationMs: 0, cases: [] };
    render(<TestResultsPanel report={emptyReport} />);
    expect(screen.getByText(/No test cases executed/)).toBeTruthy();
  });

  it('payload correctness: asserts exact numeric values not just "called"', () => {
    render(<TestResultsPanel report={SAMPLE_REPORT} maxScore={100} passingScore={60} />);
    // Verify exact score computation: 2/3 * 100 = 66.67 → rounds to 67
    const scoreEl = screen.getByText(/Score:/);
    expect(scoreEl.textContent).toBe('Score: 67/100');
    // Verify total duration
    expect(screen.getByText('230ms')).toBeTruthy();
  });
});
