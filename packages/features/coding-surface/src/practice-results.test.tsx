import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { CodingPracticeResults } from './practice-results';
import type { CodingPracticeRunResult } from './practice-run-types';

afterEach(cleanup);

const observation: CodingPracticeRunResult = {
  contextKey: 'actor.revision.attempt',
  executionId: 1,
  selection: { source: 'personal', id: 'mine' },
  case: { kind: 'stdio', id: 'mine', name: 'Personal input', stdin: 'Ada', expected: { stdout: null, stderr: null, exitCode: null } },
  observation: { stdout: 'Hello Ada', stderr: '', exitCode: 0, durationMs: 3, returnValue: null },
  comparison: { status: 'observed' },
  academic: false,
};

describe('practice diagnostic result view', () => {
  it('labels no-oracle output as observed with no fabricated pass, expectation or grade', () => {
    const { container } = render(<CodingPracticeResults state={{ status: 'completed', result: observation, stale: false }} />);
    expect(screen.getByText('Output observed')).toBeTruthy();
    expect(screen.getByText('Hello Ada')).toBeTruthy();
    expect(screen.getByText('Ada')).toBeTruthy();
    expect(container.textContent).not.toMatch(/passed|correct|score|estimate|expected/i);
  });

  it('shows explicit expected and actual values without treating empty expected output as missing', () => {
    render(
      <CodingPracticeResults
        state={{
          status: 'completed',
          stale: false,
          result: {
            ...observation,
            case: { ...observation.case, kind: 'stdio', stdin: 'Ada', expected: { stdout: '', stderr: null, exitCode: null } },
            comparison: { status: 'failed', checks: [{ field: 'stdout', expected: '', actual: 'Hello Ada', matches: false }] },
          },
        }}
      />,
    );
    expect(screen.getByText('Does not match expectation')).toBeTruthy();
    expect(screen.getByRole('region', { name: 'stdout comparison' }).textContent).toContain('Expected');
    expect(screen.getByRole('region', { name: 'stdout comparison' }).textContent).toContain('(empty)');
  });

  it('shows the actual return separately from output for a function observation', () => {
    render(
      <CodingPracticeResults
        state={{
          status: 'completed',
          stale: false,
          result: {
            ...observation,
            case: {
              id: 'mine',
              kind: 'function',
              name: 'Sum',
              inputs: [
                { type: 'integer', text: '2' },
                { type: 'integer', text: '3' },
              ],
              expected: null,
            },
            observation: { ...observation.observation, stdout: 'debug output', returnValue: { type: 'integer', value: 5 } },
          },
        }}
      />,
    );
    expect(screen.getByRole('region', { name: 'Return value' }).textContent).toContain('5');
    expect(screen.getByRole('region', { name: 'stdout' }).textContent).toContain('debug output');
    expect(screen.getByText('Output observed')).toBeTruthy();
  });

  it.each([
    [{ status: 'idle' }, 'No case run yet.'],
    [{ status: 'running', name: 'Sample' }, 'Running Sample...'],
    [{ status: 'cancelled', name: 'Sample' }, 'Run cancelled.'],
    [{ status: 'error', name: 'Sample', message: 'Compilation failed.' }, 'Compilation failed.'],
  ] as const)('exposes %j without inventing a completed verdict', (state, text) => {
    const { container } = render(<CodingPracticeResults state={state} />);
    expect(screen.getByText(text)).toBeTruthy();
    expect(container.textContent).not.toMatch(/Matches expectation|Output observed|score/i);
  });

  it('marks previous comparison evidence outdated after an edit', () => {
    render(
      <CodingPracticeResults
        state={{
          status: 'completed',
          stale: true,
          result: {
            ...observation,
            comparison: { status: 'passed', checks: [{ field: 'stdout', expected: 'Hello Ada', actual: 'Hello Ada', matches: true }] },
          },
        }}
      />,
    );
    expect(screen.getByRole('status').textContent).toContain('Outdated case result');
    expect(screen.getByRole('region', { name: 'Case execution result' }).getAttribute('data-stale')).toBe('true');
  });

  it('does not repeat the same observed value beneath its expected/actual comparison', () => {
    render(
      <CodingPracticeResults
        state={{
          status: 'completed',
          stale: false,
          result: {
            ...observation,
            comparison: { status: 'passed', checks: [{ field: 'stdout', expected: 'Hello Ada', actual: 'Hello Ada', matches: true }] },
          },
        }}
      />,
    );
    expect(screen.getByRole('region', { name: 'stdout comparison' })).toBeTruthy();
    expect(screen.queryByRole('region', { name: 'stdout' })).toBeNull();
  });
});
