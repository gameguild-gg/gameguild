// Minimal `<EmceptionRun>` example for React 19.
//
// This file lives under packages/react/examples/basic/ and is meant as
// copy-paste reference, not as a buildable demo (no example bundler
// wired up yet). Drop it in any React 19 + Vite/Next/CRA project that
// already has `@emception/react`, `@emception/browser`, and
// `@emception/webcomponent` installed.

'use client';

import { createEmception } from '@emception/browser';
import { EmceptionRun, useEmception } from '@emception/react';
import { useCallback } from 'react';

// `@emception/webcomponent` self-registers <emception-run> on import,
// so it must NOT be imported from server-rendered code paths.
import '@emception/webcomponent';

const STARTER_SOURCE = `#include <stdio.h>
int main() {
  int x;
  scanf("%d", &x);
  printf("got %d\\n", x * 2);
  return 0;
}`;

export function Demo() {
    const create = useCallback(
        (signal: AbortSignal) =>
            createEmception({
                manifestUrl: '/cdn/manifest.json',
                tty: 'none',
                signal,
            }),
        [],
    );

    const { api, status, error } = useEmception({ create });

    if (status === 'loading') return <p>Loading toolchain…</p>;
    if (status === 'error') return <pre>Failed to boot: {String(error)}</pre>;

    return (
        <EmceptionRun
            api={api}
            preset="cpp"
            autorun
            source={STARTER_SOURCE}
            onStdout={(p) => console.log('[stdout]', p.chunk)}
            onStderr={(p) => console.warn('[stderr]', p.chunk)}
            onExit={(p) => console.log('[exit]', p.code)}
            style={{ minHeight: '12rem' }}
        >
            <textarea slot="stdin" defaultValue="21" />
        </EmceptionRun>
    );
}
