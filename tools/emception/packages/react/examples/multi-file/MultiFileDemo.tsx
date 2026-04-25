// Multi-file React example for @emception/react.
//
// Demonstrates a small editor + preview UI driving <EmceptionRun>
// with a workspace seeded from in-app state. Useful as a starting
// point for tutorials, code playgrounds, and small assignments.

'use client';

import { createEmception } from '@emception/browser';
import { EmceptionRun, useEmception } from '@emception/react';
import '@emception/webcomponent';
import { useCallback, useMemo, useState } from 'react';

type FileMap = Record<string, string>;

const STARTER: FileMap = {
    'main.cpp': `#include "greet.h"
int main() { greet("world"); return 0; }
`,
    'greet.h': `#pragma once
void greet(const char* who);
`,
    'greet.cpp': `#include <stdio.h>
#include "greet.h"
void greet(const char* who) { printf("hello, %s\\n", who); }
`,
};

export function MultiFileDemo() {
    const [files, setFiles] = useState<FileMap>(STARTER);
    const [activePath, setActivePath] = useState<string>('main.cpp');

    const create = useCallback(
        (signal: AbortSignal) =>
            createEmception({ manifestUrl: '/cdn/manifest.json', tty: 'none', signal }),
        [],
    );
    const { api, status } = useEmception({ create });

    // Workspace seed is derived from current file map. Re-seeding is
    // cheap because seedPolicy='overwrite' replaces files in place.
    const workspace = useMemo(
        () => ({
            name: 'multi-file-demo',
            seed: Object.fromEntries(
                Object.entries(files).map(([path, content]) => [path, { content, visibility: 'public' as const }]),
            ),
            seedPolicy: 'overwrite' as const,
            build: {
                std: 'c++20',
                cflags: ['-O1', '-Wall'],
                sources: ['main.cpp', 'greet.cpp'],
                output: 'a.out',
            },
        }),
        [files],
    );

    if (status === 'loading') return <p>Loading toolchain…</p>;

    return (
        <div style={{ display: 'grid', gridTemplateColumns: '12rem 1fr', gap: '1rem' }}>
            <ul style={{ listStyle: 'none', padding: 0 }}>
                {Object.keys(files).map((path) => (
                    <li key={path}>
                        <button
                            type="button"
                            onClick={() => setActivePath(path)}
                            style={{ fontWeight: path === activePath ? 'bold' : 'normal' }}
                        >
                            {path}
                        </button>
                    </li>
                ))}
            </ul>

            <div>
                <textarea
                    value={files[activePath] ?? ''}
                    onChange={(e) => setFiles((f) => ({ ...f, [activePath]: e.target.value }))}
                    style={{ width: '100%', minHeight: '12rem', fontFamily: 'monospace' }}
                />

                <EmceptionRun
                    api={api}
                    preset="cpp"
                    workspace={workspace}
                    autorun
                    onExit={(p) => console.log('[exit]', p.code)}
                />
            </div>
        </div>
    );
}
