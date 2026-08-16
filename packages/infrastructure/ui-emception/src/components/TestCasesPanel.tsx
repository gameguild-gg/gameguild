import type { CSSProperties } from 'react';
import type { GradingCase } from './ide-types';

export interface TestCasesPanelProps {
  cases: GradingCase[];
}

const thStyle: CSSProperties = {
  textAlign: 'left',
  padding: '4px 8px',
  color: '#6c7086',
  fontWeight: 500,
  fontSize: '0.7rem',
  borderBottom: '1px solid #313244',
  whiteSpace: 'nowrap',
};

const tdStyle: CSSProperties = {
  padding: '4px 8px',
  borderBottom: '1px solid #313244',
  overflow: 'hidden',
  textOverflow: 'ellipsis',
  whiteSpace: 'nowrap',
};

const monoStyle: CSSProperties = {
  ...tdStyle,
  fontFamily: 'ui-monospace, SFMono-Regular, Menlo, monospace',
};

/** Read-only table of a grading plan's cases (student-facing spec view). */
export default function TestCasesPanel({ cases }: TestCasesPanelProps) {
  return (
    <div
      data-testid="test-cases-panel"
      style={{
        flex: 1,
        overflow: 'auto',
        background: '#11111b',
        padding: '8px',
        color: '#cdd6f4',
        fontSize: '0.75rem',
      }}
    >
      <table style={{ width: '100%', tableLayout: 'fixed', borderCollapse: 'collapse' }}>
        <colgroup>
          <col style={{ width: '24%' }} />
          <col style={{ width: '26%' }} />
          <col style={{ width: '36%' }} />
          <col style={{ width: '14%' }} />
        </colgroup>
        <thead>
          <tr>
            <th style={thStyle}>Name</th>
            <th style={thStyle}>Stdin</th>
            <th style={thStyle}>Expected output</th>
            <th style={thStyle}>Weight</th>
          </tr>
        </thead>
        <tbody>
          {cases.map((c, i) => {
            const stdinText = c.stdin && c.stdin.length > 0 ? c.stdin : '(empty)';
            const expectedText = c.expectedStdout === undefined ? '' : String(c.expectedStdout);
            return (
              <tr key={i} data-testid="test-case-row">
                <td style={tdStyle} title={c.name ?? ''}>
                  {c.name || '(unnamed)'}
                </td>
                <td style={monoStyle} title={stdinText}>
                  {stdinText}
                </td>
                <td style={monoStyle} title={expectedText}>
                  {expectedText}
                </td>
                <td style={tdStyle}>{c.weight ?? 1}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
      {cases.length === 0 && (
        <div style={{ color: '#6c7086', padding: '0.5rem 0' }}>No test cases.</div>
      )}
    </div>
  );
}
