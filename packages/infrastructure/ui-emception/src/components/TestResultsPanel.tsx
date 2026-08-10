import { useState } from 'react';

/** Mirrors emception's TestCaseResult shape (avoids adding emception as a dep for types only). */
export interface TestCaseResult {
  name: string;
  passed: boolean;
  durationMs: number;
  diagnostic?: string;
}

/** Mirrors emception's TestReport shape. */
export interface TestReport {
  passed: number;
  failed: number;
  totalDurationMs: number;
  cases: TestCaseResult[];
}

export interface TestResultsPanelProps {
  report: TestReport;
  /** When provided, compute + display a weighted score. */
  maxScore?: number;
  passingScore?: number;
  /** Per-case weights for weighted scoring. Falls back to weight=1 per case. */
  weights?: number[];
}

/** Weighted score — mirrors computeScore logic from emception/testing/score.ts. */
function computeLocalScore(report: TestReport, maxScore: number, passingScore: number, weights?: number[]): { score: number; passed: boolean } {
  const caseWeights = weights ?? report.cases.map(() => 1);
  const totalWeight = caseWeights.reduce((sum, w) => sum + w, 0) || 1;
  let passedWeight = 0;
  report.cases.forEach((r, i) => {
    if (r.passed) passedWeight += caseWeights[i] ?? 1;
  });
  const score = Math.round((passedWeight / totalWeight) * maxScore);
  return { score, passed: score >= passingScore };
}

export default function TestResultsPanel({ report, maxScore, passingScore, weights }: TestResultsPanelProps) {
  const [expandedCase, setExpandedCase] = useState<number | null>(null);
  const total = report.cases.length;
  const hasScore = maxScore != null && passingScore != null;
  const scoreResult = hasScore ? computeLocalScore(report, maxScore!, passingScore!, weights) : null;

  return (
    <div
      data-testid="test-results-panel"
      style={{
        background: '#1e1e2e',
        border: '1px solid #313244',
        borderRadius: 6,
        padding: '0.5rem 0.75rem',
        fontSize: '0.8rem',
        fontFamily: 'system-ui, sans-serif',
        color: '#cdd6f4',
      }}
    >
      {/* ── Header totals ── */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', marginBottom: '0.5rem' }}>
        <span style={{ fontWeight: 600 }}>
          <span style={{ color: '#a6e3a1' }}>{report.passed} passed</span>
          {' / '}
          <span style={{ color: '#f38ba8' }}>{report.failed} failed</span>
          {' / '}
          <span>{total} total</span>
        </span>
        <span style={{ color: '#6c7086', fontSize: '0.7rem' }}>{report.totalDurationMs}ms</span>
        {hasScore && scoreResult && (
          <span style={{ marginLeft: 'auto', fontWeight: 600, color: scoreResult.passed ? '#a6e3a1' : '#f38ba8' }}>
            Score: {scoreResult.score}/{maxScore}
          </span>
        )}
      </div>

      {/* ── Per-case rows ── */}
      {report.cases.map((c, i) => (
        <div key={i} style={{ borderTop: '1px solid #313244', paddingTop: '0.35rem', paddingBottom: '0.35rem' }}>
          <div
            style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', cursor: c.diagnostic ? 'pointer' : 'default' }}
            onClick={() => c.diagnostic && setExpandedCase(expandedCase === i ? null : i)}
            data-testid={`test-case-${i}`}
          >
            <span style={{ color: c.passed ? '#a6e3a1' : '#f38ba8', fontWeight: 600, minWidth: '1.2em' }}>
              {c.passed ? '\u2713' : '\u2717'}
            </span>
            <span style={{ flex: 1 }}>{c.name || `Case ${i + 1}`}</span>
            <span style={{ color: '#6c7086', fontSize: '0.7rem' }}>{c.durationMs}ms</span>
            {c.diagnostic && (
              <span style={{ color: '#6c7086', fontSize: '0.65rem' }}>{expandedCase === i ? '\u25B2' : '\u25BC'}</span>
            )}
          </div>
          {expandedCase === i && c.diagnostic && (
            <pre
              data-testid={`test-case-diagnostic-${i}`}
              style={{
                margin: '0.25rem 0 0 1.7rem',
                padding: '0.4rem',
                background: '#181825',
                borderRadius: 4,
                fontSize: '0.72rem',
                color: '#f38ba8',
                whiteSpace: 'pre-wrap',
                wordBreak: 'break-word',
              }}
            >
              {c.diagnostic}
            </pre>
          )}
        </div>
      ))}

      {total === 0 && (
        <div style={{ color: '#6c7086', padding: '0.5rem 0' }}>No test cases executed.</div>
      )}
    </div>
  );
}
