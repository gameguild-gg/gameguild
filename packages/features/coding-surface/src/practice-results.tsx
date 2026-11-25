import type { ReactElement } from 'react';
import type { CodingPracticeExecutionState } from './practice-run-types';
import './learner-solving.css';

export interface CodingPracticeResultsProps {
  readonly state: CodingPracticeExecutionState;
}

export function CodingPracticeResults({ state }: CodingPracticeResultsProps): ReactElement {
  if (state.status === 'idle') return <p>No case run yet.</p>;
  if (state.status === 'running') return <p role="status">Running {state.name}...</p>;
  if (state.status === 'cancelled') return <p role="status">Run cancelled.</p>;
  if (state.status === 'error') return <p role="alert">{state.message}</p>;
  const result = state.result;
  const observed = result.observation;
  const compared = new Set(result.comparison.status === 'observed' ? [] : result.comparison.checks.map((check) => check.field));
  const label =
    result.comparison.status === 'observed' ? 'Output observed' : result.comparison.status === 'passed' ? 'Matches expectation' : 'Does not match expectation';
  return (
    <section aria-label="Case execution result" className="cs-practice-result" data-stale={state.stale} data-outcome={result.comparison.status}>
      <header>
        <strong>{result.case.name || 'Case result'}</strong>
        <span>{observed.durationMs} ms</span>
        <span>Exit {observed.exitCode}</span>
      </header>
      {state.stale ? (
        <p role="status" className="cs-practice-stale">
          Outdated case result. Code or cases changed.
        </p>
      ) : null}
      <h3>{label}</h3>
      <section aria-label="Case input">
        <h4>{result.case.kind === 'stdio' ? 'Input' : 'Inputs'}</h4>
        <pre>{(result.case.kind === 'stdio' ? result.case.stdin : result.case.inputs.map((value) => value.text).join(', ')) || '(empty)'}</pre>
      </section>
      {result.comparison.status !== 'observed'
        ? result.comparison.checks.map((check) => (
            <section key={check.field} aria-label={`${check.field} comparison`} data-match={check.matches}>
              <h4>{check.field === 'returnValue' ? 'Return value' : check.field}</h4>
              <dl>
                <dt>Expected</dt>
                <dd>
                  <pre>{check.expected || '(empty)'}</pre>
                </dd>
                <dt>Actual</dt>
                <dd>
                  <pre>{check.actual || '(empty)'}</pre>
                </dd>
              </dl>
            </section>
          ))
        : null}
      {observed.returnValue && !compared.has('returnValue') ? (
        <section aria-label="Return value">
          <h4>Return value</h4>
          <pre>{String(observed.returnValue.value) || '(empty)'}</pre>
        </section>
      ) : null}
      {!compared.has('stdout') ? (
        <section aria-label="stdout">
          <h4>stdout</h4>
          <pre>{observed.stdout || '(empty)'}</pre>
        </section>
      ) : null}
      {!compared.has('stderr') ? (
        <section aria-label="stderr">
          <h4>stderr</h4>
          <pre>{observed.stderr || '(empty)'}</pre>
        </section>
      ) : null}
    </section>
  );
}
