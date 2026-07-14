// Multi-target build example for @emception/react.
//
// Demonstrates two <EmceptionRun> components driven by the SAME
// EmceptionAPI instance (shared WASM worker) but compiling different
// source files into different output binaries. This is the pattern for
// "compile once, run many" or side-by-side comparison of implementations.
//
// One worker is booted; both panels share it via the `api` prop.

'use client';

import { createEmception } from '@emception/browser';
import { EmceptionRun, useEmception } from '@emception/react';
import '@emception/webcomponent';
import { useCallback } from 'react';

const SOURCE_A = `#include <stdio.h>
// Implementation A — iterative
int fib(int n) {
  int a = 0, b = 1;
  for (int i = 0; i < n; i++) { int t = a + b; a = b; b = t; }
  return a;
}
int main() { printf("fib(10) = %d\\n", fib(10)); return 0; }
`;

const SOURCE_B = `#include <stdio.h>
// Implementation B — recursive
int fib(int n) { return n <= 1 ? n : fib(n-1) + fib(n-2); }
int main() { printf("fib(10) = %d\\n", fib(10)); return 0; }
`;

export function MultiTargetDemo() {
    const create = useCallback(
        (signal: AbortSignal) =>
            createEmception({ manifestUrl: '/cdn/manifest.json', tty: 'none', signal }),
        [],
    );

    // A single shared API boots the toolchain once for both panels.
    const { api, status, error } = useEmception({ create });

    if (status === 'loading') return <p>Loading shared toolchain…</p>;
    if (status === 'error') return <pre>Boot error: {String(error)}</pre>;

    return (
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '1rem' }}>
            <section>
                <h3 style={{ marginBottom: '0.5rem' }}>Iterative</h3>
                <EmceptionRun
                    api={api}
                    preset="cpp"
                    source={SOURCE_A}
                    autorun
                    style={{ minHeight: '8rem' }}
                />
            </section>

            <section>
                <h3 style={{ marginBottom: '0.5rem' }}>Recursive</h3>
                <EmceptionRun
                    api={api}
                    preset="cpp"
                    source={SOURCE_B}
                    autorun
                    style={{ minHeight: '8rem' }}
                />
            </section>
        </div>
    );
}
