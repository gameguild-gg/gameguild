'use client';

import { compileAndRun, createEmception, type CompilePhase, type EmceptionAPI } from '@gameguild/emception-browser';
import { ToolchainPreset } from 'emception';
import { useCallback, useEffect, useRef, useState } from 'react';

const STARTER_SOURCE = `#include <stdio.h>
int main() {
  int x;
  if (scanf("%d", &x) != 1) return 1;
  printf("got %d\\n", x * 2);
  return 0;
}
`;

const manifestUrl = `${import.meta.env.BASE_URL}cdn/manifest.json`;
const PRESET = ToolchainPreset.CPP;

type LogKind = 'info' | 'stdout' | 'stderr' | 'error';

interface LogLine {
    kind: LogKind;
    text: string;
}

export default function App() {
    const [bootStatus, setBootStatus] = useState<'idle' | 'loading' | 'ready' | 'error'>('idle');
    const [bootError, setBootError] = useState<string | null>(null);
    const apiRef = useRef<EmceptionAPI | null>(null);

    const [source, setSource] = useState(STARTER_SOURCE);
    const [stdin, setStdin] = useState('21');
    const [running, setRunning] = useState(false);
    const [log, setLog] = useState<LogLine[]>([]);

    const append = useCallback((kind: LogKind, text: string) => {
        setLog((prev) => [...prev, { kind, text }]);
    }, []);

    useEffect(() => {
        let cancelled = false;
        setBootStatus('loading');
        setBootError(null);
        (async () => {
            try {
                const api = await createEmception({ manifestUrl, tty: 'none' });
                if (cancelled) {
                    api.dispose();
                    return;
                }
                apiRef.current = api;
                setBootStatus('ready');
            } catch (err) {
                if (cancelled) return;
                setBootError(String(err));
                setBootStatus('error');
            }
        })();
        return () => {
            cancelled = true;
            apiRef.current?.dispose();
            apiRef.current = null;
        };
    }, []);

    const run = useCallback(async () => {
        const api = apiRef.current;
        if (!api || running) return;
        setRunning(true);
        setLog([]);
        try {
            const result = await compileAndRun(api, {
                toolchain: PRESET,
                source,
                stdin,
                onPhase: (p: CompilePhase) => append('info', `[${p}]\n`),
                onStdout: (t) => append('stdout', t),
                onStderr: (t) => append('stderr', t),
            });
            append('info', `[exit] phase=${result.finalPhase} code=${result.exitCode}\n`);
        } catch (err) {
            append('error', String(err));
        } finally {
            setRunning(false);
        }
    }, [source, stdin, running, append]);

    return (
        <main className="page">
            <header>
                <h1>emception-run-react demo</h1>
                <p>
                    Headless C++ compile + run via <code>@gameguild/emception-browser</code> using the <code>compileAndRun</code> preset helper. The{' '}
                    <code>&lt;emception-run&gt;</code> declarative wrapper depends on event-bus orchestration that is not yet implemented in the browser API, so this demo
                    drives the preset directly.
                </p>
            </header>

            {bootStatus === 'loading' && <p>Booting toolchain...</p>}
            {bootStatus === 'error' && <pre className="error">Failed to boot: {bootError}</pre>}

            {bootStatus === 'ready' && (
                <section className="run">
                    <label>
                        <span>main.cpp</span>
                        <textarea value={source} onChange={(e) => setSource(e.target.value)} spellCheck={false} rows={12} />
                    </label>

                    <label>
                        <span>stdin</span>
                        <textarea value={stdin} onChange={(e) => setStdin(e.target.value)} spellCheck={false} rows={2} />
                    </label>

                    <div className="actions">
                        <button onClick={run} disabled={running}>
                            {running ? 'Running...' : 'Compile & run'}
                        </button>
                    </div>

                    <pre className="output">
                        {log.map((line, i) => (
                            <span key={i} className={`log log-${line.kind}`}>
                                {line.text}
                            </span>
                        ))}
                    </pre>
                </section>
            )}
        </main>
    );
}
