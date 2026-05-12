// Test-runner UI for @emception/react.
//
// Listens for the `test-report` event emitted by <EmceptionRun> when a
// GTest / Catch2 binary is compiled with the `test` preset. Renders a
// simple pass/fail table next to the run output.
//
// The `test` preset compiles with GTest linked and wraps the binary in
// a test runner that produces JSON on stdout. Emception parses the JSON
// and fires `emception-test-report` with a structured payload.

'use client';

import { createEmception } from '@emception/browser';
import { EmceptionRun, useEmception } from '@emception/react';
import '@emception/webcomponent';
import { useCallback, useState } from 'react';

type TestCase = { name: string; status: 'pass' | 'fail' | 'skip'; message?: string };

const SOURCE = `#include <gtest/gtest.h>
TEST(Math, AddPositive)  { EXPECT_EQ(1 + 2, 3); }
TEST(Math, AddNegative)  { EXPECT_EQ(-1 + 1, 0); }
TEST(Math, Broken)       { EXPECT_EQ(1 + 1, 3); }  // intentionally fails
`;

export function TestRunnerDemo() {
    const [cases, setCases] = useState<TestCase[]>([]);

    const create = useCallback(
        (signal: AbortSignal) =>
            createEmception({ manifestUrl: '/cdn/manifest.json', tty: 'none', signal }),
        [],
    );
    const { api, status } = useEmception({ create });

    const handleReport = useCallback(
        (payload: { cases: TestCase[] }) => setCases(payload.cases),
        [],
    );

    if (status === 'loading') return <p>Loading toolchain…</p>;

    return (
        <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
            <EmceptionRun
                api={api}
                source={SOURCE}
                preset="test"
                autorun
                onTestReport={handleReport as (p: unknown) => void}
                style={{ flex: '1 1 28rem', minHeight: '10rem' }}
            />

            {cases.length > 0 && (
                <table style={{ flex: '1 1 20rem', borderCollapse: 'collapse', fontSize: '0.875rem' }}>
                    <thead>
                        <tr>
                            <th style={{ textAlign: 'left', padding: '0.25rem 0.5rem' }}>Test</th>
                            <th style={{ padding: '0.25rem 0.5rem' }}>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        {cases.map((tc) => (
                            <tr key={tc.name}>
                                <td style={{ padding: '0.25rem 0.5rem', fontFamily: 'monospace' }}>{tc.name}</td>
                                <td
                                    style={{
                                        padding: '0.25rem 0.5rem',
                                        color: tc.status === 'pass' ? 'green' : tc.status === 'fail' ? 'red' : 'gray',
                                        textAlign: 'center',
                                    }}
                                >
                                    {tc.status === 'pass' ? '✓' : tc.status === 'fail' ? '✗' : '—'}
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            )}
        </div>
    );
}
