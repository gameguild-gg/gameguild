// Auto-grader UI example for @emception/react.
//
// Shows how to embed a "Submit & grade" workflow in a React app:
//   1. Student types code in a <textarea>.
//   2. On submit the hidden grader test file is injected into the workspace.
//   3. <EmceptionRun> compiles + runs both files. The exit code decides the
//      grade (0 = all tests pass, non-zero = failure).
//
// The `graderSource` string would normally be served from a backend so
// the student cannot see it — this is just a client-side demo.

'use client';

import { createEmception } from '@emception/browser';
import { EmceptionRun, useEmception } from '@emception/react';
import '@emception/webcomponent';
import { useCallback, useMemo, useState } from 'react';

const GRADER_SOURCE = `// hidden — not shown to student
#include <stdio.h>
extern int add(int a, int b);
int main() {
  int ok = 1;
  ok &= (add(1, 2) == 3);
  ok &= (add(-1, 1) == 0);
  ok &= (add(0, 0) == 0);
  printf(ok ? "ALL PASS\\n" : "FAIL\\n");
  return ok ? 0 : 1;
}`;

const STARTER_SOURCE = `// Implement the function below.
int add(int a, int b) {
  return a + b;
}`;

export function GraderDemo() {
    const [source, setSource] = useState(STARTER_SOURCE);
    const [submitted, setSubmitted] = useState(false);
    const [result, setResult] = useState<'pass' | 'fail' | null>(null);

    const create = useCallback(
        (signal: AbortSignal) =>
            createEmception({ manifestUrl: '/cdn/manifest.json', tty: 'none', signal }),
        [],
    );
    const { api, status } = useEmception({ create });

    const workspace = useMemo(
        () => ({
            name: 'grader-demo',
            seed: {
                'submission.cpp': { content: source, visibility: 'public' as const },
                'grader.cpp': { content: GRADER_SOURCE, visibility: 'hidden' as const },
            },
            seedPolicy: 'overwrite' as const,
            build: {
                std: 'c++17',
                cflags: ['-Wall'],
                sources: ['submission.cpp', 'grader.cpp'],
                output: 'a.out',
            },
        }),
        [source],
    );

    return (
        <div>
            <textarea
                value={source}
                onChange={(e) => setSource(e.target.value)}
                rows={8}
                style={{ width: '100%', fontFamily: 'monospace' }}
            />
            <button
                type="button"
                disabled={status !== 'ready' || submitted}
                onClick={() => setSubmitted(true)}
            >
                {status === 'loading' ? 'Loading…' : 'Submit & grade'}
            </button>

            {submitted && (
                <EmceptionRun
                    api={api}
                    workspace={workspace}
                    autorun
                    onExit={(p) => setResult(p.code === 0 ? 'pass' : 'fail')}
                    style={{ minHeight: '6rem' }}
                />
            )}

            {result && (
                <p style={{ color: result === 'pass' ? 'green' : 'red', fontWeight: 'bold' }}>
                    {result === 'pass' ? '✓ All tests passed' : '✗ Some tests failed'}
                </p>
            )}
        </div>
    );
}
