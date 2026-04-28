'use client';

import { createEmception } from '@emception/browser';
import { EmceptionRun, useEmception, type EmceptionAPI } from '@emception/react';
import { useCallback } from 'react';

// `@emception/webcomponent` self-registers <emception-run> on import.
// Keep it in a client-only entry point.
import '@emception/webcomponent';

const STARTER_SOURCE = `#include <stdio.h>
int main() {
  int x;
  scanf("%d", &x);
  printf("got %d\\n", x * 2);
  return 0;
}
`;

const manifestUrl = `${import.meta.env.BASE_URL}cdn/manifest.json`;

export default function App() {
    const create = useCallback(
        async (_signal: AbortSignal): Promise<EmceptionAPI> => {
            // The browser EmceptionAPI is a strict subset of the core API
            // imported by `@emception/react`; cast through unknown for now.
            const api = await createEmception({
                manifestUrl,
                tty: 'none',
            });
            return api as unknown as EmceptionAPI;
        },
        [],
    );

    const { api, status, error } = useEmception({ create });

    return (
        <main className="page">
            <header>
                <h1>&lt;EmceptionRun /&gt; React demo</h1>
                <p>
                    Minimal example using the declarative React wrapper from{' '}
                    <code>@emception/react</code> on top of <code>@emception/browser</code>.
                </p>
            </header>

            {status === 'loading' && <p>Loading toolchain…</p>}
            {status === 'error' && (
                <pre className="error">Failed to boot: {String(error)}</pre>
            )}
            {status === 'ready' && (
                <EmceptionRun
                    api={api}
                    preset="cpp"
                    autorun
                    source={STARTER_SOURCE}
                    onStdout={(p) => console.log('[stdout]', p.chunk)}
                    onStderr={(p) => console.warn('[stderr]', p.chunk)}
                    onExit={(p) => console.log('[exit]', p.exitCode)}
                    style={{ minHeight: '20rem', display: 'block' }}
                >
                    <textarea slot="stdin" defaultValue="21" rows={2} />
                </EmceptionRun>
            )}
        </main>
    );
}
