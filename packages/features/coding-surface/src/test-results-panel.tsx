import { useState } from 'react';
import type { CodingPreviewResult } from './types';

export interface CodingTestResultsPanelProps {
  readonly result: CodingPreviewResult;
  readonly maxScore: number;
}

/** Presents local execution evidence without treating it as an academic result. */
export function CodingTestResultsPanel({ result, maxScore }: CodingTestResultsPanelProps) {
  const [expandedCase, setExpandedCase] = useState<number | null>(null);
  const { report, score } = result;

  return (
    <section
      aria-label="Coding preview results"
      data-testid="test-results-panel"
      style={{ display: 'grid', gap: 8, padding: 8 }}
    >
      <header style={{ display: 'flex', alignItems: 'center', flexWrap: 'wrap', gap: 8 }}>
        <strong>Local preview</strong>
        <span>{report.passed} passed</span>
        <span>{report.failed} failed</span>
        <span>{report.cases.length} total</span>
        <span style={{ marginInlineStart: 'auto' }}>
          Estimate: {score.score} / {maxScore}
        </span>
      </header>

      {report.cases.map((testCase, index) => {
        const expanded = expandedCase === index;
        return (
          <div key={`${testCase.name}:${index}`} style={{ borderTop: '1px solid rgb(63 63 70)', paddingTop: 6 }}>
            <button
              type="button"
              data-testid={`test-case-${index}`}
              aria-expanded={testCase.diagnostic ? expanded : undefined}
              onClick={() => {
                if (testCase.diagnostic) setExpandedCase(expanded ? null : index);
              }}
              style={{
                alignItems: 'center',
                background: 'transparent',
                border: 0,
                color: 'inherit',
                cursor: testCase.diagnostic ? 'pointer' : 'default',
                display: 'flex',
                gap: 8,
                padding: 0,
                textAlign: 'start',
                width: '100%',
              }}
            >
              <span aria-hidden="true">{testCase.passed ? '✓' : '✗'}</span>
              <span style={{ flex: 1 }}>{testCase.name || `Case ${index + 1}`}</span>
              <span>{testCase.durationMs}ms</span>
              {testCase.diagnostic ? <span aria-hidden="true">{expanded ? '▲' : '▼'}</span> : null}
            </button>
            {expanded && testCase.diagnostic ? (
              <pre
                data-testid={`test-case-diagnostic-${index}`}
                style={{ margin: '6px 0 0', overflowWrap: 'anywhere', whiteSpace: 'pre-wrap' }}
              >
                {testCase.diagnostic}
              </pre>
            ) : null}
          </div>
        );
      })}

      {report.cases.length === 0 ? <p>No test cases executed.</p> : null}
      <p>This preview is not an official grade.</p>
    </section>
  );
}
