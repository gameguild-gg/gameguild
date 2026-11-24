import type { CodingExerciseValidationState } from './exercise-validation-types';

const STATUS = {
  'reference-passed': 'Reference passed',
  'reference-failed': 'Reference failed',
  'incorrect-detected': 'Incorrect solution detected',
  'incorrect-undetected': 'Incorrect solution was not detected',
  error: 'Execution error',
};

export function CodingAuthoringValidationResults({ state, stale }: { readonly state: CodingExerciseValidationState; readonly stale: boolean }) {
  return (
    <section aria-label="Exercise check results" className="cs-author-check-results">
      {stale ? (
        <p role="status" className="cs-author-pending">
          Outdated checks. Exercise changed.
        </p>
      ) : null}
      {state.operation === 'running' || state.operation === 'stopping' ? (
        <p role="status">{state.operation === 'stopping' ? 'Stopping checks...' : 'Checking solutions...'}</p>
      ) : null}
      {state.error ? <p role="alert">{state.error}</p> : null}
      {state.results.map((check) => (
        <article key={check.solutionId} data-solution-status={check.status} data-stale={stale || undefined}>
          <header>
            <strong>{check.name}</strong>
            <span>{STATUS[check.status]}</span>
          </header>
          {check.status === 'error' ? (
            <p role="alert">{check.message}</p>
          ) : (
            <>
              <p>
                {check.report.passed} passed / {check.report.failed} failed / {check.report.cases.length} total
              </p>
              {check.report.cases.map((test, index) => (
                <details key={index} open={!test.passed}>
                  <summary>
                    {test.name || `Case ${index + 1}`} / {test.passed ? 'Passed' : 'Failed'} / {test.durationMs} ms
                  </summary>
                  {test.diagnostic ? <pre>{test.diagnostic}</pre> : null}
                </details>
              ))}
            </>
          )}
        </article>
      ))}
    </section>
  );
}
